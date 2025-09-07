namespace DockComponent.Base;

/// <summary>
/// Simple message container for all plugin communication
/// This is the ONLY shared type across all plugins - the fundamental transport protocol
/// </summary>
public record ComponentMessage(string Name, string Payload, int Version = 1);