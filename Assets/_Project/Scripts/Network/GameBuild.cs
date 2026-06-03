using System.Text;

/// <summary>
/// Build identity used to gate connections so mismatched builds can't join and desync.
/// Bump Version on any change that breaks networked compatibility.
/// </summary>
public static class GameBuild
{
    public const string Version = "0.1-mvp";

    public static byte[] VersionPayload => Encoding.UTF8.GetBytes(Version);

    public static string ReadVersion(byte[] payload)
    {
        if (payload == null || payload.Length == 0) return "";
        return Encoding.UTF8.GetString(payload);
    }
}
