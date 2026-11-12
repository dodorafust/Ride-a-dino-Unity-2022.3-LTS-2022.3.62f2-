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

    float t;
    Action<FloatingPopup> release;
    Camera cam;

    public void Play(Vector3 pos, string msg, Sprite sprite, Color color, Action<FloatingPopup> onDone)
    {
        transform.position = pos;
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

        if (cam == null) cam = Camera.main;
        if (cam != null) transform.rotation = cam.transform.rotation;

        float a = 1f - Mathf.InverseLerp(lifetime * 0.5f, lifetime, t);
        if (text != null) text.alpha = a;
        if (icon != null) icon.color = new Color(1f, 1f, 1f, a);

        if (t >= lifetime)
        {
            gameObject.SetActive(false);
            if (release != null) release(this);
        }
    }
}
