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

        /// <summary>
        /// Initialize the SplatterVault client
        /// </summary>
        /// <param name="apiKey">Your SplatterVault API key (sv_...)</param>
        public SplatterVaultClient(string apiKey, string baseUrl = "https://splattervault.com/rest")
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException("API key is required");

            this.apiKey = apiKey;
            this.baseUrl = baseUrl.TrimEnd('/');
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
        /// Get all sessions for the authenticated user
        /// </summary>
        public async Task<List<GameSession>> GetMySessionsAsync(
            Action<List<GameSession>> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                string response = await GetAsync("/game-sessions/my-sessions");
                List<GameSession> sessions = JsonConvert.DeserializeObject<List<GameSession>>(response, SerializerSettings);
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
        /// Get current subscription details
        /// </summary>
        public async Task<Subscription> GetSubscriptionAsync(
            Action<Subscription> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                string response = await GetAsync("/subscriptions");
                Subscription subscription = Deserialize<Subscription>(response);
                onSuccess?.Invoke(subscription);
                return subscription;
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
