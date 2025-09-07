========================================
📁 SolutionExplorer Component Plugin
========================================

A professional file system explorer with VS Code-inspired visual styling and intelligent file type recognition.

📦 PLUGIN FEATURES:
• Hierarchical file/folder navigation with expand/collapse
• VS Code-inspired file type icons and color coding
• Material Design iconography integration
• Intelligent file extension recognition (50+ types)
• Folder type detection (assets, bin, obj, .git, etc.)
• Context operations (expand all, collapse all, refresh)
• File selection and navigation

🔗 MESSAGE CONTRACTS:
========================================

MESSAGES THIS COMPONENT EMITS:
----------------------------------------
1. SolutionExplorerComponent_FileSelected (v1)
   Data: {"FilePath": string, "FileName": string, "FileExtension": string, "Timestamp": datetime}
   Sent when: User clicks on a file in the explorer

2. SolutionExplorerComponent_DirectoryExpanded (v1)
   Data: {"DirectoryPath": string, "ItemCount": int, "Timestamp": datetime}
   Sent when: User expands a directory node

3. SolutionExplorerComponent_DirectoryCollapsed (v1)
   Data: {"DirectoryPath": string, "Timestamp": datetime}
   Sent when: User collapses a directory node

MESSAGES THIS COMPONENT CONSUMES:
----------------------------------------
1. EditorComponent_NavigateToFile (v1)
   Data: {"FilePath": string, "LineNumber": int}
   Action: Expands tree and highlights the specified file

2. UIComponent_RefreshExplorer (v1)
   Data: {"Timestamp": datetime}
   Action: Refreshes the entire file tree

🎨 VISUAL FEATURES:
========================================

FILE TYPE RECOGNITION:
• Blue: .NET files (.cs, .csproj, .sln)
• Green: Web markup (HTML, CSS) and documentation (.md, .txt)
• Orange: Scripts (JS, TS) and configuration (JSON, XML)
• Purple: Programming languages (Python, Java, C++, etc.)
• Teal: Images and media files
• Yellow: Folders (with intelligent recognition)
• Red: Git files and system configs
• Pink: Archives and compressed files

FOLDER INTELLIGENCE:
• Assets folder → Image icon
• bin/obj folders → Build icon
• .git folder → Git icon
• node_modules → Package icon
• src/source → Code icon

🛠️ INTEGRATION OPTIONS:
========================================

OPTION 1: Source Code Integration
• Add project reference to DockComponent.SolutionExplorer.csproj
• Register in DI container: services.AddTransient&lt;SolutionExplorerViewModel&gt;()
• Full access to internals, debugging, and customization

OPTION 2: Plugin Integration  
• Drop SolutionExplorer-Component.dockplugin into plugins folder
• Plugin auto-discovery and loading
• Message-based communication only

💻 SAMPLE USAGE:
========================================

// Listen for file selection events
MessageBus.Current.Listen&lt;ComponentMessage&gt;()
    .Where(msg =&gt; msg.Name == "SolutionExplorerComponent_FileSelected")
    .Subscribe(msg =&gt; {
        var data = JsonSerializer.Deserialize&lt;FileSelectedData&gt;(msg.Data);
        Console.WriteLine($"File selected: {data.FilePath}");
        // Open file in editor, etc.
    });

// Request navigation to specific file
var navData = new { FilePath = "src/MyClass.cs", LineNumber = 42 };
var json = JsonSerializer.Serialize(navData);
var message = new ComponentMessage("EditorComponent_NavigateToFile", 1, json);
MessageBus.Current.SendMessage(message);

🎯 DEPENDENCIES:
• Material.Icons.Avalonia (beautiful file type icons)
• ReactiveUI (MVVM framework)
• NLog (logging)

🚀 Perfect for teams building development tools who want professional file navigation with zero setup complexity!