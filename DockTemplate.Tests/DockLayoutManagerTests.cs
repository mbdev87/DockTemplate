using DockComponent.Base;
using DockTemplate.Services;

namespace DockTemplate.Tests;

public class DockLayoutManagerTests
{
    private readonly DockLayoutManager _sut = new();

    private static ComponentRegistration MakeTool(string id, DockPosition pos, Guid? instanceId = null)
        => new(id, new object(), pos, instanceId ?? Guid.NewGuid());

    private static ComponentRegistration MakeDoc(string id, Guid? instanceId = null)
        => new(id, new object(), DockPosition.Document, instanceId ?? Guid.NewGuid());

    // --- Tool dedup ---

    [Fact]
    public void TryAddTool_FirstTime_ReturnsTrue()
    {
        Assert.True(_sut.TryAddTool(MakeTool("SolutionExplorer", DockPosition.Left)));
    }

    [Fact]
    public void TryAddTool_SameIdSamePosition_ReturnsFalse()
    {
        _sut.TryAddTool(MakeTool("SolutionExplorer", DockPosition.Left));
        Assert.False(_sut.TryAddTool(MakeTool("SolutionExplorer", DockPosition.Left)));
    }

    [Fact]
    public void TryAddTool_SameIdDifferentPosition_BothAccepted()
    {
        Assert.True(_sut.TryAddTool(MakeTool("Properties", DockPosition.Left)));
        Assert.True(_sut.TryAddTool(MakeTool("Properties", DockPosition.Right)));
    }

    [Fact]
    public void TryAddTool_DifferentIds_BothAccepted()
    {
        Assert.True(_sut.TryAddTool(MakeTool("SolutionExplorer", DockPosition.Left)));
        Assert.True(_sut.TryAddTool(MakeTool("Efmig", DockPosition.Left)));
    }

    [Fact]
    public void TryAddTool_SameIdDifferentInstanceId_StillRejected()
    {
        _sut.TryAddTool(MakeTool("SolutionExplorer", DockPosition.Left, Guid.NewGuid()));
        Assert.False(_sut.TryAddTool(MakeTool("SolutionExplorer", DockPosition.Left, Guid.NewGuid())));
    }

    // --- Document dedup ---

    [Fact]
    public void TryAddDocument_FirstTime_ReturnsTrue()
    {
        Assert.True(_sut.TryAddDocument(MakeDoc("Dashboard")));
    }

    [Fact]
    public void TryAddDocument_SameId_ReturnsFalse()
    {
        _sut.TryAddDocument(MakeDoc("Dashboard"));
        Assert.False(_sut.TryAddDocument(MakeDoc("Dashboard")));
    }

    // --- IsToolPresent ---

    [Fact]
    public void IsToolPresent_AfterAdd_ReturnsTrue()
    {
        _sut.TryAddTool(MakeTool("Output", DockPosition.Bottom));
        Assert.True(_sut.IsToolPresent("Output", DockPosition.Bottom));
    }

    [Fact]
    public void IsToolPresent_WrongPosition_ReturnsFalse()
    {
        _sut.TryAddTool(MakeTool("Output", DockPosition.Bottom));
        Assert.False(_sut.IsToolPresent("Output", DockPosition.Left));
    }

    // --- GetToolIds ---

    [Fact]
    public void GetToolIds_ReturnsAllToolsInPosition()
    {
        _sut.TryAddTool(MakeTool("SolutionExplorer", DockPosition.Left));
        _sut.TryAddTool(MakeTool("Efmig", DockPosition.Left));
        _sut.TryAddTool(MakeTool("Output", DockPosition.Bottom));

        var leftIds = _sut.GetToolIds(DockPosition.Left);
        Assert.Equal(2, leftIds.Count);
        Assert.Contains("SolutionExplorer", leftIds);
        Assert.Contains("Efmig", leftIds);
    }

    // --- ClearPosition ---

    [Fact]
    public void ClearPosition_OnlyAffectsTargetPosition()
    {
        _sut.TryAddTool(MakeTool("SolutionExplorer", DockPosition.Left));
        _sut.TryAddTool(MakeTool("Output", DockPosition.Bottom));

        _sut.ClearPosition(DockPosition.Left);

        Assert.Empty(_sut.GetToolIds(DockPosition.Left));
        Assert.Single(_sut.GetToolIds(DockPosition.Bottom));
    }

    [Fact]
    public void ClearPosition_AllowsReAddingSameToolId()
    {
        _sut.TryAddTool(MakeTool("SolutionExplorer", DockPosition.Left));
        _sut.ClearPosition(DockPosition.Left);

        Assert.True(_sut.TryAddTool(MakeTool("SolutionExplorer", DockPosition.Left)));
    }

    // --- ClearAll ---

    [Fact]
    public void ClearAll_ResetsEverything()
    {
        _sut.TryAddTool(MakeTool("SolutionExplorer", DockPosition.Left));
        _sut.TryAddTool(MakeTool("Output", DockPosition.Bottom));
        _sut.TryAddDocument(MakeDoc("Dashboard"));

        _sut.ClearAll();

        Assert.Empty(_sut.GetToolIds(DockPosition.Left));
        Assert.Empty(_sut.GetToolIds(DockPosition.Bottom));
        Assert.Empty(_sut.GetDocumentIds());
    }

    // --- Simulates the duplication bug scenario ---

    [Fact]
    public void IntegrateCalledTwice_WithoutReset_NoToolDuplication()
    {
        var tools = new[]
        {
            MakeTool("SolutionExplorer", DockPosition.Left),
            MakeTool("Efmig", DockPosition.Left),
            MakeTool("Output", DockPosition.Bottom),
            MakeTool("ErrorList", DockPosition.Bottom),
        };

        // First integration pass
        foreach (var t in tools) _sut.TryAddTool(t);

        // Second integration pass (e.g. theme switch) — should all be rejected
        foreach (var t in tools) Assert.False(_sut.TryAddTool(t));

        Assert.Equal(2, _sut.GetToolIds(DockPosition.Left).Count);
        Assert.Equal(2, _sut.GetToolIds(DockPosition.Bottom).Count);
    }

    [Fact]
    public void LayoutReset_ClearThenReIntegrate_ExactlyOncePerTool()
    {
        var tools = new[]
        {
            MakeTool("SolutionExplorer", DockPosition.Left),
            MakeTool("Efmig", DockPosition.Left),
        };

        // Initial integrate
        foreach (var t in tools) _sut.TryAddTool(t);

        // Layout reset
        _sut.ClearAll();

        // Re-integrate
        int added = 0;
        foreach (var t in tools)
            if (_sut.TryAddTool(t)) added++;

        Assert.Equal(2, added);
        Assert.Equal(2, _sut.GetToolIds(DockPosition.Left).Count);
    }

    // --- Event notifications ---

    [Fact]
    public void ToolAdded_EventFires_OnlyWhenActuallyAdded()
    {
        var firedFor = new List<string>();
        _sut.ToolAdded += reg => firedFor.Add(reg.Id);

        _sut.TryAddTool(MakeTool("SolutionExplorer", DockPosition.Left));
        _sut.TryAddTool(MakeTool("SolutionExplorer", DockPosition.Left)); // dup

        Assert.Single(firedFor);
        Assert.Equal("SolutionExplorer", firedFor[0]);
    }
}
