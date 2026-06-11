/// <summary>
/// How a mission settled. Drives reward math (MissionRewardCalculator) and company
/// progression deltas (CompanyState.ApplyMissionResult). Serialized over the network
/// as an int (see LostItemMissionManager.SetPendingRewardClientRpc) — keep member
/// order stable.
/// </summary>
public enum MvpMissionResultKind
{
    Success,
    Partial,
    Failed
}
