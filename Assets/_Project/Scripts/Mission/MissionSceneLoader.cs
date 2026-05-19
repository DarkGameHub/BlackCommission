using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionSceneLoader : MonoBehaviour
{
    public static void LoadMallScene()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[MissionSceneLoader] LoadMallScene called on non-server.");
            return;
        }
        NetworkManager.Singleton.SceneManager.LoadScene("Mall_B2", LoadSceneMode.Single);
    }

    void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoadComplete;
    }

    void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
    }

    void OnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (sceneName != "Mall_B2") return;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        var gm = GameManager.Instance;
        if (gm != null && gm.IsSpawned)
            Debug.Log("[MissionSceneLoader] Mall_B2 loaded, GameManager ready.");
        else
            Debug.Log("[MissionSceneLoader] Mall_B2 loaded. GameManager will initialize on spawn.");
    }
}
