using UnityEngine;

public class CatchPreview : MonoBehaviour
{
    public int dashCount = 14;
    public float radius = 2.4f;
    public float dashLength = 0.75f;
    public float dashWidth = 0.2f;
    public float spinSpeed = 80f;
    public float followSpeed = 16f;
    public Color color = new Color(1f, 0.92f, 0.1f, 1f);

    Transform ring, target, ropeFrom, ropeTo;
    LineRenderer rope;
    bool shown;

    void Awake()
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Standard");
        var mat = new Material(sh) { color = color };

        ring = new GameObject("Ring").transform;
        ring.SetParent(transform, false);

        for (int i = 0; i < dashCount; i++)
        {
            float a = i * Mathf.PI * 2f / dashCount;
            var dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(dash.GetComponent<Collider>());
            dash.transform.SetParent(ring, false);
            dash.transform.localPosition = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * radius;
            dash.transform.localRotation =
                Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a)));
            dash.transform.localScale = new Vector3(dashWidth, 0.04f, dashLength);

            var mr = dash.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }
        ring.gameObject.SetActive(false);

        rope = gameObject.AddComponent<LineRenderer>();
        rope.positionCount = 2;
        rope.startWidth = rope.endWidth = 0.12f;
        rope.sharedMaterial = mat;
        rope.startColor = rope.endColor = color;
        rope.enabled = false;
    }

    public void Show(Transform t, Vector3 startPos)
    {
        target = t;
        if (!shown)
        {
            shown = true;
            ring.position = startPos;
            ring.gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        target = null;
        shown = false;
        ring.gameObject.SetActive(false);
    }

    public void SetRope(Transform from, Transform to)
    {
        ropeFrom = from; ropeTo = to;
        rope.enabled = from != null && to != null;
    }

    void LateUpdate()
    {
        if (shown && target != null)
        {
            Vector3 goal = target.position + Vector3.up * 0.06f;
            ring.position = Vector3.Lerp(ring.position, goal,
                                         1f - Mathf.Exp(-followSpeed * Time.deltaTime));
            ring.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            ring.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(Time.time * 8f));
        }

        if (rope.enabled && ropeFrom != null && ropeTo != null)
        {
            rope.SetPosition(0, ropeFrom.position);
            rope.SetPosition(1, ropeTo.position);
        }
    }
}