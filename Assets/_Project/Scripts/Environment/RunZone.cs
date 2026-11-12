using UnityEngine;

// Vùng đường chạy (BoxCollider, Is Trigger). Chỉ khi TOÀN BỘ cơ thể người chơi nằm trong vùng này
// thì mới bật tính năng bắt Dino. Chỉnh kích thước / vị trí bằng tay cho khớp map của bạn.
[RequireComponent(typeof(Collider))]
public class RunZone : MonoBehaviour
{
    Collider zone;

    void Reset() { GetComponent<Collider>().isTrigger = true; }
    void Awake() { zone = GetComponent<Collider>(); }

    // Chỉ xét mặt phẳng XZ
    public bool ContainsBody(Bounds b)
    {
        if (zone == null) zone = GetComponent<Collider>();
        Bounds z = zone.bounds;
        return b.min.x >= z.min.x && b.max.x <= z.max.x &&
               b.min.z >= z.min.z && b.max.z <= z.max.z;
    }

    public bool ContainsPoint(Vector3 p, float margin)
    {
        if (zone == null) zone = GetComponent<Collider>();
        Bounds z = zone.bounds;
        return p.x >= z.min.x - margin && p.x <= z.max.x + margin &&
               p.z >= z.min.z - margin && p.z <= z.max.z + margin;
    }

    void OnDrawGizmos()
    {
        Collider c = GetComponent<Collider>();
        if (c == null) return;
        Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.9f);
        Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
    }
}
