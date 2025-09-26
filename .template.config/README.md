# 🚀 DockTemplate - .NET Project Template

## Quick Start

```bash
# Install the template
dotnet new install DockTemplate.ProjectTemplate

# Create your awesome IDE
dotnet new dock-ide --name MyAwesomeIDE

# Navigate and run
cd MyAwesomeIDE
dotnet run
```

## Template Parameters

### Basic Parameters
- `--name` : Your project name (default: MyAwesomeIDE)
- `--framework` : Target framework (`net9.0` or `net8.0`)
- `--company` : Company name for metadata
- `--author` : Author name for metadata

### Feature Toggles
- `--include-blazor` : Include Blazor dashboard (default: true)
- `--enable-plugin-system` : Enable plugin architecture (default: true)

## Advanced Usage Examples

```bash
# Minimal desktop app without Blazor
dotnet new dock-ide --name MyDesktopApp --include-blazor false

# Enterprise application
dotnet new dock-ide --name CorporateIDE \
  --company "Acme Corp" \
  --author "Development Team" \
  --framework net8.0

# Plugin-focused IDE
dotnet new dock-ide --name PluginStudio \
  --enable-plugin-system true \
  --include-blazor true
```

## What You Get

### 📦 Complete Solution Structure
```
MyAwesomeIDE/
├── MyAwesomeIDE.sln
├── MyAwesomeIDE/              # Main application
├── Components/                # Plugin components
│   ├── MyAwesomeIDEComponent.Output/
│   ├── MyAwesomeIDEComponent.ErrorList/
│   ├── MyAwesomeIDEComponent.SolutionExplorer/
│   ├── MyAwesomeIDEComponent.Editor/
│   └── MyAwesomeIDEComponent.BlazorHost/
├── MyAwesomeIDEBlazorExample/ # Blazor dashboard (optional)
└── MyAwesomeIDEComponent.Base/ # Shared component infrastructure
```

### 🎯 Key Features Out-of-the-Box
- **Professional dock layout** with resizable panels
- **Material Design icons** with VS Code-inspired colors
- **Plugin architecture** for extensibility
- **Blazor integration** for web-based UI components
- **Reactive MVVM** with ReactiveUI
- **Dependency injection** throughout
- **Professional logging** with NLog
- **Theme system** with light/dark modes

### 🔌 Plugin Components Included
- **Output Panel**: Real-time logging with filtering
- **Error List**: Error tracking with source navigation
- **Solution Explorer**: File system browser with Material icons
- **Text Editor**: Extensible document editing
- **Blazor Dashboard**: Web-based analytics and controls

## First Run Experience

After creating your project:

1. **Build everything**: `dotnet build`
2. **Run your IDE**: `dotnet run --project MyAwesomeIDE`
3. **See the magic**: Professional IDE interface loads immediately
4. **Open files**: Use Solution Explorer to browse and open files
5. **Check logs**: Output panel shows real-time application logs
6. **View dashboard**: Blazor component shows project analytics

## Customization Quick Start

### Add Your Own Component
```bash
# Create new component
mkdir Components/MyAwesomeIDEComponent.MyTool
cd Components/MyAwesomeIDEComponent.MyTool

# Copy structure from existing component
# Implement IDockComponent interface
# Register in Program.cs
```

### Modify Blazor Dashboard
```bash
cd MyAwesomeIDEBlazorExample
# Edit Components/Pages/Dashboard.razor
# Add your custom analytics
```

### Theme Customization
```csharp
// In MyAwesomeIDE/Services/ThemeService.cs
public void ApplyCustomTheme()
{
    // Your theme logic here
}
```

## Template Development

Want to modify this template?

```bash
# Clone the source
git clone https://github.com/mbdev87/DockTemplate
cd DockTemplate

# Install as local template
dotnet new install ./

# Test your changes
dotnet new dock-ide --name TestProject
```

## Community & Support

- **GitHub**: https://github.com/mbdev87/DockTemplate
- **Issues**: Report bugs or request features
- **Discussions**: Share your creations and get help
- **Contributions**: PRs welcome!

## License

This template is open source. Create amazing desktop applications and share them with the world! 🌟