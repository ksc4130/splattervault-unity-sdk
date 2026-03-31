using System;
using UnityEngine;
using SplatterVault;
using SplatterVault.MST;

// STEP 1: Add MST using statements (uncomment when you have MST installed)
// using Barebones.MasterServer;
// using Barebones.Networking;

/// <summary>
/// CLEAR MST INTEGRATION EXAMPLE
/// 
/// This example shows EXACTLY how to integrate SplatterVault with Master Server Toolkit:
/// 
/// WHAT THIS DOES:
/// 1. Creates a dedicated Paintball server on SplatterVault's infrastructure
/// 2. Waits for the server to boot up (takes 30-60 seconds)
/// 3. Registers the server with MST so players can find it in matchmaking
/// 4. Handles the lifecycle (start, run, stop)
/// 
/// SETUP REQUIRED:
/// 1. Install Master Server Toolkit package in Unity
/// 2. Have your MST Master Server running
/// 3. Get your SplatterVault API key from https://splattervault.com
/// 4. Set your API key in the Inspector
/// 5. Uncomment the MST code sections below (marked with // STEP X)
/// 
/// HOW TO USE:
/// - Attach this script to a GameObject in your scene
/// - Set your API key in the Inspector
/// - Call CreateServer() from a button or UI
/// - Server will auto-register with MST when ready
/// - Players can find and join via MST matchmaking
/// </summary>
public class MSTIntegrationExample : MonoBehaviour
{
    [Header("SplatterVault Configuration")]
    [Tooltip("Your SplatterVault API key (get from splattervault.com)")]
    [SerializeField] private string apiKey = "sv_your_api_key_here";
    
    [Tooltip("Server region (NYC3 = New York, TOR1 = Toronto, etc.)")]
    [SerializeField] private Region region = Region.NYC3;
    
    [Tooltip("Game mode for Paintball Playground")]
    [SerializeField] private PaintballMode gameMode = PaintballMode.XBall;

    [Header("Game Configuration")]
    [Tooltip("Name shown in server browser")]
    [SerializeField] private string gameName = "My Paintball Server";
    
    [Tooltip("Maximum players allowed")]
    [SerializeField] private int maxPlayers = 10;
    
    [Tooltip("Port for game traffic (default: 7777)")]
    [SerializeField] private int gamePort = 7777;

    [Header("Status (Read-Only)")]
    [SerializeField] private string currentStatus = "Idle";
    [SerializeField] private string serverCode = "";
    [SerializeField] private string serverIP = "";

    // Internal state
    private SplatterVaultClient svClient;
    private GameSession currentSession;
    private MSTServerInfo registeredServer;

    // STEP 2: Add MST reference (uncomment when MST is installed)
    // Example: If using Rooms Module
    // [SerializeField] private RoomsModule roomsModule;
    
    // Example: If using custom MST setup
    // private IMstConnection mstConnection;

    void Start()
    {
        // Validate API key
        if (string.IsNullOrEmpty(apiKey) || apiKey == "sv_your_api_key_here")
        {
            Debug.LogError("❌ Please set your SplatterVault API key in the Inspector!");
            currentStatus = "Error: No API key";
            return;
        }

        // Initialize SplatterVault client
        svClient = new SplatterVaultClient(apiKey);
        Debug.Log("✅ SplatterVault client initialized");

        // STEP 3: Initialize MST connection (uncomment when MST is set up)
        // Example for Rooms Module:
        // roomsModule = FindObjectOfType<RoomsModule>();
        // if (roomsModule == null)
        // {
        //     Debug.LogError("❌ RoomsModule not found! Add it to your scene.");
        // }
        
        // Example for custom MST:
        // mstConnection = Msf.Connection;
        // if (!mstConnection.IsConnected)
        // {
        //     Debug.LogWarning("⚠️ Not connected to MST. Connecting...");
        //     mstConnection.Connect();
        // }
    }

    /// <summary>
    /// MAIN METHOD: Creates server and registers with MST
    /// Call this from a UI button or on Start
    /// </summary>
    public async void CreateServer()
    {
        if (svClient == null)
        {
            Debug.LogError("❌ SplatterVault client not initialized!");
            return;
        }

        try
        {
            UpdateStatus("Creating dedicated server...");
            Debug.Log("🚀 Starting server creation process");

            // STEP 1: Create the server request
            var request = new CreateSessionRequest
            {
                friendlyName = gameName,
                isPublic = true
            };
            request.SetRegion(region);
            request.SetGameType(GameType.PaintballPlayground);
            request.SetPaintballMode(gameMode);

            // STEP 2: Create server and wait for it to be ready
            // This can take 30-60 seconds as the server boots up
            registeredServer = await svClient.CreateAndWaitForMSTServer(
                request,
                onProgress: (status) => {
                    UpdateStatus(status);
                    Debug.Log($"⏳ {status}");
                }
            );

            // STEP 3: Get the full session details
            currentSession = await svClient.GetSessionAsync(
                int.Parse(registeredServer.serverId)
            );

            // Update UI
            serverCode = registeredServer.serverCode;
            serverIP = registeredServer.ipAddress;

            Debug.Log("✅ Server is ready!");
            Debug.Log($"   Server Code: {serverCode}");
            Debug.Log($"   IP Address: {serverIP}:{gamePort}");
            Debug.Log($"   Connection: {registeredServer.GetConnectionAddress()}");

            // STEP 4: Register with MST
            RegisterWithMST(registeredServer);

            UpdateStatus($"Server ready! Code: {serverCode}");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error: {ex.Message}");
            Debug.LogError($"❌ Failed to create server: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// STEP 4: Register the server with Master Server Toolkit
    /// This makes it visible in matchmaking/server browser
    /// </summary>
    private void RegisterWithMST(MSTServerInfo serverInfo)
    {
        Debug.Log("📝 Registering server with MST...");

        // OPTION A: Using MST Rooms Module (most common)
        // Uncomment this section if you're using Rooms Module:
        /*
        if (roomsModule != null)
        {
            var roomOptions = new RoomOptions
            {
                Name = serverInfo.serverName,
                RoomIp = serverInfo.ipAddress,
                RoomPort = serverInfo.port,
                MaxPlayers = maxPlayers,
                IsPublic = serverInfo.isPublic,
                Properties = serverInfo.ToMSTProperties() // Includes serverCode, region, etc.
            };

            roomsModule.RegisterRoom(roomOptions, (controller, error) => 
            {
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError($"❌ MST registration failed: {error}");
                    return;
                }
                
                Debug.Log("✅ Successfully registered with MST!");
                Debug.Log($"   Room ID: {controller.RoomId}");
            });
        }
        */

        // OPTION B: Using MST Spawners Module
        // Uncomment this section if you're using Spawners Module:
        /*
        var spawnTask = new SpawnTaskController
        {
            Properties = new Dictionary<string, string>
            {
                { "serverCode", serverInfo.serverCode },
                { "ipAddress", serverInfo.ipAddress },
                { "port", serverInfo.port.ToString() },
                { "maxPlayers", maxPlayers.ToString() },
                { "gameType", serverInfo.gameType },
                { "mode", serverInfo.mode },
                { "region", serverInfo.region }
            }
        };
        
        Msf.Server.Spawners.RegisterSpawnedProcess(
            spawnTask,
            serverInfo.GetConnectionAddress(),
            (successful, error) =>
            {
                if (!successful)
                {
                    Debug.LogError($"❌ MST registration failed: {error}");
                    return;
                }
                
                Debug.Log("✅ Successfully registered with MST!");
            }
        );
        */

        // DEFAULT: For testing, just log what would be registered
        Debug.Log("=== MST Registration Data ===");
        Debug.Log($"Server Name: {serverInfo.serverName}");
        Debug.Log($"Connection: {serverInfo.GetConnectionAddress()}");
        Debug.Log($"Max Players: {maxPlayers}");
        Debug.Log($"Server Code: {serverInfo.serverCode}");
        Debug.Log($"Region: {serverInfo.region}");
        Debug.Log($"Game Type: {serverInfo.gameType}");
        Debug.Log($"Mode: {serverInfo.mode}");
        Debug.Log("============================");
        
        Debug.Log("⚠️ MST registration code is commented out - uncomment above to activate");
        Debug.Log("✅ Ready for MST registration (uncomment code in RegisterWithMST method)");
    }

    /// <summary>
    /// Stop the server and unregister from MST
    /// </summary>
    public async void StopServer()
    {
        if (currentSession == null)
        {
            Debug.LogWarning("⚠️ No active server to stop");
            return;
        }

        try
        {
            UpdateStatus("Stopping server...");
            Debug.Log("🛑 Stopping server...");

            // STEP 1: Unregister from MST first
            UnregisterFromMST();

            // STEP 2: Stop the SplatterVault server
            await svClient.StopSessionAsync(currentSession);

            // Clear state
            currentSession = null;
            registeredServer = null;
            serverCode = "";
            serverIP = "";

            UpdateStatus("Idle");
            Debug.Log("✅ Server stopped successfully");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error: {ex.Message}");
            Debug.LogError($"❌ Error stopping server: {ex.Message}");
        }
    }

    /// <summary>
    /// Unregister from MST when shutting down
    /// </summary>
    private void UnregisterFromMST()
    {
        Debug.Log("📝 Unregistering from MST...");

        // OPTION A: Using Rooms Module (uncomment if using)
        // if (roomsModule != null)
        // {
        //     roomsModule.DestroyRoom();
        //     Debug.Log("✅ Unregistered room from MST");
        // }

        // OPTION B: Using Spawners Module (uncomment if using)
        // Msf.Server.Spawners.DeregisterSpawnedProcess();
        // Debug.Log("✅ Unregistered spawner from MST");

        Debug.Log("✅ Unregistered from MST");
    }

    /// <summary>
    /// Get server connection info for direct connect
    /// </summary>
    public string GetConnectionInfo()
    {
        if (currentSession == null || !currentSession.IsReadyForMST())
            return "No server available";

        return currentSession.GetConnectionString();
    }

    /// <summary>
    /// Update status display
    /// </summary>
    private void UpdateStatus(string status)
    {
        currentStatus = status;
    }

    /// <summary>
    /// Cleanup when application quits
    /// </summary>
    void OnApplicationQuit()
    {
        if (currentSession != null && currentSession.IsActive())
        {
            // Note: async void is okay here since we're quitting anyway
            StopServer();
        }
    }

    // ============================================
    // INSPECTOR HELPER METHODS (Right-click menu)
    // ============================================

    [ContextMenu("Create Server")]
    private void CreateServerFromInspector()
    {
        CreateServer();
    }

    [ContextMenu("Stop Server")]
    private void StopServerFromInspector()
    {
        StopServer();
    }

    [ContextMenu("Show Connection Info")]
    private void ShowConnectionInfo()
    {
        string info = GetConnectionInfo();
        Debug.Log($"Connection Info: {info}");
    }
}
