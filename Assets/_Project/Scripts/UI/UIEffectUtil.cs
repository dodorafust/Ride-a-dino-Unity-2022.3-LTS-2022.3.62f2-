using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Helper hiệu ứng Pop / Bounce (nảy lò xo) dùng chung cho Popup và Nút bấm UI.
public static class UIEffectUtil
{
    static readonly System.Collections.Generic.Dictionary<RectTransform, Vector3> popBaseScales = new System.Collections.Generic.Dictionary<RectTransform, Vector3>();

    public static Coroutine PlaySpringPop(MonoBehaviour host, RectTransform target, Coroutine running,
                                          float duration = 0.28f, float startScale = 0.68f, float overshoot = 1.07f)
    {
        if (host == null || target == null || !host.gameObject.activeInHierarchy) return null;
        if (running != null) host.StopCoroutine(running);
        if (!popBaseScales.TryGetValue(target, out Vector3 baseScale) || baseScale.sqrMagnitude < 0.0001f)
        {
            baseScale = target.localScale;
            if (baseScale.sqrMagnitude < 0.0001f) baseScale = Vector3.one;
            popBaseScales[target] = baseScale;
        }
        return host.StartCoroutine(SpringPopRoutine(target, baseScale, duration, startScale, overshoot));
    }

    public static void AttachButtonBounce(MonoBehaviour host, Button btn, float punch = 0.14f, float duration = 0.18f)
    {
        if (btn == null) return;
        AttachSpring(btn.gameObject, btn, punch);
    }

    public static void AttachSpring(GameObject go, Selectable sel = null, float punch = 0.14f)
    {
        if (go == null) return;
        UIButtonBouncer b = go.GetComponent<UIButtonBouncer>();
        if (b == null) b = go.AddComponent<UIButtonBouncer>();
        b.Configure(sel, punch);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitGlobalSpringAttacher()
    {
        if (Object.FindObjectOfType<UISpringGlobalAttacher>() != null) return;
        GameObject go = new GameObject("_UISpringGlobalAttacher");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<UISpringGlobalAttacher>();
    }

    class UISpringGlobalAttacher : MonoBehaviour
    {
        float scanTimer;

        void Start() { ScanAndAttach(); }

        void Update()
        {
            scanTimer -= Time.unscaledDeltaTime;
            if (scanTimer <= 0f)
            {
                scanTimer = 0.35f;
                ScanAndAttach();
            }
        }

        void ScanAndAttach()
        {
            Button[] btns = Object.FindObjectsOfType<Button>(false);
            for (int i = 0; i < btns.Length; i++)
            {
                Button b = btns[i];
                if (b == null) continue;
                if (b.gameObject.name.IndexOf("Backdrop", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (b.GetComponent<UIButtonBouncer>() == null)
                    AttachSpring(b.gameObject, b, 0.14f);
            }

            Toggle[] toggles = Object.FindObjectsOfType<Toggle>(false);
            for (int i = 0; i < toggles.Length; i++)
            {
                Toggle t = toggles[i];
                if (t != null && t.GetComponent<UIButtonBouncer>() == null)
                    AttachSpring(t.gameObject, t, 0.12f);
            }

            JumpButton[] jumps = Object.FindObjectsOfType<JumpButton>(false);
            for (int i = 0; i < jumps.Length; i++)
            {
                JumpButton j = jumps[i];
                if (j != null && j.GetComponent<UIButtonBouncer>() == null)
                    AttachSpring(j.gameObject, j.GetComponent<Selectable>(), 0.15f);
            }
        }
    }

    class UIButtonBouncer : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Selectable selectable;
        float pressScale = 0.86f;
        float stiffness = 420f;
        float damping = 20f;
        Vector3 baseScale = Vector3.one;
        bool hasBaseScale;
        bool isPressed;
        float curScaleMul = 1f;
        float targetScaleMul = 1f;
        float vel;

        void Awake()
        {
            baseScale = transform.localScale;
            if (baseScale.sqrMagnitude < 0.0001f) baseScale = Vector3.one;
            hasBaseScale = true;
            if (selectable == null) selectable = GetComponent<Selectable>();
        }

        public void Configure(Selectable sel, float punchAmt)
        {
            if (sel != null) selectable = sel;
            pressScale = Mathf.Clamp(1f - punchAmt, 0.65f, 0.96f);
            if (!hasBaseScale) Awake();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (selectable != null && !selectable.interactable) return;
            isPressed = true;
            targetScaleMul = pressScale;
            vel = Mathf.Min(vel, -3.5f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isPressed) return;
            isPressed = false;
            targetScaleMul = 1f;
            vel = Mathf.Max(vel, 4.5f); // bật nảy lò xo vượt ngưỡng 1.0 khi thả tay
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isPressed) return;
            isPressed = false;
            targetScaleMul = 1f;
        }

        void OnDisable()
        {
            isPressed = false;
            curScaleMul = 1f;
            targetScaleMul = 1f;
            vel = 0f;
            if (hasBaseScale) transform.localScale = baseScale;
        }

        void Update()
        {
            if (!hasBaseScale) return;
            if (!isPressed && Mathf.Abs(curScaleMul - 1f) < 0.0005f && Mathf.Abs(vel) < 0.001f)
            {
                if (curScaleMul != 1f)
                {
                    curScaleMul = 1f;
                    transform.localScale = baseScale;
                }
                return;
            }

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.033f);
            float accel = (targetScaleMul - curScaleMul) * stiffness - vel * damping;
            vel += accel * dt;
            curScaleMul += vel * dt;
            transform.localScale = baseScale * Mathf.Max(0.4f, curScaleMul);
        }
    }

    static IEnumerator SpringPopRoutine(RectTransform target, Vector3 baseScale, float duration, float startScale, float overshoot)
    {
        float dur = Mathf.Max(0.08f, duration);
        float t = 0f;
        target.localScale = baseScale * startScale;

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
            target.localScale = baseScale * scale;
            yield return null;
        }
        if (target != null) target.localScale = baseScale;
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
