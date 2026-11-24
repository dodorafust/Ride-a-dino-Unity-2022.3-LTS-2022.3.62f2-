using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Helper hiệu ứng Pop / Bounce (nảy lò xo) dùng chung cho Popup và Nút bấm UI.
public static class UIEffectUtil
{
    public static Coroutine PlaySpringPop(MonoBehaviour host, RectTransform target, Coroutine running,
                                          float duration = 0.28f, float startScale = 0.68f, float overshoot = 1.07f)
    {
        if (host == null || target == null || !host.gameObject.activeInHierarchy) return null;
        if (running != null) host.StopCoroutine(running);
        return host.StartCoroutine(SpringPopRoutine(target, duration, startScale, overshoot));
    }

    public static void AttachButtonBounce(MonoBehaviour host, Button btn, float punch = 0.14f, float duration = 0.18f)
    {
        if (btn == null) return;
        UIButtonBouncer b = btn.GetComponent<UIButtonBouncer>();
        if (b == null) b = btn.gameObject.AddComponent<UIButtonBouncer>();
        b.Configure(btn, punch, duration);
    }

    class UIButtonBouncer : MonoBehaviour, IPointerClickHandler
    {
        Button button;
        float punch = 0.14f;
        float duration = 0.18f;
        Vector3 baseScale = Vector3.one;
        bool hasBaseScale;
        Coroutine routine;

        void Awake()
        {
            baseScale = transform.localScale;
            if (baseScale.sqrMagnitude < 0.0001f) baseScale = Vector3.one;
            hasBaseScale = true;
        }

        public void Configure(Button btn, float punchAmt, float dur)
        {
            button = btn;
            punch = punchAmt;
            duration = dur;
            if (!hasBaseScale) Awake();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;
            if (!gameObject.activeInHierarchy) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(BounceRoutine());
        }

        void OnDisable()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
            if (hasBaseScale) transform.localScale = baseScale;
        }

        IEnumerator BounceRoutine()
        {
            RectTransform rt = transform as RectTransform;
            float dur = Mathf.Max(0.06f, duration);
            float t = 0f;
            while (t < 1f && rt != null)
            {
                t = Mathf.Min(1f, t + Time.unscaledDeltaTime / dur);
                float s;
                if (t < 0.4f)
                    s = Mathf.Lerp(1f, 1f - punch, t / 0.4f);
                else if (t < 0.78f)
                    s = Mathf.Lerp(1f - punch, 1f + punch * 0.55f, (t - 0.4f) / 0.38f);
                else
                    s = Mathf.Lerp(1f + punch * 0.55f, 1f, (t - 0.78f) / 0.22f);
                rt.localScale = baseScale * s;
                yield return null;
            }
            if (rt != null) rt.localScale = baseScale;
            routine = null;
        }
    }

    static IEnumerator SpringPopRoutine(RectTransform target, float duration, float startScale, float overshoot)
    {
        float dur = Mathf.Max(0.08f, duration);
        float t = 0f;
        target.localScale = Vector3.one * startScale;

        while (t < 1f && target != null)
        {
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / dur);
            float scale;
            if (t < 0.60f)
            {
                float p = t / 0.60f;
                float ease = 1f - (1f - p) * (1f - p);
                scale = Mathf.Lerp(startScale, overshoot, ease);
            }
            else if (t < 0.84f)
            {
                float p = (t - 0.60f) / 0.24f;
                scale = Mathf.Lerp(overshoot, 0.975f, p);
            }
            else
            {
                float p = (t - 0.84f) / 0.16f;
                scale = Mathf.Lerp(0.975f, 1f, p);
            }
            target.localScale = Vector3.one * scale;
            yield return null;
        }
        if (target != null) target.localScale = Vector3.one;
    }

    static readonly System.Collections.Generic.Dictionary<int, Sprite> roundedCache = new System.Collections.Generic.Dictionary<int, Sprite>();
    static readonly System.Collections.Generic.Dictionary<int, Sprite> outlineCache = new System.Collections.Generic.Dictionary<int, Sprite>();

    public static Sprite GetRoundedRectSprite(int size = 64, int radius = 14)
    {
        int key = size * 1000 + radius;
        if (roundedCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[size * size];
        float r = Mathf.Clamp(radius, 1, size / 2);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0f, Mathf.Max(r - 0.5f - x, x - (size - 1 - r + 0.5f)));
                float dy = Mathf.Max(0f, Mathf.Max(r - 0.5f - y, y - (size - 1 - r + 0.5f)));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(r - dist + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();

        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        roundedCache[key] = sp;
        return sp;
    }

    public static Sprite GetRoundedOutlineSprite(int size = 64, int radius = 14, int thickness = 4)
    {
        int key = size * 100000 + radius * 100 + thickness;
        if (outlineCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[size * size];
        float rOuter = Mathf.Clamp(radius, 2, size / 2);
        float t = Mathf.Clamp(thickness, 1, size / 4);
        float rInner = Mathf.Max(1f, rOuter - t);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dxO = Mathf.Max(0f, Mathf.Max(rOuter - 0.5f - x, x - (size - 1 - rOuter + 0.5f)));
                float dyO = Mathf.Max(0f, Mathf.Max(rOuter - 0.5f - y, y - (size - 1 - rOuter + 0.5f)));
                float distO = Mathf.Sqrt(dxO * dxO + dyO * dyO);
                float aOuter = Mathf.Clamp01(rOuter - distO + 0.5f);

                float xMinI = t + rInner - 0.5f;
                float xMaxI = size - 1 - t - rInner + 0.5f;
                float yMinI = t + rInner - 0.5f;
                float yMaxI = size - 1 - t - rInner + 0.5f;
                float dxI = Mathf.Max(0f, Mathf.Max(xMinI - x, x - xMaxI));
                float dyI = Mathf.Max(0f, Mathf.Max(yMinI - y, y - yMaxI));
                float distI = Mathf.Sqrt(dxI * dxI + dyI * dyI);
                float aInner = Mathf.Clamp01(rInner - distI + 0.5f);

                float a = Mathf.Clamp01(aOuter * (1f - aInner));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();

        float border = Mathf.Max(rOuter, t + rInner);
        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        outlineCache[key] = sp;
        return sp;
    }
}
