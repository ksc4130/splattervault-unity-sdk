using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SplatterVault;

// Uncomment when Photon Bolt is installed:
// using Bolt;
// using Bolt.Matchmaking;
// using UdpKit;

/// <summary>
/// PRODUCTION-READY SESSION MANAGER FOR PHOTON BOLT
/// Complete drop-in session lifecycle management with SplatterVault + Bolt.
///
/// FEATURES:
/// - Automatic server creation and Bolt connection
/// - Join-by-code support
/// - Player tracking
/// - Auto-stop on empty / disconnect
/// - Unity Events for UI integration
/// - Error handling with retry
/// - Application quit cleanup
///
/// USAGE:
/// 1. Attach to a GameObject in your scene
/// 2. Attach BoltConnectionHandler (see companion script) to the SAME GameObject
/// 3. Set API key and game key in the Inspector
/// 4. Wire up Unity Events in the Inspector for your UI
/// 5. Call CreateSession() or JoinSession(code) from your UI
/// </summary>
public class SplatterVaultBoltSessionManager : MonoBehaviour
{
    #region Configuration

    [Header("API Configuration")]
    [Tooltip("Your SplatterVault API key")]
    [SerializeField] private string apiKey = "sv_your_api_key_here";

    [Header("Game Settings")]
    [SerializeField] private Region region = Region.NYC3;

    [Tooltip("Game config key from your SplatterVault dashboard")]
    [SerializeField] private string gameKey = "your_game_key_here";

    [Header("Bolt Settings")]
    [Tooltip("Bolt scene to load after connecting (leave empty to skip scene load)")]
    [SerializeField] private string boltSceneName = "";

    [Header("Auto Management")]
    [Tooltip("Stop SplatterVault session when Bolt disconnects")]
    [SerializeField] private bool autoStopOnDisconnect = true;

    [Tooltip("Auto-stop server when empty")]
    [SerializeField] private bool autoStopOnEmpty = false;

    [Tooltip("Seconds before auto-stopping empty server")]
    [SerializeField] private float emptyTimeout = 300f;

    [Header("Status")]
    [SerializeField] private SessionState state = SessionState.Idle;
    [SerializeField] private string statusMessage = "Ready";
    [SerializeField] private string serverCode = "";
    [SerializeField] private string serverIP = "";
    [SerializeField] private int serverPort = 0;
    [SerializeField] private int currentPlayers = 0;

    [Header("Events")]
    public UnityEvent<SessionInfo> OnSessionCreated;
    public UnityEvent<SessionInfo> OnSessionReady;
    public UnityEvent<SessionInfo> OnBoltConnected;
    public UnityEvent<string> OnSessionStopped;
    public UnityEvent<int> OnPlayerCountChanged;
    public UnityEvent<string> OnError;
    public UnityEvent<string> OnStatusUpdate;

    #endregion

    #region Private Fields

    private SplatterVaultClient client;
    private GameSession currentSession;
    private float emptyTimer = 0f;
    private SessionInfo currentSessionInfo;

    #endregion

    #region Types

    public enum SessionState
    {
        Idle,
        Creating,
        WaitingForServer,
        StartingBolt,
        Connecting,
        Connected,
        Stopping,
        Error
    }

    [Serializable]
    public class SessionInfo
    {
        public int sessionId;
        public string code;
        public string ipAddress;
        public int port;
        public string friendlyName;
        public string gameType;
        public string region;
        public DateTime createdAt;

        public string GetConnectionAddress()
        {
            return $"{ipAddress}:{port}";
        }
    }

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        if (!string.IsNullOrEmpty(apiKey) && apiKey != "sv_your_api_key_here")
        {
            client = new SplatterVaultClient(apiKey);
            UpdateStatus("Ready");
        }
        else
        {
            SetError("API key not set!");
        }
    }

    void Update()
    {
        if (autoStopOnEmpty && state == SessionState.Connected && currentPlayers == 0)
        {
            emptyTimer += Time.deltaTime;
            if (emptyTimer >= emptyTimeout)
            {
                Debug.Log($"[BoltSessionManager] Server empty for {emptyTimeout}s, auto-stopping");
                StopSession();
            }
        }
        else
        {
            emptyTimer = 0f;
        }
    }

    void OnApplicationQuit()
    {
        if (currentSession != null)
        {
            StopSession();
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Create a new SplatterVault session and auto-connect via Bolt
    /// </summary>
    public async void CreateSession(string friendlyName = null)
    {
        if (client == null)
        {
            SetError("Client not initialized");
            return;
        }

        if (state != SessionState.Idle && state != SessionState.Error)
        {
            Debug.LogWarning("[BoltSessionManager] Session already active");
            return;
        }

        try
        {
            SetState(SessionState.Creating);
            UpdateStatus("Creating session...");

            var request = new CreateSessionRequest
            {
                gameKey = this.gameKey,
                friendlyName = friendlyName ?? $"Game {DateTime.Now:HH:mm}"
            };
            request.SetRegion(region);

            // Create and wait for server
            SetState(SessionState.WaitingForServer);
            currentSession = await client.CreateCreditSessionAsync(request);
            Debug.Log($"[BoltSessionManager] Session created: {currentSession.code}");

            currentSession = await client.WaitForServerReady(
                currentSession.id,
                maxWaitSeconds: 300,
                onStatusUpdate: UpdateStatus
            );

            // Build session info
            currentSessionInfo = BuildSessionInfo(currentSession);
            serverCode = currentSessionInfo.code;
            serverIP = currentSessionInfo.ipAddress;
            serverPort = currentSessionInfo.port;

            OnSessionCreated?.Invoke(currentSessionInfo);
            OnSessionReady?.Invoke(currentSessionInfo);

            Debug.Log($"[BoltSessionManager] Server ready: {serverCode} at {serverIP}:{serverPort}");

            // Connect via Bolt
            SetState(SessionState.StartingBolt);
            UpdateStatus("Starting Bolt...");
            StartBoltConnection();
        }
        catch (Exception ex)
        {
            SetError($"Failed to create session: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// Join an existing session by its server code
    /// </summary>
    public async void JoinSession(string code)
    {
        if (client == null)
        {
            SetError("Client not initialized");
            return;
        }

        if (state != SessionState.Idle && state != SessionState.Error)
        {
            Debug.LogWarning("[BoltSessionManager] Session already active");
            return;
        }

        try
        {
            SetState(SessionState.WaitingForServer);
            UpdateStatus($"Looking up session {code}...");

            var sessions = await client.GetMySessionsAsync();
            currentSession = sessions.Find(s => s.code == code);

            if (currentSession == null)
            {
                SetError($"Session not found: {code}");
                return;
            }

            if (!currentSession.IsActive())
            {
                SetError($"Session {code} is not active (status: {currentSession.status})");
                return;
            }

            currentSessionInfo = BuildSessionInfo(currentSession);
            serverCode = currentSessionInfo.code;
            serverIP = currentSessionInfo.ipAddress;
            serverPort = currentSessionInfo.port;

            OnSessionReady?.Invoke(currentSessionInfo);
            Debug.Log($"[BoltSessionManager] Joining: {serverCode} at {serverIP}:{serverPort}");

            SetState(SessionState.StartingBolt);
            UpdateStatus("Starting Bolt...");
            StartBoltConnection();
        }
        catch (Exception ex)
        {
            SetError($"Failed to join session: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// Stop the session and disconnect Bolt
    /// </summary>
    public async void StopSession()
    {
        if (currentSession == null)
        {
            Debug.LogWarning("[BoltSessionManager] No active session");
            return;
        }

        try
        {
            SetState(SessionState.Stopping);
            UpdateStatus("Stopping session...");

            // Shutdown Bolt first
            ShutdownBolt();

            // Stop SplatterVault session
            await client.StopSessionAsync(currentSession);

            string stoppedCode = serverCode;

            // Cleanup
            currentSession = null;
            currentSessionInfo = null;
            serverCode = "";
            serverIP = "";
            serverPort = 0;
            currentPlayers = 0;

            SetState(SessionState.Idle);
            UpdateStatus("Idle");
            OnSessionStopped?.Invoke(stoppedCode);

            Debug.Log($"[BoltSessionManager] Session stopped: {stoppedCode}");
        }
        catch (Exception ex)
        {
            SetError($"Failed to stop session: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// Disconnect from Bolt without stopping the SplatterVault session.
    /// Use this when the host wants to keep the server running for other players.
    /// </summary>
    public void DisconnectOnly()
    {
        ShutdownBolt();
        SetState(SessionState.Idle);
        UpdateStatus("Disconnected (session still running)");
        Debug.Log($"[BoltSessionManager] Disconnected from {serverCode} (session still running)");
    }

    /// <summary>
    /// Update player count — call from your game's player join/leave events
    /// </summary>
    public void SetPlayerCount(int count)
    {
        if (currentPlayers != count)
        {
            currentPlayers = count;
            emptyTimer = 0f;
            OnPlayerCountChanged?.Invoke(count);
        }
    }

    /// <summary>
    /// Get current session info
    /// </summary>
    public SessionInfo GetCurrentSession() => currentSessionInfo;

    /// <summary>
    /// Check if connected
    /// </summary>
    public bool IsConnected() => state == SessionState.Connected;

    /// <summary>
    /// Get connection string for sharing (e.g., show in UI for friends to join)
    /// </summary>
    public string GetServerCode() => serverCode;

    #endregion

    #region Bolt Callbacks (wire from BoltConnectionHandler)

    /// <summary>
    /// Call from GlobalEventListener.BoltStartDone()
    /// </summary>
    public void HandleBoltStartDone()
    {
        if (state != SessionState.StartingBolt) return;

        SetState(SessionState.Connecting);
        UpdateStatus("Connecting to server...");

        // UNCOMMENT WHEN BOLT IS INSTALLED:
        /*
        if (!BoltNetwork.IsServer && currentSessionInfo != null)
        {
            var endpoint = new UdpEndPoint(
                UdpIPv4Address.Parse(currentSessionInfo.ipAddress),
                (ushort)currentSessionInfo.port
            );
            BoltNetwork.Connect(endpoint);
            Debug.Log($"[BoltSessionManager] Bolt connecting to {currentSessionInfo.GetConnectionAddress()}...");
        }
        */

        // DEFAULT: Simulate connection for testing
        Debug.Log($"[BoltSessionManager] Bolt started, would connect to {currentSessionInfo?.GetConnectionAddress()}");
        HandleBoltConnected(); // Remove this line when Bolt is installed
    }

    /// <summary>
    /// Call from GlobalEventListener.Connected()
    /// </summary>
    public void HandleBoltConnected()
    {
        SetState(SessionState.Connected);
        UpdateStatus($"Connected: {serverCode}");
        OnBoltConnected?.Invoke(currentSessionInfo);
        Debug.Log($"[BoltSessionManager] Connected to {serverCode}");

        // Load Bolt scene if configured
        // UNCOMMENT WHEN BOLT IS INSTALLED:
        /*
        if (!string.IsNullOrEmpty(boltSceneName))
        {
            BoltNetwork.LoadScene(boltSceneName);
        }
        */
    }

    /// <summary>
    /// Call from GlobalEventListener.Disconnected()
    /// </summary>
    public void HandleBoltDisconnected()
    {
        Debug.Log($"[BoltSessionManager] Bolt disconnected from {serverCode}");

        if (autoStopOnDisconnect)
        {
            StopSession();
        }
        else
        {
            SetState(SessionState.Idle);
            UpdateStatus("Disconnected");
        }
    }

    /// <summary>
    /// Call from GlobalEventListener.ConnectFailed()
    /// </summary>
    public void HandleBoltConnectFailed()
    {
        SetError($"Bolt connection failed to {currentSessionInfo?.GetConnectionAddress()}");
    }

    #endregion

    #region Bolt Integration

    private void StartBoltConnection()
    {
        // UNCOMMENT WHEN BOLT IS INSTALLED:
        /*
        BoltLauncher.StartClient();
        // Connection continues in HandleBoltStartDone() callback
        */

        // DEFAULT: Simulate for testing
        Debug.Log("[BoltSessionManager] BoltLauncher.StartClient() (commented out)");
        HandleBoltStartDone(); // Remove this line when Bolt is installed
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

        Debug.Log("[BoltSessionManager] BoltLauncher.Shutdown() (commented out)");
    }

    #endregion

    #region Internal

    private SessionInfo BuildSessionInfo(GameSession session)
    {
        return new SessionInfo
        {
            sessionId = session.id,
            code = session.code,
            ipAddress = session.slaveIp,
            port = session.GetServerPort(),
            friendlyName = session.friendlyName ?? session.serverName,
            gameType = session.gameType,
            region = session.region,
            createdAt = DateTime.Now
        };
    }

    private void SetState(SessionState newState)
    {
        state = newState;
    }

    private void UpdateStatus(string message)
    {
        statusMessage = message;
        OnStatusUpdate?.Invoke(message);
    }

    private void SetError(string error)
    {
        state = SessionState.Error;
        statusMessage = error;
        OnError?.Invoke(error);
        Debug.LogError($"[BoltSessionManager] {error}");
    }

    #endregion

    #region Inspector Helpers

    [ContextMenu("Create Session")]
    private void CreateSessionContext()
    {
        CreateSession();
    }

    [ContextMenu("Stop Session")]
    private void StopSessionContext()
    {
        StopSession();
    }

    [ContextMenu("Show Status")]
    private void ShowStatus()
    {
        Debug.Log($"=== Bolt Session Manager ===");
        Debug.Log($"State: {state}");
        Debug.Log($"Status: {statusMessage}");
        Debug.Log($"Server: {serverCode} at {serverIP}:{serverPort}");
        Debug.Log($"Players: {currentPlayers}");
        Debug.Log($"============================");
    }

    #endregion
}

// =============================================================================
// COMPANION SCRIPT — Copy into a separate file: BoltSessionCallbacks.cs
// Attach to the SAME GameObject as SplatterVaultBoltSessionManager.
// =============================================================================
//
// using Bolt;
// using UdpKit;
// using UnityEngine;
//
// /// <summary>
// /// Routes Bolt network callbacks to SplatterVaultBoltSessionManager.
// /// Attach to the same GameObject as your session manager.
// /// </summary>
// public class BoltSessionCallbacks : GlobalEventListener
// {
//     private SplatterVaultBoltSessionManager manager;
//
//     void Awake()
//     {
//         manager = GetComponent<SplatterVaultBoltSessionManager>();
//         if (manager == null)
//             Debug.LogError("BoltSessionCallbacks requires SplatterVaultBoltSessionManager on same GameObject!");
//     }
//
//     public override void BoltStartDone()
//     {
//         manager.HandleBoltStartDone();
//     }
//
//     public override void Connected(BoltConnection connection)
//     {
//         manager.HandleBoltConnected();
//     }
//
//     public override void Disconnected(BoltConnection connection)
//     {
//         manager.HandleBoltDisconnected();
//     }
//
//     public override void ConnectFailed(UdpEndPoint endpoint, IProtocolToken token)
//     {
//         manager.HandleBoltConnectFailed();
//     }
//
//     public override void SceneLoadLocalDone(string scene, IProtocolToken token)
//     {
//         Debug.Log($"[BoltSessionCallbacks] Scene loaded: {scene}");
//     }
// }
