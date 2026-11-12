using UnityEngine;

// Bảng nâng cấp (dùng cho chuồng và thú nhún). Kéo Pen hoặc DinoMachine vào ô Target.
// Bảng cần có Collider để chạm vào.
public class UpgradeSign : MonoBehaviour, IClickable
{
    public MonoBehaviour target;               // Pen hoặc DinoMachine (phải implement IUpgradable)
    public float interactRange = 12f;
    public Vector3 labelOffset = new Vector3(0f, 4.4f, 0f);

    IUpgradable up;
    WorldLabel label;

    void Start()
    {
        up = target as IUpgradable;
        if (up == null) { Debug.LogWarning("UpgradeSign: Target không phải Pen / DinoMachine.", this); return; }

        if (GameManager.Instance != null)
        {
            label = GameManager.Instance.CreateLabel(transform, labelOffset, "");
            GameManager.Instance.StatsChanged += Refresh;
        }
        up.Changed += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (up != null) up.Changed -= Refresh;
        if (GameManager.Instance != null) GameManager.Instance.StatsChanged -= Refresh;
    }

    void Refresh()
    {
        if (label == null || up == null) return;
        if (up.IsMaxLevel) { label.SetText("UPGRADE " + up.UpgradeName + "\nMAX LEVEL"); return; }

        bool afford = GameManager.Instance != null && GameManager.Instance.Cash >= up.UpgradeCost;
        label.SetText("UPGRADE " + up.UpgradeName + "\nLv " + up.Level + " > " + (up.Level + 1) +
                      "  (" + up.NextLevelInfo + ")\n" +
                      "<color=#" + (afford ? "55FF55" : "FF5555") + ">$" + NumberFormat.Short(up.UpgradeCost) + "</color>");
    }

    public void OnClicked(PlayerController player)
    {
        if (up == null) return;
        if (Vector3.Distance(player.transform.position, transform.position) > interactRange)
        {
            HudController.Say("Get closer to the sign");
            return;
        }
        up.TryUpgrade();
    }
}
