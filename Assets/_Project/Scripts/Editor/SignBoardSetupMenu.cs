using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SignBoardSetupMenu
{
    [MenuItem("Tools/Ride a Dino/Build SignBoard UI (Upgrade Signs)")]
    public static void BuildAllSignBoards()
    {
        UpgradeSign[] signs = Object.FindObjectsOfType<UpgradeSign>(true);
        int built = 0;

        for (int i = 0; i < signs.Length; i++)
        {
            UpgradeSign sign = signs[i];
            if (sign == null) continue;

            SignBoardUI existing = sign.GetComponentInChildren<SignBoardUI>(true);
            if (existing != null)
            {
                sign.boardUI = existing;
                sign.RefreshPreviewInEditor();
                EditorUtility.SetDirty(sign);
                continue;
            }

            // Tự tìm tấm bảng gỗ (Board) để gắn Canvas vừa khít mặt trước
            Transform board = FindBoardTransform(sign.transform);
            Vector3 localPos = board != null
                ? new Vector3(board.localPosition.x, board.localPosition.y, board.localPosition.z + Mathf.Abs(board.localScale.z) * 0.5f + 0.02f)
                : new Vector3(0f, 2.6f, 0.17f);
            Vector2 sizeMeters = board != null
                ? new Vector2(Mathf.Abs(board.localScale.x), Mathf.Abs(board.localScale.y))
                : new Vector2(3f, 1.4f);

            SignBoardUI ui = SignBoardUI.CreateOnBoard(sign.transform, localPos, new Vector3(0f, 180f, 0f), sizeMeters);
            IUpgradable up = sign.target as IUpgradable;
            if (up != null)
                ui.SetUpgradeInfo(up.UpgradeName, up.Level, up.NextLevelInfo, up.UpgradeCost, true, up.IsMaxLevel);

            Undo.RegisterCreatedObjectUndo(ui.gameObject, "Build SignBoard UI");
            Undo.RecordObject(sign, "Assign SignBoard UI");
            sign.boardUI = ui;
            EditorUtility.SetDirty(sign);
            built++;
        }

        if (!Application.isPlaying)
            EditorSceneManager.MarkAllScenesDirty();

        Debug.Log($"[Ride a Dino] Đã tạo/gắn SignBoardUI cho {built} UpgradeSign.");
    }

    [MenuItem("Tools/Ride a Dino/Build Tame Secured UI (HUD)")]
    public static void BuildTameSecuredUI()
    {
        HudController hud = Object.FindObjectOfType<HudController>(true);
        if (hud == null)
        {
            Debug.LogWarning("[Ride a Dino] Không tìm thấy HudController trong Scene.");
            return;
        }

        TameSecuredUI ui = hud.tameSecuredUI != null ? hud.tameSecuredUI : hud.GetComponentInChildren<TameSecuredUI>(true);
        if (ui == null) ui = Object.FindObjectOfType<TameSecuredUI>(true);
        if (ui == null)
        {
            Transform existing = hud.transform.Find("TameSecuredUI");
            GameObject go = existing != null ? existing.gameObject : new GameObject("TameSecuredUI", typeof(RectTransform));
            if (existing == null)
            {
                go.transform.SetParent(hud.transform, false);
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                Undo.RegisterCreatedObjectUndo(go, "Build Tame Secured UI");
            }
            ui = Undo.AddComponent<TameSecuredUI>(go);
        }

        Undo.RecordObject(hud, "Assign TameSecuredUI");
        Undo.RecordObject(ui, "Build TameSecuredUI");
        hud.tameSecuredUI = ui;
        ui.BuildInEditor();
        EditorUtility.SetDirty(hud);
        EditorUtility.SetDirty(ui);

        if (!Application.isPlaying)
            EditorSceneManager.MarkAllScenesDirty();

        Selection.activeGameObject = ui.gameObject;
        Debug.Log("[Ride a Dino] Đã dựng TameSecuredUI trong HUD để bạn chỉnh sửa trực tiếp.");
    }

    [MenuItem("Tools/Ride a Dino/Build Sell Dinos Shop & Popup")]
    public static void BuildSellDinosShopAndPopup()
    {
        SellDinoShop shop = Object.FindObjectOfType<SellDinoShop>(true);
        if (shop == null)
        {
            GameObject shopGo = new GameObject("SellDinoShop");
            shopGo.transform.position = new Vector3(14.5f, -2.1f, -52f);
            Undo.RegisterCreatedObjectUndo(shopGo, "Create SellDinoShop");
            shop = Undo.AddComponent<SellDinoShop>(shopGo);
        }
        Undo.RecordObject(shop, "Build SellDinoShop Visuals");
        shop.BuildVisualsInEditor();
        EditorUtility.SetDirty(shop);

        HudController hud = Object.FindObjectOfType<HudController>(true);
        if (hud != null)
        {
            SellDinosPopup popup = hud.sellDinosPopup != null ? hud.sellDinosPopup : Object.FindObjectOfType<SellDinosPopup>(true);
            if (popup == null)
            {
                Canvas canvas = hud.GetComponentInParent<Canvas>();
                Transform parent = canvas != null ? canvas.transform : hud.transform;
                GameObject popGo = new GameObject("SellDinosPopup", typeof(RectTransform));
                popGo.transform.SetParent(parent, false);
                RectTransform rt = popGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                Undo.RegisterCreatedObjectUndo(popGo, "Create SellDinosPopup");
                popup = Undo.AddComponent<SellDinosPopup>(popGo);
            }
            Undo.RecordObject(hud, "Assign SellDinosPopup");
            Undo.RecordObject(popup, "Build SellDinosPopup");
            hud.sellDinosPopup = popup;
            popup.BuildInEditor();
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(popup);
        }

        if (!Application.isPlaying)
            EditorSceneManager.MarkAllScenesDirty();

        Selection.activeGameObject = shop.gameObject;
        Debug.Log("[Ride a Dino] Đã dựng SellDinoShop & SellDinosPopup để bạn tự do chỉnh sửa.");
    }

    [InitializeOnLoadMethod]
    static void AutoBuildDinoIndexIfMissing()
    {
        EditorApplication.delayCall += delegate
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying) return;
            HudController hud = Object.FindObjectOfType<HudController>(true);
            if (hud != null && hud.dinoIndexPopup == null)
                BuildDinoIndexPopup();
        };
    }

    [MenuItem("Tools/Ride a Dino/Build Dino Index Popup (HUD)")]
    public static void BuildDinoIndexPopup()
    {
        HudController hud = Object.FindObjectOfType<HudController>(true);
        if (hud == null)
        {
            Debug.LogWarning("[Ride a Dino] Không tìm thấy HudController trong Scene.");
            return;
        }

        DinoIndexPopup popup = hud.dinoIndexPopup != null ? hud.dinoIndexPopup : Object.FindObjectOfType<DinoIndexPopup>(true);
        if (popup == null)
        {
            Canvas canvas = hud.GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : hud.transform;
            Transform existing = parent.Find("DinoIndexPopup");
            GameObject popGo = existing != null ? existing.gameObject : new GameObject("DinoIndexPopup", typeof(RectTransform));
            if (existing == null)
            {
                popGo.transform.SetParent(parent, false);
                RectTransform rt = popGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                Undo.RegisterCreatedObjectUndo(popGo, "Create DinoIndexPopup");
            }
            popup = popGo.GetComponent<DinoIndexPopup>();
            if (popup == null) popup = Undo.AddComponent<DinoIndexPopup>(popGo);
        }

        Undo.RecordObject(hud, "Assign DinoIndexPopup");
        Undo.RecordObject(popup, "Build DinoIndexPopup");
        hud.dinoIndexPopup = popup;
        popup.BuildInEditor();
        EditorUtility.SetDirty(hud);
        EditorUtility.SetDirty(popup);

        if (!Application.isPlaying)
            EditorSceneManager.MarkAllScenesDirty();

        Selection.activeGameObject = popup.gameObject;
        Debug.Log("[Ride a Dino] Đã dựng DinoIndexPopup trong Scene để bạn tự do chỉnh sửa.");
    }

    static Transform FindBoardTransform(Transform root)
    {
        Transform best = null;
        float bestWidth = 0f;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c.GetComponent<Canvas>() != null) continue;
            float w = Mathf.Abs(c.localScale.x);
            if (w > bestWidth)
            {
                bestWidth = w;
                best = c;
            }
        }
        return best;
    }
}
