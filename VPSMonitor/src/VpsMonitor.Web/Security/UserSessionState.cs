namespace VpsMonitor.Web.Security;

public sealed class UserSessionState
{
    public bool IsAuthenticated { get; private set; }
    public string? Username { get; private set; }
    public event Action? OnChange;

    public void Login(string username)
    {
        IsAuthenticated = true;
        Username = username;
        OnChange?.Invoke();
    }

    public void Logout()
    {
        IsAuthenticated = false;
        Username = null;
        OnChange?.Invoke();
    }
}
