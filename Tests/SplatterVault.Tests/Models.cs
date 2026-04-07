using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SplatterVault
{
    public enum Region
    {
        NYC1,
        NYC3,
        TOR1,
        SFO1,
        SFO2,
        SFO3,
        LON1
    }

    public static class EnumExtensions
    {
        public static string ToApiString(this Region region) => region.ToString();
    }

    [Serializable]
    public class CreateSessionRequest
    {
        public string region = Region.NYC3.ToApiString();

        [JsonProperty("gameTypeConfigKey")]
        public string? gameKey;

        public string? friendlyName;
        public string? scheduledStartTime;
        public string? scheduledEndTime;
        public int? serverSizeId;
        public int? organizationId;
        public int? buildId;
        public string? channel;
        public Dictionary<string, object>? customVariables;

        public void SetRegion(Region region) => this.region = region.ToApiString();

        public void SetScheduledStartTime(DateTime dateTime)
        {
            scheduledStartTime = dateTime.ToUniversalTime().ToString("o");
        }

        public void SetScheduledEndTime(DateTime dateTime)
        {
            scheduledEndTime = dateTime.ToUniversalTime().ToString("o");
        }

        public void SetOrganizationId(int orgId) => organizationId = orgId;
        public void SetBuildId(int id) => buildId = id;
        public void SetChannel(string channelName) => channel = channelName;

        public void AddCustomVariable(string flag, object value)
        {
            customVariables ??= new Dictionary<string, object>();
            customVariables[flag] = value;
        }

        public void SetCustomVariables(Dictionary<string, object> variables) => customVariables = variables;
        public void ClearCustomVariables() => customVariables = null;
    }

    [Serializable]
    public class GameSession
    {
        public int id;
        public string? code;
        public string? serverName;
        public string? hostname;
        public string? friendlyName;
        public string? status;
        public string? gameType;
        public string? region;
        public string? mode;
        public string? scheduledStartTime;
        public string? scheduledEndTime;
        public string? serverStart;
        public string? serverStoppedAt;
        public string? slaveIp;
        public int? slavePort;
        public int createdById;
        public string? serverType;
        public int serverSizeId;
        public int? buildId;
        public int? volumeId;
        public bool creditsDeducted;
        public int? organizationId;
        public string? stopReason;
        public object? stopReasonDetails;
        public ServerSizeInfo? serverSize;

        public DateTime? GetScheduledStartTime() =>
            string.IsNullOrEmpty(scheduledStartTime) ? null : DateTime.Parse(scheduledStartTime);
        public DateTime? GetScheduledEndTime() =>
            string.IsNullOrEmpty(scheduledEndTime) ? null : DateTime.Parse(scheduledEndTime);
        public DateTime? GetServerStartTime() =>
            string.IsNullOrEmpty(serverStart) ? null : DateTime.Parse(serverStart);

        public bool IsActive() => status == "Active";
        public bool IsPending() => status == "Pending";
        public bool IsScheduled() => status == "Scheduled";
        public bool IsStopped() => status == "Not Active";
        public int GetServerPort() => slavePort ?? 8100;
    }

    [Serializable]
    public class StructuredLaunchArg
    {
        public string? flag;
        public string? value;
        public string? type;
        public bool userConfigurable;
        public string? label;
        public string? description;
        public bool required;
        public List<SelectOption>? options;
        public float? min;
        public float? max;
        public string? trueValue;
        public string? falseValue;
        public string? pattern;
        public string? semantic;
        public bool excludeWhenPublic;
    }

    [Serializable]
    public class SelectOption
    {
        public string? label;
        public string? value;
    }

    [Serializable]
    public class ServerSizeInfo
    {
        public int id;
        public string? friendlyName;
        public float creditsPerMinute;
    }

    [Serializable]
    public class StopSessionResult
    {
        public GameSession? session;
        public float totalHours;
        public float totalCost;
    }

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
        public string? lastAlertSent;
        public string? createdAt;
        public string? updatedAt;

        public float GetAvailableBalance() =>
            subscriptionCreditsFrozen ? adHocBalance : subscriptionBalance + adHocBalance;
        public float GetBalanceInHours(float creditsPerMinute) =>
            creditsPerMinute <= 0 ? 0 : GetAvailableBalance() / creditsPerMinute / 60f;
        public bool HasEnoughCredits(float minutes, float creditsPerMinute) =>
            GetAvailableBalance() >= minutes * creditsPerMinute;
    }

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
        public List<CreditTransaction>? recentTransactions;
    }

    [Serializable]
    public class CreditTransaction
    {
        public int id;
        public float amount;
        public string? type;
        public string? description;
        public string? createdAt;
    }

    [Serializable]
    public class ApiResponse<T>
    {
        public T? data;
        public string? message;
        public string? name;
        public int status;
        public List<string>? errors;
    }

    [Serializable]
    public class ApiError
    {
        public string? name;
        public string? message;
        public int status;
        public List<string>? errors;
    }

    [Serializable]
    public class Subscription
    {
        public int id;
        public string? tier;
        public string? status;
        public string? periodStart;
        public string? periodEnd;
        public int monthlyCredits;
        public int currentInstances;
    }

    [Serializable]
    public class SubscriptionDetails
    {
        public Subscription? current;
        public List<Subscription>? all;
    }

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

        public float GetAvailableBalance() => subscriptionBalance + adHocBalance;
    }

    [Serializable]
    public class OrgSubscriptionInfo
    {
        public Subscription? current;
        public List<Subscription>? all;
    }

    [Serializable]
    public class CancelScheduleResult
    {
        public string? message;
        public GameSession? session;
    }
}
