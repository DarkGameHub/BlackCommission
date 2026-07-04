using UnityEngine;

namespace BlackCommission.Monsters
{
    /// <summary>
    /// Tuning knobs for the site danger track (danger-infection quick-spec 2026-06-18 §Tuning),
    /// authored at <c>Assets/Resources/Config/DangerConfig.asset</c> so the runtime can
    /// <c>Resources.Load</c> it. Missing asset = spec defaults (8/10/10 min). Feeds
    /// <see cref="DangerClock"/> via <see cref="CreateClock"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Black Commission/Danger Config", fileName = "DangerConfig")]
    public class DangerConfig : ScriptableObject
    {
        [Tooltip("Survey phase length in minutes (first safe exploration window; spec 8, range 5–12).")]
        public float surveyMinutes = 8f;
        [Tooltip("Active phase length in minutes (middle tension; spec 10, range 6–15).")]
        public float activeMinutes = 10f;
        [Tooltip("Pursuit phase length in minutes (high pressure; spec 10, range 5–15). Saturation follows.")]
        public float pursuitMinutes = 10f;

        /// <summary>Build a danger clock wired to these knobs.</summary>
        public DangerClock CreateClock()
            => new DangerClock(surveyMinutes * 60f, activeMinutes * 60f, pursuitMinutes * 60f);

        /// <summary>Load the authored asset, or a spec-default clock when none exists.</summary>
        public static DangerClock LoadClockOrDefaults()
        {
            var config = Resources.Load<DangerConfig>("Config/DangerConfig");
            return config != null ? config.CreateClock() : new DangerClock();
        }
    }
}
