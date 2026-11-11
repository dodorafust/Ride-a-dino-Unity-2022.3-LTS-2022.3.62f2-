using UnityEngine;

public class DinoSpawner : MonoBehaviour
{
    public Dino dinoPrefab;
    public float interval = 5f;
    public int herdMin = 3, herdMax = 6;
    public float laneHalfWidth = 8f;
    public float spacing = 6f;
    public Vector2 speedRange = new Vector2(5f, 8f);

    float timer;

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = interval;
        SpawnHerd();
    }

    void SpawnHerd()
    {
        int count = Random.Range(herdMin, herdMax + 1);
        float baseX = Random.Range(-laneHalfWidth, laneHalfWidth);
        float herdSpeed = Random.Range(speedRange.x, speedRange.y);

        for (int i = 0; i < count; i++)
        {
            float x = Mathf.Clamp(baseX + Random.Range(-3f, 3f), -laneHalfWidth, laneHalfWidth);
            Vector3 pos = transform.position + new Vector3(x, 0f, i * spacing);
            Dino d = Instantiate(dinoPrefab, pos, Quaternion.identity);
            d.speed = herdSpeed + Random.Range(-0.5f, 0.5f);
        }
    }
}