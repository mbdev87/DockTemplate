========================================
🎯 Editor Component Plugin
========================================

A professional text editor component with syntax highlighting, file operations, and error navigation.

📦 PLUGIN FEATURES:
• Multi-language syntax highlighting (C#, JS, Python, etc.)
• File operations (New, Open, Save, Save As)
• Theme-aware editor styling
• Line-by-line error navigation
• Reactive UI with AvaloniaEdit integration

🔗 MESSAGE CONTRACTS:
========================================

MESSAGES THIS COMPONENT EMITS:
----------------------------------------
1. EditorComponent_FileOpened (v1)
   Data: {"FilePath": string, "DocumentTitle": string, "Language": string, "Timestamp": datetime}
   Sent when: User opens a file successfully

2. EditorComponent_FileSaved (v1)
   Data: {"FilePath": string, "DocumentTitle": string, "Timestamp": datetime}
   Sent when: User saves a file successfully

3. EditorComponent_EditorReady (v1)
   Data: {"FilePath": string, "DocumentTitle": string, "Timestamp": datetime}
   Sent when: Editor is ready for external operations (e.g. error navigation)

MESSAGES THIS COMPONENT CONSUMES:
----------------------------------------
1. UIComponent_ThemeChanged (v1)
   Data: {"Theme": string, "Timestamp": datetime}
   Action: Updates syntax highlighting and editor colors

2. ErrorList_NavigateToError (v1)
   Data: {"FilePath": string, "LineNumber": int, "ErrorMessage": string, "ErrorLevel": string}
   Action: Opens file (if needed) and navigates to error line with highlighting

🛠️ INTEGRATION OPTIONS:
========================================

OPTION 1: Source Code Integration
• Add project reference to DockComponent.Editor.csproj
• Register in DI container: services.AddTransient<EditorToolViewModel>()
• Full access to internals, debugging, and customization

OPTION 2: Plugin Integration  
• Drop Editor-Component.dockplugin into plugins folder
• Plugin auto-discovery and loading
• Message-based communication only

💻 SAMPLE USAGE:
========================================

// Listen for editor events
MessageBus.Current.Listen&lt;ComponentMessage&gt;()
    .Where(msg => msg.Name == "EditorComponent_FileOpened")
    .Subscribe(msg => {
        var data = JsonSerializer.Deserialize&lt;FileOpenedData&gt;(msg.Data);
        Console.WriteLine($"File opened: {data.FilePath}");
    });

// Send error navigation
var errorData = new { FilePath = "myfile.cs", LineNumber = 42, ErrorMessage = "Null reference", ErrorLevel = "Error" };
var json = JsonSerializer.Serialize(errorData);
var message = new ComponentMessage("ErrorList_NavigateToError", 1, json);
MessageBus.Current.SendMessage(message);

🎨 DEPENDENCIES:
• AvaloniaEdit (text editor control)
• AvaloniaEdit.TextMate (syntax highlighting)  
• ReactiveUI (MVVM framework)
• NLog (logging)

🚀 Perfect for teams building IDE-like applications who want professional text editing without the complexity!