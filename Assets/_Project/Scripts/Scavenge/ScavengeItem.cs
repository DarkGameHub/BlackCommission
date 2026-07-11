using UnityEngine;
using BlackCommission.Scavenge;

/// <summary>
/// A scavengeable world item: a <see cref="Carriable"/> (picked up / carried / dropped via
/// the existing <see cref="CarrySystem"/>, exactly like the eco column) plus
/// <see cref="IInteractable"/> so the standard E-key crosshair interaction picks it up.
/// It carries its own identity/economy data — item id, weight class, category, and a HIDDEN
/// base value the host reads only at settlement (never shown mid-mission). Heavy items use
/// two-hand carry (<c>Carriable.isHeavy</c> baked on the prefab); light/medium are single-hand.
/// Weight class (van capacity units) is intentionally separate from isHeavy (carry style).
/// Spawned at runtime by the loot spawner (the carriable pattern the former eco column used).
/// </summary>
public class ScavengeItem : Carriable, IInteractable
{
    [SerializeField] string itemId;
    [SerializeField] WeightClass weightClass = WeightClass.Light;
    [SerializeField] ScavengeCategory category;

    [Tooltip("Hidden base value (G). Read host-side at settlement; never shown during the mission.")]
    [SerializeField] int baseValue;

    [SerializeField] ScavengeTier tier = ScavengeTier.Salvage;
    [SerializeField] MaterialClass materialClass = MaterialClass.Domestic;

    public string ItemId => itemId;
    public WeightClass Weight => weightClass;
    public ScavengeCategory Category => category;
    public int BaseValue => baseValue;
    public ScavengeTier Tier => tier;
    public MaterialClass MaterialClass => materialClass;
    /// <summary>False for information-only objects such as a dispatch work order.</summary>
    public virtual bool CanEnterCargo => true;

    public string InteractHint => CanBeCarried
        ? (IsHeavy ? MvpLocale.Pick("LIFT (HEAVY · TWO HANDS)", "扛起（重物·双手）") : MvpLocale.Pick("PICK UP", "拾取"))
        : "";

    public void OnInteractStart(PlayerController player)
    {
        // Carrying is owner/server-RPC driven, so this needs a hosted session
        // (the offline PreviewWalker passes null and is ignored).
        if (player == null) return;
        if (player.TryGetComponent<CarrySystem>(out var carry))
            carry.TryPickUp(this);
    }

    public void OnInteractEnd(PlayerController player) { }

    /// <summary>
    /// Host-side: stamp the run-specific identity/economy data onto a freshly spawned
    /// instance. The prefab only bakes the weight-class visual + carry style; which actual
    /// item this is (and its hidden value) is decided per spawn by the loot spawner.
    /// </summary>
    public void ConfigureServer(string id, WeightClass weight, int value, ScavengeCategory cat,
        ScavengeTier itemTier = ScavengeTier.Salvage, MaterialClass itemClass = MaterialClass.Domestic)
    {
        itemId = id;
        weightClass = weight;
        baseValue = value;
        category = cat;
        tier = itemTier;
        materialClass = itemClass;
    }
}
