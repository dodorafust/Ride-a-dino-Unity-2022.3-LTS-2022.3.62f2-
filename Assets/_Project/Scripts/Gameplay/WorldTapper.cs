using UnityEngine;

// Chạm / click vào thế giới 3D: sàn chuồng, bảng nâng cấp, thú nhún (vật có IClickable).
// LookPad gọi WorldTapper.Tap(...) khi phát hiện 1 cú chạm ngắn (không kéo).
public class WorldTapper : MonoBehaviour
{
    public static WorldTapper Instance { get; private set; }

    public float maxRayDistance = 300f;
    readonly RaycastHit[] hits = new RaycastHit[24];

    void Awake() { Instance = this; }

    public static void Tap(Vector2 screenPos)
    {
        if (Instance != null) Instance.DoTap(screenPos);
    }

    void DoTap(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        PlayerController player = PlayerController.Instance;
        if (cam == null || player == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        int n = Physics.RaycastNonAlloc(ray, hits, maxRayDistance, ~0, QueryTriggerInteraction.Collide);

        IClickable best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            if (hits[i].distance >= bestDist) continue;
            IClickable c = hits[i].collider.GetComponentInParent<IClickable>();
            if (c == null) continue;
            best = c;
            bestDist = hits[i].distance;
        }
        if (best != null) best.OnClicked(player);
    }
}
