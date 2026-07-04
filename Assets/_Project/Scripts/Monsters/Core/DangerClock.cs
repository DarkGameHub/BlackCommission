namespace BlackCommission.Monsters
{
    /// <summary>Site danger escalation phase (danger-infection quick-spec 2026-06-18).</summary>
    public enum DangerPhase
    {
        Survey = 0,
        Active = 1,
        Pursuit = 2,
        Saturation = 3,
    }

    /// <summary>
    /// Pure math for the site danger track (design authority:
    /// <c>design/quick-specs/danger-infection-system-2026-06-18.md</c> + <c>monster-system.md</c>
    /// Formula 1). <c>danger_level</c> is a continuous 0→100 value driven ONLY by elapsed time —
    /// no player-action spikes (PM locked 2026-06-18) — and the four phases are discrete markers
    /// on that curve gating spawn activation. Re-entering the site advances the starting phase
    /// by one per trip (multi-trip escalation). Never shown as a number to players.
    ///
    /// <para>Engine-free (asmdef <c>noEngineReferences</c>) so the EditMode test assembly can
    /// exercise it directly; <c>MonsterSpawnBootstrap</c> feeds elapsed time in.</para>
    /// </summary>
    public sealed class DangerClock
    {
        readonly float surveySeconds;
        readonly float activeSeconds;
        readonly float pursuitSeconds;

        /// <summary>Spec defaults: Survey 8 min / Active 10 min / Pursuit 10 min → saturation at 28 min.</summary>
        public DangerClock(float surveySeconds = 480f, float activeSeconds = 600f, float pursuitSeconds = 600f)
        {
            this.surveySeconds = surveySeconds < 0f ? 0f : surveySeconds;
            this.activeSeconds = activeSeconds < 0f ? 0f : activeSeconds;
            this.pursuitSeconds = pursuitSeconds < 0f ? 0f : pursuitSeconds;
        }

        /// <summary>Seconds of elapsed time at which the site saturates (forced-evac trigger).</summary>
        public float SaturationSeconds => surveySeconds + activeSeconds + pursuitSeconds;

        /// <summary>Elapsed-time head start a re-entry trip carries (trip 0 = first entry).</summary>
        public float TripOffsetSeconds(int tripIndex)
        {
            if (tripIndex <= 0) return 0f;
            if (tripIndex == 1) return surveySeconds;             // second trip enters at Active
            return surveySeconds + activeSeconds;                  // third+ trip enters at Pursuit
        }

        /// <summary>Continuous 0→100 danger level (monster-system Formula 1: min(SAT, base_rate·t)).</summary>
        public float DangerLevelAt(float elapsedSeconds, int tripIndex = 0)
        {
            float sat = SaturationSeconds;
            if (sat <= 0f) return 100f;
            float t = (elapsedSeconds < 0f ? 0f : elapsedSeconds) + TripOffsetSeconds(tripIndex);
            float level = 100f * t / sat;
            return level > 100f ? 100f : level;
        }

        /// <summary>Discrete phase marker for an elapsed time (0/8/18/28 min defaults).</summary>
        public DangerPhase PhaseAt(float elapsedSeconds, int tripIndex = 0)
        {
            float t = (elapsedSeconds < 0f ? 0f : elapsedSeconds) + TripOffsetSeconds(tripIndex);
            if (t < surveySeconds) return DangerPhase.Survey;
            if (t < surveySeconds + activeSeconds) return DangerPhase.Active;
            if (t < SaturationSeconds) return DangerPhase.Pursuit;
            return DangerPhase.Saturation;
        }

        /// <summary>
        /// How many of a map's authored monster seeds are live in a phase. Spec mapping:
        /// Survey = fixed sparse presence (⌈N/3⌉, min 1); Active = "new spawns activate"
        /// (⌈2N/3⌉); Pursuit/Saturation = "multiple spawn points live" (all N).
        /// </summary>
        public static int ActiveSeedCount(int totalSeeds, DangerPhase phase)
        {
            if (totalSeeds <= 0) return 0;
            switch (phase)
            {
                case DangerPhase.Survey: return (totalSeeds + 2) / 3;
                case DangerPhase.Active: return (totalSeeds * 2 + 2) / 3;
                default: return totalSeeds;
            }
        }
    }
}
