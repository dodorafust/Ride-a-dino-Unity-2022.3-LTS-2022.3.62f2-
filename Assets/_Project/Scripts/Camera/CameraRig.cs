using Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(-100)]   // chạy LateUpdate TRƯỚC CinemachineBrain
public class CameraRig : MonoBehaviour
{
    public static CameraRig Instance { get; private set; }

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
    public Vector3 ridingTargetOffset = new Vector3(0f, -0.35f, 0f); // bù độ cao tâm ngắm về đúng gáy/đầu khi ngồi cưỡi
    [Range(0f, 1f)] public float ridingBobWeight = 0.2f;             // mức độ rung camera theo bước chạy Dino (0 = êm hoàn toàn)
    public float ridingSeatSmooth = 10f;                             // làm mượt rung của xương lưng Dino trên camera
    public float alignDuration = 0.6f;
    public float alignSpeed = 6f;

    [Header("Ngước nhìn lên trời")]
    public float upwardZoomStartPitch = 15f;
    public float upwardMinDistanceRatio = 0.42f;
    public float upwardHeightBoost = 0.35f;

    Cinemachine3rdPersonFollow follow;
    float yaw, pitch = 22f, targetYaw, targetPitch, curDistance;
    Vector3 targetBaseLocalPos, smoothedPetSeatLocal, sittingAnchorWorldPos;
    bool hasSmoothedPetSeat, hasSittingAnchor;
    PlayerState lastState;
    float alignTimer, shakeTimer, shakeDuration, shakeIntensity;

    void Awake()
    {
        Instance = this;
        if (GetComponent<CameraOcclusionFader>() == null)
            gameObject.AddComponent<CameraOcclusionFader>();
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

    public void Shake(float intensity, float duration)
    {
        if (intensity <= 0f || duration <= 0f) return;
        shakeIntensity = Mathf.Max(shakeIntensity, intensity);
        shakeDuration = Mathf.Max(shakeDuration, duration);
        shakeTimer = Mathf.Max(shakeTimer, duration);
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
        bool riding = state == PlayerState.Riding || (MountController.Instance != null && MountController.Instance.Mounted);

        // --- Xoay (kéo ngón tay / chuột) ---
        bool modalOpen = SellDinosPopup.Instance != null && SellDinosPopup.Instance.Visible;
        Vector2 look = modalOpen ? Vector2.zero : GameInput.ConsumeLook() * rotateSpeed;
        if (modalOpen) GameInput.ConsumeLook();
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
        float wheel = (modalOpen || IsPointerOverOpenPopup()) ? 0f : Input.GetAxis("Mouse ScrollWheel");
        float zoom = (modalOpen ? 0f : GameInput.ConsumeZoom()) - wheel * zoomSpeed * 10f;
        if (modalOpen) GameInput.ConsumeZoom();
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

        Vector3 localTarget = targetBaseLocalPos + (riding ? ridingTargetOffset : Vector3.zero)
                            + Vector3.up * (upwardHeightBoost * upT);
        if (state == PlayerState.Sitting)
        {
            hasSmoothedPetSeat = false;
            Vector3 desiredAnchor = player.CurrentMachine != null
                ? player.CurrentMachine.transform.position + Vector3.up * (targetBaseLocalPos.y + 1.1f)
                : cameraTarget.position;
            if (!hasSittingAnchor)
            {
                sittingAnchorWorldPos = cameraTarget.position;
                hasSittingAnchor = true;
            }
            sittingAnchorWorldPos = Vector3.Lerp(sittingAnchorWorldPos, desiredAnchor, 1f - Mathf.Exp(-8f * dt));
            cameraTarget.position = sittingAnchorWorldPos;
        }
        else if (state != PlayerState.Riding && MountController.Instance != null &&
                 MountController.Instance.TryGetSeatWorldPosition(out Vector3 petSeatPos))
        {
            hasSittingAnchor = false;
            Vector3 localSeat = player.transform.InverseTransformPoint(petSeatPos);
            if (!hasSmoothedPetSeat)
            {
                smoothedPetSeatLocal = localSeat;
                hasSmoothedPetSeat = true;
            }
            else
            {
                float seatK = ridingSeatSmooth <= 0f ? 1f : 1f - Mathf.Exp(-ridingSeatSmooth * dt);
                smoothedPetSeatLocal = Vector3.Lerp(smoothedPetSeatLocal, localSeat, seatK);
            }
            Vector3 bob = new Vector3(player.RideBounceOffset.x, player.RideBounceOffset.y, 0f) * ridingBobWeight;
            cameraTarget.position = player.transform.TransformPoint(smoothedPetSeatLocal)
                                  + player.transform.TransformVector(localTarget + bob);
        }
        else
        {
            hasSittingAnchor = false;
            hasSmoothedPetSeat = false;
            cameraTarget.localPosition = localTarget;
        }

        if (shakeTimer > 0f)
        {
            shakeTimer = Mathf.Max(0f, shakeTimer - dt);
            float kShake = shakeDuration > 0.001f ? (shakeTimer / shakeDuration) : 0f;
            cameraTarget.position += Random.insideUnitSphere * (shakeIntensity * kShake);
            if (shakeTimer <= 0f) shakeIntensity = 0f;
        }
    }

    static bool IsPointerOverOpenPopup()
    {
        if (PenPanel.Instance != null && PenPanel.Instance.Visible &&
            IsPointerInRect(PenPanel.Instance.transform as RectTransform))
            return true;
        if (InventoryPopup.Instance != null && InventoryPopup.Instance.Visible &&
            IsPointerInRect(InventoryPopup.Instance.transform as RectTransform))
            return true;
        if (SellDinosPopup.Instance != null && SellDinosPopup.Instance.Visible)
        {
            RectTransform rt = SellDinosPopup.Instance.panelRoot != null
                ? SellDinosPopup.Instance.panelRoot
                : SellDinosPopup.Instance.transform as RectTransform;
            if (IsPointerInRect(rt)) return true;
        }
        return false;
    }

    static bool IsPointerInRect(RectTransform rt)
    {
        if (rt == null) return false;
        Canvas canvas = rt.GetComponentInParent<Canvas>();
        Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, uiCam);
    }
}