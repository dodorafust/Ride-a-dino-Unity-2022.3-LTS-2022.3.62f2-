using System;
using TMPro;
using UnityEngine;

// Số bay lên có icon (sấm sét +Stamina, tiền +$). Là PREFAB: chỉnh icon / font / tốc độ bay trong prefab.
public class FloatingPopup : MonoBehaviour
{
    public TextMeshPro text;
    public SpriteRenderer icon;
    public float lifetime = 1.1f;
    public float riseSpeed = 1.6f;
    [Header("Hiệu ứng bay lên")]
    [Range(0f, 1f)] public float fadeStartRatio = 0.5f;
    public float startScale = 0.85f;
    public float peakScale = 1.15f;
    public float popDuration = 0.14f;

    float t;
    Vector3 baseScale = Vector3.one;
    Action<FloatingPopup> release;
    Camera cam;

    void Awake()
    {
        baseScale = transform.localScale;
        if (baseScale.sqrMagnitude < 0.0001f) baseScale = Vector3.one;
    }

    public void Play(Vector3 pos, string msg, Sprite sprite, Color color, Action<FloatingPopup> onDone)
    {
        transform.position = pos;
        transform.localScale = baseScale * startScale;
        t = 0f;
        release = onDone;
        if (text != null) { text.text = msg; text.color = color; }
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            icon.color = Color.white;
        }
        gameObject.SetActive(true);
    }

    void LateUpdate()
    {
        t += Time.deltaTime;
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        if (popDuration > 0.001f && t < popDuration)
        {
            float p = Mathf.Clamp01(t / popDuration);
            float s = Mathf.Lerp(startScale, peakScale, Mathf.Sin(p * Mathf.PI * 0.5f));
            transform.localScale = baseScale * s;
        }
        else
        {
            float settleT = Mathf.Clamp01((t - popDuration) / 0.12f);
            transform.localScale = baseScale * Mathf.Lerp(peakScale, 1f, settleT);
        }

        if (cam == null) cam = Camera.main;
        if (cam != null) transform.rotation = cam.transform.rotation;

        float a = 1f - Mathf.InverseLerp(lifetime * fadeStartRatio, lifetime, t);
        if (text != null) text.alpha = a;
        if (icon != null) icon.color = new Color(1f, 1f, 1f, a);

        if (t >= lifetime)
        {
            transform.localScale = baseScale;
            gameObject.SetActive(false);
            if (release != null) release(this);
        }
    }
}
