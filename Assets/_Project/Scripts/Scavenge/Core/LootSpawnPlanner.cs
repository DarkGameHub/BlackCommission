using System.Collections.Generic;

namespace BlackCommission.Scavenge
{
    /// <summary>
    /// Deterministic, seed-driven loot placement planner. Given the loot anchors a map
    /// instance exposes (placed by RoomDresser) plus an item pool, it decides which
    /// anchors get which item for this run. Same seed + inputs ⇒ identical plan, so the
    /// host can replay it and the structure varies per run (quick-spec
    /// scavenging-item-system-2026-06-16, knob <c>itemsPerMapInstance</c> 10–14). Pure
    /// logic: no Unity, no scene access.
    /// </summary>
    public static class LootSpawnPlanner
    {
        /// <summary>
        /// Plan loot for one run. Picks a seed-stable target count in
        /// [<paramref name="minItems"/>, <paramref name="maxItems"/>] (clamped to the
        /// anchor count), then assigns each chosen anchor an item whose allowed surfaces
        /// include that anchor's surface. Anchors with no matching item are skipped, so
        /// the returned count may be below target. No anchor is used twice. Returns an
        /// empty list for null/empty inputs. The result is independent of the caller's
        /// enumeration order (anchors are sorted by Id, candidates by item Id).
        /// </summary>
        public static List<LootPlacement> Plan(
            int seed,
            IReadOnlyList<LootAnchorSlot> anchors,
            IReadOnlyList<ScavengeItemSpec> pool,
            int minItems,
            int maxItems)
        {
            var result = new List<LootPlacement>();
            if (anchors == null || pool == null || anchors.Count == 0 || pool.Count == 0)
                return result;

            if (minItems < 0) minItems = 0;
            if (maxItems < minItems) maxItems = minItems;

            // Stable ordering first so the plan depends only on (seed, inputs).
            var ordered = new List<LootAnchorSlot>(anchors);
            ordered.Sort((a, b) => a.Id.CompareTo(b.Id));

            var rng = new System.Random(seed);

            int target = rng.Next(minItems, maxItems + 1);
            if (target > ordered.Count) target = ordered.Count;

            // Fisher–Yates shuffle of the anchor order using the same rng.
            for (int i = ordered.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (ordered[i], ordered[j]) = (ordered[j], ordered[i]);
            }

            var candidates = new List<ScavengeItemSpec>();
            for (int i = 0; i < ordered.Count && result.Count < target; i++)
            {
                var anchor = ordered[i];

                candidates.Clear();
                for (int k = 0; k < pool.Count; k++)
                    if (pool[k].AllowsSurface(anchor.Surface))
                        candidates.Add(pool[k]);

                if (candidates.Count == 0) continue; // nothing fits this surface — leave it empty

                candidates.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                var item = candidates[rng.Next(candidates.Count)];
                result.Add(new LootPlacement(anchor.Id, item.Id));
            }

            return result;
        }
    }
}
