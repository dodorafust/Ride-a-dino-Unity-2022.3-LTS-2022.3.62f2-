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
    public Color ropeColor = new Color(0.95f, 0.85f, 0.2f, 1f);

    [Header("Lasso Loop (Torus)")]
    public GameObject lassoLoopPrefab;
    public float lassoLoopScale = 1.15f;
    public float lassoOrbitRadius = 0.12f;
    public float lassoRightOffset = 0.48f;
    public float lassoForwardOffset = 0.2f;
    public float lassoHeightOffset = 0.5f;
    public float lassoTilt = 22f;
    public float lassoWobbleAmp = 8f;
    public float lassoRopeSpinMultiplier = 1f;
    public float lassoCatchScale = 1.6f;
    public float ropeSag = 0.18f;
    public float catchRopeSag = 0.25f;

    const int RopePointCount = 12;

    Transform ring, target, ropeFrom, ropeTo, lassoLoop, twirlHand, twirlRoot;
    Vector3 ropeToLocalOffset;
    LineRenderer rope;
    bool shown, projectToGround, twirling;
    int groundMask;
    float twirlPhase, lassoBaseNormalize = 1f;
    Quaternion lassoFlatFix = Quaternion.identity;
    Vector3 lassoPlaneNormalLocal = Vector3.up;

    void Awake()
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Standard");
        var ringMat = new Material(sh) { color = color };
        var ropeMat = new Material(sh) { color = ropeColor };

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
            mr.sharedMaterial = ringMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }
        ring.gameObject.SetActive(false);

        rope = gameObject.AddComponent<LineRenderer>();
        rope.positionCount = RopePointCount;
        rope.useWorldSpace = true;
        rope.numCapVertices = 4;
        rope.startWidth = rope.endWidth = 0.055f;
        rope.sharedMaterial = ropeMat;
        rope.startColor = rope.endColor = ropeColor;
        rope.enabled = false;

        if (lassoLoopPrefab != null)
        {
            GameObject loopGo = Instantiate(lassoLoopPrefab, transform);
            loopGo.name = "LassoLoop";
            loopGo.transform.localPosition = Vector3.zero;
            loopGo.transform.localRotation = Quaternion.identity;
            loopGo.transform.localScale = Vector3.one;
            Collider[] cols = loopGo.GetComponentsInChildren<Collider>();
            for (int i = 0; i < cols.Length; i++) Destroy(cols[i]);

            Shader loopSh = Shader.Find("Unlit/Color");
            if (loopSh == null) loopSh = Shader.Find("Sprites/Default");
            Material loopMat = loopSh != null ? new Material(loopSh) : new Material(ropeMat);
            loopMat.color = ropeColor;

            MeshRenderer[] mrs = loopGo.GetComponentsInChildren<MeshRenderer>();
            Bounds combined = new Bounds(loopGo.transform.position, Vector3.zero);
            bool hasBounds = false;
            for (int i = 0; i < mrs.Length; i++)
            {
                mrs[i].sharedMaterial = loopMat;
                mrs[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (!hasBounds) { combined = mrs[i].bounds; hasBounds = true; }
                else combined.Encapsulate(mrs[i].bounds);
            }
            if (hasBounds)
            {
                Vector3 worldSz = combined.size;
                float maxDim = Mathf.Max(worldSz.x, Mathf.Max(worldSz.y, worldSz.z));
                if (maxDim > 0.0001f) lassoBaseNormalize = 1f / maxDim;

                // Đảm bảo mặt vòng Torus nằm ngang (trục mỏng nhất hướng lên Y)
                if (worldSz.z <= worldSz.x && worldSz.z <= worldSz.y)
                    lassoFlatFix = Quaternion.Euler(90f, 0f, 0f);
                else if (worldSz.x <= worldSz.y && worldSz.x <= worldSz.z)
                    lassoFlatFix = Quaternion.Euler(0f, 0f, 90f);
                lassoPlaneNormalLocal = Vector3.up;
            }
            lassoLoop = loopGo.transform;
            lassoLoop.gameObject.SetActive(false);
        }
    }

    public void Show(Transform t, Vector3 startPos, bool followGroundUnderTarget = false, int mask = 0)
    {
        target = t;
        projectToGround = followGroundUnderTarget;
        groundMask = mask;
        if (!shown)
        {
            shown = true;
            ring.position = GoalFor(startPos);
            ring.gameObject.SetActive(true);
        }
    }

    Vector3 GoalFor(Vector3 pos)
    {
        if (projectToGround && groundMask != 0 &&
            Physics.Raycast(pos + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 50f,
                            groundMask, QueryTriggerInteraction.Ignore))
        {
            return new Vector3(pos.x, hit.point.y + 0.06f, pos.z);
        }
        return pos + Vector3.up * 0.06f;
    }

    public void Hide()
    {
        target = null;
        shown = false;
        projectToGround = false;
        ring.gameObject.SetActive(false);
    }

    public void SetRope(Transform from, Transform to, Vector3 toLocalOffset = default)
    {
        ropeFrom = from; ropeTo = to; ropeToLocalOffset = toLocalOffset;
        UpdateLassoVisual();
    }

    public void SetTwirlingLasso(Transform hand, bool active, float spinPhase, Transform playerRoot)
    {
        twirlHand = hand;
        twirling = active && hand != null;
        twirlPhase = spinPhase;
        twirlRoot = playerRoot;
        UpdateLassoVisual();
    }

    void SetRopeCurve(Vector3 start, Vector3 end, float sag)
    {
        rope.enabled = true;
        if (rope.positionCount != RopePointCount) rope.positionCount = RopePointCount;
        Vector3 mid = (start + end) * 0.5f + Vector3.down * sag;
        for (int i = 0; i < RopePointCount; i++)
        {
            float t = i / (float)(RopePointCount - 1);
            float u = 1f - t;
            rope.SetPosition(i, u * u * start + 2f * u * t * mid + t * t * end);
        }
    }

    void UpdateLassoVisual()
    {
        bool catching = ropeFrom != null && ropeTo != null;
        if (catching)
        {
            SetRopeCurve(ropeFrom.position, ropeTo.TransformPoint(ropeToLocalOffset), catchRopeSag);
            if (lassoLoop != null) lassoLoop.gameObject.SetActive(false);
            return;
        }

        if (twirling && twirlHand != null)
        {
            Vector3 up = twirlRoot != null ? twirlRoot.up : Vector3.up;
            Vector3 fwd = twirlRoot != null ? twirlRoot.forward : Vector3.forward;
            Vector3 right = twirlRoot != null ? twirlRoot.right : Vector3.right;

            // Vị trí tâm vòng Torus cố định lệch trên-phải tay cầm; vòng vẫn xoay/chao, còn đầu dây chạy vòng quanh mép vòng
            float tiltRoll = lassoTilt + Mathf.Sin(twirlPhase * 0.7f) * lassoWobbleAmp;
            float tiltPitch = -lassoTilt * 0.45f + Mathf.Cos(twirlPhase * 1.1f) * lassoWobbleAmp;
            Quaternion loopRot = Quaternion.AngleAxis(tiltRoll, fwd) * Quaternion.AngleAxis(tiltPitch, right);

            Vector3 loopCenter = twirlHand.position + up * lassoHeightOffset + right * lassoRightOffset + fwd * lassoForwardOffset;
            float ropePhase = twirlPhase * lassoRopeSpinMultiplier;
            Vector3 orbitLocal = right * Mathf.Cos(ropePhase) + fwd * Mathf.Sin(ropePhase);
            Vector3 orbitInPlane = (loopRot * orbitLocal).normalized;
            Vector3 rimPoint = loopCenter + orbitInPlane * (0.46f * lassoLoopScale);

            if (lassoLoop != null)
            {
                lassoLoop.gameObject.SetActive(true);
                lassoLoop.position = loopCenter;
                lassoLoop.rotation = loopRot * Quaternion.Euler(0f, -twirlPhase * Mathf.Rad2Deg, 0f) * lassoFlatFix;
                lassoLoop.localScale = Vector3.one * (lassoLoopScale * lassoBaseNormalize);
            }

            SetRopeCurve(twirlHand.position, rimPoint, ropeSag);
            return;
        }

        rope.enabled = false;
        if (lassoLoop != null) lassoLoop.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (shown && target != null)
        {
            Vector3 goal = GoalFor(target.position);
            ring.position = Vector3.Lerp(ring.position, goal,
                                         1f - Mathf.Exp(-followSpeed * Time.deltaTime));
            ring.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            ring.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(Time.time * 8f));
        }

        if (ropeFrom != null && ropeTo != null) UpdateLassoVisual();
    }
}
