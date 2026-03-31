using System;
using UnityEngine;
using UnityEngine.Events;
using SplatterVault;
using SplatterVault.MST;

/// <summary>
/// Automated bridge between SplatterVault and Master Server Toolkit
/// Handles server lifecycle, registration, and synchronization automatically
/// </summary>
public class SplatterVaultMSTBridge : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Your SplatterVault API key")]
    [SerializeField] private string apiKey = "sv_your_api_key_here";
    
    [SerializeField] private Region region = Region.NYC3;

    [Tooltip("Game config key from your SplatterVault dashboard")]
    [SerializeField] private string gameKey = "your_game_key_here";
    
    [Header("Server Settings")]
    [SerializeField] private int maxPlayers = 20;
    [SerializeField] private int gamePort = 7777;
    [SerializeField] private bool autoRegisterWithMST = true;
    [SerializeField] private bool autoStopOnEmpty = false;
    [SerializeField] private float emptyServerTimeout = 300f; // 5 minutes

    [Header("Status (Read Only)")]
    [SerializeField] private BridgeState currentState = BridgeState.Idle;
    [SerializeField] private string statusMessage = "Not started";
    [SerializeField] private string serverCode;
    [SerializeField] private string connectionAddress;
    [SerializeField] private int connectedPlayers = 0;

    [Header("Events")]
    public UnityEvent<MSTServerInfo> OnServerReady;
    public UnityEvent<string> OnServerStopped;
    public UnityEvent<string> OnError;
    public UnityEvent<string> OnStatusChanged;

    private SplatterVaultClient svClient;
    private GameSession currentSession;
    private MSTServerInfo serverInfo;
    private float emptyServerTimer = 0f;

    // MST Integration References (customize to your setup)
    // Add your MST components here
    // private MstConnection mstConnection;
    // private RoomsModule roomsModule;

    public enum BridgeState
    {
        Idle,
        CreatingServer,
        WaitingForServer,
        RegisteringWithMST,
        Running,
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
        // Auto-stop empty server if enabled
        if (autoStopOnEmpty && currentState == BridgeState.Running && connectedPlayers == 0)
        {
            emptyServerTimer += Time.deltaTime;
            if (emptyServerTimer >= emptyServerTimeout)
            {
                Debug.Log($"Server empty for {emptyServerTimeout}s, auto-stopping...");
                StopServer();
            }
        }
        else
        {
            emptyServerTimer = 0f;
        }
    }

    void OnApplicationQuit()
    {
        if (currentSession != null && currentState == BridgeState.Running)
        {
            StopServer();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Create and start a SplatterVault server with automatic MST integration
    /// </summary>
    public async void CreateServer(string friendlyName = null)
    {
        if (currentState != BridgeState.Idle && currentState != BridgeState.Error)
        {
            Debug.LogWarning("Server creation already in progress or server is running");
            return;
        }

        try
        {
            SetStatus(BridgeState.CreatingServer, "Creating SplatterVault server...");

            // Create the server request
            var request = new CreateSessionRequest
            {
                gameKey = this.gameKey,
                friendlyName = friendlyName ?? $"Game {DateTime.Now:HH:mm}",
                isPublic = true
            };
            request.SetRegion(region);

            // Create and wait for server to be ready
            SetStatus(BridgeState.WaitingForServer, "Waiting for server to start...");
            
            serverInfo = await svClient.CreateAndWaitForMSTServer(
                request,
                onProgress: (status) => {
                    statusMessage = status;
                    OnStatusChanged?.Invoke(status);
                }
            );

            // Get the session for tracking
            currentSession = await svClient.GetSessionAsync(int.Parse(serverInfo.serverId));
            serverCode = serverInfo.serverCode;
            connectionAddress = serverInfo.GetConnectionAddress();

            // Register with MST if enabled
            if (autoRegisterWithMST)
            {
                SetStatus(BridgeState.RegisteringWithMST, "Registering with Master Server...");
                RegisterWithMST();
            }

            // Server is ready!
            SetStatus(BridgeState.Running, "Server running and ready for players");
            OnServerReady?.Invoke(serverInfo);

            Debug.Log($"✓ Server ready! Code: {serverCode}, Address: {connectionAddress}");
        }
        catch (Exception ex)
        {
            SetError($"Failed to create server: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop the server and clean up both SV and MST
    /// </summary>
    public async void StopServer()
    {
        if (currentSession == null)
        {
            Debug.LogWarning("No active session to stop");
            return;
        }

        try
        {
            SetStatus(BridgeState.Stopping, "Stopping server...");

            // Unregister from MST first
            if (autoRegisterWithMST)
            {
                UnregisterFromMST();
            }

            // Stop the SplatterVault server
            await svClient.StopSessionAsync(currentSession);

            string stoppedCode = serverCode;
            
            // Clean up
            currentSession = null;
            serverInfo = null;
            serverCode = null;
            connectionAddress = null;
            connectedPlayers = 0;

            SetStatus(BridgeState.Idle, "Server stopped");
            OnServerStopped?.Invoke(stoppedCode);

            Debug.Log("✓ Server stopped successfully");
        }
        catch (Exception ex)
        {
            SetError($"Error stopping server: {ex.Message}");
        }
    }

    /// <summary>
    /// Get current server information
    /// </summary>
    public MSTServerInfo GetServerInfo()
    {
        return serverInfo;
    }

    /// <summary>
    /// Check if server is currently running
    /// </summary>
    public bool IsServerRunning()
    {
        return currentState == BridgeState.Running;
    }

    /// <summary>
    /// Update player count (call this when players join/leave)
    /// </summary>
    public void UpdatePlayerCount(int count)
    {
        connectedPlayers = count;
        emptyServerTimer = 0f; // Reset empty timer when count changes
    }

    #endregion

    #region MST Integration

    private void RegisterWithMST()
    {
        if (serverInfo == null)
        {
            Debug.LogError("Cannot register with MST: No server info available");
            return;
        }

        Debug.Log("Registering with Master Server Toolkit...");

        // CUSTOMIZE THIS SECTION FOR YOUR MST SETUP
        // Example implementations:

        /*
        // Example 1: Using MST Rooms Module
        if (roomsModule != null)
        {
            var roomOptions = new RoomOptions
            {
                Name = serverInfo.serverName,
                RoomIp = serverInfo.ipAddress,
                RoomPort = serverInfo.port,
                MaxPlayers = serverInfo.maxPlayers,
                IsPublic = serverInfo.isPublic,
                Properties = serverInfo.ToMSTProperties()
            };
            
            roomsModule.RegisterRoom(roomOptions, (controller, error) => {
                if (!string.IsNullOrEmpty(error))
                {
                    SetError($"MST registration failed: {error}");
                    return;
                }
                
                Debug.Log("✓ Successfully registered with MST");
            });
        }
        */

        /*
        // Example 2: Using MST Spawners Module
        if (mstConnection != null)
        {
            var properties = serverInfo.ToMSTProperties();
            mstConnection.SendMessage(MessageTypes.RegisterSpawner, properties, (status, response) => {
                if (status != ResponseStatus.Success)
                {
                    SetError($"MST registration failed: {response}");
                    return;
                }
                
                Debug.Log("✓ Successfully registered with MST");
            });
        }
        */

        // Default: Just log the info that would be registered
        Debug.Log("=== MST Registration Data ===");
        Debug.Log($"Server: {serverInfo.serverName}");
        Debug.Log($"Address: {serverInfo.GetConnectionAddress()}");
        Debug.Log($"Max Players: {serverInfo.maxPlayers}");
        Debug.Log($"Code: {serverInfo.serverCode}");
        foreach (var prop in serverInfo.ToMSTProperties())
        {
            Debug.Log($"  {prop.Key}: {prop.Value}");
        }
        Debug.Log("============================");

        Debug.Log("✓ MST registration info prepared (customize RegisterWithMST() method)");
    }

    private void UnregisterFromMST()
    {
        Debug.Log("Unregistering from Master Server Toolkit...");

        // CUSTOMIZE THIS SECTION FOR YOUR MST SETUP
        // Example: roomsModule?.UnregisterRoom();
        // Example: mstConnection?.SendMessage(MessageTypes.UnregisterSpawner);

        Debug.Log("✓ Unregistered from MST");
    }

    #endregion

    #region State Management

    private void SetStatus(BridgeState newState, string message)
    {
        currentState = newState;
        statusMessage = message;
        OnStatusChanged?.Invoke(message);
        Debug.Log($"[Bridge] {newState}: {message}");
    }

    private void SetError(string error)
    {
        currentState = BridgeState.Error;
        statusMessage = error;
        OnError?.Invoke(error);
        Debug.LogError($"[Bridge] Error: {error}");
    }

    #endregion

    #region Inspector Helpers

    [ContextMenu("Create Test Server")]
    private void CreateTestServer()
    {
        CreateServer("Inspector Test Server");
    }

    [ContextMenu("Stop Current Server")]
    private void StopCurrentServer()
    {
        StopServer();
    }

    [ContextMenu("Log Server Info")]
    private void LogServerInfo()
    {
        if (serverInfo != null)
        {
            Debug.Log($"Server Code: {serverInfo.serverCode}");
            Debug.Log($"Address: {serverInfo.GetConnectionAddress()}");
            Debug.Log($"Status: {currentState}");
            Debug.Log($"Players: {connectedPlayers}/{serverInfo.maxPlayers}");
        }
        else
        {
            Debug.Log("No active server");
        }
    }

    #endregion
}
