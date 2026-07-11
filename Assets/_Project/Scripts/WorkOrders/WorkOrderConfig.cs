using UnityEngine;

[CreateAssetMenu(menuName = "Black Commission/Work Order Config")]
public class WorkOrderConfig : ScriptableObject
{
    [Range(2f, 6f)] public float printDuration = 3.5f;
    [Range(0, 50)] public int reprintCost = 5;
    [Range(1f, 3f)] public float tearInteractRange = 2f;
}
