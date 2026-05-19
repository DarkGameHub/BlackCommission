using UnityEngine;

public class WaterVisual : MonoBehaviour
{
    void Update()
    {
        if (WaterLevelManager.Instance == null) return;
        var pos = transform.position;
        pos.y = WaterLevelManager.Instance.CurrentWaterHeight.Value;
        transform.position = pos;
    }
}
