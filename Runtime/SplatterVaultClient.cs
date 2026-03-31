using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace SplatterVault
{
    /// <summary>
    /// Main client for interacting with the SplatterVault API
    /// </summary>
    public class SplatterVaultClient
    {
        private readonly string apiKey;
        private readonly string baseUrl;
        private readonly bool isOrgKey;
        private int? organizationId;

        /// <summary>
        /// Whether this client is using an organization API key (sv_org_ prefix)
        /// </summary>
        public bool IsOrganizationKey => isOrgKey;

        /// <summary>
        /// The organization ID for org-scoped endpoints (credit balance, subscription info).
        /// Automatically used when creating sessions with org API keys.
        /// </summary>
        public int? OrganizationId
        {
            get => organizationId;
            set => organizationId = value;
        }

        /// <summary>
        /// Initialize the SplatterVault client
        /// </summary>
        /// <param name="apiKey">Your SplatterVault API key (sv_... or sv_org_...)</param>
        public SplatterVaultClient(string apiKey, string baseUrl = "https://splattervault.com/rest")
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException("API key is required");

            this.apiKey = apiKey;
            this.baseUrl = baseUrl.TrimEnd('/');
            this.isOrgKey = apiKey.StartsWith("sv_org_");
        }

        /// <summary>
        /// Initialize the SplatterVault client with an organization API key and explicit org ID.
        /// The org ID is required for org-specific endpoints like credit balance and subscription info.
        /// </summary>
        /// <param name="apiKey">Your organization API key (sv_org_...)</param>
        /// <param name="organizationId">The organization ID this key belongs to</param>
        public SplatterVaultClient(string apiKey, int organizationId, string baseUrl = "https://splattervault.com/rest")
            : this(apiKey, baseUrl)
        {
            this.organizationId = organizationId;
        }

        #region Session Management

        /// <summary>
        /// Create a new credit-based game session
        /// </summary>
        public async Task<GameSession> CreateCreditSessionAsync(
            CreateSessionRequest request,
            Action<GameSession> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                // Auto-inject organizationId for org API keys
                if (isOrgKey && organizationId.HasValue && !request.organizationId.HasValue)
                    request.organizationId = organizationId.Value;

                string json = JsonConvert.SerializeObject(request, SerializerSettings);
                string response = await PostAsync("/credits/sessions", json);
                GameSession session = Deserialize<GameSession>(response);
                onSuccess?.Invoke(session);
                return session;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Create a new subscription-based game session
        /// </summary>
        public async Task<GameSession> CreateSubscriptionSessionAsync(
            CreateSessionRequest request,
            Action<GameSession> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                // Auto-inject organizationId for org API keys
                if (isOrgKey && organizationId.HasValue && !request.organizationId.HasValue)
                    request.organizationId = organizationId.Value;

                string json = JsonConvert.SerializeObject(request, SerializerSettings);
                string response = await PostAsync("/subscriptions/sessions", json);
                GameSession session = Deserialize<GameSession>(response);
                onSuccess?.Invoke(session);
                return session;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Get details of a specific session
        /// </summary>
        public async Task<GameSession> GetSessionAsync(
            int sessionId,
            Action<GameSession> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                string response = await GetAsync($"/game-sessions/{sessionId}");
                GameSession session = Deserialize<GameSession>(response);
                onSuccess?.Invoke(session);
                return session;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Get all sessions for the authenticated user or organization.
        /// For org API keys, fetches org-scoped sessions from credit and subscription endpoints.
        /// </summary>
        public async Task<List<GameSession>> GetMySessionsAsync(
            Action<List<GameSession>> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                List<GameSession> sessions;

                if (isOrgKey)
                {
                    // Org API keys: fetch from credit and subscription session endpoints
                    // which properly scope by organization via the middleware
                    string creditResponse = await GetAsync("/credits/sessions");
                    var creditSessions = JsonConvert.DeserializeObject<List<GameSession>>(creditResponse, SerializerSettings)
                        ?? new List<GameSession>();

                    string subResponse = await GetAsync("/subscriptions/sessions");
                    var subSessions = JsonConvert.DeserializeObject<List<GameSession>>(subResponse, SerializerSettings)
                        ?? new List<GameSession>();

                    sessions = new List<GameSession>();
                    sessions.AddRange(creditSessions);
                    sessions.AddRange(subSessions);
                }
                else
                {
                    string response = await GetAsync("/game-sessions/my-sessions");
                    sessions = JsonConvert.DeserializeObject<List<GameSession>>(response, SerializerSettings);
                }

                onSuccess?.Invoke(sessions);
                return sessions;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Stop a credit-based game session
        /// </summary>
        public async Task<StopSessionResult> StopCreditSessionAsync(
            int sessionId,
            Action<StopSessionResult> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                string response = await PostAsync($"/credits/sessions/{sessionId}/stop", "{}");
                StopSessionResult result = Deserialize<StopSessionResult>(response);
                onSuccess?.Invoke(result);
                return result;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Stop a subscription-based game session
        /// </summary>
        public async Task<GameSession> StopSubscriptionSessionAsync(
            int sessionId,
            Action<GameSession> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                string response = await PostAsync($"/subscriptions/sessions/{sessionId}/stop", "{}");
                GameSession session = Deserialize<GameSession>(response);
                onSuccess?.Invoke(session);
                return session;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Stop a game session, automatically routing to the correct endpoint based on serverType.
        /// Requires the session's serverType to be known (fetch with GetSessionAsync first if needed).
        /// </summary>
        public async Task<StopSessionResult> StopSessionAsync(
            GameSession session,
            Action<StopSessionResult> onSuccess = null,
            Action<string> onError = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (session.serverType == "Subscription")
            {
                var stoppedSession = await StopSubscriptionSessionAsync(session.id, onError: onError);
                var result = new StopSessionResult { session = stoppedSession };
                onSuccess?.Invoke(result);
                return result;
            }

            return await StopCreditSessionAsync(session.id, onSuccess, onError);
        }

        /// <summary>
        /// Update the friendly name of a credit-based session
        /// </summary>
        public async Task<GameSession> UpdateCreditSessionFriendlyNameAsync(
            int sessionId,
            string friendlyName,
            Action<GameSession> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                string json = JsonConvert.SerializeObject(new { friendlyName }, SerializerSettings);
                string response = await PutAsync($"/credits/sessions/{sessionId}/friendly-name", json);
                GameSession session = Deserialize<GameSession>(response);
                onSuccess?.Invoke(session);
                return session;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Update the friendly name of a subscription-based session
        /// </summary>
        public async Task<GameSession> UpdateSubscriptionSessionFriendlyNameAsync(
            int sessionId,
            string friendlyName,
            Action<GameSession> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                string json = JsonConvert.SerializeObject(new { friendlyName }, SerializerSettings);
                string response = await PutAsync($"/subscriptions/sessions/{sessionId}/friendly-name", json);
                GameSession session = Deserialize<GameSession>(response);
                onSuccess?.Invoke(session);
                return session;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Update the friendly name of a session, automatically routing based on serverType.
        /// </summary>
        public async Task<GameSession> UpdateSessionFriendlyNameAsync(
            GameSession session,
            string friendlyName,
            Action<GameSession> onSuccess = null,
            Action<string> onError = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (session.serverType == "Subscription")
                return await UpdateSubscriptionSessionFriendlyNameAsync(session.id, friendlyName, onSuccess, onError);

            return await UpdateCreditSessionFriendlyNameAsync(session.id, friendlyName, onSuccess, onError);
        }

        /// <summary>
        /// Update the schedule for a credit-based session
        /// </summary>
        public async Task<GameSession> UpdateCreditSessionScheduleAsync(
            int sessionId,
            DateTime? scheduledStartTime = null,
            DateTime? scheduledEndTime = null,
            Action<GameSession> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                var body = new Dictionary<string, object>();
                if (scheduledStartTime.HasValue)
                    body["scheduledStartTime"] = scheduledStartTime.Value.ToUniversalTime().ToString("o");
                if (scheduledEndTime.HasValue)
                    body["scheduledEndTime"] = scheduledEndTime.Value.ToUniversalTime().ToString("o");

                string json = JsonConvert.SerializeObject(body, SerializerSettings);
                string response = await PutAsync($"/credits/sessions/{sessionId}/schedule", json);
                GameSession session = Deserialize<GameSession>(response);
                onSuccess?.Invoke(session);
                return session;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Cancel a scheduled credit session
        /// </summary>
        public async Task<GameSession> CancelCreditSessionScheduleAsync(
            int sessionId,
            Action<GameSession> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                string response = await PostAsync($"/credits/sessions/{sessionId}/cancel-schedule", "{}");
                CancelScheduleResult result = Deserialize<CancelScheduleResult>(response);
                onSuccess?.Invoke(result.session);
                return result.session;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Update the schedule for a subscription-based session
        /// </summary>
        public async Task<GameSession> UpdateSubscriptionSessionScheduleAsync(
            int sessionId,
            DateTime? scheduledStartTime = null,
            DateTime? scheduledEndTime = null,
            Action<GameSession> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                var body = new Dictionary<string, object>();
                if (scheduledStartTime.HasValue)
                    body["scheduledStartTime"] = scheduledStartTime.Value.ToUniversalTime().ToString("o");
                if (scheduledEndTime.HasValue)
                    body["scheduledEndTime"] = scheduledEndTime.Value.ToUniversalTime().ToString("o");

                string json = JsonConvert.SerializeObject(body, SerializerSettings);
                string response = await PutAsync($"/subscriptions/sessions/{sessionId}/schedule", json);
                GameSession session = Deserialize<GameSession>(response);
                onSuccess?.Invoke(session);
                return session;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Cancel a scheduled subscription session
        /// </summary>
        public async Task<GameSession> CancelSubscriptionSessionScheduleAsync(
            int sessionId,
            Action<GameSession> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                string response = await PostAsync($"/subscriptions/sessions/{sessionId}/cancel-schedule", "{}");
                CancelScheduleResult result = Deserialize<CancelScheduleResult>(response);
                onSuccess?.Invoke(result.session);
                return result.session;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Update the schedule for a session, automatically routing based on serverType.
        /// </summary>
        public async Task<GameSession> UpdateSessionScheduleAsync(
            GameSession session,
            DateTime? scheduledStartTime = null,
            DateTime? scheduledEndTime = null,
            Action<GameSession> onSuccess = null,
            Action<string> onError = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (session.serverType == "Subscription")
                return await UpdateSubscriptionSessionScheduleAsync(session.id, scheduledStartTime, scheduledEndTime, onSuccess, onError);

            return await UpdateCreditSessionScheduleAsync(session.id, scheduledStartTime, scheduledEndTime, onSuccess, onError);
        }

        /// <summary>
        /// Cancel a scheduled session, automatically routing based on serverType.
        /// </summary>
        public async Task<GameSession> CancelSessionScheduleAsync(
            GameSession session,
            Action<GameSession> onSuccess = null,
            Action<string> onError = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (session.serverType == "Subscription")
                return await CancelSubscriptionSessionScheduleAsync(session.id, onSuccess, onError);

            return await CancelCreditSessionScheduleAsync(session.id, onSuccess, onError);
        }

        #endregion

        #region Credits

        /// <summary>
        /// Get the current credit balance
        /// </summary>
        public async Task<CreditBalance> GetCreditBalanceAsync(
            Action<CreditBalance> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                string response = await GetAsync("/credits");
                CreditBalance balance = Deserialize<CreditBalance>(response);
                onSuccess?.Invoke(balance);
                return balance;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Get detailed credit statistics
        /// </summary>
        public async Task<CreditStats> GetCreditStatsAsync(
            Action<CreditStats> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                string response = await GetAsync("/credits/stats");
                CreditStats stats = Deserialize<CreditStats>(response);
                onSuccess?.Invoke(stats);
                return stats;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
        }

        #endregion

        #region Subscription

        /// <summary>
        /// Get current subscription details (returns wrapper with current, all subscriptions)
        /// </summary>
        public async Task<SubscriptionDetails> GetSubscriptionAsync(
            Action<SubscriptionDetails> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                string response = await GetAsync("/subscriptions");
                SubscriptionDetails details = Deserialize<SubscriptionDetails>(response);
                onSuccess?.Invoke(details);
                return details;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Get subscription usage statistics
        /// </summary>
        public async Task<UsageStats> GetUsageStatsAsync(
            Action<UsageStats> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                string response = await GetAsync("/subscriptions/usage");
                UsageStats stats = Deserialize<UsageStats>(response);
                onSuccess?.Invoke(stats);
                return stats;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
        }

        #endregion

        #region Organization

        /// <summary>
        /// Get the organization's credit balance and stats.
        /// Requires an org API key or OrganizationId to be set.
        /// </summary>
        /// <param name="orgId">Optional org ID override. Falls back to OrganizationId property.</param>
        public async Task<OrgCreditStats> GetOrgCreditBalanceAsync(
            int? orgId = null,
            Action<OrgCreditStats> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                int resolvedOrgId = ResolveOrgId(orgId);
                string response = await GetAsync($"/organizations/{resolvedOrgId}/credits");
                OrgCreditStats stats = Deserialize<OrgCreditStats>(response);
                onSuccess?.Invoke(stats);
                return stats;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Get the organization's subscription info.
        /// Requires an org API key or OrganizationId to be set.
        /// </summary>
        /// <param name="orgId">Optional org ID override. Falls back to OrganizationId property.</param>
        public async Task<OrgSubscriptionInfo> GetOrgSubscriptionAsync(
            int? orgId = null,
            Action<OrgSubscriptionInfo> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                int resolvedOrgId = ResolveOrgId(orgId);
                string response = await GetAsync($"/organizations/{resolvedOrgId}/subscription");
                OrgSubscriptionInfo info = Deserialize<OrgSubscriptionInfo>(response);
                onSuccess?.Invoke(info);
                return info;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
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
            using (UnityWebRequest request = UnityWebRequest.Get(baseUrl + endpoint))
            {
                request.SetRequestHeader("X-API-Key", apiKey);
                await SendRequest(request);
                return request.downloadHandler.text;
            }
        }

        private async Task<string> PostAsync(string endpoint, string json)
        {
            using (UnityWebRequest request = new UnityWebRequest(baseUrl + endpoint, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-API-Key", apiKey);
                await SendRequest(request);
                return request.downloadHandler.text;
            }
        }

        private async Task<string> PutAsync(string endpoint, string json)
        {
            using (UnityWebRequest request = new UnityWebRequest(baseUrl + endpoint, "PUT"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("X-API-Key", apiKey);
                await SendRequest(request);
                return request.downloadHandler.text;
            }
        }

        private async Task DeleteAsync(string endpoint)
        {
            using (UnityWebRequest request = UnityWebRequest.Delete(baseUrl + endpoint))
            {
                request.SetRequestHeader("X-API-Key", apiKey);
                await SendRequest(request);
            }
        }

        private async Task SendRequest(UnityWebRequest request)
        {
            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                string errorMessage = request.error;

                if (!string.IsNullOrEmpty(request.downloadHandler?.text))
                {
                    try
                    {
                        ApiError error = JsonConvert.DeserializeObject<ApiError>(request.downloadHandler.text, SerializerSettings);
                        if (error != null && !string.IsNullOrEmpty(error.message))
                        {
                            errorMessage = $"{error.name}: {error.message}";
                        }
                    }
                    catch
                    {
                        errorMessage = request.downloadHandler.text;
                    }
                }

                throw new Exception($"API Error: {errorMessage}");
            }
        }

        /// <summary>
        /// Shared serializer settings for consistent JSON handling
        /// </summary>
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            FloatParseHandling = FloatParseHandling.Double
        };

        private T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, SerializerSettings);
        }

        #endregion
    }
}
