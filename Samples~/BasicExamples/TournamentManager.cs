using System;
using System.Collections.Generic;
using UnityEngine;
using SplatterVault;

/// <summary>
/// Advanced example showing tournament session management
/// Demonstrates scheduling, session monitoring, and bracket management
/// </summary>
public class TournamentManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string apiKey = "sv_your_api_key_here";
    
    [Header("Tournament Settings")]
    [SerializeField] private int numberOfMatches = 4;
    [SerializeField] private int matchDurationMinutes = 30;
    [SerializeField] private int breakBetweenMatches = 10;

    [Header("Status")]
    [SerializeField] private int scheduledMatches = 0;
    [SerializeField] private int activeMatches = 0;

    private SplatterVaultClient client;
    private List<GameSession> tournamentSessions = new List<GameSession>();

    void Start()
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey == "sv_your_api_key_here")
        {
            Debug.LogError("Please set your API key!");
            return;
        }

        client = new SplatterVaultClient(apiKey);
    }

    /// <summary>
    /// Schedule all tournament matches at once
    /// </summary>
    public async void ScheduleTournamentMatches()
    {
        if (client == null)
        {
            Debug.LogError("Client not initialized!");
            return;
        }

        try
        {
            // Check if we have enough credits
            var balance = await client.GetCreditBalanceAsync();
            int requiredCredits = numberOfMatches * matchDurationMinutes;
            
            if (!balance.HasEnoughCredits(requiredCredits))
            {
                Debug.LogError($"Insufficient credits! Need {requiredCredits}, have {balance.balance}");
                return;
            }

            Debug.Log($"Scheduling {numberOfMatches} tournament matches...");
            DateTime startTime = DateTime.UtcNow.AddMinutes(30); // Start in 30 minutes

            for (int i = 0; i < numberOfMatches; i++)
            {
                // Calculate start and end times for this match
                DateTime matchStart = startTime.AddMinutes(i * (matchDurationMinutes + breakBetweenMatches));
                DateTime matchEnd = matchStart.AddMinutes(matchDurationMinutes);

                var request = new CreateSessionRequest
                {
                    region = "NYC3",
                    gameType = "PaintballPlayground",
                    mode = "XBall",
                    isPublic = true,
                    friendlyName = $"Tournament Match {i + 1}"
                };

                request.SetScheduledStartTime(matchStart);
                request.SetScheduledEndTime(matchEnd);

                try
                {
                    var session = await client.CreateCreditSessionAsync(request);
                    tournamentSessions.Add(session);
                    scheduledMatches++;

                    Debug.Log($"✓ Scheduled Match {i + 1}");
                    Debug.Log($"  Code: {session.code}");
                    Debug.Log($"  Start: {matchStart:HH:mm}");
                    Debug.Log($"  End: {matchEnd:HH:mm}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to schedule match {i + 1}: {ex.Message}");
                }

                // Small delay between requests
                await System.Threading.Tasks.Task.Delay(500);
            }

            Debug.Log($"✓ Tournament scheduling complete! {scheduledMatches} matches scheduled.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Tournament scheduling failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Monitor all tournament sessions and update their status
    /// </summary>
    public async void MonitorTournamentStatus()
    {
        if (client == null)
        {
            Debug.LogError("Client not initialized!");
            return;
        }

        try
        {
            Debug.Log("Checking tournament status...");

            // Get all sessions
            var allSessions = await client.GetMySessionsAsync();

            activeMatches = 0;
            scheduledMatches = 0;

            foreach (var session in tournamentSessions)
            {
                // Find the updated session data
                var updated = allSessions.Find(s => s.id == session.id);
                if (updated != null)
                {
                    if (updated.IsActive())
                    {
                        activeMatches++;
                        Debug.Log($"  ⚡ {updated.friendlyName} is ACTIVE");
                    }
                    else if (updated.IsScheduled())
                    {
                        scheduledMatches++;
                        var startTime = updated.GetScheduledStartTime();
                        Debug.Log($"  ⏰ {updated.friendlyName} starts at {startTime:HH:mm}");
                    }
                    else
                    {
                        Debug.Log($"  ✓ {updated.friendlyName} completed");
                    }
                }
            }

            Debug.Log($"Status: {activeMatches} active, {scheduledMatches} scheduled");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Status check failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a single match with custom settings
    /// </summary>
    public async void CreateCustomMatch(string matchName, DateTime startTime, int durationMinutes)
    {
        if (client == null)
        {
            Debug.LogError("Client not initialized!");
            return;
        }

        try
        {
            var request = new CreateSessionRequest
            {
                region = "NYC3",
                gameType = "PaintballPlayground",
                mode = "XBall",
                isPublic = true,
                friendlyName = matchName
            };

            request.SetScheduledStartTime(startTime);
            request.SetScheduledEndTime(startTime.AddMinutes(durationMinutes));

            var session = await client.CreateCreditSessionAsync(request);
            tournamentSessions.Add(session);

            Debug.Log($"✓ Created custom match: {matchName}");
            Debug.Log($"  Code: {session.code}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create custom match: {ex.Message}");
        }
    }

    /// <summary>
    /// Emergency stop all tournament matches
    /// </summary>
    public async void StopAllMatches()
    {
        if (client == null)
        {
            Debug.LogError("Client not initialized!");
            return;
        }

        Debug.Log("Stopping all tournament matches...");

        int stopped = 0;
        foreach (var session in tournamentSessions)
        {
            try
            {
                await client.StopSessionAsync(session.id);
                stopped++;
                Debug.Log($"  ✓ Stopped {session.friendlyName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"  ✗ Failed to stop {session.friendlyName}: {ex.Message}");
            }

            await System.Threading.Tasks.Task.Delay(250);
        }

        Debug.Log($"✓ Stopped {stopped}/{tournamentSessions.Count} matches");
        tournamentSessions.Clear();
        scheduledMatches = 0;
        activeMatches = 0;
    }

    /// <summary>
    /// Get a summary of all matches with their server codes
    /// Useful for sharing with participants
    /// </summary>
    public void PrintMatchCodes()
    {
        Debug.Log("=== Tournament Server Codes ===");
        for (int i = 0; i < tournamentSessions.Count; i++)
        {
            var session = tournamentSessions[i];
            Debug.Log($"Match {i + 1}: {session.friendlyName}");
            Debug.Log($"  Server Code: {session.code}");
            
            if (session.GetScheduledStartTime() != null)
            {
                Debug.Log($"  Start Time: {session.GetScheduledStartTime():HH:mm}");
            }
        }
        Debug.Log("=============================");
    }

    /// <summary>
    /// Example: Schedule a championship bracket (4 matches)
    /// </summary>
    public async void ScheduleChampionshipBracket()
    {
        Debug.Log("Scheduling championship bracket...");

        DateTime now = DateTime.UtcNow;
        
        // Quarterfinals (2 matches, parallel)
        await CreateCustomMatch("Quarterfinal 1", now.AddHours(1), 30);
        await CreateCustomMatch("Quarterfinal 2", now.AddHours(1), 30);

        // Semifinals (1 hour after quarterfinals)
        await CreateCustomMatch("Semifinal", now.AddHours(2), 30);

        // Finals (1 hour after semifinals)
        await CreateCustomMatch("Championship Finals", now.AddHours(3), 45);

        Debug.Log("✓ Championship bracket scheduled!");
        PrintMatchCodes();
    }
}
