using UnityEngine;

/// <summary>
/// The 「真实海岸」生态柱 — a heavy Carriable whose hard landings cost mission
/// completeness. Forwards the base class's drop-damage hook to the
/// TowerMissionManager (which only listens on the mission authority).
/// Also an IInteractable so the standard E-key interaction (crosshair hint via
/// PlayerInteraction) picks it up — players should never need to know that
/// carriables historically used a separate F-key path.
/// </summary>
public class EcoColumnCarriable : Carriable, IInteractable
{
    /// <summary>Raised on every hard landing (impact at/over dropDamageThreshold).</summary>
    public event System.Action<float> HardImpact;

    public string InteractHint => CanBeCarried ? "扛起生态柱（重物，双手）" : "";

    public void OnInteractStart(PlayerController player)
    {
        // Offline preview (PreviewWalker passes null): carrying is owner/server RPC
        // driven, so the column genuinely needs a hosted session.
        if (player == null) return;
        if (player.TryGetComponent<CarrySystem>(out var carry))
            carry.TryPickUp(this);
    }

    public void OnInteractEnd(PlayerController player) { }

    protected override void OnDropDamage(float impactForce)
    {
        base.OnDropDamage(impactForce);
        // Glass thud on every peer that simulates the landing — the sound IS the
        // completeness-loss feedback.
        AudioManager.Instance?.PlayHeavyImpact(transform.position);
        HardImpact?.Invoke(impactForce);
    }
}
