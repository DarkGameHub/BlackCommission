using System.Collections.Generic;
using UnityEngine;

namespace BlackCommission.Level
{
    /// <summary>
    /// Door-fill clearance: after a room module is dropped into a slot, slide aside only the
    /// wall-hugging props that actually sit across one of the slab's real doors. Everything else
    /// stays exactly where it was authored — no whole-room rotation, no emptied walls.
    ///
    /// The room is instantiated unrotated at the slot anchor (the anchor carries identity rotation),
    /// so the room's local axes equal the plan axes: local +x = east, local +z = north. That lets a
    /// prop's authored <see cref="RoomPropPlacement.along"/> compare directly with the plan-derived
    /// door offsets from <see cref="TowerPlanV8.DoorOpeningsForSlab"/>.
    /// </summary>
    public static class RoomDoorClearance
    {
        // Keep a prop just off the wall corner when sliding. Smaller than the builder's 0.3 m authoring
        // inset so a nudged prop can reach closer to the corner and still clear a wide central door.
        const float EdgeInset = 0.1f;

        public static void Clear(GameObject room, RoomSlot slot)
        {
            if (room == null || slot == null) return;

            List<SlabDoorOpening> openings = TowerPlanV8.DoorOpeningsForSlab(slot.slotId);
            if (openings.Count == 0) return;

            var props = room.GetComponentsInChildren<RoomPropPlacement>(includeInactive: false);
            if (props.Length == 0) return;

            Vector2 fp = Footprint(slot.size);

            foreach (RoomPropPlacement p in props)
            {
                if (p.wall == DoorEdge.None) continue;

                bool nsWall = p.wall == DoorEdge.N || p.wall == DoorEdge.S;
                float wallLen = nsWall ? fp.x : fp.y;      // length of the running axis
                float propHalf = HalfExtentAlongWall(p.gameObject, nsWall);
                float maxOffset = wallLen * 0.5f - EdgeInset - propHalf;

                // Forbidden bands on this wall: each door opening on it, widened by the prop's own
                // half-extent and the clearance margin so the prop's CENTRE must stay outside.
                var forbidden = new List<(float lo, float hi)>();
                bool blocked = false;
                foreach (SlabDoorOpening o in openings)
                {
                    if (o.Edge != p.wall) continue;
                    float band = o.HalfWidth + propHalf + DoorClearance.Clearance;
                    forbidden.Add((o.Offset - band, o.Offset + band));
                    if (Mathf.Abs(p.along - o.Offset) < band) blocked = true;
                }
                if (!blocked) continue;

                // Other props on the same wall keep their spots → treat them as occupied bands so the
                // slid prop doesn't land on top of one.
                foreach (RoomPropPlacement q in props)
                {
                    if (q == p || q.wall != p.wall) continue;
                    float qHalf = HalfExtentAlongWall(q.gameObject, nsWall);
                    float margin = qHalf + propHalf;
                    forbidden.Add((q.along - margin, q.along + margin));
                }

                float newAlong = DoorClearance.NearestFree(p.along, maxOffset, forbidden);
                if (float.IsNaN(newAlong))
                {
                    Debug.LogWarning($"[RoomDoorClearance] {room.name}: prop '{p.name}' blocks the " +
                        $"{p.wall} door and can't slide clear (wall too short) — disabled.");
                    p.gameObject.SetActive(false);
                    continue;
                }

                float delta = newAlong - p.along;
                // Move along the wall's running axis in room-local space (room is unrotated).
                p.transform.localPosition += nsWall ? new Vector3(delta, 0f, 0f)
                                                    : new Vector3(0f, 0f, delta);
                p.along = newAlong;
            }
        }

        // Combined world-space half-extent of the prop's renderers along the wall's running axis.
        // Identity room rotation means world x/z equal local x/z.
        static float HalfExtentAlongWall(GameObject go, bool nsWall)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return 0.15f;
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return nsWall ? b.extents.x : b.extents.z;
        }

        // Footprint per size class — mirrors TowerRoomPoolBuilder.Footprint (S 4×4 / M 8×8 / L 12×8).
        static Vector2 Footprint(RoomSizeClass s) => s switch
        {
            RoomSizeClass.Small  => new Vector2(4f, 4f),
            RoomSizeClass.Medium => new Vector2(8f, 8f),
            _                    => new Vector2(12f, 8f),
        };
    }
}
