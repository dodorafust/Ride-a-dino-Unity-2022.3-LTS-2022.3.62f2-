using UnityEngine;

// Đặt object này ở đầu đường chạy, xoay sao cho trục xanh dương (forward) hướng dọc đường chạy.
public class DinoSpawner : MonoBehaviour
{
    public Dino dinoPrefab;
    public DinoSpawnTable table;      // loài / tỉ lệ / biến thể
    public RunZone zone;              // Dino ra khỏi vùng này sẽ biến mất
    public float interval = 5f;
    public float laneHalfWidth = 8f;
    public float spacing = 6f;
    public int maxAlive = 40;

    float timer;

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = interval;
        if (Dino.Alive >= maxAlive) return;
        SpawnHerd();
    }

    void SpawnHerd()
    {
        if (table == null || dinoPrefab == null)
        {
            Debug.LogWarning("DinoSpawner: thiếu Table hoặc Dino Prefab.");
            return;
        }

        DinoSpawnTable.Entry entry = table.PickEntry();
        if (entry == null) return;

        int count = Random.Range(entry.herdMin, entry.herdMax + 1);
        float baseX = Random.Range(-laneHalfWidth, laneHalfWidth);
        float yaw = transform.eulerAngles.y;

        for (int i = 0; i < count; i++)
        {
            float x = Mathf.Clamp(baseX + Random.Range(-3f, 3f), -laneHalfWidth, laneHalfWidth);
            Vector3 pos = transform.position + transform.right * x + transform.forward * (i * spacing);
            Dino d = Instantiate(dinoPrefab, pos, Quaternion.Euler(0f, yaw, 0f));
            var owned = new OwnedDino(entry.dino, table.PickVariant());
            float speed = entry.dino.runSpeed + Random.Range(-entry.speedJitter, entry.speedJitter);
            d.Setup(owned, speed, yaw, zone);
        }
    }
}
