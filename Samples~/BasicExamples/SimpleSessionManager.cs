using System;
using System.Collections.Generic;
using UnityEngine;
using SplatterVault;

/// <summary>
/// Simple example of managing game sessions with the SplatterVault SDK
/// Attach this to a GameObject in your scene
/// </summary>
public class SimpleSessionManager : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Your SplatterVault API key (keep this secret!)")]
    [SerializeField] private string apiKey = "sv_your_api_key_here";
    
    [SerializeField] private Region region = Region.NYC3;
    [SerializeField] private GameType gameType = GameType.PaintballPlayground;
    [SerializeField] private PaintballMode paintballMode = PaintballMode.XBall;
    [SerializeField] private bool isPublic = false;

    [Header("Session Info")]
    [SerializeField] private string currentSessionCode;
    [SerializeField] private int currentSessionId;
    [SerializeField] private string sessionStatus;

    private SplatterVaultClient client;
    private GameSession activeSession;

    void Start()
    {
        // Initialize the client
        if (string.IsNullOrEmpty(apiKey) || apiKey == "sv_your_api_key_here")
        {
            Debug.LogError("Please set your API key in the inspector!");
            return;
        }

        client = new SplatterVaultClient(apiKey);
        Debug.Log("SplatterVault client initialized");
    }

    /// <summary>
    /// Create a new game session
    /// Call this from a button or other UI element
    /// </summary>
    public async void CreateSession()
    {
        if (client == null)
        {
            Debug.LogError("Client not initialized!");
            return;
        }

        try
        {
            Debug.Log("Creating new session...");

            var request = new CreateSessionRequest
            {
                isPublic = this.isPublic,
                friendlyName = $"Unity Game - {DateTime.Now:HH:mm}"
            };
            request.SetRegion(this.region);
            request.SetGameType(this.gameType);
            request.SetPaintballMode(this.paintballMode);

            activeSession = await client.CreateCreditSessionAsync(request);

            currentSessionCode = activeSession.code;
            currentSessionId = activeSession.id;
            sessionStatus = activeSession.status;

            Debug.Log($"✓ Session created! Code: {activeSession.code}");
            Debug.Log($"  Session ID: {activeSession.id}");
            Debug.Log($"  Status: {activeSession.status}");

            // You can now use activeSession.code to connect to the server
            ConnectToServer(activeSession.code);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create session: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a session with auto-stop (automatic cleanup after 2 hours)
    /// </summary>
    public async void CreateSessionWithAutoStop()
    {
        if (client == null)
        {
            Debug.LogError("Client not initialized!");
            return;
        }

        try
        {
            Debug.Log("Creating session with auto-stop...");

            var request = new CreateSessionRequest
            {
                isPublic = this.isPublic,
                friendlyName = "Practice Match"
            };
            request.SetRegion(this.region);
            request.SetGameType(this.gameType);
            request.SetPaintballMode(this.paintballMode);

            // Set auto-stop time to 2 hours from now
            request.SetScheduledEndTime(DateTime.UtcNow.AddHours(2));

            activeSession = await client.CreateCreditSessionAsync(request);

            currentSessionCode = activeSession.code;
            currentSessionId = activeSession.id;
            sessionStatus = activeSession.status;

            Debug.Log($"✓ Session created with auto-stop! Code: {activeSession.code}");
            Debug.Log($"  Will auto-stop at: {activeSession.GetScheduledEndTime()}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create session: {ex.Message}");
        }
    }

    /// <summary>
    /// List all your active sessions
    /// </summary>
    public async void ListMySessions()
    {
        if (client == null)
        {
            Debug.LogError("Client not initialized!");
            return;
        }

        try
        {
            Debug.Log("Fetching your sessions...");

            List<GameSession> sessions = await client.GetMySessionsAsync();

            Debug.Log($"Found {sessions.Count} session(s):");
            foreach (var session in sessions)
            {
                string name = session.friendlyName ?? session.code;
                Debug.Log($"  - {name} (ID: {session.id}, Status: {session.status})");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to list sessions: {ex.Message}");
        }
    }

    /// <summary>
    /// Check your credit balance
    /// </summary>
    public async void CheckCredits()
    {
        if (client == null)
        {
            Debug.LogError("Client not initialized!");
            return;
        }

        try
        {
            Debug.Log("Checking credit balance...");

            CreditBalance balance = await client.GetCreditBalanceAsync();

            float hours = balance.GetBalanceInHours();
            Debug.Log($"✓ Credit Balance: {balance.balance} credits ({hours:F1} hours)");
            Debug.Log($"  Total Purchased: {balance.totalPurchased}");
            Debug.Log($"  Total Used: {balance.totalUsed}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to get credits: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop the currently active session
    /// </summary>
    public async void StopCurrentSession()
    {
        if (client == null || activeSession == null)
        {
            Debug.LogError("No active session to stop!");
            return;
        }

        try
        {
            Debug.Log($"Stopping session {activeSession.code}...");

            await client.StopSessionAsync(activeSession.id);

            Debug.Log("✓ Session stopped successfully");

            currentSessionCode = null;
            currentSessionId = 0;
            sessionStatus = "Stopped";
            activeSession = null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to stop session: {ex.Message}");
        }
    }

    /// <summary>
    /// Your game's logic to connect to the server
    /// </summary>
    private void ConnectToServer(string serverCode)
    {
        Debug.Log($"Connecting to server with code: {serverCode}");
        // Implement your connection logic here
        // For example:
        // NetworkManager.ConnectToServer(serverCode);
    }

    void OnApplicationQuit()
    {
        // Optional: Stop the session when quitting the game
        if (activeSession != null && activeSession.IsActive())
        {
            StopCurrentSession();
        }
    }
}
