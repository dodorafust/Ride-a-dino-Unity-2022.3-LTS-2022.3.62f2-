using UnityEngine;

public class CameraController : MonoBehaviour
{
    public PlayerController player;
    public Vector3 lookOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Orbit & Zoom")]
    public float rotateSpeed = 4f;
    public float zoomSpeed = 5f;
    public float minDistance = 4f;
    public float maxDistance = 16f;
    public float distance = 8f;
    public Vector2 pitchLimits = new Vector2(10f, 70f);

    [Header("Walking / Riding view")]
    public float walkPitch = 25f;
    public float ridingPitch = 30f;
    public float ridingExtraDistance = 4f;
    public float autoAlignSpeed = 2f;

    float yaw, pitch, curDistance;
    Vector3 focus, focusVel;

    void Start()
    {
        pitch = walkPitch;
        curDistance = distance;
        focus = player.transform.position + lookOffset;
    }

    void LateUpdate()
    {
        if (player == null) return;

        bool riding = player.State == PlayerState.Riding;
        bool rotating = Input.GetMouseButton(1);   // giữ chuột phải để xoay

        if (rotating)
        {
            yaw   += Input.GetAxis("Mouse X") * rotateSpeed;
            pitch -= Input.GetAxis("Mouse Y") * rotateSpeed;
        }
        else if (riding)
        {
            // Tự động chuyển góc nhìn về phía sau lưng Dino
            yaw   = Mathf.LerpAngle(yaw, 0f, Time.deltaTime * autoAlignSpeed);
            pitch = Mathf.Lerp(pitch, ridingPitch, Time.deltaTime * autoAlignSpeed);
        }
        pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);

        distance = Mathf.Clamp(
            distance - Input.GetAxis("Mouse ScrollWheel") * zoomSpeed * 10f,
            minDistance, maxDistance);

        float goalDist = distance + (riding ? ridingExtraDistance : 0f);
        curDistance = Mathf.Lerp(curDistance, goalDist, Time.deltaTime * 4f);

        focus = Vector3.SmoothDamp(focus, player.transform.position + lookOffset,
                                   ref focusVel, 0.08f);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = focus - rot * Vector3.forward * curDistance;
        transform.rotation = rot;
    }
}