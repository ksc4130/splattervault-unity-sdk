using System;
using UnityEngine;
using UnityEngine.Events;
using SplatterVault;

// Uncomment when Photon Bolt is installed:
// using Bolt;
// using Bolt.Matchmaking;
// using UdpKit;

/// <summary>
/// Automated bridge between SplatterVault and Photon Bolt.
/// Handles server lifecycle: create session -> wait for ready -> connect Bolt -> cleanup.
///
/// Drop this on a GameObject, set your API key and game key, then call CreateAndConnect().
/// Everything else is automatic.
/// </summary>
public class SplatterVaultBoltBridge : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Your SplatterVault API key")]
    [SerializeField] private string apiKey = "sv_your_api_key_here";

    [SerializeField] private Region region = Region.NYC3;

    [Tooltip("Game config key from your SplatterVault dashboard")]
    [SerializeField] private string gameKey = "your_game_key_here";

    [Header("Server Settings")]
    [SerializeField] private bool autoStopOnDisconnect = true;
    [SerializeField] private bool autoStopOnEmpty = false;
    [SerializeField] private float emptyServerTimeout = 300f;

    [Header("Status (Read Only)")]
    [SerializeField] private BridgeState currentState = BridgeState.Idle;
    [SerializeField] private string statusMessage = "Not started";
    [SerializeField] private string serverCode;
    [SerializeField] private string connectionAddress;
    [SerializeField] private int connectedPlayers = 0;

    [Header("Events")]
    public UnityEvent<BoltServerInfo> OnServerReady;
    public UnityEvent<BoltServerInfo> OnConnected;
    public UnityEvent<string> OnServerStopped;
    public UnityEvent<string> OnError;
    public UnityEvent<string> OnStatusChanged;

    private SplatterVaultClient svClient;
    private GameSession currentSession;
    private BoltServerInfo serverInfo;
    private float emptyServerTimer = 0f;

    public enum BridgeState
    {
        Idle,
        CreatingServer,
        WaitingForServer,
        StartingBolt,
        Connecting,
        Connected,
        Stopping,
        Error
    }

    #region Unity Lifecycle

    void Start()
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey == "sv_your_api_key_here")
        {
            SetError("Please set your SplatterVault API key!");
            return;
        }

        svClient = new SplatterVaultClient(apiKey);
        SetStatus(BridgeState.Idle, "Ready to create server");
    }

    void Update()
    {
        if (autoStopOnEmpty && currentState == BridgeState.Connected && connectedPlayers == 0)
        {
            emptyServerTimer += Time.deltaTime;
            if (emptyServerTimer >= emptyServerTimeout)
            {
                Debug.Log($"Server empty for {emptyServerTimeout}s, auto-stopping...");
                Disconnect();
            }
        }
        else
        {
            emptyServerTimer = 0f;
        }
    }

    void OnApplicationQuit()
    {
        if (currentSession != null && currentState == BridgeState.Connected)
        {
            Disconnect();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Create a SplatterVault server and connect to it via Bolt.
    /// This is the main entry point — call from UI or game logic.
    /// </summary>
    public async void CreateAndConnect(string friendlyName = null)
    {
        if (currentState != BridgeState.Idle && currentState != BridgeState.Error)
        {
            Debug.LogWarning("Server creation already in progress or connected");
            return;
        }

        try
        {
            // Create session
            SetStatus(BridgeState.CreatingServer, "Creating SplatterVault server...");

            var request = new CreateSessionRequest
            {
                gameKey = this.gameKey,
                friendlyName = friendlyName ?? $"Game {DateTime.Now:HH:mm}"
            };
            request.SetRegion(region);

            currentSession = await svClient.CreateCreditSessionAsync(request);
            Debug.Log($"Session created: {currentSession.code}");

            // Wait for server to boot
            SetStatus(BridgeState.WaitingForServer, "Waiting for server to start...");

            currentSession = await svClient.WaitForServerReady(
                currentSession.id,
                maxWaitSeconds: 300,
                onStatusUpdate: (status) =>
                {
                    statusMessage = status;
                    OnStatusChanged?.Invoke(status);
                }
            );

            // Build server info
            serverInfo = new BoltServerInfo
            {
                sessionId = currentSession.id,
                serverCode = currentSession.code,
                serverName = currentSession.friendlyName ?? currentSession.serverName,
                ipAddress = currentSession.slaveIp,
                port = currentSession.GetServerPort(),
                gameType = currentSession.gameType,
                region = currentSession.region,
                status = currentSession.status
            };

            serverCode = serverInfo.serverCode;
            connectionAddress = serverInfo.GetConnectionAddress();

            OnServerReady?.Invoke(serverInfo);
            Debug.Log($"Server ready: {serverCode} at {connectionAddress}");

            // Start Bolt and connect
            SetStatus(BridgeState.StartingBolt, "Starting Bolt client...");
            StartBoltClient();
        }
        catch (Exception ex)
        {
            SetError($"Failed to create server: {ex.Message}");
        }
    }

    /// <summary>
    /// Connect to an existing active session by code
    /// </summary>
    public async void ConnectToSession(string code)
    {
        if (currentState != BridgeState.Idle && currentState != BridgeState.Error)
        {
            Debug.LogWarning("Already connected or connecting");
            return;
        }

        try
        {
            SetStatus(BridgeState.WaitingForServer, $"Looking up session {code}...");

            var sessions = await svClient.GetMySessionsAsync();
            currentSession = sessions.Find(s => s.code == code);

            if (currentSession == null || !currentSession.IsActive())
            {
                SetError($"No active session found with code: {code}");
                return;
            }

            serverInfo = new BoltServerInfo
            {
                sessionId = currentSession.id,
                serverCode = currentSession.code,
                serverName = currentSession.friendlyName ?? currentSession.serverName,
                ipAddress = currentSession.slaveIp,
                port = currentSession.GetServerPort(),
                gameType = currentSession.gameType,
                region = currentSession.region,
                status = currentSession.status
            };

            serverCode = serverInfo.serverCode;
            connectionAddress = serverInfo.GetConnectionAddress();

            OnServerReady?.Invoke(serverInfo);

            SetStatus(BridgeState.StartingBolt, "Starting Bolt client...");
            StartBoltClient();
        }
        catch (Exception ex)
        {
            SetError($"Failed to connect: {ex.Message}");
        }
    }

    /// <summary>
    /// Disconnect from Bolt and optionally stop the SplatterVault session
    /// </summary>
    public async void Disconnect(bool stopSession = true)
    {
        if (currentSession == null)
        {
            Debug.LogWarning("No active session");
            return;
        }

        try
        {
            SetStatus(BridgeState.Stopping, "Disconnecting...");

            // Shutdown Bolt
            ShutdownBolt();

            // Stop SplatterVault session
            if (stopSession)
            {
                await svClient.StopSessionAsync(currentSession);
            }

            string stoppedCode = serverCode;

            currentSession = null;
            serverInfo = null;
            serverCode = null;
            connectionAddress = null;
            connectedPlayers = 0;

            SetStatus(BridgeState.Idle, "Disconnected");
            OnServerStopped?.Invoke(stoppedCode);

            Debug.Log("Disconnected and session stopped");
        }
        catch (Exception ex)
        {
            SetError($"Error disconnecting: {ex.Message}");
        }
    }

    /// <summary>
    /// Get current server info
    /// </summary>
    public BoltServerInfo GetServerInfo() => serverInfo;

    /// <summary>
    /// Check if currently connected
    /// </summary>
    public bool IsConnected() => currentState == BridgeState.Connected;

    /// <summary>
    /// Update player count (call from your Bolt callbacks)
    /// </summary>
    public void UpdatePlayerCount(int count)
    {
        connectedPlayers = count;
        emptyServerTimer = 0f;
    }

    /// <summary>
    /// Called when Bolt connection succeeds.
    /// Wire this up from your GlobalEventListener.Connected() callback.
    /// </summary>
    public void OnBoltConnected()
    {
        SetStatus(BridgeState.Connected, $"Connected to {serverCode}");
        OnConnected?.Invoke(serverInfo);
        Debug.Log($"Bolt connected to {connectionAddress}");
    }

    /// <summary>
    /// Called when Bolt disconnects.
    /// Wire this up from your GlobalEventListener.Disconnected() callback.
    /// </summary>
    public void OnBoltDisconnected()
    {
        if (autoStopOnDisconnect && currentSession != null)
        {
            Disconnect();
        }
        else
        {
            SetStatus(BridgeState.Idle, "Disconnected from Bolt");
        }
    }

    #endregion

    #region Bolt Integration

    private void StartBoltClient()
    {
        // UNCOMMENT WHEN BOLT IS INSTALLED:
        /*
        BoltLauncher.StartClient();
        // Connection happens in BoltStartDone callback — see companion script below
        */

        // DEFAULT: Log for testing
        Debug.Log("=== Bolt Connection Ready ===");
        Debug.Log($"  IP: {serverInfo.ipAddress}");
        Debug.Log($"  Port: {serverInfo.port}");
        Debug.Log($"  Code: {serverInfo.serverCode}");
        Debug.Log("Bolt code is commented out — uncomment when Bolt is installed");
        Debug.Log("=============================");

        // Simulate connected state for testing without Bolt
        SetStatus(BridgeState.Connected, $"Connected to {serverCode}");
        OnConnected?.Invoke(serverInfo);
    }

    private void ShutdownBolt()
    {
        // UNCOMMENT WHEN BOLT IS INSTALLED:
        /*
        if (BoltNetwork.IsRunning)
        {
            BoltLauncher.Shutdown();
        }
        */

        Debug.Log("Bolt shutdown (commented out — uncomment when Bolt is installed)");
    }

    #endregion

    #region State Management

    private void SetStatus(BridgeState newState, string message)
    {
        currentState = newState;
        statusMessage = message;
        OnStatusChanged?.Invoke(message);
        Debug.Log($"[BoltBridge] {newState}: {message}");
    }

    private void SetError(string error)
    {
        currentState = BridgeState.Error;
        statusMessage = error;
        OnError?.Invoke(error);
        Debug.LogError($"[BoltBridge] Error: {error}");
    }

    #endregion

    #region Inspector Helpers

    [ContextMenu("Create And Connect (Test)")]
    private void CreateTestServer()
    {
        CreateAndConnect("Inspector Test Server");
    }

    [ContextMenu("Disconnect")]
    private void DisconnectFromInspector()
    {
        Disconnect();
    }

    [ContextMenu("Log Server Info")]
    private void LogServerInfo()
    {
        if (serverInfo != null)
        {
            Debug.Log($"Server Code: {serverInfo.serverCode}");
            Debug.Log($"Address: {serverInfo.GetConnectionAddress()}");
            Debug.Log($"Status: {currentState}");
            Debug.Log($"Players: {connectedPlayers}");
        }
        else
        {
            Debug.Log("No active server");
        }
    }

    #endregion
}

/// <summary>
/// Server information for Bolt connections
/// </summary>
[Serializable]
public class BoltServerInfo
{
    public int sessionId;
    public string serverCode;
    public string serverName;
    public string ipAddress;
    public int port;
    public string gameType;
    public string region;
    public string status;

    public string GetConnectionAddress()
    {
        return $"{ipAddress}:{port}";
    }
}

// =============================================================================
// COMPANION SCRIPT — Copy this into a separate file: BoltConnectionHandler.cs
// =============================================================================
//
// using Bolt;
// using UdpKit;
// using UnityEngine;
//
// /// <summary>
// /// Handles Bolt network callbacks and wires them to the SplatterVaultBoltBridge.
// /// Attach to the SAME GameObject as SplatterVaultBoltBridge.
// /// </summary>
// public class BoltConnectionHandler : GlobalEventListener
// {
//     private SplatterVaultBoltBridge bridge;
//
//     void Awake()
//     {
//         bridge = GetComponent<SplatterVaultBoltBridge>();
//         if (bridge == null)
//             Debug.LogError("BoltConnectionHandler requires SplatterVaultBoltBridge on the same GameObject!");
//     }
//
//     public override void BoltStartDone()
//     {
//         // Bolt is initialized — now connect to the SplatterVault server
//         if (!BoltNetwork.IsServer)
//         {
//             var info = bridge.GetServerInfo();
//             if (info != null)
//             {
//                 var endpoint = new UdpEndPoint(
//                     UdpIPv4Address.Parse(info.ipAddress),
//                     (ushort)info.port
//                 );
//                 BoltNetwork.Connect(endpoint);
//                 Debug.Log($"Bolt connecting to {info.GetConnectionAddress()}...");
//             }
//         }
//     }
//
//     public override void Connected(BoltConnection connection)
//     {
//         Debug.Log($"Connected to server: {connection.RemoteEndPoint}");
//         bridge.OnBoltConnected();
//     }
//
//     public override void Disconnected(BoltConnection connection)
//     {
//         Debug.Log($"Disconnected from server: {connection.RemoteEndPoint}");
//         bridge.OnBoltDisconnected();
//     }
//
//     public override void ConnectFailed(UdpEndPoint endpoint, IProtocolToken token)
//     {
//         Debug.LogError($"Bolt connection failed to {endpoint}");
//     }
//
//     public override void SessionConnected(UdpSession session, IProtocolToken token)
//     {
//         Debug.Log($"Session connected: {session.Id}");
//     }
// }
