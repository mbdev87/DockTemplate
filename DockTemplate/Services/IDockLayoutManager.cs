using System.Collections.Generic;
using DockComponent.Base;

namespace DockTemplate.Services;

/// <summary>
/// Manages the mapping of registered tools/documents to dock positions.
/// Owns dedup: a tool ID can only appear once per position.
/// Extracted from DockFactory for testability.
/// </summary>
public interface IDockLayoutManager
{
    bool TryAddTool(ComponentRegistration registration);
    bool TryAddDocument(ComponentRegistration registration);
    bool IsToolPresent(string toolId, DockPosition position);
    IReadOnlyList<string> GetToolIds(DockPosition position);
    IReadOnlyList<string> GetDocumentIds();
    void ClearPosition(DockPosition position);
    void ClearAll();
}
