using Fusion;

public static class NetworkSessionRequest
{
    public const int MainMenuSceneIndex = 0;
    public const int GameSceneIndex = 1;
    public const int MaxPlayers = 4;

    public static bool HasPendingRequest { get; private set; }
    public static bool IsMultiPeerTest { get; private set; }
    public static GameMode RequestedMode { get; private set; } = GameMode.Host;
    public static string SessionName { get; private set; } = "LambskinRoom";
    public static string LastStatus { get; private set; } = string.Empty;

    public static void Set(GameMode mode, string sessionName)
    {
        RequestedMode = mode;
        SessionName = NormalizeSessionName(sessionName);
        HasPendingRequest = true;
        IsMultiPeerTest = false;
        LastStatus = string.Empty;
    }

    public static void SetMultiPeerTest(string sessionName)
    {
        RequestedMode = GameMode.Host;
        SessionName = NormalizeSessionName(sessionName);
        HasPendingRequest = true;
        IsMultiPeerTest = true;
        LastStatus = string.Empty;
    }

    public static void ClearPending()
    {
        HasPendingRequest = false;
    }

    public static void SetStatus(string status)
    {
        LastStatus = status ?? string.Empty;
    }

    public static string NormalizeSessionName(string sessionName)
    {
        if (string.IsNullOrWhiteSpace(sessionName))
        {
            return "LambskinRoom";
        }

        return sessionName.Trim();
    }
}
