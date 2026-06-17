namespace BlackCommission.Scavenge
{
    /// <summary>
    /// Host-authoritative tally of the van's shared cargo weight. Pure logic — no Unity,
    /// no networking — so the capacity rule (the scavenging loop's primary tension lever,
    /// quick-spec §3) is unit-testable. The networked van layer owns one of these on the
    /// host and replicates <see cref="Load"/> to clients for the ticket-strip display.
    /// </summary>
    public sealed class VanWeightLedger
    {
        /// <summary>Total shared capacity in weight units (spec default 12).</summary>
        public int Capacity { get; private set; }

        /// <summary>Units currently loaded. Never below 0, never above <see cref="Capacity"/>.</summary>
        public int Load { get; private set; }

        /// <summary>Units still available.</summary>
        public int Remaining => Capacity - Load;

        /// <summary>True when no more weight can be loaded.</summary>
        public bool IsFull => Remaining <= 0;

        /// <param name="capacity">Shared capacity in units; negative values clamp to 0.</param>
        public VanWeightLedger(int capacity)
        {
            Capacity = capacity < 0 ? 0 : capacity;
        }

        /// <summary>True if <paramref name="units"/> (must be positive) would fit without exceeding capacity.</summary>
        public bool CanFit(int units) => units > 0 && Load + units <= Capacity;

        /// <summary>True if an item of <paramref name="weight"/> would fit.</summary>
        public bool CanFit(WeightClass weight) => CanFit(weight.Units());

        /// <summary>Loads <paramref name="units"/> if they fit; returns whether the load happened.</summary>
        public bool TryAdd(int units)
        {
            if (!CanFit(units)) return false;
            Load += units;
            return true;
        }

        /// <summary>Loads an item of <paramref name="weight"/> if it fits; returns whether the load happened.</summary>
        public bool TryAdd(WeightClass weight) => TryAdd(weight.Units());

        /// <summary>Removes up to <paramref name="units"/> (e.g. an item taken back off the van). Clamps at 0; ignores non-positive input.</summary>
        public void Remove(int units)
        {
            if (units <= 0) return;
            Load -= units;
            if (Load < 0) Load = 0;
        }

        /// <summary>Removes an item of <paramref name="weight"/> from the tally.</summary>
        public void Remove(WeightClass weight) => Remove(weight.Units());

        /// <summary>Empties the van (new run / full unload at settlement).</summary>
        public void Reset() => Load = 0;
    }
}
