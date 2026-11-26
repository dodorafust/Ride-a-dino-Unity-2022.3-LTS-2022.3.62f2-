using UnityEngine;

// Chạm / click vào thế giới 3D: sàn chuồng, bảng nâng cấp, thú nhún (vật có IClickable).
// LookPad gọi WorldTapper.Tap(...) khi phát hiện 1 cú chạm ngắn (không kéo).
public class WorldTapper : MonoBehaviour
{
    public static WorldTapper Instance { get; private set; }

    public float maxRayDistance = 300f;
    readonly RaycastHit[] hits = new RaycastHit[24];
    readonly System.Collections.Generic.Dictionary<Transform, Vector3> propBaseScales = new System.Collections.Generic.Dictionary<Transform, Vector3>();
    readonly System.Collections.Generic.Dictionary<Transform, Coroutine> propPunchRoutines = new System.Collections.Generic.Dictionary<Transform, Coroutine>();

    void Awake() { Instance = this; }

    public static void Tap(Vector2 screenPos)
    {
        if (Instance != null) Instance.DoTap(screenPos);
    }

    void DoTap(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        PlayerController player = PlayerController.Instance;
        if (cam == null || player == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        int n = Physics.RaycastNonAlloc(ray, hits, maxRayDistance, ~0, QueryTriggerInteraction.Collide);

        IClickable best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            if (hits[i].distance >= bestDist) continue;
            IClickable c = hits[i].collider.GetComponentInParent<IClickable>();
            if (c == null) continue;
            best = c;
            bestDist = hits[i].distance;
        }
        if (best != null)
        {
            if (best is MonoBehaviour propMb && !(best is Pen))
            {
                Transform t = propMb.transform;
                if (!propBaseScales.ContainsKey(t)) propBaseScales[t] = t.localScale;
                if (propPunchRoutines.TryGetValue(t, out Coroutine running) && running != null)
                    StopCoroutine(running);
                propPunchRoutines[t] = StartCoroutine(SpringPunch3D(t, propBaseScales[t]));
            }
            best.OnClicked(player);
        }
    }

    System.Collections.IEnumerator SpringPunch3D(Transform target, Vector3 baseScale)
    {
        if (target == null) yield break;
        float t = 0f;
        const float dur = 0.20f;
        while (t < 1f && target != null)
        {
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / dur);
            float s = t < 0.35f
                ? Mathf.Lerp(1f, 0.90f, t / 0.35f)
                : (t < 0.75f
                    ? Mathf.Lerp(0.90f, 1.07f, (t - 0.35f) / 0.40f)
                    : Mathf.Lerp(1.07f, 1f, (t - 0.75f) / 0.25f));
            target.localScale = baseScale * s;
            yield return null;
        }
        if (target != null) target.localScale = baseScale;
    }
}
