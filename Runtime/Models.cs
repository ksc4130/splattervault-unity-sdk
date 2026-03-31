using System;
using System.Collections.Generic;
using Newtonsoft.Json;

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
        NYC1,   // New York 1
        NYC3,   // New York 3
        TOR1,   // Toronto
        SFO1,   // San Francisco 1
        SFO2,   // San Francisco 2
        SFO3,   // San Francisco 3
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
        public int? serverSizeId;         // Optional: server size ID (defaults per game type)
        public int? organizationId;      // Optional: bill to organization credits instead of personal
        public int? buildId;             // Optional: use a specific build

        // Fields for custom game type configurations
        public string gameTypeConfigKey;  // Optional: unique key for custom game type config
        public Dictionary<string, object> customVariables; // Optional: custom variable values

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

        /// <summary>
        /// Set the organization ID to bill session to org credits
        /// </summary>
        public void SetOrganizationId(int orgId)
        {
            organizationId = orgId;
        }

        /// <summary>
        /// Set the build ID to use a specific game build
        /// </summary>
        public void SetBuildId(int id)
        {
            buildId = id;
        }

        /// <summary>
        /// Set the game type config key for custom configurations
        /// </summary>
        /// <param name="configKey">Unique key from custom game type (e.g., "usr_123_abc456xyz")</param>
        public void SetGameTypeConfigKey(string configKey)
        {
            gameTypeConfigKey = configKey;
        }

        /// <summary>
        /// Add a custom variable value
        /// </summary>
        /// <param name="name">Variable name (e.g., "MAP_NAME")</param>
        /// <param name="value">Variable value</param>
        public void AddCustomVariable(string name, object value)
        {
            if (customVariables == null)
                customVariables = new Dictionary<string, object>();
            customVariables[name] = value;
        }

        /// <summary>
        /// Set multiple custom variables at once
        /// </summary>
        /// <param name="variables">Dictionary of variable names and values</param>
        public void SetCustomVariables(Dictionary<string, object> variables)
        {
            customVariables = variables;
        }

        /// <summary>
        /// Clear all custom variables
        /// </summary>
        public void ClearCustomVariables()
        {
            customVariables = null;
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
        public string hostname;
        public string friendlyName;
        public string status;
        public string gameType;
        public string region;
        public string mode;
        public bool isPublic;
        public string scheduledStartTime;
        public string scheduledEndTime;
        public string serverStart;
        public string serverStoppedAt;
        public string slaveIp;
        public int? slavePort;
        public int createdById;
        public string serverType; // "Credit", "Subscription", etc.
        public int serverSizeId;
        public int? buildId;
        public int? volumeId;
        public bool creditsDeducted;
        public int? organizationId;
        public string stopReason;
        public object stopReasonDetails;

        /// <summary>
        /// Server size details (populated when API includes the relation)
        /// </summary>
        public ServerSizeInfo serverSize;

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
        public DateTime? GetServerStartTime()
        {
            if (string.IsNullOrEmpty(serverStart))
                return null;
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
        /// Check if the session is pending (droplet provisioning)
        /// </summary>
        public bool IsPending()
        {
            return status == "Pending";
        }

        /// <summary>
        /// Check if the session is scheduled for future start
        /// </summary>
        public bool IsScheduled()
        {
            return status == "Scheduled";
        }

        /// <summary>
        /// Check if the session has stopped
        /// </summary>
        public bool IsStopped()
        {
            return status == "Not Active";
        }

        /// <summary>
        /// Get the game server port (defaults to 8100 if not set)
        /// </summary>
        public int GetServerPort()
        {
            return slavePort ?? 8100;
        }
    }

    /// <summary>
    /// Server size info returned as a nested object on session responses
    /// </summary>
    [Serializable]
    public class ServerSizeInfo
    {
        public int id;
        public string friendlyName;
        public float creditsPerMinute;
    }

    /// <summary>
    /// Result from stopping a credit-based session
    /// </summary>
    [Serializable]
    public class StopSessionResult
    {
        public GameSession session;
        public float totalHours;
        public float totalCost;
    }

    /// <summary>
    /// Credit balance information
    /// </summary>
    [Serializable]
    public class CreditBalance
    {
        public int id;
        public float balance;
        public float subscriptionBalance;
        public float adHocBalance;
        public bool subscriptionCreditsFrozen;
        public bool isInGracePeriod;
        public float totalPurchased;
        public float totalUsed;
        public bool alertsEnabled;
        public float alertThreshold;
        public string lastAlertSent;
        public string createdAt;
        public string updatedAt;

        /// <summary>
        /// Gets the total available balance (respects frozen subscription credits)
        /// </summary>
        public float GetAvailableBalance()
        {
            if (subscriptionCreditsFrozen)
                return adHocBalance;
            return subscriptionBalance + adHocBalance;
        }

        /// <summary>
        /// Gets the balance as hours based on a given credits-per-minute rate
        /// </summary>
        public float GetBalanceInHours(float creditsPerMinute)
        {
            if (creditsPerMinute <= 0)
                return 0;
            return GetAvailableBalance() / creditsPerMinute / 60f;
        }

        /// <summary>
        /// Check if there are enough credits for a given duration at a given rate
        /// </summary>
        public bool HasEnoughCredits(float minutes, float creditsPerMinute)
        {
            return GetAvailableBalance() >= minutes * creditsPerMinute;
        }
    }

    /// <summary>
    /// Credit statistics
    /// </summary>
    [Serializable]
    public class CreditStats
    {
        public float balance;
        public float subscriptionBalance;
        public float adHocBalance;
        public bool subscriptionCreditsFrozen;
        public bool canStartSession;
        public float totalPurchased;
        public float totalUsed;
        public float monthlyUsage;
        public List<CreditTransaction> recentTransactions;
    }

    /// <summary>
    /// Credit transaction record
    /// </summary>
    [Serializable]
    public class CreditTransaction
    {
        public int id;
        public float amount;
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
        public int currentInstances;
    }

    /// <summary>
    /// Subscription details response (from GET /subscriptions)
    /// </summary>
    [Serializable]
    public class SubscriptionDetails
    {
        public Subscription current;
        public List<Subscription> all;
    }

    /// <summary>
    /// Usage statistics
    /// </summary>
    [Serializable]
    public class UsageStats
    {
        public int currentInstances;
        public int maxInstances;
        public float creditBalance;
        public float creditHours;
        public int monthlyCredits;
        public int totalSessions;
        public int activeSessionsThisMonth;
    }

    /// <summary>
    /// Organization credit statistics (from /organizations/:orgId/credits)
    /// </summary>
    [Serializable]
    public class OrgCreditStats
    {
        public float balance;
        public float subscriptionBalance;
        public float adHocBalance;
        public float totalPurchased;
        public float totalUsed;
        public bool autoBuyEnabled;
        public float? autoBuyThreshold;
        public float? autoBuyCreditAmount;

        /// <summary>
        /// Gets the total available balance
        /// </summary>
        public float GetAvailableBalance()
        {
            return subscriptionBalance + adHocBalance;
        }
    }

    /// <summary>
    /// Organization subscription info (from /organizations/:orgId/subscription)
    /// </summary>
    [Serializable]
    public class OrgSubscriptionInfo
    {
        public Subscription current;
        public List<Subscription> all;
    }

    /// <summary>
    /// Result from canceling a scheduled session
    /// </summary>
    [Serializable]
    public class CancelScheduleResult
    {
        public string message;
        public GameSession session;
    }
}
