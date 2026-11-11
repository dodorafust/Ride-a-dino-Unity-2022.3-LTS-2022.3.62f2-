using UnityEngine;

public class Dino : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 6f;
    public float steerSpeed = 6f;
    public float maxX = 9f;          // nửa bề rộng lane (trừ tường)
    public float laneEndZ = 300f;
    public float lifeTime = 40f;

    [Header("References")]
    public Transform seat;
    public GameObject targetRing;

    public bool IsRidden { get; set; }
    float steer;

    public void SetSteer(float s) => steer = s;
    public void ShowRing(bool v) { if (targetRing) targetRing.SetActive(v); }

    void Update()
    {
        Vector3 move = Vector3.forward * speed + Vector3.right * steer * steerSpeed;
        Vector3 p = transform.position + move * Time.deltaTime;
        p.x = Mathf.Clamp(p.x, -maxX, maxX);
        transform.position = p;

        if (IsRidden) return;

        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f || p.z > laneEndZ) Destroy(gameObject);
    }
}