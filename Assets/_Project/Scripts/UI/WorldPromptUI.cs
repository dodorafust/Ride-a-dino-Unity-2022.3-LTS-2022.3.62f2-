using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Ô chữ nhật "E | Tên Dino | Ride" bám trên đầu Dino trong chuồng. Chạm vào ô để cưỡi.
// Là object trong Canvas (bạn tự chỉnh hình / chữ). Gắn lên object gốc của ô, kéo Button + 2 Text vào.
public class WorldPromptUI : MonoBehaviour
{
    public Button button;
    public TMP_Text titleText;
    public TMP_Text actionText;
    public Vector3 worldOffset = new Vector3(0f, 2.8f, 0f);

    Transform target;
    Action onTap;
    Camera cam;

    void Awake()
    {
        if (button != null) button.onClick.AddListener(Tap);
    }

    public void Show(Transform t, string title, string action, Action tap)
    {
        target = t;
        onTap = tap;
        if (titleText != null) titleText.text = title;
        if (actionText != null) actionText.text = action;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        target = null;
        onTap = null;
        gameObject.SetActive(false);
    }

    void Tap()
    {
        if (onTap != null) onTap();
    }

    void LateUpdate()
    {
        if (target == null) { gameObject.SetActive(false); return; }
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(target.position + worldOffset);
        if (sp.z < 0f) return;
        transform.position = new Vector3(sp.x, sp.y, 0f);
    }
}
