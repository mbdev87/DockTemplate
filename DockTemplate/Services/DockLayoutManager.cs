using System;
using System.Collections.Generic;
using System.Linq;
using DockComponent.Base;
using Dock.Model.Core;
using NLog;

namespace DockTemplate.Services;

/// <summary>
/// Tracks which tools/documents are present in each dock position.
/// Dedup is by tool ID within a position — the same logical tool
/// (e.g. "SolutionExplorer") can never appear twice in the left dock.
/// </summary>
public class DockLayoutManager : IDockLayoutManager
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Dictionary<DockPosition, HashSet<string>> _toolsByPosition = new()
    {
        [DockPosition.Left]   = new(StringComparer.OrdinalIgnoreCase),
        [DockPosition.Right]  = new(StringComparer.OrdinalIgnoreCase),
        [DockPosition.Bottom] = new(StringComparer.OrdinalIgnoreCase),
        [DockPosition.Top]    = new(StringComparer.OrdinalIgnoreCase),
    };

    private readonly HashSet<string> _documentIds = new(StringComparer.OrdinalIgnoreCase);

    public event Action<ComponentRegistration>? ToolAdded;
    public event Action<ComponentRegistration>? DocumentAdded;

    public bool TryAddTool(ComponentRegistration registration)
    {
        if (!_toolsByPosition.TryGetValue(registration.Position, out var ids))
        {
            Logger.Warn($"[LayoutManager] Unknown position: {registration.Position}");
            return false;
        }

        if (!ids.Add(registration.Id))
        {
            Logger.Info($"[LayoutManager] Tool '{registration.Id}' already present at {registration.Position} — skipped");
            return false;
        }

        Logger.Info($"[LayoutManager] Added tool '{registration.Id}' to {registration.Position}");
        ToolAdded?.Invoke(registration);
        return true;
    }

    public bool TryAddDocument(ComponentRegistration registration)
    {
        if (!_documentIds.Add(registration.Id))
        {
            Logger.Info($"[LayoutManager] Document '{registration.Id}' already present — skipped");
            return false;
        }

        Logger.Info($"[LayoutManager] Added document '{registration.Id}'");
        DocumentAdded?.Invoke(registration);
        return true;
    }

    public bool IsToolPresent(string toolId, DockPosition position)
    {
        return _toolsByPosition.TryGetValue(position, out var ids) && ids.Contains(toolId);
    }

    public IReadOnlyList<string> GetToolIds(DockPosition position)
    {
        return _toolsByPosition.TryGetValue(position, out var ids)
            ? ids.ToList().AsReadOnly()
            : Array.Empty<string>().ToList().AsReadOnly();
    }

    public IReadOnlyList<string> GetDocumentIds() => _documentIds.ToList().AsReadOnly();

    public void ClearPosition(DockPosition position)
    {
        if (_toolsByPosition.TryGetValue(position, out var ids))
        {
            ids.Clear();
            Logger.Info($"[LayoutManager] Cleared {position}");
        }
    }

    public void ClearAll()
    {
        foreach (var ids in _toolsByPosition.Values) ids.Clear();
        _documentIds.Clear();
        Logger.Info("[LayoutManager] Cleared all positions");
    }
}
