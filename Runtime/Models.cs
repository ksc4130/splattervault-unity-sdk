using System;
using System.Collections.Generic;

namespace SplatterVault
{
    /// <summary>
    /// Available game types
    /// </summary>
    public enum GameType
    {
        PaintballPlayground,
        Snapshot,
        Valheim,
        Satisfactory
    }

    /// <summary>
    /// Available server regions
    /// </summary>
    public enum Region
    {
        NYC3,   // New York
        TOR1,   // Toronto
        SFO1,   // San Francisco
        LON1    // London
    }

    /// <summary>
    /// Game modes for Paintball Playground (PPVR)
    /// </summary>
    public enum PaintballMode
    {
        XBall,           // XBall competitive mode
        NXL,             // Pubs (public matches)
        KillConfirmed,   // TAGS mode
        OneVOne,         // 1v1 matches
        ESPN             // 3v3 competitive
    }

    /// <summary>
    /// Game modes for Snapshot
    /// </summary>
    public enum SnapshotMode
    {
        Deathmatch,
        TeamDeathmatch,
        CaptureTheFlag,
        Domination
    }

    /// <summary>
    /// Game modes for Valheim (if applicable)
    /// </summary>
    public enum ValheimMode
    {
        Normal,
        Casual,
        Hardcore
    }

    /// <summary>
    /// Extension methods for enum to string conversion
    /// </summary>
    public static class EnumExtensions
    {
        public static string ToApiString(this GameType gameType)
        {
            return gameType.ToString();
        }

        public static string ToApiString(this Region region)
        {
            return region.ToString();
        }

        public static string ToApiString(this PaintballMode mode)
        {
            return mode.ToString();
        }

        public static string ToApiString(this SnapshotMode mode)
        {
            return mode.ToString();
        }

        public static string ToApiString(this ValheimMode mode)
        {
            return mode.ToString();
        }
    }

    /// <summary>
    /// Request model for creating a new game session
    /// </summary>
    [Serializable]
    public class CreateSessionRequest
    {
        public string region = Region.NYC3.ToApiString();
        public string gameType = GameType.PaintballPlayground.ToApiString();
        public string mode = PaintballMode.XBall.ToApiString();
        public bool isPublic = false;
        public string friendlyName;
        public string scheduledStartTime; // ISO 8601 format
        public string scheduledEndTime;   // ISO 8601 format

        /// <summary>
        /// Set the region using strongly-typed enum
        /// </summary>
        public void SetRegion(Region region)
        {
            this.region = region.ToApiString();
        }

        /// <summary>
        /// Set the game type using strongly-typed enum
        /// </summary>
        public void SetGameType(GameType gameType)
        {
            this.gameType = gameType.ToApiString();
        }

        /// <summary>
        /// Set the game mode for Paintball Playground
        /// </summary>
        public void SetPaintballMode(PaintballMode mode)
        {
            this.mode = mode.ToApiString();
        }

        /// <summary>
        /// Set the game mode for Snapshot
        /// </summary>
        public void SetSnapshotMode(SnapshotMode mode)
        {
            this.mode = mode.ToApiString();
        }

        /// <summary>
        /// Set the game mode for Valheim
        /// </summary>
        public void SetValheimMode(ValheimMode mode)
        {
            this.mode = mode.ToApiString();
        }

        /// <summary>
        /// Set the game mode using a string (for flexibility)
        /// </summary>
        public void SetMode(string mode)
        {
            this.mode = mode;
        }

        /// <summary>
        /// Set the scheduled start time from a DateTime
        /// </summary>
        public void SetScheduledStartTime(DateTime dateTime)
        {
            scheduledStartTime = dateTime.ToUniversalTime().ToString("o");
        }

        /// <summary>
        /// Set the scheduled end time (auto-stop) from a DateTime
        /// </summary>
        public void SetScheduledEndTime(DateTime dateTime)
        {
            scheduledEndTime = dateTime.ToUniversalTime().ToString("o");
        }
    }

    /// <summary>
    /// Game session model
    /// </summary>
    [Serializable]
    public class GameSession
    {
        public int id;
        public string code;
        public string serverName;
        public string friendlyName;
        public string status;
        public string gameType;
        public string region;
        public string mode;
        public bool isPublic;
        public string scheduledStartTime;
        public string scheduledEndTime;
        public string serverStart;
        public string slaveIp;
        public int createdById;
        public string serverType; // "Credit" or "Subscription"

        /// <summary>
        /// Gets the scheduled start time as DateTime (if set)
        /// </summary>
        public DateTime? GetScheduledStartTime()
        {
            if (string.IsNullOrEmpty(scheduledStartTime))
                return null;
            return DateTime.Parse(scheduledStartTime);
        }

        /// <summary>
        /// Gets the scheduled end time as DateTime (if set)
        /// </summary>
        public DateTime? GetScheduledEndTime()
        {
            if (string.IsNullOrEmpty(scheduledEndTime))
                return null;
            return DateTime.Parse(scheduledEndTime);
        }

        /// <summary>
        /// Gets the server start time as DateTime
        /// </summary>
        public DateTime GetServerStartTime()
        {
            return DateTime.Parse(serverStart);
        }

        /// <summary>
        /// Check if the session is currently active
        /// </summary>
        public bool IsActive()
        {
            return status == "Active";
        }

        /// <summary>
        /// Check if the session is scheduled for future start
        /// </summary>
        public bool IsScheduled()
        {
            return status == "Scheduled";
        }
    }

    /// <summary>
    /// Credit balance information
    /// </summary>
    [Serializable]
    public class CreditBalance
    {
        public int id;
        public int balance;
        public int totalPurchased;
        public int totalUsed;
        public bool alertsEnabled;
        public int alertThreshold;

        /// <summary>
        /// Gets the balance as hours (60 credits = 1 hour)
        /// </summary>
        public float GetBalanceInHours()
        {
            return balance / 60f;
        }

        /// <summary>
        /// Check if there are enough credits for a given duration
        /// </summary>
        public bool HasEnoughCredits(int minutes)
        {
            return balance >= minutes;
        }
    }

    /// <summary>
    /// Credit statistics
    /// </summary>
    [Serializable]
    public class CreditStats
    {
        public int balance;
        public int totalPurchased;
        public int totalUsed;
        public int monthlyUsage;
        public bool canStartSession;
        public List<CreditTransaction> recentTransactions;
    }

    /// <summary>
    /// Credit transaction record
    /// </summary>
    [Serializable]
    public class CreditTransaction
    {
        public int id;
        public int amount;
        public string type;
        public string description;
        public string createdAt;
    }

    /// <summary>
    /// API response wrapper
    /// </summary>
    [Serializable]
    public class ApiResponse<T>
    {
        public T data;
        public string message;
        public string name;
        public int status;
        public List<string> errors;
    }

    /// <summary>
    /// Error response model
    /// </summary>
    [Serializable]
    public class ApiError
    {
        public string name;
        public string message;
        public int status;
        public List<string> errors;
    }

    /// <summary>
    /// Subscription information
    /// </summary>
    [Serializable]
    public class Subscription
    {
        public int id;
        public string tier;
        public string status;
        public string periodStart;
        public string periodEnd;
        public int monthlyCredits;
    }

    /// <summary>
    /// Usage statistics
    /// </summary>
    [Serializable]
    public class UsageStats
    {
        public int currentInstances;
        public int maxInstances;
        public int creditBalance;
        public int creditHours;
        public int monthlyCredits;
        public int totalSessions;
        public int activeSessionsThisMonth;
    }
}
