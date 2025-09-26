================================================================================
🌐 WEB HOST COMPONENT - Simple URL-based Web Integration
================================================================================

OVERVIEW:
The Web Host component is a lightweight wrapper for displaying web content
within your Avalonia desktop application. Just provide a URL and it handles
the rest - perfect for localhost Blazor apps, external websites, or any web content.

FEATURES:
✅ Simple URL input interface  
✅ Auto-protocol detection (adds http:// if missing)
✅ Copy URL to clipboard functionality
✅ Open in external browser option  
✅ Real-time status updates
✅ Ready for WebView integration (add WebViewControl-Avalonia package)
✅ Message bus integration for component communication
✅ Cross-platform compatibility (Windows/Mac/Linux)

ARCHITECTURE:
- WebHostManager: Simple URL state management
- ReactiveUI: Real-time UI updates and command binding  
- Plugin System: Completely isolated component with message bus
- WebView Ready: Architecture prepared for Chromium integration

MESSAGES WE EMIT:
================================================================================

1. BlazorHostComponent_BlazorAppStarted (v1)
   Data: {"Url": string, "Port": int, "StartedAt": datetime}
   Purpose: Notifies other components when a URL is loaded
   
   C# Class:
   public class BlazorAppStartedMsg
   {
       public string Url { get; set; } = string.Empty;
       public int Port { get; set; }  // 0 for external URLs
       public DateTime StartedAt { get; set; } = DateTime.UtcNow;
   }

2. BlazorHostComponent_BlazorAppStopped (v1)
   Data: {"Port": int, "StoppedAt": datetime}
   Purpose: Notifies cleanup when content is cleared
   
   C# Class:
   public class BlazorAppStoppedMsg
   {
       public int Port { get; set; }
       public DateTime StoppedAt { get; set; } = DateTime.UtcNow;
   }

MESSAGES WE CONSUME:
================================================================================

Currently: None (standalone component)
Future: Could listen for URL change requests from other components

USAGE SCENARIOS:
================================================================================

LOCALHOST BLAZOR APPS:
- Load http://localhost:5000 (or any port)
- Perfect for development and testing
- Instant preview of running Blazor applications

EXTERNAL WEBSITES:
- https://blazor.net
- https://docs.microsoft.com  
- Any public website for reference/documentation

INTERNAL TOOLS:
- http://127.0.0.1:3000 (React dev server)
- http://localhost:8080 (Vue/Angular apps)
- Custom web-based admin interfaces

TECHNICAL DETAILS:
================================================================================

DEPENDENCIES:
- Avalonia UI (core framework)
- ReactiveUI (reactive patterns)
- Dock.Model.Mvvm (docking support)

WEBVIEW INTEGRATION:
To enable actual web content rendering (instead of placeholder):
1. Add package: <PackageReference Include="WebViewControl-Avalonia" Version="3.120.10" />
2. Update BlazorHostView.axaml to include ChromiumBrowser control
3. Bind URL changes to WebView navigation

PERFORMANCE:
- Minimal overhead (just URL management)
- No server hosting (external responsibility)  
- ~1MB component size
- Instant startup

SECURITY:
- URL validation and sanitization
- Protocol enforcement (http/https)
- No embedded server attack surface

DEMO USAGE:
================================================================================

The component provides these example URLs to get started:
✅ localhost:5000 - Common Blazor dev port
✅ https://blazor.net - Official Blazor documentation
✅ http://127.0.0.1:3000 - Common React/Node dev port

INTEGRATION WORKFLOW:
================================================================================

1. START YOUR BLAZOR APP:
   dotnet run --urls=http://localhost:5000

2. LOAD IN WEB HOST:
   - Enter "localhost:5000" in URL field
   - Click "Load" button
   - Component shows ready state

3. FUTURE WEBVIEW INTEGRATION:
   - Add WebViewControl-Avalonia package
   - Replace placeholder with actual browser control
   - Full web rendering within desktop app

DEVELOPMENT NOTES:
================================================================================

SIMPLIFIED ARCHITECTURE:
- No embedded Kestrel server
- No Blazor hosting complexity  
- Pure URL-to-display component
- External apps handle their own hosting

WEBVIEW PLACEHOLDER:
- Current implementation shows URL ready state
- Architecture prepared for browser integration
- Add WebViewControl package when needed
- Zero licensing concerns (FOSS components)

PLUGIN INTEGRATION:
- Follows DockTemplate plugin architecture
- Zero cross-component dependencies
- Message bus for loose coupling
- Hot-pluggable via .dockplugin files

DEPLOYMENT:
================================================================================

FOR DEVELOPMENT:
1. Build the component project
2. Plugin auto-loads in DockTemplate  
3. Enter localhost URLs for testing

FOR DISTRIBUTION:
1. Include .dockplugin file in distribution
2. Drop into Components/ folder
3. No additional setup required

SIZE CONSIDERATIONS:
- Plugin bundle: ~1MB (minimal dependencies)
- Runtime: No additional overhead
- Perfect for any deployment scenario
- Add WebView when needed (~100MB with Chromium)

TROUBLESHOOTING:
================================================================================

URL ISSUES:
- Component auto-adds http:// if missing
- Check that target server is actually running
- Use 127.0.0.1 instead of localhost if DNS issues

COMPONENT INTEGRATION:
- Check message bus for URL load events
- Verify ReactiveUI bindings in parent application
- Ensure proper ViewModel registration

FUTURE WEBVIEW:
- Add WebViewControl-Avalonia package for full rendering
- Configure CEF initialization if needed
- Check for platform-specific WebView issues

================================================================================
🚀 Simple, clean, and ready for your web integration needs!
Perfect for the "just show me the URL" approach without complexity.
Add WebView rendering when you're ready for full browser embedding.
================================================================================