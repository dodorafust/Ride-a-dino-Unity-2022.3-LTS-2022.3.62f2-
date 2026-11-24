using UnityEngine;

// Quầy mua & trang bị Aura (AURAS): đi vào vòng tròn tím phía trước quầy tự mở bảng AurasPopup; đi ra tự đóng.
public class AuraShop : MonoBehaviour
{
    [Header("Vùng kích hoạt (Vòng tròn tím trước quầy)")]
    public Vector3 triggerLocalOffset = new Vector3(0f, 0.03f, 2.8f);
    public float triggerRadius = 2.6f;
    public Color ringOuterColor = new Color(0.68f, 0.16f, 0.96f, 0.92f);
    public Color ringInnerColor = new Color(0.58f, 0.25f, 0.90f, 0.38f);

    [Header("Nhãn nổi")]
    public string shopLabelText = "<color=#66FF44>AURAS</color>";
    public Vector3 labelOffset = new Vector3(0f, 3.8f, 0f);
    public bool showFloatingLabel = true;

    bool playerInside;
    WorldLabel label;
    Material matWood, matPurple, matWhite, matCrystal, matRingOuter, matRingInner;

    void Start()
    {
        EnsureVisuals();
        if (showFloatingLabel && GameManager.Instance != null)
            label = GameManager.Instance.CreateLabel(transform, labelOffset, shopLabelText);
    }

    void OnDestroy()
    {
        if (matWood != null) Destroy(matWood);
        if (matPurple != null) Destroy(matPurple);
        if (matWhite != null) Destroy(matWhite);
        if (matCrystal != null) Destroy(matCrystal);
        if (matRingOuter != null) Destroy(matRingOuter);
        if (matRingInner != null) Destroy(matRingInner);
    }

    public void BuildVisualsInEditor()
    {
        EnsureVisuals();
    }

    void EnsureVisuals()
    {
        if (transform.Find("ShopVisual") == null)
        {
            GameObject vis = new GameObject("ShopVisual");
            vis.transform.SetParent(transform, false);

            Shader sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            matWood = new Material(sh) { color = new Color(0.56f, 0.35f, 0.16f) };
            matPurple = new Material(sh) { color = new Color(0.66f, 0.20f, 0.92f) };
            matWhite = new Material(sh) { color = new Color(0.95f, 0.94f, 0.98f) };
            matCrystal = new Material(sh) { color = new Color(0.45f, 0.85f, 1.0f) };

            CreatePart(vis.transform, "Pillar_FL", PrimitiveType.Cube, new Vector3(-1.3f, 1.2f, 1.1f), new Vector3(0.32f, 2.4f, 0.32f), matWood, true);
            CreatePart(vis.transform, "Pillar_FR", PrimitiveType.Cube, new Vector3(1.3f, 1.2f, 1.1f), new Vector3(0.32f, 2.4f, 0.32f), matWood, true);
            CreatePart(vis.transform, "Pillar_BL", PrimitiveType.Cube, new Vector3(-1.3f, 1.2f, -1.1f), new Vector3(0.32f, 2.4f, 0.32f), matWood, true);
            CreatePart(vis.transform, "Pillar_BR", PrimitiveType.Cube, new Vector3(1.3f, 1.2f, -1.1f), new Vector3(0.32f, 2.4f, 0.32f), matWood, true);
            CreatePart(vis.transform, "Counter", PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), new Vector3(2.8f, 1.1f, 2.3f), matWood, true);
            CreatePart(vis.transform, "AuraOrbProp", PrimitiveType.Sphere, new Vector3(0f, 1.38f, 0.25f), new Vector3(0.55f, 0.55f, 0.55f), matCrystal, false);

            // Mái vòm sọc tím/trắng giống bản mẫu AURAS
            for (int i = 0; i < 6; i++)
            {
                float x = -1.25f + i * 0.5f;
                Material roofMat = (i % 2 == 0) ? matPurple : matWhite;
                CreatePart(vis.transform, "RoofStripe_" + i, PrimitiveType.Cube,
                           new Vector3(x, 2.55f, 0f), new Vector3(0.48f, 0.22f, 2.9f), roofMat, false);
            }
        }

        if (transform.Find("TriggerCircle") == null)
        {
            GameObject ringRoot = new GameObject("TriggerCircle");
            ringRoot.transform.SetParent(transform, false);
            ringRoot.transform.localPosition = triggerLocalOffset;

            Shader transparentShader = Shader.Find("Sprites/Default");
            if (transparentShader == null) transparentShader = Shader.Find("Standard");
            matRingOuter = new Material(transparentShader) { color = ringOuterColor };
            matRingInner = new Material(transparentShader) { color = ringInnerColor };

            float d = triggerRadius * 2f;
            CreatePart(ringRoot.transform, "OuterDisc", PrimitiveType.Cylinder,
                       Vector3.zero, new Vector3(d, 0.012f, d), matRingOuter, false);
            CreatePart(ringRoot.transform, "InnerDisc", PrimitiveType.Cylinder,
                       new Vector3(0f, 0.008f, 0f), new Vector3(d * 0.90f, 0.012f, d * 0.90f), matRingInner, false);
        }
        else
        {
            RefreshRingVisuals();
        }
    }

    public void RefreshRingVisuals()
    {
        Transform ringRoot = transform.Find("TriggerCircle");
        if (ringRoot == null) return;
        ringRoot.localPosition = triggerLocalOffset;
        float d = triggerRadius * 2f;

        Transform outerT = ringRoot.Find("OuterDisc");
        if (outerT != null)
        {
            outerT.localScale = new Vector3(d, 0.012f, d);
            MeshRenderer mr = outerT.GetComponent<MeshRenderer>();
            if (mr != null && mr.sharedMaterial != null) mr.sharedMaterial.color = ringOuterColor;
        }
        Transform innerT = ringRoot.Find("InnerDisc");
        if (innerT != null)
        {
            innerT.localScale = new Vector3(d * 0.90f, 0.012f, d * 0.90f);
            MeshRenderer mr = innerT.GetComponent<MeshRenderer>();
            if (mr != null && mr.sharedMaterial != null) mr.sharedMaterial.color = ringInnerColor;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        RefreshRingVisuals();
    }
#endif

    static void CreatePart(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null && mat != null) mr.sharedMaterial = mat;
        if (!keepCollider)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null)
            {
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }
        }
    }

    void Update()
    {
        PlayerController pc = PlayerController.Instance;
        AurasPopup popup = AurasPopup.Instance;
        if (pc == null || popup == null) return;

        bool ridingAnyDino = pc.State == PlayerState.Riding || (MountController.Instance != null && MountController.Instance.Mounted);
        bool canInteract = pc.State != PlayerState.Dead && pc.State != PlayerState.Sitting && !pc.RunActive && !ridingAnyDino;
        Vector3 circleWorld = transform.TransformPoint(triggerLocalOffset);
        Vector3 delta = pc.transform.position - circleWorld;
        delta.y = 0f;
        bool inside = canInteract && delta.sqrMagnitude <= triggerRadius * triggerRadius;

        if (inside && !playerInside)
        {
            playerInside = true;
            popup.Open();
        }
        else if (!inside && playerInside)
        {
            playerInside = false;
            popup.Hide();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = ringOuterColor;
        Gizmos.DrawWireSphere(transform.TransformPoint(triggerLocalOffset), triggerRadius);
    }
}
