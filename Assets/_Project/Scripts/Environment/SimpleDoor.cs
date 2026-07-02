using System.Collections;
using UnityEngine;

/// <summary>
/// Placeholder interactive door — E key toggles open/close, swings 90° around hinge.
/// Replaces the rubble-plug blocker on Toggle connectors until art-pass door assets arrive.
/// Not networked yet: state is local-only (host and clients each simulate independently).
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class SimpleDoor : MonoBehaviour, IInteractable
{
    [SerializeField] float swingDuration = 0.45f;

    bool isOpen;
    bool isAnimating;
    BoxCollider bodyCollider;

    Quaternion closedRot;
    Quaternion openRot;

    public string InteractHint => isAnimating ? string.Empty : isOpen ? "[E] Close" : "[E] Open";

    void Awake()
    {
        bodyCollider = GetComponent<BoxCollider>();
        closedRot = transform.localRotation;
        openRot   = closedRot * Quaternion.Euler(0f, -90f, 0f);
    }

    public void OnInteractStart(PlayerController player)
    {
        if (!isAnimating)
            StartCoroutine(Swing());
    }

    public void OnInteractEnd(PlayerController player) { }

    IEnumerator Swing()
    {
        isAnimating = true;
        // isOpen still holds the pre-swing state here: false → this swing is an open.
        if (isOpen) AudioManager.Instance?.PlayDoorClose(transform.position);
        else AudioManager.Instance?.PlayDoorOpen(transform.position);

        Quaternion start = transform.localRotation;
        Quaternion end   = isOpen ? closedRot : openRot;
        float elapsed    = 0f;

        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(start, end, elapsed / swingDuration);
            yield return null;
        }

        transform.localRotation = end;
        isOpen = !isOpen;
        bodyCollider.enabled = !isOpen;
        isAnimating = false;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Called from TowerV8WhiteboxBuilder to build door geometry and set collider.
    /// axis = 'x' → door panel lies in XY plane (faces Z); 'z' → panel in ZY plane (faces X).
    /// hingeX/Z is the world position of the hinge edge (lo side of the gap).
    /// </summary>
    public void BuildGeometry(char axis, float hingeX, float floorY, float hingeZ,
        float widthM, Material panelMat, Material handleMat)
    {
        const float thickness  = 0.07f;
        const float handleW    = 0.05f;
        const float handleH    = 0.28f;
        const float handleProj = 0.07f;
        const float clearH     = 2.25f; // must match DoorClearHeight in builder

        // ── Panel ──────────────────────────────────────────────────────────────
        // Pivot (this object) sits at the hinge edge; panel child offset by half-width.
        var panelGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panelGO.name = "Panel";
        panelGO.transform.SetParent(transform, false);
        Object.DestroyImmediate(panelGO.GetComponent<Collider>());

        if (axis == 'x')
        {
            // Wall face is perpendicular to X; panel spans Z (width) and Y (height).
            panelGO.transform.localPosition = new Vector3(0f, 0f, widthM * 0.5f);
            panelGO.transform.localScale    = new Vector3(thickness, clearH, widthM);
        }
        else
        {
            // Wall face is perpendicular to Z; panel spans X (width) and Y (height).
            panelGO.transform.localPosition = new Vector3(widthM * 0.5f, 0f, 0f);
            panelGO.transform.localScale    = new Vector3(widthM, clearH, thickness);
        }
        panelGO.GetComponent<Renderer>().sharedMaterial = panelMat;

        // ── Handle ─────────────────────────────────────────────────────────────
        // Placed at 80% height, 80% width from hinge (opposite side from hinge).
        var handleGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handleGO.name = "Handle";
        handleGO.transform.SetParent(transform, false);
        Object.DestroyImmediate(handleGO.GetComponent<Collider>());

        float hw = widthM * 0.80f; // horizontal offset from pivot (near latch side)
        float hy = clearH * 0.5f - clearH * 0.20f; // ~80% up from floor, relative to pivot centre

        if (axis == 'x')
        {
            handleGO.transform.localPosition = new Vector3(-handleProj, hy, hw);
            handleGO.transform.localScale    = new Vector3(handleProj * 2f + thickness, handleH, handleW);
        }
        else
        {
            handleGO.transform.localPosition = new Vector3(hw, hy, -handleProj);
            handleGO.transform.localScale    = new Vector3(handleW, handleH, handleProj * 2f + thickness);
        }
        handleGO.GetComponent<Renderer>().sharedMaterial = handleMat;

        // ── Collider on pivot (covers full door panel for interaction raycast) ─
        bodyCollider = gameObject.GetComponent<BoxCollider>();
        if (axis == 'x')
        {
            bodyCollider.center = new Vector3(0f, 0f, widthM * 0.5f);
            bodyCollider.size   = new Vector3(thickness + 0.12f, clearH, widthM);
        }
        else
        {
            bodyCollider.center = new Vector3(widthM * 0.5f, 0f, 0f);
            bodyCollider.size   = new Vector3(widthM, clearH, thickness + 0.12f);
        }
    }
#endif
}
