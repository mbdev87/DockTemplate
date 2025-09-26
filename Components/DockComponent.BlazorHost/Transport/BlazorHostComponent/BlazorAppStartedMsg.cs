namespace DockComponent.BlazorHost.Transport.BlazorHostComponent;

public class BlazorAppStartedMsg
{
    public string Url { get; set; } = string.Empty;
    public int Port { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}