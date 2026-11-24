using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Bảng Dino Index: theo dõi toàn bộ Dino đã mở khóa theo từng Biome & ma trận Variants (Variant x Size),
// cho phép nhận thưởng Tiền + Stamina theo từng loài, từng bộ Size/Variant, từng Biome và Claim All.
public class DinoIndexPopup : MonoBehaviour
{
    public static DinoIndexPopup Instance { get; private set; }

    [Header("Dữ liệu hiển thị (để trống sẽ tự lấy từ BiomeManager / ScriptableObjects)")]
    public List<BiomeData> biomes = new List<BiomeData>();
    public List<DinoVariant> allVariants = new List<DinoVariant>();
    public List<DinoSizeAsset> allSizes = new List<DinoSizeAsset>();

    [Header("Hiệu ứng & Nền khóa cảm ứng")]
    public bool hideOnStart = true;
    public bool closeOnBackdropClick = true;
    public Color modalDimColor = new Color(0f, 0f, 0f, 0.62f);
    public float openBounceDuration = 0.28f;

    [Header("Nội dung chữ")]
    public string windowTitle = "Dino Index";
    public string biomeClaimText = "CLAIM REWARD!";
    public string biomeCompletedText = "COMPLETED!";

    public bool Visible { get { return panelRoot != null && panelRoot.gameObject.activeSelf; } }
    public int TotalUnclaimedCount { get; private set; }
    public event Action StateChanged;

    // Trạng thái bộ sưu tập (trong phiên chơi; hỗ trợ cả scan từ Inventory & Pen)
    readonly HashSet<DinoData> unlockedSpecies = new HashSet<DinoData>();
    readonly HashSet<string> unlockedCombos = new HashSet<string>();
    readonly HashSet<DinoData> claimedSpecies = new HashSet<DinoData>();
    readonly HashSet<DinoData> claimedAllSizes = new HashSet<DinoData>();
    readonly HashSet<DinoData> claimedAllVariants = new HashSet<DinoData>();
    readonly HashSet<BiomeData> claimedBiomes = new HashSet<BiomeData>();

    readonly List<DinoData> allSpecies = new List<DinoData>();
    readonly Dictionary<BiomeData, List<DinoData>> biomeSpeciesMap = new Dictionary<BiomeData, List<DinoData>>();
    readonly Dictionary<DinoData, DinoSpawnTable> speciesTableMap = new Dictionary<DinoData, DinoSpawnTable>();

    DinoData selectedDino;
    bool showingVariants;
    bool initialized;
    bool dataLoaded;
    bool subscribed;
    GameManager gm;
    Coroutine popRoutine;

    [Header("Tham chiếu UI (Dựng sẵn trong Scene để chỉnh sửa)")]
    public Image modalBackdrop;
    public RectTransform panelRoot;
    public Button closeButton;
    public Button claimAllButton;
    public TMP_Text claimAllText;
    public GameObject biomeScrollGo;
    public RectTransform biomeScrollContent;
    public GameObject variantsPanelGo;
    public RectTransform variantsContent;
    public Image totalProgressFill;
    public TMP_Text totalProgressText;

    [Header("Tham chiếu Detail Panel (bên phải)")]
    public Image detailCardBg;
    public Image detailIcon;
    public TMP_Text detailNameText;
    public TMP_Text detailRarityText;
    public TMP_Text detailRateText;
    public Button detailVariantsBtn;
    public TMP_Text detailVariantsText;
    public TMP_Text detailCashText;
    public TMP_Text detailStaminaText;
    public Button detailClaimBtn;
    public TMP_Text detailClaimText;
    public Image detailCashIcon;
    public Image detailStaminaIcon;

    bool wired;

    static Sprite fallbackDinoSprite;
    static Sprite bookSprite;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        EnsureInitialized();
        if (hideOnStart && panelRoot != null) Hide();
    }

    void OnDestroy()
    {
        if (gm != null && subscribed)
            gm.Inventory.Changed -= OnInventoryChanged;
        subscribed = false;
    }

    public void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;
        EnsureDataLoaded();
        EnsureBuilt();
        WireButtons();
        TrySubscribeInventory();
        ScanExistingOwnedDinos();
        RecalculateUnclaimedCount();
    }

    [ContextMenu("Dựng sẵn UI ra Scene để chỉnh sửa")]
    public void BuildInEditor()
    {
        dataLoaded = false;
        EnsureDataLoaded();
        EnsureBuilt();
        WireButtons();
        if (modalBackdrop != null) modalBackdrop.gameObject.SetActive(true);
        if (panelRoot != null) panelRoot.gameObject.SetActive(true);
        RefreshUI();
    }

    void WireButtons()
    {
        if (wired) return;
        if (closeButton == null && claimAllButton == null && detailClaimBtn == null) return;
        wired = true;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
            UIEffectUtil.AttachButtonBounce(this, closeButton);
        }
        if (claimAllButton != null)
        {
            claimAllButton.onClick.RemoveAllListeners();
            claimAllButton.onClick.AddListener(ClaimAll);
            UIEffectUtil.AttachButtonBounce(this, claimAllButton);
        }
        if (detailVariantsBtn != null)
        {
            detailVariantsBtn.onClick.RemoveAllListeners();
            detailVariantsBtn.onClick.AddListener(ToggleVariantsView);
            UIEffectUtil.AttachButtonBounce(this, detailVariantsBtn);
        }
        if (detailClaimBtn != null)
        {
            detailClaimBtn.onClick.RemoveAllListeners();
            detailClaimBtn.onClick.AddListener(delegate { if (selectedDino != null) ClaimSpecies(selectedDino); });
            UIEffectUtil.AttachButtonBounce(this, detailClaimBtn);
        }
        if (modalBackdrop != null)
        {
            Button bdBtn = modalBackdrop.GetComponent<Button>();
            if (bdBtn != null)
            {
                bdBtn.onClick.RemoveAllListeners();
                bdBtn.onClick.AddListener(delegate { if (closeOnBackdropClick) Hide(); });
            }
        }
    }

    public void Toggle()
    {
        EnsureInitialized();
        if (Visible) Hide();
        else Open();
    }

    public void Open()
    {
        EnsureInitialized();
        EnsureBuilt();
        WireButtons();
        if (PenPanel.Instance != null) PenPanel.Instance.Hide();
        if (InventoryPopup.Instance != null) InventoryPopup.Instance.Hide();
        if (SellDinosPopup.Instance != null) SellDinosPopup.Instance.Hide();

        ScanExistingOwnedDinos();
        RecalculateUnclaimedCount();

        if (modalBackdrop != null)
        {
            modalBackdrop.color = modalDimColor;
            modalBackdrop.gameObject.SetActive(true);
            if (panelRoot != null && modalBackdrop.transform.parent == panelRoot.parent)
                modalBackdrop.transform.SetAsLastSibling();
            else
                transform.SetAsLastSibling();
        }
        if (panelRoot != null)
        {
            panelRoot.gameObject.SetActive(true);
            panelRoot.SetAsLastSibling();
            popRoutine = UIEffectUtil.PlaySpringPop(this, panelRoot, popRoutine, openBounceDuration);
        }

        if (selectedDino == null && allSpecies.Count > 0)
            selectedDino = allSpecies[0];
        RefreshUI();
    }

    public void Hide()
    {
        showingVariants = false;
        if (panelRoot != null) panelRoot.gameObject.SetActive(false);
        if (modalBackdrop != null) modalBackdrop.gameObject.SetActive(false);
    }

    public void RecordDinoUnlocked(OwnedDino d)
    {
        if (d == null || d.data == null) return;
        EnsureDataLoaded();
        bool changed = RegisterOwnedInternal(d);
        if (changed)
        {
            RecalculateUnclaimedCount();
            if (StateChanged != null) StateChanged();
            if (Visible) RefreshUI();
        }
    }

    void OnInventoryChanged()
    {
        bool changed = ScanExistingOwnedDinos();
        if (changed)
        {
            RecalculateUnclaimedCount();
            if (StateChanged != null) StateChanged();
        }
        if (Visible) RefreshUI();
    }

    void TrySubscribeInventory()
    {
        gm = GameManager.Instance;
        if (gm != null && !subscribed)
        {
            gm.Inventory.Changed += OnInventoryChanged;
            subscribed = true;
        }
    }

    bool ScanExistingOwnedDinos()
    {
        bool changed = false;
        if (gm == null) gm = GameManager.Instance;
        if (gm != null && gm.Inventory != null)
        {
            var items = gm.Inventory.Items;
            for (int i = 0; i < items.Count; i++)
                if (RegisterOwnedInternal(items[i])) changed = true;
        }
        Pen pen = FindObjectOfType<Pen>();
        if (pen != null && pen.Active != null)
        {
            var active = pen.Active;
            for (int i = 0; i < active.Count; i++)
                if (RegisterOwnedInternal(active[i])) changed = true;
        }
        return changed;
    }

    bool RegisterOwnedInternal(OwnedDino d)
    {
        if (d == null || d.data == null) return false;
        bool added = unlockedSpecies.Add(d.data);
        string key = GetComboKey(d.data, d.variant, d.sizeAsset, d.sizeType);
        if (unlockedCombos.Add(key)) added = true;

        // Nếu loài này chưa nằm trong danh sách biome (vd dino debug), vẫn đưa vào allSpecies để đếm đúng
        if (!allSpecies.Contains(d.data))
            allSpecies.Add(d.data);
        return added;
    }

    string GetVariantKey(DinoVariant v)
    {
        if (v == null || v.isNormal) return "NORMAL";
        return v.name;
    }

    string GetSizeKey(DinoSizeAsset sa, DinoSizeType st)
    {
        if (sa != null) return sa.isNormal ? "Size_Normal" : sa.name;
        return "Size_" + st.ToString();
    }

    string GetComboKey(DinoData dino, DinoVariant v, DinoSizeAsset sa, DinoSizeType st)
    {
        string dn = dino != null ? dino.name : "null";
        string vk = GetVariantKey(v);
        string sk = GetSizeKey(sa, st);
        // Khớp cả trường hợp asset tên "Size_Normal" hoặc enum Normal
        if (sa == null && allSizes.Count > 0)
        {
            for (int i = 0; i < allSizes.Count; i++)
            {
                if (allSizes[i] != null && allSizes[i].legacyType == st)
                {
                    sk = allSizes[i].isNormal ? "Size_Normal" : allSizes[i].name;
                    break;
                }
            }
        }
        return dn + "|" + vk + "|" + sk;
    }

    bool IsComboUnlocked(DinoData dino, DinoVariant v, DinoSizeAsset sa)
    {
        DinoSizeType st = sa != null ? sa.legacyType : DinoSizeType.Normal;
        return unlockedCombos.Contains(GetComboKey(dino, v, sa, st));
    }

    void EnsureDataLoaded()
    {
        if (dataLoaded) return;
        dataLoaded = true;

        if (biomes == null) biomes = new List<BiomeData>();
        if (biomes.Count == 0)
        {
            BiomeManager bm = BiomeManager.Instance != null ? BiomeManager.Instance : FindObjectOfType<BiomeManager>();
            if (bm != null && bm.Sections != null)
            {
                for (int i = 0; i < bm.Sections.Count; i++)
                {
                    BiomeSection sec = bm.Sections[i];
                    if (sec != null && sec.data != null && !biomes.Contains(sec.data))
                        biomes.Add(sec.data);
                }
            }
            if (biomes.Count == 0)
            {
                BiomeData[] found = Resources.FindObjectsOfTypeAll<BiomeData>();
                for (int i = 0; i < found.Length; i++)
                    if (found[i] != null && !biomes.Contains(found[i])) biomes.Add(found[i]);
#if UNITY_EDITOR
                if (biomes.Count == 0)
                {
                    string[] bGuids = UnityEditor.AssetDatabase.FindAssets("t:BiomeData");
                    for (int i = 0; i < bGuids.Length; i++)
                    {
                        BiomeData bd = UnityEditor.AssetDatabase.LoadAssetAtPath<BiomeData>(UnityEditor.AssetDatabase.GUIDToAssetPath(bGuids[i]));
                        if (bd != null && !biomes.Contains(bd)) biomes.Add(bd);
                    }
                    biomes.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
                }
#endif
            }
        }

        biomeSpeciesMap.Clear();
        speciesTableMap.Clear();
        allSpecies.Clear();

        var seenVariants = new List<DinoVariant>();
        var seenSizes = new List<DinoSizeAsset>();

        for (int b = 0; b < biomes.Count; b++)
        {
            BiomeData bd = biomes[b];
            if (bd == null) continue;
            var list = new List<DinoData>();
            if (bd.spawnTable != null)
            {
                if (bd.spawnTable.entries != null)
                {
                    for (int e = 0; e < bd.spawnTable.entries.Count; e++)
                    {
                        var en = bd.spawnTable.entries[e];
                        if (en == null || en.dino == null) continue;
                        if (!list.Contains(en.dino)) list.Add(en.dino);
                        if (!allSpecies.Contains(en.dino)) allSpecies.Add(en.dino);
                        if (!speciesTableMap.ContainsKey(en.dino)) speciesTableMap[en.dino] = bd.spawnTable;
                    }
                }
                if (bd.spawnTable.variants != null)
                {
                    for (int v = 0; v < bd.spawnTable.variants.Count; v++)
                    {
                        var vw = bd.spawnTable.variants[v];
                        if (vw != null && vw.variant != null && !seenVariants.Contains(vw.variant))
                            seenVariants.Add(vw.variant);
                    }
                }
                if (bd.spawnTable.sizes != null)
                {
                    for (int s = 0; s < bd.spawnTable.sizes.Count; s++)
                    {
                        var sw = bd.spawnTable.sizes[s];
                        if (sw != null && sw.sizeAsset != null && !seenSizes.Contains(sw.sizeAsset))
                            seenSizes.Add(sw.sizeAsset);
                    }
                }
            }
            biomeSpeciesMap[bd] = list;
        }

        if (allVariants.Count == 0)
        {
            if (seenVariants.Count == 0)
            {
                DinoVariant[] foundVars = Resources.FindObjectsOfTypeAll<DinoVariant>();
                for (int i = 0; i < foundVars.Length; i++)
                    if (foundVars[i] != null && !seenVariants.Contains(foundVars[i])) seenVariants.Add(foundVars[i]);
#if UNITY_EDITOR
                if (seenVariants.Count == 0)
                {
                    string[] vGuids = UnityEditor.AssetDatabase.FindAssets("t:DinoVariant");
                    for (int i = 0; i < vGuids.Length; i++)
                    {
                        DinoVariant dv = UnityEditor.AssetDatabase.LoadAssetAtPath<DinoVariant>(UnityEditor.AssetDatabase.GUIDToAssetPath(vGuids[i]));
                        if (dv != null && !seenVariants.Contains(dv)) seenVariants.Add(dv);
                    }
                }
#endif
            }
            seenVariants.Sort((a, b) =>
            {
                if (a.isNormal != b.isNormal) return a.isNormal ? -1 : 1;
                return a.moneyMultiplier.CompareTo(b.moneyMultiplier);
            });
            allVariants.AddRange(seenVariants);
        }

        if (allSizes.Count == 0)
        {
            if (seenSizes.Count == 0)
            {
                DinoSizeAsset[] foundSizes = Resources.FindObjectsOfTypeAll<DinoSizeAsset>();
                for (int i = 0; i < foundSizes.Length; i++)
                    if (foundSizes[i] != null && !seenSizes.Contains(foundSizes[i])) seenSizes.Add(foundSizes[i]);
#if UNITY_EDITOR
                if (seenSizes.Count == 0)
                {
                    string[] sGuids = UnityEditor.AssetDatabase.FindAssets("t:DinoSizeAsset");
                    for (int i = 0; i < sGuids.Length; i++)
                    {
                        DinoSizeAsset dsa = UnityEditor.AssetDatabase.LoadAssetAtPath<DinoSizeAsset>(UnityEditor.AssetDatabase.GUIDToAssetPath(sGuids[i]));
                        if (dsa != null && !seenSizes.Contains(dsa)) seenSizes.Add(dsa);
                    }
                }
#endif
            }
            seenSizes.Sort((a, b) => a.rankBonus.CompareTo(b.rankBonus));
            allSizes.AddRange(seenSizes);
        }

        if (selectedDino == null && allSpecies.Count > 0)
            selectedDino = allSpecies[0];
    }

    int GetTotalComboSlotsPerDino()
    {
        int v = Mathf.Max(1, allVariants.Count);
        int s = Mathf.Max(1, allSizes.Count);
        return v * s;
    }

    int CountCollectedCombos(DinoData dino)
    {
        if (dino == null) return 0;
        int count = 0;
        int vCount = allVariants.Count;
        int sCount = allSizes.Count;
        if (vCount > 0 && sCount > 0)
        {
            for (int v = 0; v < vCount; v++)
                for (int s = 0; s < sCount; s++)
                    if (IsComboUnlocked(dino, allVariants[v], allSizes[s])) count++;
        }
        return count;
    }

    int CountDistinctSizesForDino(DinoData dino)
    {
        if (dino == null) return 0;
        int count = 0;
        for (int s = 0; s < allSizes.Count; s++)
        {
            DinoSizeAsset sa = allSizes[s];
            bool any = false;
            if (allVariants.Count > 0)
            {
                for (int v = 0; v < allVariants.Count; v++)
                    if (IsComboUnlocked(dino, allVariants[v], sa)) { any = true; break; }
            }
            else
            {
                any = IsComboUnlocked(dino, null, sa);
            }
            if (any) count++;
        }
        return count;
    }

    int CountDistinctVariantsForDino(DinoData dino)
    {
        if (dino == null) return 0;
        int count = 0;
        for (int v = 0; v < allVariants.Count; v++)
        {
            DinoVariant va = allVariants[v];
            bool any = false;
            if (allSizes.Count > 0)
            {
                for (int s = 0; s < allSizes.Count; s++)
                    if (IsComboUnlocked(dino, va, allSizes[s])) { any = true; break; }
            }
            else
            {
                any = IsComboUnlocked(dino, va, null);
            }
            if (any) count++;
        }
        return count;
    }

    double GetBiomeRewardCash(BiomeData b)
    {
        if (b != null && b.indexCompleteCash > 0) return b.indexCompleteCash;
        double sum = 0;
        if (b != null && biomeSpeciesMap.TryGetValue(b, out List<DinoData> list))
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null) sum += list[i].EffectiveIndexRewardCash;
        }
        return System.Math.Max(500.0, System.Math.Round(sum * 2.5));
    }

    float GetBiomeRewardStamina(BiomeData b)
    {
        if (b != null && b.indexCompleteStamina > 0f) return b.indexCompleteStamina;
        float sum = 0f;
        if (b != null && biomeSpeciesMap.TryGetValue(b, out List<DinoData> list))
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null) sum += list[i].EffectiveIndexRewardStamina;
        }
        return Mathf.Max(25f, Mathf.Round(sum * 1.5f));
    }

    int CountUnlockedInBiome(BiomeData b, out int total)
    {
        total = 0;
        int unlocked = 0;
        if (b != null && biomeSpeciesMap.TryGetValue(b, out List<DinoData> list))
        {
            total = list.Count;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && unlockedSpecies.Contains(list[i])) unlocked++;
        }
        return unlocked;
    }

    void RecalculateUnclaimedCount()
    {
        int n = 0;
        foreach (DinoData d in unlockedSpecies)
        {
            if (d == null) continue;
            if (!claimedSpecies.Contains(d)) n++;
            if (allSizes.Count > 0 && !claimedAllSizes.Contains(d) && CountDistinctSizesForDino(d) >= allSizes.Count) n++;
            if (allVariants.Count > 0 && !claimedAllVariants.Contains(d) && CountDistinctVariantsForDino(d) >= allVariants.Count) n++;
        }
        for (int b = 0; b < biomes.Count; b++)
        {
            BiomeData bd = biomes[b];
            if (bd == null || claimedBiomes.Contains(bd)) continue;
            int u = CountUnlockedInBiome(bd, out int tot);
            if (tot > 0 && u >= tot) n++;
        }
        TotalUnclaimedCount = n;
    }

    void ClaimSpecies(DinoData d)
    {
        if (d == null || !unlockedSpecies.Contains(d) || !claimedSpecies.Add(d)) return;
        GiveReward(d.EffectiveIndexRewardCash, d.EffectiveIndexRewardStamina, d.displayName + " Index");
        AfterRewardClaimed();
    }

    void ClaimBiome(BiomeData b)
    {
        if (b == null || claimedBiomes.Contains(b)) return;
        int u = CountUnlockedInBiome(b, out int tot);
        if (tot <= 0 || u < tot) return;
        claimedBiomes.Add(b);
        GiveReward(GetBiomeRewardCash(b), GetBiomeRewardStamina(b), b.biomeName + " Complete!");
        AfterRewardClaimed();
    }

    void ClaimAllSizes(DinoData d)
    {
        if (d == null || allSizes.Count == 0 || claimedAllSizes.Contains(d)) return;
        if (CountDistinctSizesForDino(d) < allSizes.Count) return;
        claimedAllSizes.Add(d);
        GiveReward(d.EffectiveAllSizesRewardCash, 0f, d.displayName + " All Sizes");
        AfterRewardClaimed();
    }

    void ClaimAllVariants(DinoData d)
    {
        if (d == null || allVariants.Count == 0 || claimedAllVariants.Contains(d)) return;
        if (CountDistinctVariantsForDino(d) < allVariants.Count) return;
        claimedAllVariants.Add(d);
        GiveReward(d.EffectiveAllVariantsRewardCash, 0f, d.displayName + " All Variants");
        AfterRewardClaimed();
    }

    void ClaimAll()
    {
        EnsureDataLoaded();
        if (TotalUnclaimedCount <= 0) return;

        double totalCash = 0;
        float totalStamina = 0f;
        int claimedCount = 0;

        foreach (DinoData d in unlockedSpecies)
        {
            if (d == null) continue;
            if (claimedSpecies.Add(d))
            {
                totalCash += d.EffectiveIndexRewardCash;
                totalStamina += d.EffectiveIndexRewardStamina;
                claimedCount++;
            }
            if (allSizes.Count > 0 && CountDistinctSizesForDino(d) >= allSizes.Count && claimedAllSizes.Add(d))
            {
                totalCash += d.EffectiveAllSizesRewardCash;
                claimedCount++;
            }
            if (allVariants.Count > 0 && CountDistinctVariantsForDino(d) >= allVariants.Count && claimedAllVariants.Add(d))
            {
                totalCash += d.EffectiveAllVariantsRewardCash;
                claimedCount++;
            }
        }
        for (int b = 0; b < biomes.Count; b++)
        {
            BiomeData bd = biomes[b];
            if (bd == null || claimedBiomes.Contains(bd)) continue;
            int u = CountUnlockedInBiome(bd, out int tot);
            if (tot > 0 && u >= tot && claimedBiomes.Add(bd))
            {
                totalCash += GetBiomeRewardCash(bd);
                totalStamina += GetBiomeRewardStamina(bd);
                claimedCount++;
            }
        }

        if (claimedCount > 0)
            GiveReward(totalCash, totalStamina, "Claimed " + claimedCount + " Index rewards");
        AfterRewardClaimed();
    }

    void GiveReward(double cash, float stamina, string label)
    {
        if (gm == null) gm = GameManager.Instance;
        if (gm != null)
        {
            if (cash > 0) gm.AddCash(cash);
            if (stamina > 0f) gm.AddStamina(stamina);
        }
        string msg = label + ":";
        if (cash > 0) msg += " +$" + NumberFormat.Short(cash);
        if (stamina > 0f) msg += " +" + NumberFormat.Short(stamina) + " Stamina";
        HudController.Say(msg, 2.2f);
    }

    void AfterRewardClaimed()
    {
        RecalculateUnclaimedCount();
        if (StateChanged != null) StateChanged();
        RefreshUI();
    }

    // ==================== UI BUILD ====================
    void EnsureBuilt()
    {
        Transform uiParent = panelRoot != null && panelRoot.parent != null ? panelRoot.parent : transform;
        if (panelRoot == null && GetComponent<HudController>() != null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            if (canvas != null) uiParent = canvas.transform;
        }

        // 1. Nền đen mờ chắn cảm ứng (Modal Backdrop)
        if (modalBackdrop == null)
        {
            Transform existingBd = uiParent.Find("IndexModalBackdrop");
            GameObject bdGo = existingBd != null
                ? existingBd.gameObject
                : new GameObject("IndexModalBackdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            if (existingBd == null) bdGo.transform.SetParent(uiParent, false);
            RectTransform bdRt = bdGo.GetComponent<RectTransform>();
            bdRt.anchorMin = Vector2.zero;
            bdRt.anchorMax = Vector2.one;
            bdRt.offsetMin = Vector2.zero;
            bdRt.offsetMax = Vector2.zero;
            modalBackdrop = bdGo.GetComponent<Image>();
            modalBackdrop.color = modalDimColor;
            modalBackdrop.raycastTarget = true;
            Button bdBtn = bdGo.GetComponent<Button>();
            if (bdBtn != null) bdBtn.transition = Selectable.Transition.None;
            if (panelRoot != null && bdGo.transform.parent == panelRoot.parent)
                bdGo.transform.SetSiblingIndex(Mathf.Max(0, panelRoot.GetSiblingIndex()));
            bdGo.SetActive(false);
        }

        if (panelRoot != null && biomeScrollContent != null && detailCardBg != null) return;

        // 2. Panel chính
        GameObject rootGo = new GameObject("DinoIndexPanel", typeof(RectTransform), typeof(Image));
        rootGo.transform.SetParent(uiParent, false);
        panelRoot = rootGo.GetComponent<RectTransform>();
        panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
        panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
        panelRoot.pivot = new Vector2(0.5f, 0.5f);
        panelRoot.sizeDelta = new Vector2(1140f, 650f);
        panelRoot.anchoredPosition = Vector2.zero;

        Image rootBg = rootGo.GetComponent<Image>();
        rootBg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 18);
        rootBg.type = Image.Type.Sliced;
        rootBg.color = new Color(0.08f, 0.09f, 0.12f, 0.99f);

        GameObject innerGo = new GameObject("InnerBody", typeof(RectTransform), typeof(Image));
        innerGo.transform.SetParent(panelRoot, false);
        RectTransform innerRt = innerGo.GetComponent<RectTransform>();
        Stretch(innerRt, 6f);
        Image innerImg = innerGo.GetComponent<Image>();
        innerImg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 14);
        innerImg.type = Image.Type.Sliced;
        innerImg.color = new Color(0.15f, 0.17f, 0.21f, 1f);

        // Header bar (xanh dương sáng)
        GameObject hdrGo = new GameObject("Header", typeof(RectTransform), typeof(Image));
        hdrGo.transform.SetParent(innerRt, false);
        RectTransform hdrRt = hdrGo.GetComponent<RectTransform>();
        hdrRt.anchorMin = new Vector2(0f, 1f);
        hdrRt.anchorMax = new Vector2(1f, 1f);
        hdrRt.pivot = new Vector2(0.5f, 1f);
        hdrRt.offsetMin = new Vector2(0f, -74f);
        hdrRt.offsetMax = Vector2.zero;
        Image hdrImg = hdrGo.GetComponent<Image>();
        hdrImg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 12);
        hdrImg.type = Image.Type.Sliced;
        hdrImg.color = new Color(0.18f, 0.62f, 0.95f, 1f);

        TMP_Text titleTmp = CreateTmp(hdrRt, "Title", windowTitle, 38f, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        RectTransform titleRt = titleTmp.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 0f);
        titleRt.anchorMax = new Vector2(0.55f, 1f);
        titleRt.offsetMin = new Vector2(24f, 0f);
        titleRt.offsetMax = Vector2.zero;
        titleTmp.outlineWidth = 0.22f;
        titleTmp.outlineColor = Color.black;

        claimAllButton = CreateStyledButton(hdrRt, "ClaimAllButton", "CLAIM ALL", new Color(0.29f, 0.85f, 0.16f, 1f), 26f, out claimAllText);
        RectTransform caRt = (RectTransform)claimAllButton.transform;
        caRt.anchorMin = new Vector2(1f, 0.5f);
        caRt.anchorMax = new Vector2(1f, 0.5f);
        caRt.pivot = new Vector2(1f, 0.5f);
        caRt.anchoredPosition = new Vector2(-86f, 0f);
        caRt.sizeDelta = new Vector2(215f, 54f);
        claimAllButton.onClick.AddListener(ClaimAll);

        closeButton = CreateStyledButton(hdrRt, "CloseButton", "X", new Color(0.93f, 0.26f, 0.32f, 1f), 32f, out _);
        RectTransform closeRt = (RectTransform)closeButton.transform;
        closeRt.anchorMin = new Vector2(1f, 0.5f);
        closeRt.anchorMax = new Vector2(1f, 0.5f);
        closeRt.pivot = new Vector2(1f, 0.5f);
        closeRt.anchoredPosition = new Vector2(-14f, 0f);
        closeRt.sizeDelta = new Vector2(56f, 54f);

        // Khu vực trái (Chứa BiomeScroll / VariantsPanel + Thanh Unlocked dưới đáy)
        RectTransform leftArea = CreatePanel(innerRt, "LeftArea", Color.clear);
        leftArea.anchorMin = new Vector2(0f, 0f);
        leftArea.anchorMax = new Vector2(0.68f, 1f);
        leftArea.offsetMin = new Vector2(14f, 14f);
        leftArea.offsetMax = new Vector2(-6f, -84f);

        // Thanh Unlocked: X/Y dưới cùng bên trái
        GameObject totBarGo = new GameObject("TotalProgressBar", typeof(RectTransform), typeof(Image));
        totBarGo.transform.SetParent(leftArea, false);
        RectTransform totBarRt = totBarGo.GetComponent<RectTransform>();
        totBarRt.anchorMin = new Vector2(0f, 0f);
        totBarRt.anchorMax = new Vector2(1f, 0f);
        totBarRt.pivot = new Vector2(0.5f, 0f);
        totBarRt.anchoredPosition = Vector2.zero;
        totBarRt.sizeDelta = new Vector2(0f, 44f);
        Image totBg = totBarGo.GetComponent<Image>();
        totBg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 12);
        totBg.type = Image.Type.Sliced;
        totBg.color = new Color(0.08f, 0.10f, 0.13f, 1f);

        GameObject totFillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        totFillGo.transform.SetParent(totBarRt, false);
        RectTransform totFillRt = totFillGo.GetComponent<RectTransform>();
        totFillRt.anchorMin = new Vector2(0f, 0f);
        totFillRt.anchorMax = new Vector2(1f, 1f);
        totFillRt.offsetMin = new Vector2(3f, 3f);
        totFillRt.offsetMax = new Vector2(-3f, -3f);
        totalProgressFill = totFillGo.GetComponent<Image>();
        totalProgressFill.sprite = GetSolidSprite();
        totalProgressFill.type = Image.Type.Filled;
        totalProgressFill.fillMethod = Image.FillMethod.Horizontal;
        totalProgressFill.color = new Color(0.16f, 0.56f, 0.92f, 1f);
        totalProgressFill.fillAmount = 0f;

        GameObject bookGo = new GameObject("BookIcon", typeof(RectTransform), typeof(Image));
        bookGo.transform.SetParent(totBarRt, false);
        RectTransform bookRt = bookGo.GetComponent<RectTransform>();
        bookRt.anchorMin = new Vector2(0f, 0.5f);
        bookRt.anchorMax = new Vector2(0f, 0.5f);
        bookRt.pivot = new Vector2(0f, 0.5f);
        bookRt.anchoredPosition = new Vector2(8f, 0f);
        bookRt.sizeDelta = new Vector2(34f, 34f);
        Image bookImg = bookGo.GetComponent<Image>();
        bookImg.sprite = GetBookSprite();
        bookImg.preserveAspect = true;

        totalProgressText = CreateTmp(totBarRt, "Text", "Unlocked: 0/0", 24f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        totalProgressText.outlineWidth = 0.22f;
        totalProgressText.outlineColor = Color.black;

        // Khung Scroll View 1: Danh sách Biome
        biomeScrollGo = new GameObject("BiomeScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(Mask));
        biomeScrollGo.transform.SetParent(leftArea, false);
        RectTransform bsRt = biomeScrollGo.GetComponent<RectTransform>();
        bsRt.anchorMin = new Vector2(0f, 0f);
        bsRt.anchorMax = new Vector2(1f, 1f);
        bsRt.offsetMin = new Vector2(0f, 54f);
        bsRt.offsetMax = Vector2.zero;
        Image bsBg = biomeScrollGo.GetComponent<Image>();
        bsBg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 12);
        bsBg.type = Image.Type.Sliced;
        bsBg.color = new Color(0.08f, 0.09f, 0.12f, 1f);
        biomeScrollGo.GetComponent<Mask>().showMaskGraphic = true;

        GameObject bsContentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        bsContentGo.transform.SetParent(bsRt, false);
        biomeScrollContent = bsContentGo.GetComponent<RectTransform>();
        biomeScrollContent.anchorMin = new Vector2(0f, 1f);
        biomeScrollContent.anchorMax = new Vector2(1f, 1f);
        biomeScrollContent.pivot = new Vector2(0.5f, 1f);
        biomeScrollContent.offsetMin = Vector2.zero;
        biomeScrollContent.offsetMax = Vector2.zero;
        VerticalLayoutGroup bsVlg = bsContentGo.GetComponent<VerticalLayoutGroup>();
        bsVlg.padding = new RectOffset(10, 10, 10, 10);
        bsVlg.spacing = 14f;
        bsVlg.childControlWidth = true;
        bsVlg.childControlHeight = true;
        bsVlg.childForceExpandWidth = true;
        bsVlg.childForceExpandHeight = false;
        ContentSizeFitter bsCsf = bsContentGo.GetComponent<ContentSizeFitter>();
        bsCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect sr = biomeScrollGo.GetComponent<ScrollRect>();
        sr.viewport = bsRt;
        sr.content = biomeScrollContent;
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 35f;

        // Khung View 2: Ma trận Variants
        variantsPanelGo = new GameObject("VariantsMatrixPanel", typeof(RectTransform), typeof(Image));
        variantsPanelGo.transform.SetParent(leftArea, false);
        RectTransform vpRt = variantsPanelGo.GetComponent<RectTransform>();
        vpRt.anchorMin = new Vector2(0f, 0f);
        vpRt.anchorMax = new Vector2(1f, 1f);
        vpRt.offsetMin = new Vector2(0f, 54f);
        vpRt.offsetMax = Vector2.zero;
        Image vpBg = variantsPanelGo.GetComponent<Image>();
        vpBg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 12);
        vpBg.type = Image.Type.Sliced;
        vpBg.color = new Color(0.08f, 0.09f, 0.12f, 1f);
        variantsContent = vpRt;
        variantsPanelGo.SetActive(false);

        // Khu vực phải (Cố định): Chi tiết + Rewards
        RectTransform rightArea = CreatePanel(innerRt, "RightArea", Color.clear);
        rightArea.anchorMin = new Vector2(0.68f, 0f);
        rightArea.anchorMax = new Vector2(1f, 1f);
        rightArea.offsetMin = new Vector2(6f, 14f);
        rightArea.offsetMax = new Vector2(-14f, -84f);

        // Card chi tiết trên phải
        GameObject detCardGo = new GameObject("DetailCard", typeof(RectTransform), typeof(Image));
        detCardGo.transform.SetParent(rightArea, false);
        RectTransform detRt = detCardGo.GetComponent<RectTransform>();
        detRt.anchorMin = new Vector2(0f, 0.42f);
        detRt.anchorMax = new Vector2(1f, 1f);
        detRt.offsetMin = new Vector2(0f, 6f);
        detRt.offsetMax = Vector2.zero;
        detailCardBg = detCardGo.GetComponent<Image>();
        detailCardBg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 14);
        detailCardBg.type = Image.Type.Sliced;
        detailCardBg.color = new Color(0.45f, 0.48f, 0.52f, 1f);

        GameObject dIconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        dIconGo.transform.SetParent(detRt, false);
        RectTransform dIconRt = dIconGo.GetComponent<RectTransform>();
        dIconRt.anchorMin = new Vector2(0.18f, 0.45f);
        dIconRt.anchorMax = new Vector2(0.82f, 0.94f);
        dIconRt.offsetMin = Vector2.zero;
        dIconRt.offsetMax = Vector2.zero;
        detailIcon = dIconGo.GetComponent<Image>();
        detailIcon.preserveAspect = true;

        detailNameText = CreateTmp(detRt, "Name", "Dino", 28f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        RectTransform dnRt = detailNameText.rectTransform;
        dnRt.anchorMin = new Vector2(0f, 0.34f);
        dnRt.anchorMax = new Vector2(1f, 0.45f);
        dnRt.offsetMin = new Vector2(10f, 0f);
        dnRt.offsetMax = new Vector2(-10f, 0f);
        detailNameText.enableAutoSizing = true;
        detailNameText.fontSizeMin = 16f;
        detailNameText.fontSizeMax = 28f;
        detailNameText.outlineWidth = 0.22f;
        detailNameText.outlineColor = Color.black;

        detailRarityText = CreateTmp(detRt, "Rarity", "Common", 20f, new Color(0.85f, 0.85f, 0.85f), TextAlignmentOptions.Center, FontStyles.Bold);
        RectTransform drRt = detailRarityText.rectTransform;
        drRt.anchorMin = new Vector2(0f, 0.26f);
        drRt.anchorMax = new Vector2(1f, 0.35f);
        drRt.offsetMin = new Vector2(10f, 0f);
        drRt.offsetMax = new Vector2(-10f, 0f);
        detailRarityText.outlineWidth = 0.2f;
        detailRarityText.outlineColor = Color.black;

        detailRateText = CreateTmp(detRt, "Rate", "$1/s", 22f, new Color(0.45f, 1f, 0.45f), TextAlignmentOptions.Center, FontStyles.Bold);
        RectTransform drateRt = detailRateText.rectTransform;
        drateRt.anchorMin = new Vector2(0f, 0.18f);
        drateRt.anchorMax = new Vector2(1f, 0.27f);
        drateRt.offsetMin = new Vector2(10f, 0f);
        drateRt.offsetMax = new Vector2(-10f, 0f);
        detailRateText.outlineWidth = 0.2f;
        detailRateText.outlineColor = Color.black;

        detailVariantsBtn = CreateStyledButton(detRt, "VariantsButton", "VARIANTS 0/0", new Color(1f, 0.78f, 0.16f, 1f), 20f, out detailVariantsText);
        RectTransform dvRt = (RectTransform)detailVariantsBtn.transform;
        dvRt.anchorMin = new Vector2(0.08f, 0.04f);
        dvRt.anchorMax = new Vector2(0.92f, 0.17f);
        dvRt.offsetMin = Vector2.zero;
        dvRt.offsetMax = Vector2.zero;
        detailVariantsBtn.onClick.AddListener(ToggleVariantsView);

        // Hộp Rewards (dưới phải)
        GameObject rewBoxGo = new GameObject("RewardsBox", typeof(RectTransform), typeof(Image));
        rewBoxGo.transform.SetParent(rightArea, false);
        RectTransform rewRt = rewBoxGo.GetComponent<RectTransform>();
        rewRt.anchorMin = new Vector2(0f, 0f);
        rewRt.anchorMax = new Vector2(1f, 0.42f);
        rewRt.offsetMin = Vector2.zero;
        rewRt.offsetMax = new Vector2(0f, -6f);
        Image rewBorder = rewBoxGo.GetComponent<Image>();
        rewBorder.sprite = UIEffectUtil.GetRoundedRectSprite(64, 14);
        rewBorder.type = Image.Type.Sliced;
        rewBorder.color = new Color(0.18f, 0.62f, 0.22f, 1f);

        GameObject rewInner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
        rewInner.transform.SetParent(rewRt, false);
        RectTransform rewInRt = rewInner.GetComponent<RectTransform>();
        Stretch(rewInRt, 3f);
        Image rewInImg = rewInner.GetComponent<Image>();
        rewInImg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 12);
        rewInImg.type = Image.Type.Sliced;
        rewInImg.color = new Color(0.07f, 0.18f, 0.09f, 1f);

        TMP_Text rewHdr = CreateTmp(rewInRt, "Header", "Rewards:", 21f, Color.white, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        RectTransform rhRt = rewHdr.rectTransform;
        rhRt.anchorMin = new Vector2(0f, 0.80f);
        rhRt.anchorMax = new Vector2(1f, 1f);
        rhRt.offsetMin = new Vector2(12f, 0f);
        rhRt.offsetMax = new Vector2(-10f, -6f);

        CreateRewardTile(rewInRt, "CashTile", new Vector2(0.08f, 0.34f), new Vector2(0.47f, 0.78f),
                         gm != null ? gm.cashIcon : null, "$100", new Color(0.45f, 1f, 0.45f), out detailCashIcon, out detailCashText);
        CreateRewardTile(rewInRt, "StaminaTile", new Vector2(0.53f, 0.34f), new Vector2(0.92f, 0.78f),
                         gm != null ? gm.staminaIcon : null, "+20", new Color(1f, 0.88f, 0.25f), out detailStaminaIcon, out detailStaminaText);

        detailClaimBtn = CreateStyledButton(rewInRt, "ClaimButton", "CLAIMED!", new Color(0.75f, 0.78f, 0.82f, 1f), 24f, out detailClaimText);
        RectTransform dcRt = (RectTransform)detailClaimBtn.transform;
        dcRt.anchorMin = new Vector2(0.08f, 0.05f);
        dcRt.anchorMax = new Vector2(0.92f, 0.29f);
        dcRt.offsetMin = Vector2.zero;
        dcRt.offsetMax = Vector2.zero;
        detailClaimBtn.onClick.AddListener(delegate { if (selectedDino != null) ClaimSpecies(selectedDino); });

        panelRoot.gameObject.SetActive(false);
    }

    void CreateRewardTile(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, Sprite iconSp, string val, Color txtCol,
                          out Image outIcon, out TMP_Text outTxt)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image bg = go.GetComponent<Image>();
        bg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 10);
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.11f, 0.13f, 0.16f, 1f);

        GameObject icGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        icGo.transform.SetParent(rt, false);
        RectTransform icRt = icGo.GetComponent<RectTransform>();
        icRt.anchorMin = new Vector2(0.2f, 0.34f);
        icRt.anchorMax = new Vector2(0.8f, 0.90f);
        icRt.offsetMin = Vector2.zero;
        icRt.offsetMax = Vector2.zero;
        outIcon = icGo.GetComponent<Image>();
        outIcon.sprite = iconSp != null ? iconSp : GetDinoFallbackSprite();
        outIcon.color = iconSp != null ? Color.white : txtCol;
        outIcon.preserveAspect = true;

        outTxt = CreateTmp(rt, "Value", val, 18f, txtCol, TextAlignmentOptions.Bottom, FontStyles.Bold);
        RectTransform tRt = outTxt.rectTransform;
        tRt.anchorMin = new Vector2(0f, 0.04f);
        tRt.anchorMax = new Vector2(1f, 0.36f);
        tRt.offsetMin = new Vector2(4f, 0f);
        tRt.offsetMax = new Vector2(-4f, 0f);
        outTxt.enableAutoSizing = true;
        outTxt.fontSizeMin = 11f;
        outTxt.fontSizeMax = 18f;
    }

    void ToggleVariantsView()
    {
        if (selectedDino == null) return;
        showingVariants = !showingVariants;
        RefreshUI();
    }

    void RefreshUI()
    {
        if (panelRoot == null) return;
        EnsureDataLoaded();
        RecalculateUnclaimedCount();

        // Header Claim All
        if (claimAllButton != null && claimAllText != null)
        {
            bool hasUnclaimed = TotalUnclaimedCount > 0;
            claimAllButton.interactable = hasUnclaimed;
            Image bg = claimAllButton.GetComponent<Image>();
            if (bg != null) bg.color = hasUnclaimed ? new Color(0.29f, 0.85f, 0.16f, 1f) : new Color(0.75f, 0.78f, 0.82f, 1f);
            claimAllText.text = hasUnclaimed ? "CLAIM ALL (" + TotalUnclaimedCount + ")" : "CLAIM ALL";
        }

        // Tổng Unlocked
        int unlockedCount = unlockedSpecies.Count;
        int totalCount = Mathf.Max(1, allSpecies.Count);
        if (totalProgressFill != null) totalProgressFill.fillAmount = (float)unlockedCount / totalCount;
        if (totalProgressText != null) totalProgressText.text = "Unlocked: " + unlockedCount + "/" + allSpecies.Count;

        // Trái: Biome Scroll hoặc Variants Matrix
        if (biomeScrollGo != null) biomeScrollGo.SetActive(!showingVariants);
        if (variantsPanelGo != null) variantsPanelGo.SetActive(showingVariants);

        if (!showingVariants) BuildBiomeSections();
        else BuildVariantsMatrix();

        RefreshDetailPanel();
    }

    void BuildBiomeSections()
    {
        if (biomeScrollContent == null) return;
        ClearChildren(biomeScrollContent);

        for (int b = 0; b < biomes.Count; b++)
        {
            BiomeData bd = biomes[b];
            if (bd == null) continue;
            if (!biomeSpeciesMap.TryGetValue(bd, out List<DinoData> species) || species.Count == 0) continue;

            int unlockedInBiome = CountUnlockedInBiome(bd, out int totalInBiome);
            int rows = Mathf.CeilToInt(species.Count / 3f);
            float cardW = 148f, cardH = 126f, gap = 8f;
            float gridH = rows * cardH + Mathf.Max(0, rows - 1) * gap;
            float barH = 44f;
            float totalH = Mathf.Max(220f, gridH + barH + 10f);

            GameObject secGo = new GameObject("Biome_" + bd.biomeName, typeof(RectTransform), typeof(LayoutElement));
            secGo.transform.SetParent(biomeScrollContent, false);
            secGo.GetComponent<LayoutElement>().preferredHeight = totalH;

            RectTransform secRt = secGo.GetComponent<RectTransform>();

            // Cột trái: Thẻ Biome + COMPLETE rewards
            GameObject bCardGo = new GameObject("BiomeCard", typeof(RectTransform), typeof(Image));
            bCardGo.transform.SetParent(secRt, false);
            RectTransform bRt = bCardGo.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0f, 0f);
            bRt.anchorMax = new Vector2(0f, 1f);
            bRt.pivot = new Vector2(0f, 0.5f);
            bRt.offsetMin = Vector2.zero;
            bRt.offsetMax = new Vector2(158f, 0f);
            Image bBg = bCardGo.GetComponent<Image>();
            bBg.sprite = bd.icon != null ? bd.icon : UIEffectUtil.GetRoundedRectSprite(64, 12);
            bBg.type = bd.icon != null ? Image.Type.Simple : Image.Type.Sliced;
            bBg.preserveAspect = false;
            bBg.color = bd.icon != null ? Color.white : bd.cardColor;

            GameObject bShade = new GameObject("Shade", typeof(RectTransform), typeof(Image));
            bShade.transform.SetParent(bRt, false);
            Stretch(bShade.GetComponent<RectTransform>(), 0f);
            Image shadeImg = bShade.GetComponent<Image>();
            shadeImg.color = new Color(0f, 0f, 0f, 0.25f);

            TMP_Text bName = CreateTmp(bRt, "BiomeName", bd.biomeName, 24f, Color.white, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            RectTransform bnRt = bName.rectTransform;
            bnRt.anchorMin = new Vector2(0f, 0.72f);
            bnRt.anchorMax = new Vector2(1f, 1f);
            bnRt.offsetMin = new Vector2(8f, 0f);
            bnRt.offsetMax = new Vector2(-8f, -8f);
            bName.outlineWidth = 0.25f;
            bName.outlineColor = Color.black;

            // Hộp COMPLETE
            GameObject compGo = new GameObject("CompleteBox", typeof(RectTransform), typeof(Image));
            compGo.transform.SetParent(bRt, false);
            RectTransform cRt = compGo.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.06f, 0.04f);
            cRt.anchorMax = new Vector2(0.94f, 0.42f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;
            Image cBg = compGo.GetComponent<Image>();
            cBg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 10);
            cBg.type = Image.Type.Sliced;
            cBg.color = new Color(0.08f, 0.12f, 0.09f, 0.92f);

            TMP_Text compTitle = CreateTmp(cRt, "Title", "COMPLETE:", 13f, new Color(0.75f, 0.95f, 0.75f), TextAlignmentOptions.Top, FontStyles.Bold);
            compTitle.rectTransform.offsetMax = new Vector2(0f, -4f);

            double bCash = GetBiomeRewardCash(bd);
            float bStam = GetBiomeRewardStamina(bd);
            TMP_Text compVals = CreateTmp(cRt, "Values",
                "<color=#55FF55>$" + NumberFormat.Short(bCash) + "</color>\n<color=#FFD94A>+" + NumberFormat.Short(bStam) + "</color>",
                16f, Color.white, TextAlignmentOptions.Bottom, FontStyles.Bold);
            compVals.rectTransform.offsetMin = new Vector2(4f, 6f);
            compVals.rectTransform.offsetMax = new Vector2(-4f, -18f);

            // Cột phải: Lưới 3 cột Dino + Thanh Biome Progress / Claim
            RectTransform rightCol = CreatePanel(secRt, "RightCol", Color.clear);
            rightCol.anchorMin = new Vector2(0f, 0f);
            rightCol.anchorMax = new Vector2(1f, 1f);
            rightCol.offsetMin = new Vector2(168f, 0f);
            rightCol.offsetMax = Vector2.zero;

            GameObject gridGo = new GameObject("DinoGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridGo.transform.SetParent(rightCol, false);
            RectTransform gRt = gridGo.GetComponent<RectTransform>();
            gRt.anchorMin = new Vector2(0f, 0f);
            gRt.anchorMax = new Vector2(1f, 1f);
            gRt.offsetMin = new Vector2(0f, barH + 10f);
            gRt.offsetMax = Vector2.zero;
            GridLayoutGroup glg = gridGo.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(cardW, cardH);
            glg.spacing = new Vector2(gap, gap);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 3;

            for (int i = 0; i < species.Count; i++)
                BuildDinoIndexCard(gRt, species[i]);

            // Thanh Biome Progress / COMPLETED
            bool isBiomeFull = totalInBiome > 0 && unlockedInBiome >= totalInBiome;
            bool isBiomeClaimed = claimedBiomes.Contains(bd);

            GameObject bBarGo = new GameObject("BiomeProgressBar", typeof(RectTransform), typeof(Image), typeof(Button));
            bBarGo.transform.SetParent(rightCol, false);
            RectTransform bbRt = bBarGo.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0f, 0f);
            bbRt.anchorMax = new Vector2(1f, 0f);
            bbRt.pivot = new Vector2(0.5f, 0f);
            bbRt.anchoredPosition = Vector2.zero;
            bbRt.sizeDelta = new Vector2(0f, barH);
            Image bbBg = bBarGo.GetComponent<Image>();
            bbBg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 10);
            bbBg.type = Image.Type.Sliced;
            bbBg.color = new Color(0.14f, 0.16f, 0.20f, 1f);

            GameObject bFillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            bFillGo.transform.SetParent(bbRt, false);
            RectTransform bfRt = bFillGo.GetComponent<RectTransform>();
            Stretch(bfRt, 3f);
            Image bFillImg = bFillGo.GetComponent<Image>();
            bFillImg.sprite = GetSolidSprite();
            bFillImg.type = Image.Type.Filled;
            bFillImg.fillMethod = Image.FillMethod.Horizontal;
            bFillImg.fillAmount = totalInBiome > 0 ? (float)unlockedInBiome / totalInBiome : 0f;
            bFillImg.color = isBiomeFull ? new Color(0.28f, 0.85f, 0.16f, 1f) : new Color(0.22f, 0.72f, 0.24f, 1f);

            string barLabel = isBiomeClaimed
                ? biomeCompletedText
                : (isBiomeFull ? biomeClaimText : (unlockedInBiome + "/" + totalInBiome));
            TMP_Text bbTmp = CreateTmp(bbRt, "Label", barLabel, 22f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            bbTmp.outlineWidth = 0.24f;
            bbTmp.outlineColor = Color.black;

            Button bbBtn = bBarGo.GetComponent<Button>();
            bbBtn.targetGraphic = bbBg;
            bbBtn.interactable = isBiomeFull && !isBiomeClaimed;
            BiomeData capturedBiome = bd;
            bbBtn.onClick.AddListener(delegate { ClaimBiome(capturedBiome); });
            UIEffectUtil.AttachButtonBounce(this, bbBtn);

            if (bd.saddleRewardIcon != null)
            {
                GameObject sadGo = new GameObject("SaddleIcon", typeof(RectTransform), typeof(Image));
                sadGo.transform.SetParent(bbRt, false);
                RectTransform sRt = sadGo.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(1f, 0.5f);
                sRt.anchorMax = new Vector2(1f, 0.5f);
                sRt.pivot = new Vector2(1f, 0.5f);
                sRt.anchoredPosition = new Vector2(-6f, 0f);
                sRt.sizeDelta = new Vector2(40f, 40f);
                Image sadImg = sadGo.GetComponent<Image>();
                sadImg.sprite = bd.saddleRewardIcon;
                sadImg.preserveAspect = true;
            }
        }
    }

    void BuildDinoIndexCard(RectTransform parent, DinoData d)
    {
        if (d == null) return;
        bool unlocked = unlockedSpecies.Contains(d);
        bool unclaimed = unlocked && !claimedSpecies.Contains(d);
        bool isSelected = d == selectedDino;

        GameObject go = new GameObject("Card_" + d.displayName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image borderImg = go.GetComponent<Image>();
        borderImg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 12);
        borderImg.type = Image.Type.Sliced;
        borderImg.color = isSelected ? Color.white : new Color(0.06f, 0.08f, 0.10f, 1f);

        GameObject innerGo = new GameObject("Inner", typeof(RectTransform), typeof(Image));
        innerGo.transform.SetParent(go.transform, false);
        Stretch(innerGo.GetComponent<RectTransform>(), isSelected ? 4f : 3f);
        Image innerImg = innerGo.GetComponent<Image>();
        innerImg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 10);
        innerImg.type = Image.Type.Sliced;
        Color rc = DinoColors.Of(d.rarity);
        innerImg.color = unlocked ? rc : new Color(rc.r * 0.45f, rc.g * 0.45f, rc.b * 0.45f, 1f);

        TMP_Text nameTmp = CreateTmp(innerGo.GetComponent<RectTransform>(), "Name", unlocked ? d.displayName : "???", 16f, Color.white, TextAlignmentOptions.Top, FontStyles.Bold);
        RectTransform nRt = nameTmp.rectTransform;
        nRt.anchorMin = new Vector2(0f, 0.72f);
        nRt.anchorMax = new Vector2(1f, 1f);
        nRt.offsetMin = new Vector2(5f, 0f);
        nRt.offsetMax = new Vector2(-5f, -3f);
        nameTmp.enableAutoSizing = true;
        nameTmp.fontSizeMin = 9f;
        nameTmp.fontSizeMax = 16f;
        nameTmp.enableWordWrapping = false;
        nameTmp.overflowMode = TextOverflowModes.Ellipsis;
        nameTmp.outlineWidth = 0.22f;
        nameTmp.outlineColor = Color.black;

        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(innerGo.transform, false);
        RectTransform icRt = iconGo.GetComponent<RectTransform>();
        icRt.anchorMin = new Vector2(0.14f, 0.08f);
        icRt.anchorMax = new Vector2(0.86f, 0.74f);
        icRt.offsetMin = Vector2.zero;
        icRt.offsetMax = Vector2.zero;
        Image icImg = iconGo.GetComponent<Image>();
        icImg.sprite = d.icon != null ? d.icon : GetDinoFallbackSprite();
        icImg.preserveAspect = true;
        icImg.color = unlocked ? Color.white : new Color(0.08f, 0.09f, 0.11f, 0.96f);

        if (unclaimed)
        {
            GameObject badgeGo = new GameObject("ClaimAlert", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(go.transform, false);
            RectTransform bgRt = badgeGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(1f, 1f);
            bgRt.anchorMax = new Vector2(1f, 1f);
            bgRt.pivot = new Vector2(1f, 1f);
            bgRt.anchoredPosition = new Vector2(-2f, -2f);
            bgRt.sizeDelta = new Vector2(22f, 22f);
            Image bgImg = badgeGo.GetComponent<Image>();
            bgImg.sprite = UIEffectUtil.GetRoundedRectSprite(32, 11);
            bgImg.color = new Color(0.95f, 0.22f, 0.22f, 1f);
            TMP_Text ex = CreateTmp(bgRt, "Bang", "!", 15f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            ex.raycastTarget = false;
        }

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = borderImg;
        btn.onClick.AddListener(delegate
        {
            selectedDino = d;
            RefreshUI();
        });
        UIEffectUtil.AttachButtonBounce(this, btn);
    }

    void BuildVariantsMatrix()
    {
        if (variantsContent == null || selectedDino == null) return;
        ClearChildren(variantsContent);

        RectTransform root = variantsContent;
        int collected = CountCollectedCombos(selectedDino);
        int totalCombos = GetTotalComboSlotsPerDino();

        // Top Bar: "<DinoName> variants"  |  "X/Y collected" [X]
        RectTransform topRow = CreatePanel(root, "TopRow", Color.clear);
        topRow.anchorMin = new Vector2(0f, 1f);
        topRow.anchorMax = new Vector2(1f, 1f);
        topRow.pivot = new Vector2(0.5f, 1f);
        topRow.offsetMin = new Vector2(12f, -46f);
        topRow.offsetMax = new Vector2(-12f, -4f);

        string displayTitle = (unlockedSpecies.Contains(selectedDino) ? selectedDino.displayName : "???") + " variants";
        TMP_Text vTitle = CreateTmp(topRow, "Title", displayTitle, 24f, Color.white, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        vTitle.rectTransform.anchorMax = new Vector2(0.55f, 1f);

        TMP_Text vCount = CreateTmp(topRow, "Count", collected + "/" + totalCombos + " collected", 20f, new Color(1f, 0.85f, 0.25f), TextAlignmentOptions.MidlineRight, FontStyles.Bold);
        vCount.rectTransform.anchorMin = new Vector2(0.45f, 0f);
        vCount.rectTransform.anchorMax = new Vector2(1f, 1f);
        vCount.rectTransform.offsetMax = new Vector2(-48f, 0f);

        Button backBtn = CreateStyledButton(topRow, "CloseVariants", "X", new Color(0.93f, 0.26f, 0.32f, 1f), 22f, out _);
        RectTransform backRt = (RectTransform)backBtn.transform;
        backRt.anchorMin = new Vector2(1f, 0.5f);
        backRt.anchorMax = new Vector2(1f, 0.5f);
        backRt.pivot = new Vector2(1f, 0.5f);
        backRt.anchoredPosition = Vector2.zero;
        backRt.sizeDelta = new Vector2(38f, 38f);
        backBtn.onClick.AddListener(delegate { showingVariants = false; RefreshUI(); });

        // Bảng ma trận ở giữa
        int cols = Mathf.Max(1, allSizes.Count);
        int rows = Mathf.Max(1, allVariants.Count);
        float leftLabelW = 104f;
        float sizeHeaderH = 34f;

        // Hàng tên Size (cột)
        RectTransform hdrRow = CreatePanel(root, "SizeHeaders", Color.clear);
        hdrRow.anchorMin = new Vector2(0f, 1f);
        hdrRow.anchorMax = new Vector2(1f, 1f);
        hdrRow.offsetMin = new Vector2(leftLabelW + 10f, -48f - sizeHeaderH);
        hdrRow.offsetMax = new Vector2(-10f, -48f);

        for (int c = 0; c < allSizes.Count; c++)
        {
            DinoSizeAsset sa = allSizes[c];
            float x0 = (float)c / cols;
            float x1 = (float)(c + 1) / cols;
            string sName = sa != null && !string.IsNullOrEmpty(sa.displayName) ? sa.displayName.ToUpperInvariant() : ("SIZE " + c);
            Color sCol = sa != null ? sa.tagColor : Color.white;
            TMP_Text st = CreateTmp(hdrRow, "Col_" + sName, sName, 17f, sCol, TextAlignmentOptions.Center, FontStyles.Bold);
            st.rectTransform.anchorMin = new Vector2(x0, 0f);
            st.rectTransform.anchorMax = new Vector2(x1, 1f);
            st.rectTransform.offsetMin = new Vector2(2f, 0f);
            st.rectTransform.offsetMax = new Vector2(-2f, 0f);
            st.enableAutoSizing = true;
            st.fontSizeMin = 10f;
            st.fontSizeMax = 17f;
        }

        // Lưới các hàng Variant
        RectTransform gridArea = CreatePanel(root, "MatrixGrid", Color.clear);
        gridArea.anchorMin = new Vector2(0f, 0f);
        gridArea.anchorMax = new Vector2(1f, 1f);
        gridArea.offsetMin = new Vector2(10f, 54f);
        gridArea.offsetMax = new Vector2(-10f, -48f - sizeHeaderH);

        DinoSpawnTable tbl = null;
        speciesTableMap.TryGetValue(selectedDino, out tbl);

        for (int r = 0; r < rows; r++)
        {
            DinoVariant va = r < allVariants.Count ? allVariants[r] : null;
            float yTop = 1f - (float)r / rows;
            float yBot = 1f - (float)(r + 1) / rows;

            RectTransform rowRt = CreatePanel(gridArea, "Row_" + r, Color.clear);
            rowRt.anchorMin = new Vector2(0f, yBot);
            rowRt.anchorMax = new Vector2(1f, yTop);
            rowRt.offsetMin = new Vector2(0f, 3f);
            rowRt.offsetMax = new Vector2(0f, -3f);

            string vName = va != null ? va.displayName.ToUpperInvariant() : "NORMAL";
            Color vCol = (va != null && va.useTint && va.tint.maxColorComponent > 0.15f)
                ? va.tint
                : (va != null && !va.isNormal ? new Color(1f, 0.82f, 0.25f) : Color.white);
            TMP_Text rowLbl = CreateTmp(rowRt, "VarName", vName, 16f, vCol, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            rowLbl.rectTransform.anchorMin = new Vector2(0f, 0f);
            rowLbl.rectTransform.anchorMax = new Vector2(0f, 1f);
            rowLbl.rectTransform.offsetMin = Vector2.zero;
            rowLbl.rectTransform.offsetMax = new Vector2(leftLabelW - 6f, 0f);
            rowLbl.enableAutoSizing = true;
            rowLbl.fontSizeMin = 10f;
            rowLbl.fontSizeMax = 16f;

            RectTransform cellsRt = CreatePanel(rowRt, "Cells", Color.clear);
            cellsRt.anchorMin = new Vector2(0f, 0f);
            cellsRt.anchorMax = new Vector2(1f, 1f);
            cellsRt.offsetMin = new Vector2(leftLabelW, 0f);
            cellsRt.offsetMax = Vector2.zero;

            for (int c = 0; c < cols; c++)
            {
                DinoSizeAsset sa = c < allSizes.Count ? allSizes[c] : null;
                bool hasCombo = IsComboUnlocked(selectedDino, va, sa);
                float x0 = (float)c / cols;
                float x1 = (float)(c + 1) / cols;

                GameObject cellGo = new GameObject("Cell_" + r + "_" + c, typeof(RectTransform), typeof(Image));
                cellGo.transform.SetParent(cellsRt, false);
                RectTransform cellRt = cellGo.GetComponent<RectTransform>();
                cellRt.anchorMin = new Vector2(x0, 0f);
                cellRt.anchorMax = new Vector2(x1, 1f);
                cellRt.offsetMin = new Vector2(3f, 2f);
                cellRt.offsetMax = new Vector2(-3f, -2f);
                Image cellBg = cellGo.GetComponent<Image>();
                cellBg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 8);
                cellBg.type = Image.Type.Sliced;
                cellBg.color = hasCombo ? Color.white : new Color(0.14f, 0.16f, 0.20f, 1f);

                GameObject inGo = new GameObject("Inner", typeof(RectTransform), typeof(Image));
                inGo.transform.SetParent(cellRt, false);
                Stretch(inGo.GetComponent<RectTransform>(), 2f);
                Image inImg = inGo.GetComponent<Image>();
                inImg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 7);
                inImg.type = Image.Type.Sliced;
                inImg.color = hasCombo ? new Color(0.29f, 0.34f, 0.43f, 1f) : new Color(0.09f, 0.10f, 0.13f, 1f);

                GameObject icGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                icGo.transform.SetParent(inGo.transform, false);
                RectTransform icRt = icGo.GetComponent<RectTransform>();
                float sizeScale = sa != null ? Mathf.Clamp(0.65f + (sa.scaleMultiplier - 1f) * 0.22f, 0.5f, 1.05f) : 0.8f;
                float padFrac = (1f - sizeScale * 0.72f) * 0.5f;
                icRt.anchorMin = new Vector2(padFrac, 0.24f);
                icRt.anchorMax = new Vector2(1f - padFrac, 0.94f);
                icRt.offsetMin = Vector2.zero;
                icRt.offsetMax = Vector2.zero;
                Image icImg = icGo.GetComponent<Image>();
                icImg.sprite = selectedDino.icon != null ? selectedDino.icon : GetDinoFallbackSprite();
                icImg.preserveAspect = true;
                if (hasCombo)
                    icImg.color = (va != null && va.useTint) ? va.tint : Color.white;
                else
                    icImg.color = new Color(0.06f, 0.07f, 0.08f, 0.9f);

                if (!hasCombo)
                {
                    string odds = ComputeComboOddsText(va, sa, tbl);
                    TMP_Text oddsTxt = CreateTmp(inGo.GetComponent<RectTransform>(), "Odds", odds, 12f, new Color(0.75f, 0.78f, 0.82f), TextAlignmentOptions.Bottom, FontStyles.Bold);
                    oddsTxt.rectTransform.offsetMin = new Vector2(2f, 2f);
                    oddsTxt.rectTransform.offsetMax = new Vector2(-2f, 0f);
                    oddsTxt.enableAutoSizing = true;
                    oddsTxt.fontSizeMin = 8f;
                    oddsTxt.fontSizeMax = 12f;
                }
            }
        }

        // 2 thanh thưởng bên dưới: ALL SIZES & ALL MUTATIONS
        RectTransform botRow = CreatePanel(root, "BottomBonusRow", Color.clear);
        botRow.anchorMin = new Vector2(0f, 0f);
        botRow.anchorMax = new Vector2(1f, 0f);
        botRow.offsetMin = new Vector2(10f, 6f);
        botRow.offsetMax = new Vector2(-10f, 48f);

        int dSizes = CountDistinctSizesForDino(selectedDino);
        int tSizes = Mathf.Max(1, allSizes.Count);
        bool sizesFull = allSizes.Count > 0 && dSizes >= tSizes;
        bool sizesClaimed = claimedAllSizes.Contains(selectedDino);
        BuildSubBonusBar(botRow, "AllSizesBar", new Vector2(0f, 0f), new Vector2(0.49f, 1f),
                         "ALL SIZES " + dSizes + "/" + allSizes.Count + "  +$" + NumberFormat.Short(selectedDino.EffectiveAllSizesRewardCash),
                         (float)dSizes / tSizes, sizesFull, sizesClaimed, delegate { ClaimAllSizes(selectedDino); });

        int dVars = CountDistinctVariantsForDino(selectedDino);
        int tVars = Mathf.Max(1, allVariants.Count);
        bool varsFull = allVariants.Count > 0 && dVars >= tVars;
        bool varsClaimed = claimedAllVariants.Contains(selectedDino);
        BuildSubBonusBar(botRow, "AllVariantsBar", new Vector2(0.51f, 0f), new Vector2(1f, 1f),
                         "ALL MUTATIONS " + dVars + "/" + allVariants.Count + "  +$" + NumberFormat.Short(selectedDino.EffectiveAllVariantsRewardCash),
                         (float)dVars / tVars, varsFull, varsClaimed, delegate { ClaimAllVariants(selectedDino); });
    }

    void BuildSubBonusBar(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, string txt, float fill, bool canClaim, bool claimed, Action onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = go.GetComponent<Image>();
        bg.sprite = UIEffectUtil.GetRoundedRectSprite(64, 10);
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.14f, 0.16f, 0.20f, 1f);

        GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(rt, false);
        Stretch(fillGo.GetComponent<RectTransform>(), 3f);
        Image fImg = fillGo.GetComponent<Image>();
        fImg.sprite = GetSolidSprite();
        fImg.type = Image.Type.Filled;
        fImg.fillMethod = Image.FillMethod.Horizontal;
        fImg.fillAmount = Mathf.Clamp01(fill);
        fImg.color = (canClaim && !claimed) ? new Color(0.35f, 0.90f, 0.20f, 1f) : new Color(0.22f, 0.72f, 0.24f, 1f);

        string finalLabel = claimed ? txt + " ✓" : txt;
        TMP_Text labelTmp = CreateTmp(rt, "Label", finalLabel, 16f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        labelTmp.rectTransform.offsetMin = new Vector2(6f, 0f);
        labelTmp.rectTransform.offsetMax = new Vector2(-6f, 0f);
        labelTmp.enableAutoSizing = true;
        labelTmp.fontSizeMin = 10f;
        labelTmp.fontSizeMax = 16f;
        labelTmp.outlineWidth = 0.22f;
        labelTmp.outlineColor = Color.black;

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = bg;
        btn.interactable = canClaim && !claimed;
        btn.onClick.AddListener(delegate { if (onClick != null) onClick(); });
        UIEffectUtil.AttachButtonBounce(this, btn);
    }

    string ComputeComboOddsText(DinoVariant va, DinoSizeAsset sa, DinoSpawnTable tbl)
    {
        float pVar = 1f / Mathf.Max(1, allVariants.Count);
        float pSize = 1f / Mathf.Max(1, allSizes.Count);

        if (tbl != null)
        {
            if (tbl.variants != null && tbl.variants.Count > 0)
            {
                float sumV = 0f, myV = 0f;
                for (int i = 0; i < tbl.variants.Count; i++)
                {
                    var vw = tbl.variants[i];
                    if (vw == null || vw.weight <= 0f) continue;
                    sumV += vw.weight;
                    if (vw.variant == va) myV += vw.weight;
                }
                if (sumV > 0f && myV > 0f) pVar = myV / sumV;
            }
            if (tbl.sizes != null && tbl.sizes.Count > 0)
            {
                float sumS = 0f, myS = 0f;
                for (int i = 0; i < tbl.sizes.Count; i++)
                {
                    var sw = tbl.sizes[i];
                    if (sw == null || sw.weight <= 0f) continue;
                    sumS += sw.weight;
                    if (sw.sizeAsset == sa) myS += sw.weight;
                }
                if (sumS > 0f && myS > 0f) pSize = myS / sumS;
            }
        }

        float combined = Mathf.Max(0.00001f, pVar * pSize);
        float denom = Mathf.Max(1f, Mathf.Round(1f / combined));
        return "1 in " + NumberFormat.Short(denom);
    }

    void RefreshDetailPanel()
    {
        if (selectedDino == null) return;
        bool unlocked = unlockedSpecies.Contains(selectedDino);
        bool claimed = claimedSpecies.Contains(selectedDino);
        Color rc = DinoColors.Of(selectedDino.rarity);

        if (detailCardBg != null)
            detailCardBg.color = unlocked ? rc : new Color(rc.r * 0.45f, rc.g * 0.45f, rc.b * 0.45f, 1f);

        if (detailIcon != null)
        {
            detailIcon.sprite = selectedDino.icon != null ? selectedDino.icon : GetDinoFallbackSprite();
            detailIcon.color = unlocked ? Color.white : new Color(0.08f, 0.09f, 0.11f, 0.96f);
        }

        if (detailNameText != null)
            detailNameText.text = unlocked ? selectedDino.displayName : "???";

        if (detailRarityText != null)
            detailRarityText.text = selectedDino.rarity.ToString();

        if (detailRateText != null)
        {
            double baseRate = (double)selectedDino.baseMoney * DinoMultipliers.Of(selectedDino.rarity);
            detailRateText.text = unlocked ? "$" + NumberFormat.Short(baseRate) + "/s" : "???";
        }

        if (detailVariantsText != null)
        {
            int col = CountCollectedCombos(selectedDino);
            int tot = GetTotalComboSlotsPerDino();
            detailVariantsText.text = "VARIANTS " + col + "/" + tot;
        }

        if (detailCashText != null)
            detailCashText.text = "$" + NumberFormat.Short(selectedDino.EffectiveIndexRewardCash);
        if (detailStaminaText != null)
            detailStaminaText.text = "+" + NumberFormat.Short(selectedDino.EffectiveIndexRewardStamina);

        if (detailCashIcon != null && gm != null && gm.cashIcon != null)
        {
            detailCashIcon.sprite = gm.cashIcon;
            detailCashIcon.color = Color.white;
        }
        if (detailStaminaIcon != null && gm != null && gm.staminaIcon != null)
        {
            detailStaminaIcon.sprite = gm.staminaIcon;
            detailStaminaIcon.color = Color.white;
        }

        if (detailClaimBtn != null && detailClaimText != null)
        {
            Image btnBg = detailClaimBtn.GetComponent<Image>();
            if (!unlocked)
            {
                detailClaimBtn.interactable = false;
                detailClaimText.text = "LOCKED";
                if (btnBg != null) btnBg.color = new Color(0.68f, 0.71f, 0.76f, 1f);
            }
            else if (claimed)
            {
                detailClaimBtn.interactable = false;
                detailClaimText.text = "CLAIMED!";
                if (btnBg != null) btnBg.color = new Color(0.84f, 0.86f, 0.89f, 1f);
            }
            else
            {
                detailClaimBtn.interactable = true;
                detailClaimText.text = "CLAIM!";
                if (btnBg != null) btnBg.color = new Color(0.29f, 0.85f, 0.16f, 1f);
            }
        }
    }

    // ==================== UI Helpers ====================
    void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject c = parent.GetChild(i).gameObject;
            c.SetActive(false);
            if (Application.isPlaying) Destroy(c);
            else DestroyImmediate(c);
        }
    }

    RectTransform CreatePanel(Transform parent, string name, Color col)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        if (col.a > 0.001f)
        {
            Image img = go.AddComponent<Image>();
            img.color = col;
            img.raycastTarget = false;
        }
        return rt;
    }

    TMP_Text CreateTmp(Transform parent, string name, string text, float size, Color col, TextAlignmentOptions align, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = col;
        tmp.alignment = align;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        return tmp;
    }

    Button CreateStyledButton(Transform parent, string name, string label, Color bgCol, float fontSize, out TMP_Text labelTmp)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = UIEffectUtil.GetRoundedRectSprite(64, 10);
        img.type = Image.Type.Sliced;
        img.color = bgCol;

        labelTmp = CreateTmp(go.transform, "Label", label, fontSize, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
        labelTmp.outlineWidth = 0.22f;
        labelTmp.outlineColor = Color.black;

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        UIEffectUtil.AttachButtonBounce(this, btn);
        return btn;
    }

    void Stretch(RectTransform rt, float pad)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, pad);
        rt.offsetMax = new Vector2(-pad, -pad);
    }

    static Sprite solidSprite;
    static Sprite GetSolidSprite()
    {
        if (solidSprite != null) return solidSprite;
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color32[] px = new Color32[16];
        for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(px);
        tex.Apply();
        solidSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        return solidSprite;
    }

    static Sprite GetDinoFallbackSprite()
    {
        if (fallbackDinoSprite != null) return fallbackDinoSprite;
        int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool body = (x >= 16 && x <= 44 && y >= 14 && y <= 34)
                         || (x >= 28 && x <= 52 && y >= 32 && y <= 50)
                         || (x >= 18 && x <= 25 && y >= 6 && y <= 16)
                         || (x >= 33 && x <= 40 && y >= 6 && y <= 16)
                         || (x >= 8 && x <= 18 && y >= 20 && y <= 30);
                px[y * w + x] = body ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        tex.SetPixels32(px);
        tex.Apply();
        fallbackDinoSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        return fallbackDinoSprite;
    }

    static Sprite GetBookSprite()
    {
        if (bookSprite != null) return bookSprite;
        int w = 48, h = 48;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool cover = x >= 8 && x <= 40 && y >= 6 && y <= 42;
                bool pages = x >= 12 && x <= 38 && y >= 8 && y <= 14;
                bool ribbon = x >= 15 && x <= 20 && y >= 2 && y <= 10;
                if (ribbon) px[y * w + x] = new Color32(235, 55, 65, 255);
                else if (pages) px[y * w + x] = new Color32(240, 244, 248, 255);
                else if (cover) px[y * w + x] = new Color32(45, 145, 245, 255);
                else px[y * w + x] = new Color32(0, 0, 0, 0);
            }
        tex.SetPixels32(px);
        tex.Apply();
        bookSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        return bookSprite;
    }
}
