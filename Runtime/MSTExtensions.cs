using System;
using System.Threading.Tasks;
using UnityEngine;

namespace SplatterVault.MST
{
    /// <summary>
    /// Extension methods for Master Server Toolkit integration
    /// </summary>
    public static class MSTExtensions
    {
        /// <summary>
        /// Get server information formatted for MST registration
        /// </summary>
        public static MSTServerInfo GetMSTServerInfo(this GameSession session)
        {
            return new MSTServerInfo
            {
                serverId = session.id.ToString(),
                serverCode = session.code,
                serverName = session.friendlyName ?? session.serverName,
                ipAddress = session.slaveIp,
                port = 8100, // Default game port
                maxPlayers = 10, // Configure based on your game
                gameType = session.gameType,
                mode = session.mode,
                isPublic = session.isPublic,
                region = session.region,
                status = session.status
            };
        }

        /// <summary>
        /// Wait for the server to become active with polling
        /// </summary>
        public static async Task<GameSession> WaitForServerReady(
            this SplatterVaultClient client,
            int sessionId,
            int maxWaitSeconds = 300,
            Action<string> onStatusUpdate = null)
        {
            int pollInterval = 5000; // 5 seconds
            int elapsedTime = 0;

            while (elapsedTime < maxWaitSeconds * 1000)
            {
                try
                {
                    var session = await client.GetSessionAsync(sessionId);
                    
                    onStatusUpdate?.Invoke($"Server status: {session.status}");

                    if (session.IsActive())
                    {
                        onStatusUpdate?.Invoke("Server is ready!");
                        return session;
                    }

                    if (session.status == "Failed" || session.status == "Error")
                    {
                        throw new Exception($"Server failed to start: {session.status}");
                    }
                }
                catch (Exception ex)
                {
                    onStatusUpdate?.Invoke($"Error checking status: {ex.Message}");
                }

                await Task.Delay(pollInterval);
                elapsedTime += pollInterval;
            }

            throw new TimeoutException($"Server did not become ready within {maxWaitSeconds} seconds");
        }

        /// <summary>
        /// Create a server and wait for it to be ready for MST registration
        /// </summary>
        public static async Task<MSTServerInfo> CreateAndWaitForMSTServer(
            this SplatterVaultClient client,
            CreateSessionRequest request,
            Action<string> onProgress = null)
        {
            onProgress?.Invoke("Creating server...");
            var session = await client.CreateCreditSessionAsync(request);

            onProgress?.Invoke($"Server created: {session.code}");
            onProgress?.Invoke("Waiting for server to start...");

            session = await client.WaitForServerReady(
                session.id,
                maxWaitSeconds: 300,
                onStatusUpdate: onProgress
            );

            onProgress?.Invoke("Server ready for registration!");
            return session.GetMSTServerInfo();
        }

        /// <summary>
        /// Check if a session is in a valid state for MST registration
        /// </summary>
        public static bool IsReadyForMST(this GameSession session)
        {
            return session.IsActive() && 
                   !string.IsNullOrEmpty(session.slaveIp) &&
                   !string.IsNullOrEmpty(session.code);
        }

        /// <summary>
        /// Get connection string for MST clients
        /// </summary>
        public static string GetConnectionString(this GameSession session, int port = 7777)
        {
            if (string.IsNullOrEmpty(session.slaveIp))
                return null;

            return $"{session.slaveIp}:{port}";
        }
    }

    /// <summary>
    /// Server information structure for MST
    /// </summary>
    [Serializable]
    public class MSTServerInfo
    {
        public string serverId;
        public string serverCode;
        public string serverName;
        public string ipAddress;
        public int port;
        public int maxPlayers;
        public string gameType;
        public string mode;
        public bool isPublic;
        public string region;
        public string status;

        /// <summary>
        /// Get the full connection address
        /// </summary>
        public string GetConnectionAddress()
        {
            return $"{ipAddress}:{port}";
        }

        /// <summary>
        /// Convert to a dictionary for MST custom properties
        /// </summary>
        public System.Collections.Generic.Dictionary<string, string> ToMSTProperties()
        {
            return new System.Collections.Generic.Dictionary<string, string>
            {
                { "serverCode", serverCode },
                { "gameType", gameType },
                { "mode", mode },
                { "region", region },
                { "splatterVaultId", serverId }
            };
        }
    }
}
