using UnityEngine;

public class CameraController : MonoBehaviour
{
    public PlayerController player;
    public Vector3 lookOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Zoom")]
    public float zoomSpeed = 5f;
    public float minDistance = 3f;
    public float maxDistance = 16f;
    public float distance = 8.5f;
    public Vector2 pitchLimits = new Vector2(-35f, 75f);

    [Header("Look Up Sky Zoom")]
    public float upwardZoomStartPitch = 15f;
    public float upwardMinDistanceRatio = 0.42f;
    public float upwardHeightOffsetBoost = 0.35f;
    public float groundClearance = 0.35f;

    [Header("Riding")]
    public float ridingPitch = 28f;
    public float ridingExtraDistance = 4f;
    public float alignDuration = 0.6f;
    public float alignSpeed = 6f;

    float yaw, pitch = 22f, curDistance;
    Vector3 focus, focusVel;
    PlayerState lastState;
    float alignTimer;

    public void Snap()
    {
        focus = player.transform.position + lookOffset;
        focusVel = Vector3.zero;
    }

    void Start()
    {
        curDistance = distance;
        focus = player.transform.position + lookOffset;
        lastState = player.State;
    }

    void LateUpdate()
    {
        if (player == null) return;

        PlayerState state = player.State;
        if (state != lastState)
        {
            if (state == PlayerState.Riding) alignTimer = alignDuration;
            lastState = state;
        }
        bool riding = state == PlayerState.Riding;

        Vector2 look = GameInput.ConsumeLook();
        if (look.sqrMagnitude > 0f)
        {
            yaw += look.x;
            pitch -= look.y;
            alignTimer = 0f;
        }
        else if (alignTimer > 0f)
        {
            alignTimer -= Time.deltaTime;
            float dinoYaw = player.CurrentDino ? player.CurrentDino.transform.eulerAngles.y : 0f;
            yaw = Mathf.LerpAngle(yaw, dinoYaw, Time.deltaTime * alignSpeed);
            pitch = Mathf.Lerp(pitch, ridingPitch, Time.deltaTime * alignSpeed);
        }
        pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);

        float zoom = GameInput.ConsumeZoom() + (-Input.GetAxis("Mouse ScrollWheel") * zoomSpeed * 10f);
        distance = Mathf.Clamp(distance + zoom, minDistance, maxDistance);

        float upT = 0f;
        if (pitch < upwardZoomStartPitch && pitchLimits.x < upwardZoomStartPitch)
        {
            upT = Mathf.Clamp01((upwardZoomStartPitch - pitch) / (upwardZoomStartPitch - pitchLimits.x));
            upT = upT * upT * (3f - 2f * upT);
        }

        float baseDist = distance + (riding ? ridingExtraDistance : 0f);
        float skyDistRatio = Mathf.Lerp(1f, upwardMinDistanceRatio, upT);
        float goalDist = Mathf.Max(2.0f, baseDist * skyDistRatio);
        curDistance = Mathf.Lerp(curDistance, goalDist, Time.deltaTime * 8f);

        Vector3 dynamicOffset = lookOffset + Vector3.up * (upwardHeightOffsetBoost * upT);
        focus = player.transform.position + dynamicOffset;

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredDir = -(rot * Vector3.forward);
        float finalDist = curDistance;

        if (player.groundMask.value != 0)
        {
            RaycastHit hit;
            if (Physics.SphereCast(focus, 0.25f, desiredDir, out hit, curDistance, player.groundMask, QueryTriggerInteraction.Ignore))
            {
                finalDist = Mathf.Max(1.5f, hit.distance - 0.1f);
            }
        }

        Vector3 camPos = focus + desiredDir * finalDist;
        float minCamY = player.transform.position.y - 0.95f + groundClearance;
        if (camPos.y < minCamY) camPos.y = minCamY;

        transform.position = camPos;
        transform.rotation = rot;
    }
}
