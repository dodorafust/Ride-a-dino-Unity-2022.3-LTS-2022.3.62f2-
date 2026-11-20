using System.Collections;
using UnityEngine;
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
        if (host == null || btn == null) return;
        RectTransform rt = btn.GetComponent<RectTransform>();
        if (rt == null) return;
        btn.onClick.AddListener(delegate
        {
            if (host != null && host.gameObject.activeInHierarchy)
                host.StartCoroutine(ButtonBounceRoutine(rt, punch, duration));
        });
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

    static IEnumerator ButtonBounceRoutine(RectTransform rt, float punch, float duration)
    {
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
            rt.localScale = Vector3.one * s;
            yield return null;
        }
        if (rt != null) rt.localScale = Vector3.one;
    }
}
