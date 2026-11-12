using UnityEngine;
using UnityEngine.UI;

// 2 vòng (Stamina xanh, Tame hồng) hiện cạnh nhân vật khi cưỡi Dino hoang.
// Gắn lên object gốc "RideRings"; các vòng là con của nó, bạn tự dời vị trí / đổi hình.
// Image Fill phải để Image Type = Filled, Radial 360 (builder đã đặt sẵn).
public class RideRingsUI : MonoBehaviour
{
    public Image staminaFill;
    public Image tameFill;
    public Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);

    public Color staminaHigh = new Color(0.25f, 0.85f, 0.3f);
    public Color staminaMid = new Color(1f, 0.85f, 0.1f);
    public Color staminaLow = new Color(0.9f, 0.2f, 0.25f);
    public Color tameColor = new Color(1f, 0.4f, 0.65f);
    public Color tameDoneColor = new Color(1f, 0.85f, 0.1f);

    public void Tick(PlayerController player, RideSession ride, Camera cam)
    {
        bool show = ride != null && ride.Active && player != null && cam != null;
        if (gameObject.activeSelf != show) gameObject.SetActive(show);
        if (!show) return;

        Vector3 sp = cam.WorldToScreenPoint(player.transform.position + worldOffset);
        if (sp.z < 0f) return;
        transform.position = new Vector3(sp.x, sp.y, 0f);

        float s = ride.Stamina01;
        if (staminaFill != null)
        {
            staminaFill.fillAmount = s;
            staminaFill.color = s > 0.5f ? staminaHigh : (s > 0.25f ? staminaMid : staminaLow);
        }
        if (tameFill != null)
        {
            tameFill.fillAmount = ride.Tame01;
            tameFill.color = ride.Tamed ? tameDoneColor : tameColor;
        }
    }
}
