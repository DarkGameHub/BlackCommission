using MCPForUnity.Editor;
using MCPForUnity.Editor.Services.Transport.Transports;
using UnityEditor;

/// <summary>
/// Auto-starts the "MCP For Unity" (CoplayDev) stdio bridge session whenever the editor
/// loads, so Claude Code can connect without anyone clicking "Start Session" in the
/// MCP For Unity window. The package's own <c>StdioBridgeReloadHandler</c> only resumes
/// a session that was already running; this covers the initial cold start.
///
/// Bridge listens on port 6400 (auto-fallback) and writes a status file to
/// <c>~/.unity-mcp/</c> that the uvx MCP server uses to discover this Unity instance.
/// </summary>
[InitializeOnLoad]
public static class McpBridgeAutoStart
{
    static McpBridgeAutoStart()
    {
        // Static ctor runs during domain reload when editor services may not be ready;
        // delayCall defers to the first idle editor update. IsRunning guard avoids
        // fighting the package's own resume handler after script recompiles.
        EditorApplication.delayCall += () =>
        {
            if (!StdioBridgeHost.IsRunning)
            {
                McpCiBoot.StartStdioForCi();
            }
        };
    }
}
