using UnityEngine;

public class JoystickBridge : MonoBehaviour
{
    public Joystick joystick;

    void Reset() { joystick = GetComponent<Joystick>(); }

    void Update()
    {
        GameInput.JoystickMove = joystick != null ? joystick.Direction : Vector2.zero;
    }

    void OnDisable()
    {
        GameInput.JoystickMove = Vector2.zero;
    }
}