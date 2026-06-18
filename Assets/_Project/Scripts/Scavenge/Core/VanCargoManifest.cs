using System.Collections.Generic;

namespace BlackCommission.Scavenge
{
    /// <summary>
    /// Host-authoritative record of what has actually been loaded into the van: a
    /// <see cref="VanWeightLedger"/> (the shared capacity gate, quick-spec §3) plus the ordered
    /// list of <see cref="SettlementItem"/>s that passed it. This is the pure-logic bridge
    /// between the on-site deposit gate (<c>ScavengeCargoZone</c>, the networked layer) and the
    /// settlement reveal (<see cref="ScavengeSettlementCalculator"/>): the cargo zone calls
    /// <see cref="TryLoad"/> when a player sets an item down in the bay, and settlement reads
    /// <see cref="Items"/>. No Unity, no networking, so the load rule is unit-testable.
    ///
    /// Items are loaded by weight class but settle by base value/condition, so each loaded
    /// item is stored with the weight it consumed — that keeps the ledger and the manifest in
    /// lock-step even when an item is taken back off (<see cref="TryUnload"/>).
    /// </summary>
    public sealed class VanCargoManifest
    {
        readonly VanWeightLedger ledger;
        readonly List<SettlementItem> items = new();
        readonly List<WeightClass> weights = new();

        /// <summary>Builds a manifest with a fresh ledger of <paramref name="capacity"/> units.</summary>
        public VanCargoManifest(int capacity)
        {
            ledger = new VanWeightLedger(capacity);
        }

        /// <summary>Shared capacity in weight units.</summary>
        public int Capacity => ledger.Capacity;

        /// <summary>Units currently loaded (sum of every loaded item's weight cost).</summary>
        public int LoadUnits => ledger.Load;

        /// <summary>Units still available.</summary>
        public int Remaining => ledger.Remaining;

        /// <summary>True when no more weight can be loaded.</summary>
        public bool IsFull => ledger.IsFull;

        /// <summary>Number of items loaded.</summary>
        public int Count => items.Count;

        /// <summary>The loaded items, in load order (settlement input).</summary>
        public IReadOnlyList<SettlementItem> Items => items;

        /// <summary>True if an item of <paramref name="weight"/> would still fit.</summary>
        public bool CanLoad(WeightClass weight) => ledger.CanFit(weight);

        /// <summary>
        /// Loads <paramref name="item"/> if its <paramref name="weight"/> fits the remaining
        /// capacity; records it for settlement. Returns whether the load happened — a full (or
        /// too-tight) van rejects the item and leaves both ledger and manifest unchanged.
        /// </summary>
        public bool TryLoad(SettlementItem item, WeightClass weight)
        {
            if (!ledger.TryAdd(weight)) return false;
            items.Add(item);
            weights.Add(weight);
            return true;
        }

        /// <summary>
        /// Removes the loaded item at <paramref name="index"/> (e.g. a player takes it back off
        /// the van), freeing its weight. Returns whether anything was removed.
        /// </summary>
        public bool TryUnload(int index)
        {
            if (index < 0 || index >= items.Count) return false;
            ledger.Remove(weights[index]);
            items.RemoveAt(index);
            weights.RemoveAt(index);
            return true;
        }

        /// <summary>Empties the van for a new run.</summary>
        public void Reset()
        {
            ledger.Reset();
            items.Clear();
            weights.Clear();
        }

        /// <summary>A defensive copy of the loaded items for the settlement calculator.</summary>
        public List<SettlementItem> ToSettlementItems() => new List<SettlementItem>(items);
    }
}
