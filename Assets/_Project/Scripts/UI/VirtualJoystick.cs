using UnityEngine;
using UnityEngine.EventSystems;

// Joystick ảo bên trái. Gắn vào vùng nhận chạm (Image trong suốt, Pivot = 0.5, 0.5).
// Base và Handle là Image bạn tự đổi hình / kích thước / vị trí mặc định.
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform area;          // vùng nhận chạm (mặc định = chính object này)
    public RectTransform baseImage;     // vòng nền (con của area, anchor giữa)
    public RectTransform handle;        // núm (con của baseImage, anchor giữa)
    public float radius = 110f;         // bán kính kéo (đơn vị canvas)
    public bool dynamicOrigin = true;   // true: vòng nền xuất hiện ngay chỗ ngón tay chạm

    Vector2 defaultPos;
    int pointerId = -100;

    void Awake()
    {
        if (area == null) area = (RectTransform)transform;
        if (baseImage != null) defaultPos = baseImage.anchoredPosition;
    }

    void OnDisable() { Release(); }

    public void OnPointerDown(PointerEventData e)
    {
        if (pointerId != -100 || baseImage == null) return;
        pointerId = e.pointerId;
        if (dynamicOrigin)
        {
            Vector2 lp;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out lp))
                baseImage.anchoredPosition = lp;
        }
        Drag(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (e.pointerId != pointerId) return;
        Drag(e);
    }

    void Drag(PointerEventData e)
    {
        if (baseImage == null || handle == null) return;
        Vector2 lp;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out lp)) return;
        Vector2 clamped = Vector2.ClampMagnitude(lp - baseImage.anchoredPosition, radius);
        handle.anchoredPosition = clamped;
        GameInput.JoystickMove = clamped / radius;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.pointerId != pointerId) return;
        Release();
    }

    void Release()
    {
        pointerId = -100;
        GameInput.JoystickMove = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
        if (baseImage != null) baseImage.anchoredPosition = defaultPos;
    }
}
