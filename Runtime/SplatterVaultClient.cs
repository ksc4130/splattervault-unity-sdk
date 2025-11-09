using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
        /// <param name="baseUrl">API base URL (default: https://api.splattervault.com/rest)</param>
        public SplatterVaultClient(string apiKey, string baseUrl = "https://api.splattervault.com/rest")
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
                string json = JsonUtility.ToJson(request);
                string response = await PostAsync("/credits/sessions", json);
                GameSession session = ParseResponse<GameSession>(response);
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
                string json = JsonUtility.ToJson(request);
                string response = await PostAsync("/subscriptions/sessions", json);
                GameSession session = ParseResponse<GameSession>(response);
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
                GameSession session = ParseResponse<GameSession>(response);
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
                List<GameSession> sessions = JsonHelper.FromJson<GameSession>(response);
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
        /// Stop an active game session
        /// </summary>
        public async Task<bool> StopSessionAsync(
            int sessionId,
            Action<bool> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                await DeleteAsync($"/game-sessions/{sessionId}");
                onSuccess?.Invoke(true);
                return true;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Update the friendly name of a session
        /// </summary>
        public async Task<GameSession> UpdateSessionFriendlyNameAsync(
            int sessionId,
            string friendlyName,
            Action<GameSession> onSuccess = null,
            Action<string> onError = null)
        {
            try
            {
                string json = $"{{\"friendlyName\":\"{friendlyName}\"}}";
                string response = await PatchAsync($"/game-sessions/{sessionId}", json);
                GameSession session = ParseResponse<GameSession>(response);
                onSuccess?.Invoke(session);
                return session;
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
                throw;
            }
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
                CreditBalance balance = ParseResponse<CreditBalance>(response);
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
                CreditStats stats = ParseResponse<CreditStats>(response);
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
                var wrapper = JsonUtility.FromJson<SubscriptionWrapper>(response);
                Subscription subscription = wrapper.current;
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
                UsageStats stats = JsonUtility.FromJson<UsageStats>(response);
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

        private async Task<string> PatchAsync(string endpoint, string json)
        {
            using (UnityWebRequest request = new UnityWebRequest(baseUrl + endpoint, "PATCH"))
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
                        ApiError error = JsonUtility.FromJson<ApiError>(request.downloadHandler.text);
                        errorMessage = $"{error.name}: {error.message}";
                    }
                    catch
                    {
                        errorMessage = request.downloadHandler.text;
                    }
                }

                throw new Exception($"API Error: {errorMessage}");
            }
        }

        private T ParseResponse<T>(string json)
        {
            try
            {
                // Try to parse as wrapped response first
                ApiResponse<T> wrapper = JsonUtility.FromJson<ApiResponse<T>>(json);
                if (wrapper != null && wrapper.data != null)
                {
                    return wrapper.data;
                }
            }
            catch
            {
                // If that fails, try parsing directly
            }

            // Try direct parsing
            return JsonUtility.FromJson<T>(json);
        }

        #endregion

        #region Helper Classes

        [Serializable]
        private class SubscriptionWrapper
        {
            public Subscription current;
        }

        #endregion
    }

    /// <summary>
    /// Helper class for parsing JSON arrays (Unity's JsonUtility doesn't support arrays directly)
    /// </summary>
    public static class JsonHelper
    {
        public static List<T> FromJson<T>(string json)
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>("{\"items\":" + json + "}");
            return new List<T>(wrapper.items);
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] items;
        }
    }
}
