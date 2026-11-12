using UnityEngine;

// Gắn lên đá / cây / vật cản bất kỳ có Collider. Dino đang được cưỡi chạm vào sẽ bị chậm lại.
public class Obstacle : MonoBehaviour
{
    [Range(0.1f, 1f)] public float slowMultiplier = 0.4f;
    public float slowDuration = 1.5f;
}
