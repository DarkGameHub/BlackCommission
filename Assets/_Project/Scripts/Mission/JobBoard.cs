using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class JobBoard : MonoBehaviour, IInteractable
{
    public string InteractHint => "查看工单 [E]";

    float _msgTimer;

    public void OnInteractStart(PlayerController player)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening
            || !NetworkManager.Singleton.IsHost)
        {
            _msgTimer = 3f;
            return;
        }
        NetworkManager.Singleton.SceneManager.LoadScene("Mall_B2", LoadSceneMode.Single);
    }

    public void OnInteractEnd(PlayerController player) { }

    void OnGUI()
    {
        if (_msgTimer <= 0) return;
        _msgTimer -= Time.deltaTime;

        var style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.yellow }
        };
        float w = 360, h = 40;
        GUI.Box(new Rect((Screen.width - w) / 2f, Screen.height * 0.6f, w, h),
            "请先开始联机 (点 Start Host)", style);
    }
}
