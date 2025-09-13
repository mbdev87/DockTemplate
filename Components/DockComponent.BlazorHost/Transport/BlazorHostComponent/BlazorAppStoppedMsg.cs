namespace DockComponent.BlazorHost.Transport.BlazorHostComponent;

public class BlazorAppStoppedMsg
{
    public int Port { get; set; }
    public DateTime StoppedAt { get; set; } = DateTime.UtcNow;
}