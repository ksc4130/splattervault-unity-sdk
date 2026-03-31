using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SplatterVault
{
    /// <summary>
    /// .NET port of SplatterVaultClient — replaces UnityWebRequest with HttpClient.
    /// Mirrors the Unity SDK's public API exactly so tests validate the same contract.
    /// </summary>
    public class SplatterVaultClient : IDisposable
    {
        private readonly string apiKey;
        private readonly string baseUrl;
        private readonly bool isOrgKey;
        private readonly HttpClient http;
        private int? organizationId;

        public bool IsOrganizationKey => isOrgKey;

        public int? OrganizationId
        {
            get => organizationId;
            set => organizationId = value;
        }

        private static readonly JsonSerializerSettings SerializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            FloatParseHandling = FloatParseHandling.Double
        };

        public SplatterVaultClient(string apiKey, string baseUrl = "https://splattervault.com/rest")
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException("API key is required");

            this.apiKey = apiKey;
            this.baseUrl = baseUrl.TrimEnd('/');
            this.isOrgKey = apiKey.StartsWith("sv_org_");

            http = new HttpClient();
            http.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        }

        public SplatterVaultClient(string apiKey, int organizationId, string baseUrl = "https://splattervault.com/rest")
            : this(apiKey, baseUrl)
        {
            this.organizationId = organizationId;
        }

        #region Session Management

        public async Task<GameSession> CreateCreditSessionAsync(CreateSessionRequest request)
        {
            if (isOrgKey && organizationId.HasValue && !request.organizationId.HasValue)
                request.organizationId = organizationId.Value;

            string json = JsonConvert.SerializeObject(request, SerializerSettings);
            string response = await PostAsync("/credits/sessions", json);
            return Deserialize<GameSession>(response);
        }

        public async Task<GameSession> CreateSubscriptionSessionAsync(CreateSessionRequest request)
        {
            if (isOrgKey && organizationId.HasValue && !request.organizationId.HasValue)
                request.organizationId = organizationId.Value;

            string json = JsonConvert.SerializeObject(request, SerializerSettings);
            string response = await PostAsync("/subscriptions/sessions", json);
            return Deserialize<GameSession>(response);
        }

        public async Task<GameSession> GetSessionAsync(int sessionId)
        {
            string response = await GetAsync($"/game-sessions/{sessionId}");
            return Deserialize<GameSession>(response);
        }

        public async Task<List<GameSession>> GetMySessionsAsync()
        {
            if (isOrgKey)
            {
                string creditResponse = await GetAsync("/credits/sessions");
                var creditSessions = JsonConvert.DeserializeObject<List<GameSession>>(creditResponse, SerializerSettings)
                    ?? new List<GameSession>();

                string subResponse = await GetAsync("/subscriptions/sessions");
                var subSessions = JsonConvert.DeserializeObject<List<GameSession>>(subResponse, SerializerSettings)
                    ?? new List<GameSession>();

                var sessions = new List<GameSession>();
                sessions.AddRange(creditSessions);
                sessions.AddRange(subSessions);
                return sessions;
            }
            else
            {
                string response = await GetAsync("/game-sessions/my-sessions");
                return JsonConvert.DeserializeObject<List<GameSession>>(response, SerializerSettings)
                    ?? new List<GameSession>();
            }
        }

        public async Task<StopSessionResult> StopCreditSessionAsync(int sessionId)
        {
            string response = await PostAsync($"/credits/sessions/{sessionId}/stop", "{}");
            return Deserialize<StopSessionResult>(response);
        }

        public async Task<GameSession> StopSubscriptionSessionAsync(int sessionId)
        {
            string response = await PostAsync($"/subscriptions/sessions/{sessionId}/stop", "{}");
            return Deserialize<GameSession>(response);
        }

        public async Task<StopSessionResult> StopSessionAsync(GameSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (session.serverType == "Subscription")
            {
                var stoppedSession = await StopSubscriptionSessionAsync(session.id);
                return new StopSessionResult { session = stoppedSession };
            }

            return await StopCreditSessionAsync(session.id);
        }

        public async Task<GameSession> UpdateCreditSessionFriendlyNameAsync(int sessionId, string friendlyName)
        {
            string json = JsonConvert.SerializeObject(new { friendlyName }, SerializerSettings);
            string response = await PutAsync($"/credits/sessions/{sessionId}/friendly-name", json);
            return Deserialize<GameSession>(response);
        }

        public async Task<GameSession> UpdateSubscriptionSessionFriendlyNameAsync(int sessionId, string friendlyName)
        {
            string json = JsonConvert.SerializeObject(new { friendlyName }, SerializerSettings);
            string response = await PutAsync($"/subscriptions/sessions/{sessionId}/friendly-name", json);
            return Deserialize<GameSession>(response);
        }

        public async Task<GameSession> UpdateSessionFriendlyNameAsync(GameSession session, string friendlyName)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (session.serverType == "Subscription")
                return await UpdateSubscriptionSessionFriendlyNameAsync(session.id, friendlyName);

            return await UpdateCreditSessionFriendlyNameAsync(session.id, friendlyName);
        }

        public async Task<GameSession> UpdateCreditSessionScheduleAsync(
            int sessionId, DateTime? scheduledStartTime = null, DateTime? scheduledEndTime = null)
        {
            var body = new Dictionary<string, object>();
            if (scheduledStartTime.HasValue)
                body["scheduledStartTime"] = scheduledStartTime.Value.ToUniversalTime().ToString("o");
            if (scheduledEndTime.HasValue)
                body["scheduledEndTime"] = scheduledEndTime.Value.ToUniversalTime().ToString("o");

            string json = JsonConvert.SerializeObject(body, SerializerSettings);
            string response = await PutAsync($"/credits/sessions/{sessionId}/schedule", json);
            return Deserialize<GameSession>(response);
        }

        public async Task<GameSession> CancelCreditSessionScheduleAsync(int sessionId)
        {
            string response = await PostAsync($"/credits/sessions/{sessionId}/cancel-schedule", "{}");
            CancelScheduleResult result = Deserialize<CancelScheduleResult>(response);
            return result.session!;
        }

        public async Task<GameSession> UpdateSubscriptionSessionScheduleAsync(
            int sessionId, DateTime? scheduledStartTime = null, DateTime? scheduledEndTime = null)
        {
            var body = new Dictionary<string, object>();
            if (scheduledStartTime.HasValue)
                body["scheduledStartTime"] = scheduledStartTime.Value.ToUniversalTime().ToString("o");
            if (scheduledEndTime.HasValue)
                body["scheduledEndTime"] = scheduledEndTime.Value.ToUniversalTime().ToString("o");

            string json = JsonConvert.SerializeObject(body, SerializerSettings);
            string response = await PutAsync($"/subscriptions/sessions/{sessionId}/schedule", json);
            return Deserialize<GameSession>(response);
        }

        public async Task<GameSession> CancelSubscriptionSessionScheduleAsync(int sessionId)
        {
            string response = await PostAsync($"/subscriptions/sessions/{sessionId}/cancel-schedule", "{}");
            CancelScheduleResult result = Deserialize<CancelScheduleResult>(response);
            return result.session!;
        }

        public async Task<GameSession> UpdateSessionScheduleAsync(
            GameSession session, DateTime? scheduledStartTime = null, DateTime? scheduledEndTime = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (session.serverType == "Subscription")
                return await UpdateSubscriptionSessionScheduleAsync(session.id, scheduledStartTime, scheduledEndTime);

            return await UpdateCreditSessionScheduleAsync(session.id, scheduledStartTime, scheduledEndTime);
        }

        public async Task<GameSession> CancelSessionScheduleAsync(GameSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (session.serverType == "Subscription")
                return await CancelSubscriptionSessionScheduleAsync(session.id);

            return await CancelCreditSessionScheduleAsync(session.id);
        }

        #endregion

        #region Credits

        public async Task<CreditBalance> GetCreditBalanceAsync()
        {
            string response = await GetAsync("/credits");
            return Deserialize<CreditBalance>(response);
        }

        public async Task<CreditStats> GetCreditStatsAsync()
        {
            string response = await GetAsync("/credits/stats");
            return Deserialize<CreditStats>(response);
        }

        #endregion

        #region Subscription

        public async Task<SubscriptionDetails> GetSubscriptionAsync()
        {
            string response = await GetAsync("/subscriptions");
            return Deserialize<SubscriptionDetails>(response);
        }

        public async Task<UsageStats> GetUsageStatsAsync()
        {
            string response = await GetAsync("/subscriptions/usage");
            return Deserialize<UsageStats>(response);
        }

        #endregion

        #region Organization

        public async Task<OrgCreditStats> GetOrgCreditBalanceAsync(int? orgId = null)
        {
            int resolvedOrgId = ResolveOrgId(orgId);
            string response = await GetAsync($"/organizations/{resolvedOrgId}/credits");
            return Deserialize<OrgCreditStats>(response);
        }

        public async Task<OrgSubscriptionInfo> GetOrgSubscriptionAsync(int? orgId = null)
        {
            int resolvedOrgId = ResolveOrgId(orgId);
            string response = await GetAsync($"/organizations/{resolvedOrgId}/subscription");
            return Deserialize<OrgSubscriptionInfo>(response);
        }

        private int ResolveOrgId(int? orgId)
        {
            int? resolved = orgId ?? organizationId;
            if (!resolved.HasValue)
                throw new InvalidOperationException(
                    "Organization ID required. Set it via the constructor or OrganizationId property, or pass it explicitly.");
            return resolved.Value;
        }

        #endregion

        #region HTTP Methods

        private async Task<string> GetAsync(string endpoint)
        {
            var response = await http.GetAsync(baseUrl + endpoint);
            return await HandleResponse(response);
        }

        private async Task<string> PostAsync(string endpoint, string json)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await http.PostAsync(baseUrl + endpoint, content);
            return await HandleResponse(response);
        }

        private async Task<string> PutAsync(string endpoint, string json)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await http.PutAsync(baseUrl + endpoint, content);
            return await HandleResponse(response);
        }

        private async Task<string> HandleResponse(HttpResponseMessage response)
        {
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = response.ReasonPhrase ?? "Unknown error";

                if (!string.IsNullOrEmpty(body))
                {
                    try
                    {
                        var error = JsonConvert.DeserializeObject<ApiError>(body, SerializerSettings);
                        if (error != null && !string.IsNullOrEmpty(error.message))
                            errorMessage = $"{error.name}: {error.message}";
                    }
                    catch
                    {
                        errorMessage = body;
                    }
                }

                throw new Exception($"API Error ({(int)response.StatusCode}): {errorMessage}");
            }

            return body;
        }

        private T Deserialize<T>(string json) =>
            JsonConvert.DeserializeObject<T>(json, SerializerSettings)!;

        #endregion

        public void Dispose()
        {
            http.Dispose();
        }
    }
}
