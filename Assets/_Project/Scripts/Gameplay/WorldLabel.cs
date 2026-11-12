using TMPro;
using UnityEngine;

// Chữ nổi 3D bám theo 1 object (tên Dino, bảng nâng cấp...). Là PREFAB: bạn chỉnh font, màu, cỡ chữ trong prefab.
[RequireComponent(typeof(TextMeshPro))]
public class WorldLabel : MonoBehaviour
{
    public bool outline = true;
    public bool constantScreenSize = true;
    public float sizePerMeter = 0.05f;
    public float minScale = 0.6f;
    public float maxScale = 2.5f;
    public float maxDistance = 45f;
    public bool hidden;

    Transform target;
    Vector3 offset;
    TextMeshPro tmp;
    Camera cam;

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
        if (outline)
        {
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color32(0, 0, 0, 255);
        }
    }

    public void Init(Transform t, Vector3 worldOffset)
    {
        target = t;
        offset = worldOffset;
    }

    public void SetText(string s)
    {
        if (tmp == null) tmp = GetComponent<TextMeshPro>();
        tmp.text = s;
    }

    void LateUpdate()
    {
        if (target == null) { Destroy(gameObject); return; }
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 pos = target.position + offset;
        transform.position = pos;
        transform.rotation = cam.transform.rotation;

        float dist = Vector3.Distance(cam.transform.position, pos);
        bool visible = !hidden && dist < maxDistance;
        if (tmp.enabled != visible) tmp.enabled = visible;

        if (constantScreenSize)
            transform.localScale = Vector3.one * Mathf.Clamp(dist * sizePerMeter, minScale, maxScale);
    }
}
