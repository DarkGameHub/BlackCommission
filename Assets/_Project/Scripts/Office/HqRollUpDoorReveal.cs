using UnityEngine;

/// <summary>
/// Animates the HQ "Option A" roll-up departure door — the axial reveal beat of the locked
/// floor plan (the team stares at the closed bay door all prep, then it rolls up to reveal the
/// dispatch van + fogged city before they board). Lives on a clean identity-scaled wrapper so
/// its sealing <see cref="BoxCollider"/> and the door-panel mesh roll up together.
///
/// Cosmetic juice, NOT a gameplay gate (boarding is gated separately on
/// <see cref="OfficeComputer.HasSelectedDemoTask"/>) and NOT networked — each peer simulates
/// locally, same precedent as <see cref="SimpleDoor"/>. The panel translates straight up by its
/// own height; from the interior reveal viewpoint the roof occludes the raised panel, so it
/// reads as rolling away into the header.
///
/// Trigger modes (set by HqOptionAProductionBuilder via <see cref="Configure"/>):
///   • Manual           — [E] interactable: aim at the door, press E to roll it up / down. (HQ default, PM 2026-06-23)
///   • CommissionLocked — opens when a commission is locked at the office computer, closes when it clears.
///   • OnDepart         — same auto signal, kept distinct for intent/readability.
/// </summary>
public class HqRollUpDoorReveal : MonoBehaviour, IInteractable
{
    public enum Trigger { Manual, CommissionLocked, OnDepart }

    [SerializeField] Trigger trigger = Trigger.Manual;
    [SerializeField] float openHeight = 3.2f;   // slide distance == door opening height
    [SerializeField] float openSeconds = 1.6f;

    Vector3 closedPos;
    Vector3 openPos;
    float t;                 // 0 = closed, 1 = open
    bool targetOpen;
    bool animating;
    OfficeComputer computer;

    /// <summary>Called by the builder right after attach to set mode + measured geometry.</summary>
    public void Configure(Trigger mode, float height, float seconds)
    {
        trigger = mode;
        openHeight = height;
        openSeconds = Mathf.Max(0.1f, seconds);
    }

    void Awake()
    {
        // The wrapper's parent (Shell) is identity, so localPosition lives in world space and
        // Vector3.up is a true vertical slide regardless of the door's own yaw.
        closedPos = transform.localPosition;
        openPos = closedPos + Vector3.up * openHeight;
    }

    void Update()
    {
        if (trigger != Trigger.Manual)
        {
            OfficeComputer c = GetComputer();
            bool want = c != null && c.HasSelectedDemoTask;
            if (want != targetOpen) Set(want);
        }

        if (!animating) return;
        t = Mathf.Clamp01(t + (targetOpen ? 1f : -1f) * Time.deltaTime / openSeconds);
        float ease = t * t * (3f - 2f * t); // smoothstep
        transform.localPosition = Vector3.Lerp(closedPos, openPos, ease);
        if (t <= 0f || t >= 1f) animating = false;
    }

    void Set(bool open)
    {
        targetOpen = open;
        animating = true;
        // Excel-aligned door sounds (audio pass v2) — the synth creak is retired here.
        if (open) AudioManager.Instance?.PlayDoorOpen(transform.position);
        else AudioManager.Instance?.PlayDoorClose(transform.position);
    }

    OfficeComputer GetComputer()
        => computer != null ? computer : (computer = Object.FindAnyObjectByType<OfficeComputer>());

    // ── Manual mode (IInteractable). An empty hint in auto modes makes PlayerInteraction skip it. ──
    public string InteractHint =>
        trigger != Trigger.Manual || animating ? string.Empty
        : targetOpen ? "[E] 关闭卷帘门" : "[E] 升起卷帘门";

    public void OnInteractStart(PlayerController player)
    {
        if (trigger == Trigger.Manual && !animating)
            Set(!targetOpen);
    }

    public void OnInteractEnd(PlayerController player) { }
}
