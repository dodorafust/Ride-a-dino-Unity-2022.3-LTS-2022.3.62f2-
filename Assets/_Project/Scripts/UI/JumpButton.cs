using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Nút nhảy / bắt Dino / nhảy chuyền. Gắn lên Image (hoặc Button) bên phải màn hình.
// Khi đang ở trên không và có Dino được nhắm -> icon đổi thành mũi tên xuống, bấm = bắt ngay.
public class JumpButton : MonoBehaviour, IPointerDownHandler
{
    [Tooltip("Icon nhảy (tuỳ chọn). Để trống sẽ dùng chữ ↑")]
    public Sprite jumpIcon;
    [Tooltip("Icon bắt / mũi tên xuống (tuỳ chọn). Để trống sẽ dùng chữ ↓")]
    public Sprite catchIcon;
    public Color normalTint = Color.white;
    public Color catchTint = new Color(1f, 0.85f, 0.15f, 1f);
    public float iconSize = 0.55f;                 // tỉ lệ so với kích thước nút

    Image bg, icon;
    Text glyph;
    bool lastReady, initialized;

    public void OnPointerDown(PointerEventData e) { GameInput.PressJump(); }

    void Awake()
    {
        bg = GetComponent<Image>();
        if (bg != null) normalTint = bg.color;

        RectTransform self = transform as RectTransform;
        float size = self != null ? Mathf.Min(self.rect.width, self.rect.height) : 100f;

        if (jumpIcon != null || catchIcon != null)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            icon = go.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            SetupRect(icon.rectTransform);
        }
        else
        {
            var go = new GameObject("Glyph", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(transform, false);
            glyph = go.GetComponent<Text>();
            glyph.raycastTarget = false;
            glyph.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            glyph.alignment = TextAnchor.MiddleCenter;
            glyph.fontStyle = FontStyle.Bold;
            glyph.fontSize = Mathf.Max(20, Mathf.RoundToInt(size * iconSize));
            glyph.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            glyph.horizontalOverflow = HorizontalWrapMode.Overflow;
            glyph.verticalOverflow = VerticalWrapMode.Overflow;
            SetupRect(glyph.rectTransform);
        }
    }

    void SetupRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        float pad = (1f - iconSize) * 0.5f;
        rt.anchorMin = new Vector2(pad, pad);
        rt.anchorMax = new Vector2(1f - pad, 1f - pad);
    }

    void Update()
    {
        PlayerController p = PlayerController.Instance;
        bool ready = p != null && p.CatchReady;
        if (initialized && ready == lastReady) return;
        initialized = true;
        lastReady = ready;

        if (bg != null) bg.color = ready ? catchTint : normalTint;
        if (icon != null)
        {
            Sprite s = ready ? catchIcon : jumpIcon;
            icon.sprite = s;
            icon.enabled = s != null;
        }
        if (glyph != null) glyph.text = ready ? "\u2193" : "\u2191";
    }
}
