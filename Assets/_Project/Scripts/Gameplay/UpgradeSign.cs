using UnityEngine;

// Bảng nâng cấp (dùng cho chuồng và thú nhún). Kéo Pen hoặc DinoMachine vào ô Target.
// Bảng cần có Collider để chạm vào.
public class UpgradeSign : MonoBehaviour, IClickable
{
    public MonoBehaviour target;               // Pen hoặc DinoMachine (phải implement IUpgradable)
    public float interactRange = 12f;
    public Vector3 labelOffset = new Vector3(0f, 4.4f, 0f);
    [Tooltip("Gắn SignBoardUI (World Space Canvas trên mặt biển). Để trống sẽ dùng chữ lơ lửng WorldLabel.")]
    public SignBoardUI boardUI;

    IUpgradable up;
    WorldLabel label;
    SignBoardUI[] allBoardUIs;

    void Start()
    {
        up = target as IUpgradable;
        if (up == null) { Debug.LogWarning("UpgradeSign: Target không phải Pen / DinoMachine.", this); return; }

        CollectBoardUIs();
        bool hasBoardUI = allBoardUIs != null && allBoardUIs.Length > 0;

        if (GameManager.Instance != null)
        {
            if (!hasBoardUI)
                label = GameManager.Instance.CreateLabel(transform, labelOffset, "");
            GameManager.Instance.StatsChanged += Refresh;
        }
        up.Changed += Refresh;
        Refresh();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;
        RefreshPreviewInEditor();
    }
#endif

    [ContextMenu("Cập nhật hiển thị biển ngay")]
    public void RefreshPreviewInEditor()
    {
        up = target as IUpgradable;
        if (up == null) return;
        CollectBoardUIs();
        Refresh();
    }

    void CollectBoardUIs()
    {
        allBoardUIs = GetComponentsInChildren<SignBoardUI>(true);
        if (boardUI != null && System.Array.IndexOf(allBoardUIs, boardUI) < 0)
        {
            var list = new System.Collections.Generic.List<SignBoardUI>(allBoardUIs);
            list.Add(boardUI);
            allBoardUIs = list.ToArray();
        }
    }

    void OnDestroy()
    {
        if (up != null) up.Changed -= Refresh;
        if (GameManager.Instance != null) GameManager.Instance.StatsChanged -= Refresh;
    }

    void Refresh()
    {
        if (up == null) return;
        bool afford = GameManager.Instance != null ? GameManager.Instance.Cash >= up.UpgradeCost : true;
        DinoMachine dm = target as DinoMachine;
        string detailOverride = dm != null ? dm.NextDisplayName : null;
        Sprite iconOverride = dm != null ? dm.NextIcon : null;

        if (allBoardUIs != null && allBoardUIs.Length > 0)
        {
            for (int i = 0; i < allBoardUIs.Length; i++)
            {
                if (allBoardUIs[i] != null)
                    allBoardUIs[i].SetUpgradeInfo(up.UpgradeName, up.Level, up.NextLevelInfo, up.UpgradeCost,
                                                  afford, up.IsMaxLevel, detailOverride, iconOverride);
            }
            return;
        }

        if (label == null) return;
        if (up.IsMaxLevel) { label.SetText("UPGRADE " + up.UpgradeName + "\nMAX LEVEL"); return; }

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
