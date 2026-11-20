using System;
using System.Collections.Generic;
using UnityEngine;

public enum PopupKind { Stamina, Cash }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene")]
    public Transform respawnPoint;

    [Header("Giá trị khởi đầu")]
    public double startCash = 500;
    public float startStamina = 10f;
    public bool debugKeys = true;
    [Tooltip("Tùy chọn: gán 4 DinoData tương ứng phím F3 (Small), F4 (Medium), F5 (Large), F6 (Giant) để test vào ô Q.")]
    public DinoData[] debugMountDinos = new DinoData[4];

    [Header("Prefab chữ nổi + số bay lên")]
    public WorldLabel worldLabelPrefab;
    public FloatingPopup popupPrefab;

    [Header("Popup +Stamina / +$")]
    public Sprite staminaIcon;
    public Sprite cashIcon;
    public Color staminaColor = new Color(1f, 0.9f, 0.2f);
    public Color cashColor = new Color(0.4f, 1f, 0.4f);

    public double Cash { get; private set; }
    public float Stamina { get; private set; }
    public Inventory Inventory { get; private set; }

    public event Action StatsChanged;

    readonly Stack<FloatingPopup> pool = new Stack<FloatingPopup>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
#if UNITY_ANDROID || UNITY_IOS
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
#endif
        Inventory = new Inventory();
        Cash = startCash;
        Stamina = startStamina;
    }

    void Update()
    {
        if (!debugKeys) return;
        double cashAmt = HudController.Instance != null ? HudController.Instance.cheatCashAmount : 1000;
        float stamAmt = HudController.Instance != null ? HudController.Instance.cheatStaminaAmount : 10f;
        if (Input.GetKeyDown(KeyCode.F1)) AddCash(cashAmt);
        if (Input.GetKeyDown(KeyCode.F2)) AddStamina(stamAmt);
        if (Input.GetKeyDown(KeyCode.F3)) DebugMountBySize(ObjectSize.Small);
        if (Input.GetKeyDown(KeyCode.F4)) DebugMountBySize(ObjectSize.Medium);
        if (Input.GetKeyDown(KeyCode.F5)) DebugMountBySize(ObjectSize.Large);
        if (Input.GetKeyDown(KeyCode.F6)) DebugMountBySize(ObjectSize.Giant);
    }

    public void DebugMountBySize(ObjectSize targetSize)
    {
        MountController mount = MountController.Instance;
        if (mount == null) return;

        int idx = (int)targetSize;
        DinoData chosen = (debugMountDinos != null && idx < debugMountDinos.Length) ? debugMountDinos[idx] : null;
        if (chosen == null)
        {
            DinoData[] loaded = Resources.FindObjectsOfTypeAll<DinoData>();
            for (int i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] != null && DinoSizeModifiers.ToObjectSize(loaded[i].modelScale) == targetSize)
                {
                    chosen = loaded[i];
                    break;
                }
            }
            if (chosen == null && loaded.Length > 0) chosen = loaded[0];
        }

        if (chosen == null)
        {
            chosen = ScriptableObject.CreateInstance<DinoData>();
            chosen.displayName = targetSize + " Dino";
            chosen.modelScale = 0.6f + idx * 0.45f;
            chosen.runSpeed = 8f + idx * 3f;
        }

        DinoSizeType debugSizeType = DinoSizeType.Normal;
        if (targetSize == ObjectSize.Small) debugSizeType = DinoSizeType.Tiny;
        else if (targetSize == ObjectSize.Large) debugSizeType = DinoSizeType.Huge;
        else if (targetSize == ObjectSize.Giant) debugSizeType = DinoSizeType.Colossal;

        mount.DebugSetSlotAndRide(new OwnedDino(chosen, null, DinoTrait.None, debugSizeType));
        HudController.Say("Slot Q: " + chosen.displayName + " (" + targetSize + ")", 1.5f);
    }

    public void AddCash(double amount) { Cash += amount; Fire(); }
    public void AddStamina(float amount) { Stamina += amount; Fire(); }

    public bool TrySpend(double amount)
    {
        if (Cash < amount) return false;
        Cash -= amount;
        Fire();
        return true;
    }

    public bool Sell(OwnedDino d)
    {
        if (d == null || !Inventory.Remove(d)) return false;
        AddCash(d.SellValue);
        HudController.Say("Sold " + d.Title + " for $" + NumberFormat.Short(d.SellValue));
        return true;
    }
    public WorldLabel CreateLabel(Transform target, Vector3 offset, string text)
    {
        if (worldLabelPrefab == null) return null;
        WorldLabel l = Instantiate(worldLabelPrefab);
        l.Init(target, offset);
        l.SetText(text);
        return l;
    }
    public void ShowPopup(Vector3 worldPos, string text, PopupKind kind)
    {
        if (popupPrefab == null) return;
        FloatingPopup p = pool.Count > 0 ? pool.Pop() : Instantiate(popupPrefab, transform);
        bool stamina = kind == PopupKind.Stamina;
        p.Play(worldPos, text, stamina ? staminaIcon : cashIcon, stamina ? staminaColor : cashColor, Release);
    }

    void Release(FloatingPopup p) { pool.Push(p); }

    void Fire() { if (StatsChanged != null) StatsChanged(); }
}
