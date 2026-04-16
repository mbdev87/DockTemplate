using DockComponent.Base;
using DockTemplate.Services;

namespace DockTemplate.Tests;

public class ComponentRegistryTests : IDisposable
{
    public ComponentRegistryTests()
    {
        ComponentRegistry.Instance.Clear();
    }

    public void Dispose()
    {
        ComponentRegistry.Instance.Clear();
    }

    [Fact]
    public void TryRegisterComponent_SameNameVersion_RejectedSecondTime()
    {
        var comp1 = new FakeComponent("TestPlugin", "1.0.0");
        var comp2 = new FakeComponent("TestPlugin", "1.0.0");

        Assert.True(ComponentRegistry.Instance.TryRegisterComponent(comp1, "a.dll"));
        Assert.False(ComponentRegistry.Instance.TryRegisterComponent(comp2, "b.dll"));
    }

    [Fact]
    public void TryRegisterComponent_DifferentVersion_BothAccepted()
    {
        var comp1 = new FakeComponent("TestPlugin", "1.0.0");
        var comp2 = new FakeComponent("TestPlugin", "2.0.0");

        Assert.True(ComponentRegistry.Instance.TryRegisterComponent(comp1, "a.dll"));
        Assert.True(ComponentRegistry.Instance.TryRegisterComponent(comp2, "b.dll"));
    }

    [Fact]
    public void AddComponents_SameToolIdAndInstance_NotDuplicated()
    {
        var instanceId = Guid.NewGuid();
        var reg = new ComponentRegistration("SolutionExplorer", new object(), DockPosition.Left, instanceId);

        ComponentRegistry.Instance.AddComponents(new[] { reg }, Array.Empty<ComponentRegistration>());
        ComponentRegistry.Instance.AddComponents(new[] { reg }, Array.Empty<ComponentRegistration>());

        Assert.Single(ComponentRegistry.Instance.ComponentTools);
    }

    [Fact]
    public void AddComponents_DifferentToolIds_BothAdded()
    {
        var instanceId = Guid.NewGuid();
        var reg1 = new ComponentRegistration("SolutionExplorer", new object(), DockPosition.Left, instanceId);
        var reg2 = new ComponentRegistration("Efmig", new object(), DockPosition.Left, instanceId);

        ComponentRegistry.Instance.AddComponents(new[] { reg1, reg2 }, Array.Empty<ComponentRegistration>());

        Assert.Equal(2, ComponentRegistry.Instance.ComponentTools.Count);
    }

    [Fact]
    public void Clear_RemovesAllRegistrations()
    {
        var comp = new FakeComponent("TestPlugin", "1.0.0");
        ComponentRegistry.Instance.TryRegisterComponent(comp, "a.dll");
        var reg = new ComponentRegistration("Tool1", new object(), DockPosition.Left, Guid.NewGuid());
        ComponentRegistry.Instance.AddComponents(new[] { reg }, Array.Empty<ComponentRegistration>());

        ComponentRegistry.Instance.Clear();

        Assert.Empty(ComponentRegistry.Instance.ComponentTools);
        Assert.Empty(ComponentRegistry.Instance.LoadedComponents);
    }

    private class FakeComponent : IDockComponent
    {
        public string Name { get; }
        public string Version { get; }
        public Guid InstanceId { get; } = Guid.NewGuid();

        public FakeComponent(string name, string version)
        {
            Name = name;
            Version = version;
        }

        public void Register(IDockComponentContext context) { }
    }
}
