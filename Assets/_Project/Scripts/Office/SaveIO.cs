using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Tiny JSON-file persistence helper. All game-progression saves live as files under
/// persistentDataPath/save/ (Steam Cloud-friendly), instead of PlayerPrefs. Device-local
/// settings (volume, sensitivity, name, voice) intentionally stay in PlayerPrefs.
/// </summary>
public static class SaveIO
{
    public static string SaveDir => Path.Combine(Application.persistentDataPath, "save");

    public static T ReadJson<T>(string fileName) where T : class
    {
        try
        {
            string path = Path.Combine(SaveDir, fileName);
            if (File.Exists(path))
                return JsonUtility.FromJson<T>(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveIO] read '{fileName}' failed: {e.Message}");
        }
        return null;
    }

    public static void WriteJson(string fileName, object data)
    {
        try
        {
            Directory.CreateDirectory(SaveDir);
            File.WriteAllText(Path.Combine(SaveDir, fileName), JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveIO] write '{fileName}' failed: {e.Message}");
        }
    }
}
