========================================
📋 Output Component Plugin
========================================

Professional logging and output display with real-time filtering and search capabilities.

📦 PLUGIN FEATURES:
• Real-time log entry display with color-coded levels
• Advanced filtering by log level (Debug, Info, Warn, Error)
• Live text search with highlight
• Auto-scroll with manual control
• NLog integration for system logging
• Reactive UI with high-performance rendering

🔗 MESSAGE CONTRACTS:
========================================

MESSAGES THIS COMPONENT EMITS:
----------------------------------------
1. OutputComponent_LogEntry (v1)
   Data: {"Level": string, "Message": string, "Source": string, "Timestamp": datetime}
   Sent when: System logs any message

MESSAGES THIS COMPONENT CONSUMES:
----------------------------------------
1. UIComponent_ThemeChanged (v1)
   Data: {"Theme": string}
   Action: Updates output colors and styling

🛠️ INTEGRATION OPTIONS:
========================================

OPTION 1: Source Code Integration
• Add project reference to DockComponent.Output.csproj
• Register in DI container: services.AddTransient<OutputViewModel>()

OPTION 2: Plugin Integration  
• Drop Output-Component.dockplugin into plugins folder
• Message-based communication only

🚀 Perfect for development tools needing professional logging capabilities!