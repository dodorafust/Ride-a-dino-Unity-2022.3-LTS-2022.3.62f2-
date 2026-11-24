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

    [Header("Icon tay nhấn (thay nút E)")]
    public Sprite tapHandSprite;
    public Vector2 handIconSize = new Vector2(46f, 46f);

    [Header("Vị trí hiển thị nút Ride")]
    [Tooltip("Bật để đặt nút Ride ở chân Dino (dùng Feet Offset + Screen Offset). Tắt để dùng World Offset.")]
    public bool placeAtFeet = true;
    public Vector3 feetOffset = new Vector3(0f, 0.1f, 0f);
    public Vector2 screenOffset = new Vector2(0f, -60f);
    public Vector3 worldOffset = new Vector3(0f, 2.8f, 0f);

    Transform target;
    Action onTap;
    Camera cam;

    void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(Tap);
            UIEffectUtil.AttachButtonBounce(this, button);
        }
        SetupHandIcon();
    }

    void SetupHandIcon()
    {
        Transform keyIcon = transform.Find("KeyIcon");
        if (keyIcon == null) return;

        Transform keyText = keyIcon.Find("KeyText");
        if (keyText != null) keyText.gameObject.SetActive(false);

        Transform existingHand = keyIcon.Find("HandIcon");
        Image handImg = existingHand != null ? existingHand.GetComponent<Image>() : null;
        if (handImg == null)
        {
            GameObject go = new GameObject("HandIcon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(keyIcon, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = handIconSize;
            rt.anchoredPosition = Vector2.zero;
            handImg = go.GetComponent<Image>();
            handImg.raycastTarget = false;
        }
        else
        {
            handImg.rectTransform.sizeDelta = handIconSize;
        }

        Sprite sp = tapHandSprite != null ? tapHandSprite : LoadDefaultHandSprite();
        if (sp != null)
        {
            handImg.sprite = sp;
            handImg.preserveAspect = true;
            handImg.color = Color.white;
        }
    }

    static Sprite cachedHandSprite;
    static Sprite LoadDefaultHandSprite()
    {
        if (cachedHandSprite != null) return cachedHandSprite;
        cachedHandSprite = Resources.Load<Sprite>("UI/TapHandIcon");
        if (cachedHandSprite != null) return cachedHandSprite;

        Texture2D tex = Resources.Load<Texture2D>("UI/TapHandIcon");
        if (tex != null)
            cachedHandSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        return cachedHandSprite;
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

        Vector3 activeOffset = placeAtFeet ? feetOffset : worldOffset;
        Vector3 sp = cam.WorldToScreenPoint(target.position + activeOffset);
        if (sp.z < 0f) return;
        transform.position = new Vector3(sp.x + screenOffset.x, sp.y + screenOffset.y, 0f);
    }
}
