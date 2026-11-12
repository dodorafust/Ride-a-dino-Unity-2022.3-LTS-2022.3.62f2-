using UnityEngine;

// Gom input từ cảm ứng (joystick, nút, kéo màn hình) và bàn phím/chuột (để test trên PC).
public static class GameInput
{
    public static Vector2 JoystickMove;    // VirtualJoystick ghi vào
    static Vector2 look;
    static float zoom;
    static bool jumpQueued;

    public static Vector2 Move
    {
        get
        {
            Vector2 kb = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Vector2 m = JoystickMove.sqrMagnitude > 0.01f ? JoystickMove : kb;
            return Vector2.ClampMagnitude(m, 1f);
        }
    }

    public static void PressJump() { jumpQueued = true; }

    public static bool ConsumeJump()
    {
        bool v = jumpQueued || Input.GetButtonDown("Jump");
        jumpQueued = false;
        return v;
    }

    public static void AddLook(Vector2 degrees) { look += degrees; }
    public static Vector2 ConsumeLook() { Vector2 v = look; look = Vector2.zero; return v; }

    public static void AddZoom(float amount) { zoom += amount; }
    public static float ConsumeZoom() { float v = zoom; zoom = 0f; return v; }
}
