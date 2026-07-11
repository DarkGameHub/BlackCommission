using Unity.Netcode;
using UnityEngine;

/// <summary>Central runtime placement for the HQ printer; scene-authored printers win.</summary>
public static class WorkOrderPrinterSpawner
{
    const string PrinterPrefabPath = "WorkOrder/WorkOrderPrinter";

    public static bool EnsureHqPrinter(OfficeComputer computer)
    {
        if (!CanSpawn() || computer == null) return false;
        // The OfficeComputer transform is the CRT screen centre and +forward points toward
        // the player/chair. Move into that interior half-space first, then along the wall.
        // The old right-only offset crossed the exterior wall and placed the printer beside
        // the van, making the instruction impossible to follow.
        Vector3 candidate = computer.transform.position
            - computer.transform.forward * 0.68f
            - computer.transform.right * 1.45f;
        Vector3 position = Ground(candidate, computer.transform.position.y - 1f) + Vector3.up * 0.01f;

        WorkOrderPrinter existing = WorkOrderPrinter.HqPrinter;
        if (existing != null)
        {
            // Older placement used the first downward ray hit, which could be the ceiling.
            // Replace an already-spawned bad instance so the self-heal also fixes a live session.
            bool misplaced = Mathf.Abs(existing.transform.position.y - position.y) > 0.25f ||
                              Vector3.Distance(existing.transform.position, position) > 0.40f;
            if (!misplaced) return true;

            NetworkObject oldNetworkObject = existing.GetComponent<NetworkObject>();
            Debug.LogWarning($"[WorkOrderPrinterSpawner] Replacing misplaced HQ printer at " +
                             $"{existing.transform.position}; expected {position}.");
            if (oldNetworkObject != null && oldNetworkObject.IsSpawned)
                oldNetworkObject.Despawn(true);
            else
                Object.Destroy(existing.gameObject);
        }

        return Spawn(position, computer.transform.rotation, false);
    }

    static bool CanSpawn()
    {
        NetworkManager nm = NetworkManager.Singleton;
        return nm != null && nm.IsListening && nm.IsServer;
    }

    static bool Spawn(Vector3 position, Quaternion rotation, bool reprint)
    {
        GameObject prefab = Resources.Load<GameObject>(PrinterPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[WorkOrderPrinterSpawner] Missing WorkOrderPrinter prefab; run the Work Order asset builder.");
            return false;
        }
        GameObject go = Object.Instantiate(prefab, position, rotation);
        WorkOrderPrinter printer = go.GetComponent<WorkOrderPrinter>();
        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        if (printer == null || networkObject == null)
        {
            Debug.LogError("[WorkOrderPrinterSpawner] Printer prefab is missing WorkOrderPrinter or NetworkObject.");
            Object.Destroy(go);
            return false;
        }
        printer.ConfigureServer(reprint);
        networkObject.Spawn(true);
        Debug.Log($"[WorkOrderPrinterSpawner] Spawned {(reprint ? "mission" : "HQ")} printer at {position}.");
        return true;
    }

    static Vector3 Ground(Vector3 position, float fallbackY)
    {
        Vector3 origin = position + Vector3.up * 4f;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore);
        bool found = false;
        RaycastHit best = default;
        float bestDelta = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            // Ignore walls/undersides and choose the horizontal surface closest to the authored
            // floor height. A single Raycast selected the ceiling because it was encountered first.
            if (hit.normal.y < 0.55f) continue;
            float delta = Mathf.Abs(hit.point.y - fallbackY);
            if (delta > 0.75f || delta >= bestDelta) continue;
            best = hit;
            bestDelta = delta;
            found = true;
        }

        return found ? best.point : new Vector3(position.x, fallbackY, position.z);
    }
}
