using System;
using System.Collections.Generic;
using UnityEngine;

public enum PopupKind { Stamina, Cash }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene")]
    public Transform respawnPoint;

    [Header("Giá trị khởi đầu (để test)")]
    public double startCash = 500;
    public float startStamina = 10f;
    public bool debugKeys = true;              // F1: +1000 cash, F2: +10 stamina

    [Header("Prefab chữ nổi (chỉnh bằng tay trong prefab)")]
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
        Inventory = new Inventory();
        Cash = startCash;
        Stamina = startStamina;
    }

    void Update()
    {
        if (!debugKeys) return;
        if (Input.GetKeyDown(KeyCode.F1)) AddCash(1000);
        if (Input.GetKeyDown(KeyCode.F2)) AddStamina(10f);
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

    // Tạo chữ nổi bám theo target (tên Dino, bảng nâng cấp...). Trả về null nếu chưa gán prefab.
    public WorldLabel CreateLabel(Transform target, Vector3 offset, string text)
    {
        if (worldLabelPrefab == null) return null;
        WorldLabel l = Instantiate(worldLabelPrefab);
        l.Init(target, offset);
        l.SetText(text);
        return l;
    }

    // Số bay lên (+Stamina / +$)
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
