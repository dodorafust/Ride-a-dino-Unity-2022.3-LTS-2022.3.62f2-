using UnityEngine;

// Vùng chết (vực, nước, dung nham...). Cần Collider với Is Trigger. Người chơi chạm vào = chết và về điểm spawn.
[RequireComponent(typeof(Collider))]
public class DeathZone : MonoBehaviour
{
    void Reset() { GetComponent<Collider>().isTrigger = true; }

    void OnTriggerEnter(Collider other)
    {
        PlayerController p = other.GetComponentInParent<PlayerController>();
        if (p != null)
        {
            p.Kill(DeathReason.Fell);
            return;
        }

        Dino d = other.GetComponentInParent<Dino>();
        if (d != null)
        {
            if (d.IsRidden && PlayerController.Instance != null)
                PlayerController.Instance.Kill(DeathReason.Fell);
            else if (!d.IsRidden && !d.IsPetOwned)
                Destroy(d.gameObject);
        }
    }
}
