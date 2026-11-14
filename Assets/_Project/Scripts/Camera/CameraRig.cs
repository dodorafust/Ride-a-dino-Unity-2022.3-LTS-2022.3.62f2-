using Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(-100)]   // chạy LateUpdate TRƯỚC CinemachineBrain
public class CameraRig : MonoBehaviour
{
    [Header("Tham chiếu")]
    public PlayerController player;
    public Transform cameraTarget;                  // con của Player, đặt ở gáy / đầu
    public CinemachineVirtualCamera vcam;

    [Header("Tốc độ xoay")]
    public float rotateSpeed = 1f;                  // nhân thêm với "Degrees Per Pixel" của LookPad
    public float rotateSmoothing = 0.08f;           // 0 = tức thì, lớn hơn = mượt hơn nhưng trễ hơn
    public Vector2 pitchLimits = new Vector2(-35f, 75f);

    [Header("Zoom")]
    public float distance = 8.5f;
    public float minDistance = 3f;
    public float maxDistance = 16f;
    public float zoomSpeed = 5f;
    public float zoomSmooth = 8f;

    [Header("Khi cưỡi Dino")]
    public float ridingPitch = 28f;
    public float ridingExtraDistance = 4f;
    public float alignDuration = 0.6f;
    public float alignSpeed = 6f;

    [Header("Ngước nhìn lên trời")]
    public float upwardZoomStartPitch = 15f;
    public float upwardMinDistanceRatio = 0.42f;
    public float upwardHeightBoost = 0.35f;

    Cinemachine3rdPersonFollow follow;
    float yaw, pitch = 22f, targetYaw, targetPitch, curDistance;
    Vector3 targetBaseLocalPos;
    PlayerState lastState;
    float alignTimer;

    void Awake()
    {
        if (player == null) player = FindObjectOfType<PlayerController>();
        if (vcam != null) follow = vcam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        if (cameraTarget != null) targetBaseLocalPos = cameraTarget.localPosition;
        curDistance = distance;
        targetPitch = pitch;
        if (player != null)
        {
            yaw = targetYaw = player.transform.eulerAngles.y;
            lastState = player.State;
        }
    }

    // Gọi khi hồi sinh để camera không trượt từ chỗ cũ sang
    public void Snap()
    {
        if (vcam != null) vcam.PreviousStateIsValid = false;
    }

    void LateUpdate()
    {
        if (player == null || cameraTarget == null) return;
        float dt = Time.deltaTime;

        PlayerState state = player.State;
        if (state != lastState)
        {
            if (state == PlayerState.Riding) alignTimer = alignDuration;
            lastState = state;
        }
        bool riding = state == PlayerState.Riding;

        // --- Xoay (kéo ngón tay / chuột) ---
        Vector2 look = GameInput.ConsumeLook() * rotateSpeed;
        if (look.sqrMagnitude > 0f)
        {
            targetYaw += look.x;
            targetPitch -= look.y;
            alignTimer = 0f;
        }
        else if (alignTimer > 0f)
        {
            alignTimer -= dt;
            float dinoYaw = player.CurrentDino ? player.CurrentDino.transform.eulerAngles.y : targetYaw;
            targetYaw = Mathf.LerpAngle(targetYaw, dinoYaw, dt * alignSpeed);
            targetPitch = Mathf.Lerp(targetPitch, ridingPitch, dt * alignSpeed);
        }
        targetPitch = Mathf.Clamp(targetPitch, pitchLimits.x, pitchLimits.y);

        float udt = Time.unscaledDeltaTime;
        float k = rotateSmoothing <= 0f ? 1f : 1f - Mathf.Exp(-udt / rotateSmoothing);
        yaw = Mathf.LerpAngle(yaw, targetYaw, k);
        pitch = Mathf.Lerp(pitch, targetPitch, k);
        cameraTarget.rotation = Quaternion.Euler(pitch, yaw, 0f);

        // --- Zoom + ngước nhìn trời thì kéo camera lại gần ---
        float zoom = GameInput.ConsumeZoom() - Input.GetAxis("Mouse ScrollWheel") * zoomSpeed * 10f;
        distance = Mathf.Clamp(distance + zoom, minDistance, maxDistance);

        float upT = 0f;
        if (pitch < upwardZoomStartPitch && pitchLimits.x < upwardZoomStartPitch)
        {
            upT = Mathf.Clamp01((upwardZoomStartPitch - pitch) / (upwardZoomStartPitch - pitchLimits.x));
            upT = upT * upT * (3f - 2f * upT);
        }

        float goal = Mathf.Max(2f, (distance + (riding ? ridingExtraDistance : 0f)) *
                                   Mathf.Lerp(1f, upwardMinDistanceRatio, upT));
        curDistance = Mathf.Lerp(curDistance, goal, 1f - Mathf.Exp(-zoomSmooth * dt));
        if (follow != null) follow.CameraDistance = curDistance;

        cameraTarget.localPosition = targetBaseLocalPos + Vector3.up * (upwardHeightBoost * upT);
    }
}