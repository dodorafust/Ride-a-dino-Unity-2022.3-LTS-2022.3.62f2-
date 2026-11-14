using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Vùng kéo để xoay camera (lái Dino khi đang cưỡi), chụm 2 ngón để zoom, chạm ngắn để tương tác thế giới.
// Gắn lên Image trong suốt phủ toàn màn hình, đặt DƯỚI cùng trong Canvas (các nút khác nằm trên nó).
public class LookPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public float degreesPerInch = 75f;             // số độ xoay mỗi 1 inch (2.54cm) vuốt tay, đồng nhất giữa tablet và mobile
    public float pinchSensitivity = 0.02f;
    public float tapMaxMovePixels = 24f;
    public float tapMaxSeconds = 0.35f;

    readonly Dictionary<int, Vector2> pointers = new Dictionary<int, Vector2>();
    Vector2 downPos;
    float downTime;
    bool maybeTap;
    bool skipNextDelta;
    float lastPinch;

    void OnDisable()
    {
        pointers.Clear();
        maybeTap = false;
        skipNextDelta = false;
        lastPinch = 0f;
    }

    public void OnPointerDown(PointerEventData e)
    {
        pointers[e.pointerId] = e.position;
        if (pointers.Count == 1)
        {
            downPos = e.position;
            downTime = Time.unscaledTime;
            maybeTap = true;
            skipNextDelta = false;
        }
        else
        {
            maybeTap = false;
            skipNextDelta = true;
            lastPinch = PinchDistance();
        }
    }

    public void OnDrag(PointerEventData e)
    {
        Vector2 prevPos;
        if (!pointers.TryGetValue(e.pointerId, out prevPos)) return;
        Vector2 delta = e.position - prevPos;
        pointers[e.pointerId] = e.position;

        if (pointers.Count >= 2)
        {
            float d = PinchDistance();
            if (lastPinch > 0f) GameInput.AddZoom(-(d - lastPinch) * pinchSensitivity);
            lastPinch = d;
            skipNextDelta = true;
            return;
        }

        if (skipNextDelta)
        {
            skipNextDelta = false;
            return;
        }

        if (maybeTap && (e.position - downPos).magnitude > tapMaxMovePixels)
        {
            maybeTap = false;
            return; // bỏ qua cú nhảy pixelDragThreshold ở frame đầu tiên
        }

        float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
        float scale = degreesPerInch / dpi;
        GameInput.AddLook(delta * scale);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!pointers.Remove(e.pointerId)) return;

        if (pointers.Count == 0)
        {
            bool quick = Time.unscaledTime - downTime <= tapMaxSeconds;
            bool still = (e.position - downPos).magnitude <= tapMaxMovePixels;
            if (maybeTap && quick && still) WorldTapper.Tap(e.position);
            maybeTap = false;
        }
        lastPinch = 0f;
    }

    float PinchDistance()
    {
        Vector2 a = Vector2.zero, b = Vector2.zero;
        int i = 0;
        foreach (KeyValuePair<int, Vector2> kv in pointers)
        {
            if (i == 0) a = kv.Value;
            else { b = kv.Value; break; }
            i++;
        }
        return Vector2.Distance(a, b);
    }
}
