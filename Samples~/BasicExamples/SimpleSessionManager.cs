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
    [Tooltip("Your SplatterVault API key (sv_... for personal, sv_org_... for organization)")]
    [SerializeField] private string apiKey = "sv_your_api_key_here";

    [SerializeField] private Region region = Region.NYC3;

    [Tooltip("Game config key from your SplatterVault dashboard")]
    [SerializeField] private string gameKey = "your_game_key_here";

    [SerializeField] private bool isPublic = false;

    [Header("Session Info")]
    [SerializeField] private string currentSessionCode;
    [SerializeField] private int currentSessionId;
    [SerializeField] private string sessionStatus;

    private SplatterVaultClient client;
    private GameSession activeSession;

    async void Start()
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey == "sv_your_api_key_here")
        {
            Debug.LogError("Please set your API key in the inspector!");
            return;
        }

        try
        {
            // CreateAsync auto-resolves org ID for org API keys
            client = await SplatterVaultClient.CreateAsync(apiKey);
            Debug.Log($"SplatterVault client initialized (org key: {client.IsOrganizationKey})");

            if (client.AuthContext?.organizationId != null)
                Debug.Log($"  Organization: {client.AuthContext.organizationName} (ID: {client.AuthContext.organizationId})");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize client: {ex.Message}");
        }
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
                gameKey = this.gameKey,
                isPublic = this.isPublic,
                friendlyName = $"Unity Game - {DateTime.Now:HH:mm}"
            };
            request.SetRegion(this.region);

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
                gameKey = this.gameKey,
                isPublic = this.isPublic,
                friendlyName = "Practice Match"
            };
            request.SetRegion(this.region);

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

            // 0.25 credits/min is a typical small server rate — adjust based on your server size
            float hours = balance.GetBalanceInHours(0.25f);
            Debug.Log($"✓ Credit Balance: {balance.balance} credits (~{hours:F1} hours at 0.25/min)");
            Debug.Log($"  Total Purchased: {balance.totalPurchased}");
            Debug.Log($"  Total Used: {balance.totalUsed}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to get credits: {ex.Message}");
        }
    }

    /// <summary>
    /// Check organization credit balance (only works with org API keys)
    /// </summary>
    public async void CheckOrgCredits()
    {
        if (client == null)
        {
            Debug.LogError("Client not initialized!");
            return;
        }

        if (!client.IsOrganizationKey)
        {
            Debug.LogWarning("CheckOrgCredits requires an organization API key (sv_org_...)");
            return;
        }

        try
        {
            Debug.Log("Checking organization credit balance...");

            OrgCreditStats stats = await client.GetOrgCreditBalanceAsync();

            Debug.Log($"Org Credit Balance: {stats.balance}");
            Debug.Log($"  Subscription Credits: {stats.subscriptionBalance}");
            Debug.Log($"  Ad-hoc Credits: {stats.adHocBalance}");
            Debug.Log($"  Total Purchased: {stats.totalPurchased}");
            Debug.Log($"  Total Used: {stats.totalUsed}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to get org credits: {ex.Message}");
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

            await client.StopSessionAsync(activeSession);

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
