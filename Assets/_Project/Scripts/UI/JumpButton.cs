using UnityEngine;
using UnityEngine.EventSystems;

// Nút nhảy / bắt Dino / nhảy chuyền. Gắn lên Image (hoặc Button) bên phải màn hình.
public class JumpButton : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData e) { GameInput.PressJump(); }
}
