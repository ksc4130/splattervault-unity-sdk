using System;
using UnityEngine;
using SplatterVault;

// STEP 1: Uncomment when Photon Bolt is installed
// using Bolt;
// using Bolt.Matchmaking;
// using UdpKit;

/// <summary>
/// CLEAR PHOTON BOLT INTEGRATION EXAMPLE
///
/// This example shows EXACTLY how to integrate SplatterVault with Photon Bolt:
///
/// WHAT THIS DOES:
/// 1. Creates a dedicated game server on SplatterVault's infrastructure
/// 2. Waits for the server to boot up (takes 30-60 seconds)
/// 3. Connects to the server using Photon Bolt
/// 4. Handles the lifecycle (create, connect, disconnect)
///
/// SETUP REQUIRED:
/// 1. Install Photon Bolt package in Unity
/// 2. Get your SplatterVault API key from https://splattervault.com
/// 3. Set your API key and game key in the Inspector
///
/// HOW TO USE:
/// - Attach this script to a GameObject in your scene
/// - Set your API key and game key in the Inspector
/// - Call CreateAndConnect() from a button or UI
/// - Server boots on SplatterVault, then Bolt auto-connects
///
/// FLOW:
///   CreateAndConnect() -> SDK creates server -> polls until Active
///   -> BoltLauncher.StartClient() -> BoltNetwork.Connect(ip:port)
///   -> Your game's Bolt callbacks fire as normal
/// </summary>
public class BoltIntegrationExample : MonoBehaviour
{
    [Header("SplatterVault Configuration")]
    [Tooltip("Your SplatterVault API key (get from splattervault.com)")]
    [SerializeField] private string apiKey = "sv_your_api_key_here";

    [Tooltip("Server region (NYC3 = New York, TOR1 = Toronto, etc.)")]
    [SerializeField] private Region region = Region.NYC3;

    [Tooltip("Game config key from your SplatterVault dashboard")]
    [SerializeField] private string gameKey = "your_game_key_here";

    [Header("Game Configuration")]
    [Tooltip("Name shown in session list")]
    [SerializeField] private string gameName = "My Game Server";

    [Header("Status (Read-Only)")]
    [SerializeField] private string currentStatus = "Idle";
    [SerializeField] private string serverCode = "";
    [SerializeField] private string serverIP = "";
    [SerializeField] private int serverPort = 0;

    private SplatterVaultClient svClient;
    private GameSession currentSession;

    void Start()
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey == "sv_your_api_key_here")
        {
            Debug.LogError("Please set your SplatterVault API key in the Inspector!");
            currentStatus = "Error: No API key";
            return;
        }

        svClient = new SplatterVaultClient(apiKey);
        Debug.Log("SplatterVault client initialized");
    }

    /// <summary>
    /// MAIN METHOD: Creates a SplatterVault server and connects via Bolt
    /// Call this from a UI button
    /// </summary>
    public async void CreateAndConnect()
    {
        if (svClient == null)
        {
            Debug.LogError("SplatterVault client not initialized!");
            return;
        }

        try
        {
            // STEP 1: Create the session request
            UpdateStatus("Creating dedicated server...");
            Debug.Log("Starting server creation...");

            var request = new CreateSessionRequest
            {
                gameKey = this.gameKey,
                friendlyName = gameName
            };
            request.SetRegion(region);

            // STEP 2: Create session
            currentSession = await svClient.CreateCreditSessionAsync(request);

            Debug.Log($"Session created: {currentSession.code} (status: {currentSession.status})");
            serverCode = currentSession.code;

            // STEP 3: Wait for server to become Active (30-60 seconds)
            UpdateStatus("Waiting for server to start...");

            currentSession = await svClient.WaitForServerReady(
                currentSession.id,
                maxWaitSeconds: 300,
                onStatusUpdate: (status) =>
                {
                    UpdateStatus(status);
                    Debug.Log($"Server status: {status}");
                }
            );

            // STEP 4: Server is ready — get connection info
            serverIP = currentSession.slaveIp;
            serverPort = currentSession.GetServerPort();

            Debug.Log($"Server ready!");
            Debug.Log($"  Code: {serverCode}");
            Debug.Log($"  IP: {serverIP}:{serverPort}");

            // STEP 5: Connect via Photon Bolt
            UpdateStatus("Connecting via Bolt...");
            ConnectWithBolt(serverIP, serverPort);
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error: {ex.Message}");
            Debug.LogError($"Failed: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// Connect to the SplatterVault server using Photon Bolt
    /// </summary>
    private void ConnectWithBolt(string ip, int port)
    {
        Debug.Log($"Connecting Bolt client to {ip}:{port}...");

        // UNCOMMENT THIS SECTION WHEN BOLT IS INSTALLED:
        /*
        // Start Bolt as a client
        BoltLauncher.StartClient();

        // Note: The actual BoltNetwork.Connect() call should happen in your
        // GlobalEventListener.BoltStartDone() callback, because Bolt needs
        // to finish initializing before you can connect.
        //
        // See the BoltCallbacks region below for the complete pattern.
        */

        // DEFAULT: Log connection info for testing
        Debug.Log("=== Bolt Connection Data ===");
        Debug.Log($"  Server IP: {ip}");
        Debug.Log($"  Server Port: {port}");
        Debug.Log($"  Server Code: {serverCode}");
        Debug.Log("============================");
        Debug.Log("Bolt connection code is commented out — uncomment when Bolt is installed");

        UpdateStatus($"Ready to connect: {ip}:{port}");
    }

    #region Bolt Callbacks

    // UNCOMMENT THIS ENTIRE REGION WHEN BOLT IS INSTALLED:
    /*
    // Create a separate MonoBehaviour that extends GlobalEventListener
    // and place it on the same GameObject, or use the pattern below.
    //
    // IMPORTANT: GlobalEventListener is a MonoBehaviour — you can't
    // inherit from it AND MonoBehaviour. Create a companion script:
    //
    //   public class BoltConnectionHandler : Bolt.GlobalEventListener
    //   {
    //       private BoltIntegrationExample manager;
    //
    //       public void Initialize(BoltIntegrationExample mgr)
    //       {
    //           manager = mgr;
    //       }
    //
    //       public override void BoltStartDone()
    //       {
    //           // Bolt is ready — now connect to the SplatterVault server
    //           if (!BoltNetwork.IsServer)
    //           {
    //               var endpoint = new UdpEndPoint(
    //                   UdpIPv4Address.Parse(manager.ServerIP),
    //                   (ushort)manager.ServerPort
    //               );
    //               BoltNetwork.Connect(endpoint);
    //               Debug.Log($"Bolt connecting to {manager.ServerIP}:{manager.ServerPort}...");
    //           }
    //       }
    //
    //       public override void Connected(BoltConnection connection)
    //       {
    //           Debug.Log($"Connected to server! Connection: {connection}");
    //       }
    //
    //       public override void Disconnected(BoltConnection connection)
    //       {
    //           Debug.Log($"Disconnected from server: {connection}");
    //       }
    //
    //       public override void ConnectFailed(UdpEndPoint endpoint, IProtocolToken token)
    //       {
    //           Debug.LogError($"Failed to connect to {endpoint}");
    //       }
    //   }
    */

    #endregion

    /// <summary>
    /// Join an existing session by code
    /// </summary>
    public async void JoinByCode(string code)
    {
        if (svClient == null)
        {
            Debug.LogError("SplatterVault client not initialized!");
            return;
        }

        try
        {
            UpdateStatus("Looking up session...");
            Debug.Log($"Looking up session code: {code}");

            // Get session details by code
            var sessions = await svClient.GetMySessionsAsync();
            currentSession = sessions.Find(s => s.code == code);

            if (currentSession == null)
            {
                UpdateStatus("Session not found");
                Debug.LogError($"No session found with code: {code}");
                return;
            }

            if (!currentSession.IsActive())
            {
                UpdateStatus("Session not active");
                Debug.LogError($"Session {code} is not active (status: {currentSession.status})");
                return;
            }

            serverCode = currentSession.code;
            serverIP = currentSession.slaveIp;
            serverPort = currentSession.GetServerPort();

            Debug.Log($"Found session: {serverIP}:{serverPort}");

            // Connect via Bolt
            UpdateStatus("Connecting via Bolt...");
            ConnectWithBolt(serverIP, serverPort);
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error: {ex.Message}");
            Debug.LogError($"Failed to join: {ex.Message}");
        }
    }

    /// <summary>
    /// Disconnect from Bolt and stop the session
    /// </summary>
    public async void DisconnectAndStop()
    {
        try
        {
            UpdateStatus("Disconnecting...");

            // STEP 1: Disconnect Bolt
            // UNCOMMENT WHEN BOLT IS INSTALLED:
            // if (BoltNetwork.IsRunning)
            // {
            //     BoltLauncher.Shutdown();
            // }

            // STEP 2: Stop the SplatterVault session
            if (currentSession != null)
            {
                await svClient.StopSessionAsync(currentSession);
                Debug.Log($"Session {serverCode} stopped");
            }

            // Clean up
            currentSession = null;
            serverCode = "";
            serverIP = "";
            serverPort = 0;

            UpdateStatus("Idle");
            Debug.Log("Disconnected and session stopped");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error: {ex.Message}");
            Debug.LogError($"Error disconnecting: {ex.Message}");
        }
    }

    private void UpdateStatus(string status)
    {
        currentStatus = status;
    }

    void OnApplicationQuit()
    {
        if (currentSession != null && currentSession.IsActive())
        {
            DisconnectAndStop();
        }
    }

    #region Inspector Helpers

    [ContextMenu("Create And Connect")]
    private void CreateAndConnectFromInspector()
    {
        CreateAndConnect();
    }

    [ContextMenu("Disconnect And Stop")]
    private void DisconnectAndStopFromInspector()
    {
        DisconnectAndStop();
    }

    [ContextMenu("Show Connection Info")]
    private void ShowConnectionInfo()
    {
        Debug.Log($"Server Code: {serverCode}");
        Debug.Log($"Address: {serverIP}:{serverPort}");
        Debug.Log($"Status: {currentStatus}");
    }

    #endregion

    // Public accessors for the companion BoltConnectionHandler script
    public string ServerIP => serverIP;
    public int ServerPort => serverPort;
}
