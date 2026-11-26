using TMPro;
using UnityEngine;

// Chữ nổi 3D bám theo 1 object (tên Dino, bảng nâng cấp...). Là PREFAB: bạn chỉnh font, màu, cỡ chữ trong prefab.
[RequireComponent(typeof(TextMeshPro))]
public class WorldLabel : MonoBehaviour
{
    public bool outline = true;
    [Range(0f, 1f)] public float outlineWidth = 0.35f;
    public Color outlineColor = Color.black;
    [Tooltip("Tắt để kích thước chữ nhỏ dần theo khoảng cách (giống game gốc). Bật để giữ nguyên cỡ trên màn hình.")]
    public bool constantScreenSize = false;
    public float sizePerMeter = 0.05f;
    public float minScale = 0.6f;
    public float maxScale = 2.5f;

    [Header("Kích thước theo khoảng cách (khi tắt Constant Screen Size)")]
    [Tooltip("Khoảng cách gần nhất đạt kích thước tối đa (gần hơn nữa cũng giữ nguyên maxScreenSize).")]
    public float minDistance = 8f;
    [Tooltip("Kích thước hiển thị lớn nhất trên màn hình (ở gần <= Min Distance).")]
    public float maxScreenSize = 0.075f;
    [Tooltip("Kích thước hiển thị nhỏ nhất trên màn hình (ở xa sát Max Distance).")]
    public float minScreenSize = 0.022f;
    [Tooltip("Khoảng cách tối đa hiển thị chữ; đi ra khỏi vùng này sẽ tự ẩn.")]
    public float maxDistance = 70f;
    public bool hidden;

    Transform target;
    Vector3 offset;
    TextMeshPro tmp;
    Camera cam;

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
        ApplyOutline();
    }

    void ApplyOutline()
    {
        if (tmp == null) tmp = GetComponent<TextMeshPro>();
        if (tmp == null) return;

        if (!outline || outlineWidth <= 0f)
        {
            tmp.outlineWidth = 0f;
            if (tmp.fontMaterial != null)
            {
                tmp.fontMaterial.DisableKeyword("OUTLINE_ON");
                tmp.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
                tmp.fontMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
            }
            return;
        }

        tmp.outlineWidth = outlineWidth;
        tmp.outlineColor = outlineColor;
        Material mat = tmp.fontMaterial;
        if (mat != null)
        {
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, outlineWidth * 0.25f);
        }
        tmp.UpdateMeshPadding();
    }

    public void Init(Transform t, Vector3 worldOffset)
    {
        target = t;
        offset = worldOffset;
    }

    public void SetOffset(Vector3 worldOffset)
    {
        offset = worldOffset;
    }

    public void SetText(string s)
    {
        if (tmp == null) tmp = GetComponent<TextMeshPro>();
        tmp.text = s;
        ApplyOutline();
    }

    void LateUpdate()
    {
        if (target == null) { Destroy(gameObject); return; }
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 pos = target.position + offset;
        float distSqr = (cam.transform.position - pos).sqrMagnitude;
        bool visible = !hidden && distSqr < maxDistance * maxDistance;
        if (tmp.enabled != visible) tmp.enabled = visible;
        if (!visible) return;

        transform.position = pos;
        transform.rotation = cam.transform.rotation;

        float dist = Mathf.Sqrt(distSqr);
        if (constantScreenSize)
        {
            transform.localScale = Vector3.one * Mathf.Clamp(dist * sizePerMeter, minScale, maxScale);
        }
        else
        {
            float t = Mathf.InverseLerp(minDistance, maxDistance, dist);
            float screenSize = Mathf.Lerp(maxScreenSize, minScreenSize, t);
            transform.localScale = Vector3.one * (dist * screenSize);
        }
    }
}
