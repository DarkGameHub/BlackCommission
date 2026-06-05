using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class CrtScreenSkew : MonoBehaviour
{
    [SerializeField] RectTransform screenRect;
    [SerializeField] float bottomLeftOffsetX = 10f;
    [SerializeField] float bottomRightOffsetX = 18f;
    [SerializeField] float bottomLeftOffsetY = -16f;
    [SerializeField] float bottomRightOffsetY = -4f;
    float lastBottomLeftOffsetX;
    float lastBottomRightOffsetX;
    float lastBottomLeftOffsetY;
    float lastBottomRightOffsetY;

    public float SkewX
    {
        get => (bottomLeftOffsetX + bottomRightOffsetX) * 0.5f;
        set
        {
            if (Mathf.Approximately(bottomLeftOffsetX, value) &&
                Mathf.Approximately(bottomRightOffsetX, value))
                return;
            bottomLeftOffsetX = value;
            bottomRightOffsetX = value;
            MarkDirty();
        }
    }

    public float BottomLeftOffsetX
    {
        get => bottomLeftOffsetX;
        set
        {
            if (Mathf.Approximately(bottomLeftOffsetX, value)) return;
            bottomLeftOffsetX = value;
            MarkDirty();
        }
    }

    public float BottomRightOffsetX
    {
        get => bottomRightOffsetX;
        set
        {
            if (Mathf.Approximately(bottomRightOffsetX, value)) return;
            bottomRightOffsetX = value;
            MarkDirty();
        }
    }

    public float BottomLeftOffsetY
    {
        get => bottomLeftOffsetY;
        set
        {
            if (Mathf.Approximately(bottomLeftOffsetY, value)) return;
            bottomLeftOffsetY = value;
            MarkDirty();
        }
    }

    public float BottomRightOffsetY
    {
        get => bottomRightOffsetY;
        set
        {
            if (Mathf.Approximately(bottomRightOffsetY, value)) return;
            bottomRightOffsetY = value;
            MarkDirty();
        }
    }

    void Reset()
    {
        screenRect = transform as RectTransform;
    }

    void OnEnable()
    {
        if (screenRect == null)
            screenRect = transform as RectTransform;
        CacheOffsetValues();
        Refresh();
        MarkDirty();
    }

    void OnValidate()
    {
        if (screenRect == null)
            screenRect = transform as RectTransform;
        MarkDirty();
    }

    void LateUpdate()
    {
        Refresh();
        if (OffsetsChanged())
        {
            CacheOffsetValues();
            MarkDirty();
        }
    }

    void CacheOffsetValues()
    {
        lastBottomLeftOffsetX = bottomLeftOffsetX;
        lastBottomRightOffsetX = bottomRightOffsetX;
        lastBottomLeftOffsetY = bottomLeftOffsetY;
        lastBottomRightOffsetY = bottomRightOffsetY;
    }

    bool OffsetsChanged()
    {
        return !Mathf.Approximately(lastBottomLeftOffsetX, bottomLeftOffsetX) ||
            !Mathf.Approximately(lastBottomRightOffsetX, bottomRightOffsetX) ||
            !Mathf.Approximately(lastBottomLeftOffsetY, bottomLeftOffsetY) ||
            !Mathf.Approximately(lastBottomRightOffsetY, bottomRightOffsetY);
    }

    public void Refresh()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (graphic == null) continue;

            if (graphic is TMP_Text tmpText)
            {
                var textEffect = tmpText.GetComponent<CrtScreenSkewText>();
                if (textEffect == null)
                    textEffect = tmpText.gameObject.AddComponent<CrtScreenSkewText>();
                textEffect.SetOwner(this);
                continue;
            }

            var effect = graphic.GetComponent<CrtScreenSkewGraphic>();
            if (effect == null)
                effect = graphic.gameObject.AddComponent<CrtScreenSkewGraphic>();
            effect.SetOwner(this);
        }
    }

    public Vector3 WarpWorldPoint(Vector3 worldPoint)
    {
        if (screenRect == null) return worldPoint;

        Vector3 rootLocal = screenRect.InverseTransformPoint(worldPoint);
        Rect rect = screenRect.rect;
        float topToBottom = Mathf.InverseLerp(rect.yMax, rect.yMin, rootLocal.y);
        float leftToRight = Mathf.InverseLerp(rect.xMin, rect.xMax, rootLocal.x);
        float clampedX = Mathf.Clamp01(leftToRight);
        float clampedY = Mathf.Clamp01(topToBottom);
        float bottomOffsetX = Mathf.Lerp(bottomLeftOffsetX, bottomRightOffsetX, clampedX);
        float bottomOffsetY = Mathf.Lerp(bottomLeftOffsetY, bottomRightOffsetY, clampedX);
        rootLocal.x += bottomOffsetX * clampedY;
        rootLocal.y += bottomOffsetY * clampedY;
        return screenRect.TransformPoint(rootLocal);
    }

    public void MarkDirty()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (graphic == null) continue;
            graphic.SetVerticesDirty();
            if (graphic is TMP_Text tmpText)
                tmpText.ForceMeshUpdate();
        }
    }
}

public class CrtScreenSkewGraphic : BaseMeshEffect
{
    [SerializeField] CrtScreenSkew owner;

    public void SetOwner(CrtScreenSkew newOwner)
    {
        if (owner == newOwner) return;
        owner = newOwner;
        if (graphic != null)
            graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || owner == null || vh.currentVertCount == 0)
            return;

        var rt = transform as RectTransform;
        if (rt == null) return;

        UIVertex vertex = default;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            Vector3 world = rt.TransformPoint(vertex.position);
            Vector3 warped = owner.WarpWorldPoint(world);
            vertex.position = rt.InverseTransformPoint(warped);
            vh.SetUIVertex(vertex, i);
        }
    }
}

public class CrtScreenSkewText : MonoBehaviour
{
    [SerializeField] CrtScreenSkew owner;
    TMP_Text text;
    RectTransform rectTransform;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
        rectTransform = transform as RectTransform;
    }

    void OnEnable()
    {
        if (text == null) text = GetComponent<TMP_Text>();
        if (rectTransform == null) rectTransform = transform as RectTransform;
        if (text != null)
            text.OnPreRenderText += HandlePreRenderText;
    }

    void OnDisable()
    {
        if (text != null)
            text.OnPreRenderText -= HandlePreRenderText;
    }

    public void SetOwner(CrtScreenSkew newOwner)
    {
        if (owner == newOwner) return;
        owner = newOwner;
        if (text != null)
            text.ForceMeshUpdate();
    }

    void HandlePreRenderText(TMP_TextInfo textInfo)
    {
        if (owner == null || rectTransform == null || textInfo == null)
            return;

        for (int materialIndex = 0; materialIndex < textInfo.meshInfo.Length; materialIndex++)
        {
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
            if (vertices == null) continue;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 world = rectTransform.TransformPoint(vertices[i]);
                Vector3 warped = owner.WarpWorldPoint(world);
                vertices[i] = rectTransform.InverseTransformPoint(warped);
            }
        }
    }
}
