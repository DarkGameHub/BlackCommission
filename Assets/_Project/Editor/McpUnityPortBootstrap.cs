#if UNITY_EDITOR
using UnityEditor;
using McpUnity.Unity;

namespace BlackCommission.EditorTools
{
    [InitializeOnLoad]
    internal static class McpUnityPortBootstrap
    {
        private const int Port = 8091;
        private const int RequestTimeoutSeconds = 30;

        static McpUnityPortBootstrap()
        {
            EditorApplication.delayCall += Apply;
        }

        private static void Apply()
        {
            var settings = McpUnitySettings.Instance;
            var changed = false;

            if (settings.Port != Port)
            {
                settings.Port = Port;
                changed = true;
            }

            if (settings.AllowRemoteConnections)
            {
                settings.AllowRemoteConnections = false;
                changed = true;
            }

            if (settings.RequestTimeoutSeconds != RequestTimeoutSeconds)
            {
                settings.RequestTimeoutSeconds = RequestTimeoutSeconds;
                changed = true;
            }

            if (changed)
            {
                settings.SaveSettings();
            }

            var server = McpUnityServer.Instance;
            server?.StopServer();
            server?.StartServer();
        }
    }
}
#endif
