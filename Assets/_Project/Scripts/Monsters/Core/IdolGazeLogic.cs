namespace BlackCommission.Monsters
{
    /// <summary>
    /// Pure-math core of the Civic Idol's freeze-when-watched rule (市政圣像).
    /// Design: <c>design/gdd/monster-civic-idol.md</c>.
    ///
    /// <para>The archetype (a statue that only moves while unobserved) is a public-domain
    /// genre pattern; rules and numbers here are Black Commission's own. Camera pitch is
    /// owner-local and never synced, so the server judges "watched" purely on the
    /// horizontal plane from body yaw — deliberately generous to players (facing the idol
    /// while looking at the floor still counts as watching). Errs frozen = errs safe.</para>
    ///
    /// <para>Engine-free (asmdef <c>noEngineReferences</c>) so the EditMode test assembly
    /// can exercise it directly; the <c>CivicIdol</c> brain feeds transform values in and
    /// layers the physics line-of-sight check on top.</para>
    /// </summary>
    public static class IdolGazeLogic
    {
        /// <summary>
        /// Horizontal-plane view-cone test: is the target within <paramref name="maxRange"/>
        /// of the eye AND within <paramref name="halfAngleDeg"/> of the flattened forward
        /// direction? A degenerate forward (near-zero after flattening) never watches;
        /// a coincident eye/target always does (you are standing inside it).
        /// </summary>
        public static bool IsWithinViewCone(
            float eyeX, float eyeZ, float forwardX, float forwardZ,
            float targetX, float targetZ, float maxRange, float halfAngleDeg)
        {
            float dx = targetX - eyeX;
            float dz = targetZ - eyeZ;
            float distSq = dx * dx + dz * dz;
            if (maxRange <= 0f || distSq > maxRange * maxRange) return false;

            float forwardLen = (float)System.Math.Sqrt(forwardX * forwardX + forwardZ * forwardZ);
            if (forwardLen < 1e-4f) return false;
            if (distSq < 1e-6f) return true;

            float dist = (float)System.Math.Sqrt(distSq);
            float cosAngle = (forwardX * dx + forwardZ * dz) / (forwardLen * dist);
            float cosHalf = (float)System.Math.Cos(halfAngleDeg * System.Math.PI / 180.0);
            return cosAngle >= cosHalf;
        }

        /// <summary>
        /// Freeze hysteresis: frozen instantly while watched; stays frozen for
        /// <paramref name="unfreezeGrace"/> seconds after the last watcher looked away,
        /// so line-of-sight raycast flicker (door frames, shelf edges) cannot let it
        /// inch forward under a steady gaze.
        /// </summary>
        public static bool ShouldFreeze(bool watchedNow, float lastWatchedTime, float now, float unfreezeGrace)
            => watchedNow || now - lastWatchedTime < unfreezeGrace;
    }
}
