using System;

namespace DockTemplate.Messages;

public class UILoadedMessage
{
    public DateTime LoadedTime { get; set; } = DateTime.Now;

    public UILoadedMessage()
    {
    }

    public override string ToString()
    {
        return $"UILoadedMessage(LoadedTime:{LoadedTime})";
    }
}