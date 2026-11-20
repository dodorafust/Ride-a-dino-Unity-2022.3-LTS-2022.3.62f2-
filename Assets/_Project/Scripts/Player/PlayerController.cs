using System.Collections;
using UnityEngine;

public enum PlayerState { Walking, Jumping, Catching, Riding, Sitting, Dead }
public enum DeathReason { StaminaEmpty, WallHit, MissedJump, Fell, CarnivoreKill }

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Walk & Jump (Kyle Pittman + Tarodev Controller)")]
    public float walkSpeed = 9f;
    public float jumpHeight = 4.8f;                // độ cao đỉnh nhảy mong muốn (H, tính bằng mét)
    public float timeToJumpApex = 0.36f;           // thời gian từ lúc nhảy đến lúc đạt đỉnh (Th, giây)
    public float fallGravityMultiplier = 1.85f;    // hệ số tăng trọng lực khi rơi xuống (>1 giúp rơi nhanh, đầm, không bị lơ lửng)
    public float maxFallSpeed = 45f;               // giới hạn tốc độ rơi tối đa
    public float coyoteTime = 0.12f;               // thời gian ân huệ cho phép nhảy sau khi vừa bước hụt khỏi mép sàn
    public float jumpBufferTime = 0.14f;           // bấm nhảy sớm trước khi chạm đất vẫn tự bật nhảy ngay khi vừa đáp
    public float apexThreshold = 2.5f;             // ngưỡng |vy| gần đỉnh nhảy để kích hoạt Apex Hang & Apex Bonus
    [Range(0.2f, 1f)] public float apexGravityMultiplier = 0.55f; // làm dịu trọng lực ngay tại đỉnh parabol giúp đỉnh nhảy tròn và dễ lái
    public float apexBonusSpeed = 2.5f;            // cộng thêm chút tốc độ ngang & độ nhạy lái ở đỉnh nhảy
    public float airControl = 15f;

    [Header("Jump from Dino (nhảy chuyền)")]
    public float dinoJumpHeight = 6.5f;            // độ cao đỉnh nhảy khi nhảy chuyền bắt Dino
    public float dinoTimeToJumpApex = 0.55f;       // thời gian đạt đỉnh khi nhảy chuyền bắt Dino
    public float dinoJumpForward = 6f;             // cộng thêm vào tốc độ Dino/đi bộ để lao về phía trước
    public float maxJumpHeight = 0f;               // độ cao tối đa so với mặt đất khi nhảy (<= 0 để tự đạt đỉnh theo parabol)
    public float airTurnRate = 140f;               // độ/giây: xoay camera để bẻ hướng bay
    public float maxAirSpeed = 30f;
    public float catchLockoutAfterJump = 0.08f;    // chỉ để chống "ăn" 2 lần cùng 1 lần bấm

    [Header("Catch")]
    public float scanRadius = 20f;
    public float frontDotMin = 0f;
    public float aimMaxAngle = 65f;                // lệch tối đa so với tâm camera (độ, theo phương ngang)
    public float aimDistanceWeight = 1.2f;         // 1m xa hơn ~ lệch thêm 1.2 độ
    public float aimSwitchDegrees = 4f;            // chống nhấp nháy khi 2 con gần như thẳng hàng
    public float airAimSteerWeight = 1.5f;         // độ nhạy kéo nút di chuyển để chọn Dino trong vùng target khi đang bay
    public float catchPullSpeed = 45f;             // vận tốc thu dây (m/s): thời gian thu dây = khoảng cách đến Dino / vận tốc
    public float autoCatchHeight = 0.5f;
    public float airGlideDuration = 1.6f;          // thời gian cố định lướt chậm trên không (hết mà không target được Dino -> rơi chết)
    public float airGlideGravityScale = 0.25f;     // trọng lực nhẹ khi đang rơi chậm trên không
    public float airGlideMaxFallSpeed = 1.2f;      // tốc độ rơi tối đa khi đang lướt trên không
    public float airGlideMinHeight = 1.8f;         // độ cao tối thiểu giữ trên mặt đất trong lúc còn thời gian trên không
    public float steerDelayAfterMount = 0f;

    [Header("Dino Riding Steering")]
    [Tooltip("Bật: xoay camera sẽ tự điều hướng khi chạy/bay/cưỡi. Tắt: chỉ điều khiển hướng bằng joystick hoặc WASD.")]
    public bool cameraSteerEnabled = false;
    public bool limitDinoTurnAngle = false;        // tắt = cho phép Dino quay đầu tự do theo camera & nút di chuyển (không giới hạn góc)
    public float dinoMaxTurnAngle = 60f;           // góc quay đầu tối đa (độ) khi bật giới hạn

    [Header("Throw (Dino ăn cỏ hất văng khi hết Stamina)")]
    public float throwForward = 14f;
    public float throwUp = 15f;

    [Header("Chết khi đang cưỡi Dino (hết Stamina / đâm tường)")]
    public float deathThrowForward = 4.2f;         // lực hất thêm về phía trước mặt Dino
    public float deathThrowUp = 5.5f;              // lực nảy lên trước khi rơi xuống đất
    public float deathFallGravityScale = 1.5f;     // trọng lực lúc bị quăng rơi xuống đất

    [Header("Dino Ăn thịt hết Stamina (Dừng & Giết / Hook Cutscene)")]
    [Min(0f)] public float carnivoreStopHoldDuration = 0.45f; // thời gian dừng hẳn trước khi giết
    [Tooltip("Bật sau này khi đã gắn cutscene riêng cho sự kiện On Carnivore Kill.")]
    public bool useCustomCarnivoreKillCutscene = false;
    public UnityEngine.Events.UnityEvent onCarnivoreKill;

    [Header("Rơi khỏi map")]
    public float fallDeathY = -20f;            // thấp hơn mức này = chết (DeathZone là cách khác)

    [Header("References")]
    public LayerMask groundMask;
    public LayerMask dinoMask;
    public Transform cam;
    public CatchPreview preview;

    [Header("Model & Animation")]
    public Animator animator;
    public Transform ropeHand;                     // điểm xuất phát dây kéo (để trống = tự lấy tay phải Humanoid)
    public float modelTurnSpeed = 14f;
    public Vector3 ridingModelOffset = Vector3.zero;   // chỉnh độ cao/tiến lùi của nhân vật khi ngồi trên lưng Dino
    public float rideBounceHeight = 0.05f;             // biên độ nhún lên xuống khi cưỡi Dino chạy
    public float rideSwayX = 0.03f;                    // biên độ lắc ngang trái/phải theo nhịp bước chân Dino
    public float rideBounceBaseFreq = 5.5f;            // tần số nhún cơ bản (tự tăng tỉ lệ theo tốc độ Dino)
    [Range(0.3f, 1.5f)] public float rideBounceSharpness = 0.65f; // < 1: đạp chân nảy gắt có gia tốc hơn, = 1: sin đều
    public float rideBounceTilt = 2.5f;                // độ nghiêng lắc nhẹ người theo nhịp chạy của Dino
    [Range(0f, 1f)] public float airLegRelax = 0.5f;   // độ duỗi bớt cẳng chân trên không (0 = co sát mông như gốc, 1 = duỗi thẳng)
    [Range(0f, 1f)] public float airFootSync = 0.75f;  // đồng bộ cổ chân trên không để không bị duỗi xuống rồi vểnh ngược lên
    public float airFootPitch = 12f;                   // góc mũi chân khi ở trên không (độ, dương = hơi duỗi tự nhiên, âm = vểnh lên)
    public float landDipDepth = 0f;                    // độ nhún hạ model khi tiếp đất (0 = tắt để chân không bị lún xuống sàn)
    public float landDipDuration = 0.18f;              // thời gian nhún đệm tiếp đất rồi đứng thẳng dậy (giây)
    public float deathRespawnDelay = 1.3f;             // thời gian chờ phát animation chết trước khi về điểm spawn
    [Header("Khớp tay quăng dây (chỉnh trực tiếp)")]
    public float lassoSpinSpeed = 9f;                  // tốc độ quay vòng dây thòng lọng (rad/s)
    [Range(0f, 1f)] public float lassoShoulderLiftWeight = 0.45f;      // mức nâng bả vai theo bắp tay (tăng lên để hết lõm vai)
    public Vector3 lassoShoulderOffset = new Vector3(0f, 0f, 0f);        // góc bù (X,Y,Z) của xương bả vai (RightShoulder)
    public Vector3 lassoShoulderPosOffset = new Vector3(0f, 0.015f, 0f); // dịch nhẹ vị trí xương vai để làm phồng phần vai áo
    public Vector3 lassoUpperArmDir = new Vector3(0.82f, 0.25f, 0.38f);  // hướng bắp tay trên (X=phải, Y=lên, Z=trước)
    public float lassoUpperArmTwist = -70f;            // xoay trục bắp tay trên (độ, âm = xoay cùi chỏ xuống dưới/ra sau)
    public Vector3 lassoForearmDir = new Vector3(0.32f, 0.90f, 0.22f);   // hướng cẳng tay (gập khuỷu tay hướng lên trên tự nhiên)
    public float lassoForearmTwist = -35f;             // xoay trục cẳng tay (độ, âm = xoay lòng bàn tay hướng vào trong)
    public Vector3 lassoHandAngles = new Vector3(0f, -25f, 10f);         // góc xoay tinh chỉnh 3 trục (X,Y,Z) của cổ tay/bàn tay
    public Vector3 lassoGripLocalOffset = new Vector3(0f, 0.09f, 0f);    // vị trí điểm cầm dây trên bàn tay (tinh chỉnh live)
    public float lassoBobAmplitude = 4f;               // biên độ nhấp nâng lên/hạ xuống nhẹ của tay (độ)
    public float catchArmOutward = 0.65f;              // độ mở tay phải ra ngoài khi thu dây bắt Dino (tránh ép vào ngực/đầu)
    public float catchArmUpward = 0.25f;               // độ nâng tay phải lên nhẹ khi thu dây bắt Dino

    [Header("Tự động triệu hồi Pet (nút Q) khi vào RunZone")]
    public float petSummonRiseDuration = 0.36f;        // thời gian Dino trồi lên và đẩy người chơi lên lưng

    [Header("Vác Dino trên đầu (2 tay giơ lên)")]
    public Vector3 holdDinoOffset = new Vector3(0f, 1.85f, 0.12f);
    public Vector3 holdDinoEuler = Vector3.zero;
    [Min(0.1f)] public float holdDinoScaleMultiplier = 1f; // 1 = giữ nguyên 100% kích thước thật của model Dino
    [Range(0f, 1.5f)] public float holdBoneSwayWeight = 0.85f; // mức độ rung/nghiêng Dino theo nhịp xương ngực/cơ thể khi di chuyển
    public bool holdFollowPitch = true;                    // bật/tắt nghiêng trước-sau theo xương lưng Player
    [Range(0f, 20f)] public float holdMaxPitchAngle = 6f;  // giới hạn góc ngửa/chúi tối đa (độ) để không bị ngửa mặt lên trời
    public bool freezeHoldDinoAnimator = false;            // bật nếu muốn đóng băng animation Dino khi cầm trên tay
    public float holdLabelHeight = 0.8f;

    public PlayerState State { get; private set; } = PlayerState.Walking;
    public Dino CurrentDino { get { return currentDino; } }
    public DinoMachine CurrentMachine { get { return machine; } }
    public Vector3 Velocity { get { return velocity; } }
    public Vector2 RideBounceOffset { get; private set; }
    public float RideBounceOffsetY { get { return RideBounceOffset.y; } }
    public bool RunActive { get; private set; }     // đang trong chế độ bắt Dino
    // Đang ở trên không và có Dino được nhắm -> bấm nút nhảy là bắt ngay (UI đổi icon mũi tên xuống)
    public bool CatchReady { get { return State == PlayerState.Jumping && targetDino != null; } }

    CharacterController cc;
    RideSession ride;
    CameraRig camRig;
    RunZone runZone;
    DinoMachine machine;
    MountController mount;
    DinoSpawner spawner;

    Dino currentDino, targetDino, lastDino;
    Transform leftLowerLeg, rightLowerLeg, leftFoot, rightFoot,
              leftShoulder, leftUpperArm, leftLowerArm, leftHand,
              rightShoulder, rightUpperArm, rightLowerArm, ropeGrip, carryBone;
    Quaternion leftFootRestRot = Quaternion.identity, rightFootRestRot = Quaternion.identity;
    Quaternion leftShoulderBaseLocalRot = Quaternion.identity, leftArmBaseLocalRot = Quaternion.identity,
               leftLowerArmBaseLocalRot = Quaternion.identity, leftHandBaseLocalRot = Quaternion.identity;
    Quaternion lassoShoulderBaseLocalRot = Quaternion.identity, lassoArmBaseLocalRot = Quaternion.identity,
               lassoLowerArmBaseLocalRot = Quaternion.identity, lassoHandBaseLocalRot = Quaternion.identity;
    Vector3 lassoShoulderBaseLocalPos, leftShoulderBaseLocalPos, carryBoneRestLocalOffset, carryHandsRestLocal;
    Quaternion carryBoneRestLocalRot = Quaternion.identity;
    bool hasLassoBaseRot, hasLeftArmBaseRot, hasCarryBone, hasCarryIdleRef;
    float airLegBlend, airFootBlend, landDipTimer, rideBouncePhase, lassoSpinPhase, lassoArmBlend, holdArmBlend;
    OwnedDino currentHoldVisualDino;
    GameObject holdVisualRoot;
    Vector3 moveInput, catchStart, startPosition, visualBaseLocalPos;
    float catchTimer, activeCatchDuration, steerEnableTime, catchLockUntil, lastGroundedTime = -999f, lastJumpPressedTime = -999f, airTime;
    float lastMoveInputTime = -999f;
    const float runStopGrace = 0.05f;
    bool jumpPressed;
    bool jumpedThisAirborne;
    bool diesOnGround;
    bool deadFalling;
    bool glideExpired;
    bool waitMoveReleaseToStandUp;
    bool petAutoMountedOnRunEntry;
    bool petRising;
    float petRiseTimer;
    Vector3 petRiseStartLocal;
    Vector3 velocity;              // vận tốc tự quản (thay cho Rigidbody)
    CollisionFlags lastFlags;
    float currentUpGravity;        // g = -2H / Th^2 của cú nhảy hiện tại
    bool steerWithCamera;          // true sau khi nhảy khỏi Dino / bị hất: xoay camera = bẻ hướng bay
    bool hasIsRunning, hasIsJumping, hasIsGrounded, hasIsFalling, hasVerticalSpeed,
         hasJumpStart, hasLand, hasIsRiding, hasIsHolding, hasIsLassoing, hasJumpFromDino, hasDie, hasIsRunActive;
    readonly Collider[] hitBuf = new Collider[32];

    // Công thức Kyle Pittman (GDC "Building a Better Jump"): v0 = 2H / Th, g = -2H / Th^2
    float GetJumpVelocity(bool dinoJump)
    {
        float h = Mathf.Max(0.1f, dinoJump ? dinoJumpHeight : jumpHeight);
        float th = Mathf.Max(0.05f, dinoJump ? dinoTimeToJumpApex : timeToJumpApex);
        return (2f * h) / th;
    }

    float GetUpwardGravity(bool dinoJump)
    {
        float h = Mathf.Max(0.1f, dinoJump ? dinoJumpHeight : jumpHeight);
        float th = Mathf.Max(0.05f, dinoJump ? dinoTimeToJumpApex : timeToJumpApex);
        return (-2f * h) / (th * th);
    }

    bool HasBufferedJump()
    {
        return jumpPressed || (Time.time - lastJumpPressedTime <= jumpBufferTime);
    }

    void ConsumeJumpBuffer()
    {
        jumpPressed = false;
        lastJumpPressedTime = -999f;
    }

    float GetApexPoint(float vy)
    {
        if (apexThreshold <= 0.01f) return 0f;
        return 1f - Mathf.Clamp01(Mathf.Abs(vy) / apexThreshold);
    }

    void Awake()
    {
        Instance = this;
        cc = GetComponent<CharacterController>();
        currentUpGravity = GetUpwardGravity(false);
        ride = GetComponent<RideSession>();
        if (ride == null) ride = gameObject.AddComponent<RideSession>();
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            visualBaseLocalPos = animator.transform.localPosition;
            if (animator.isHuman)
            {
                if (ropeHand == null) ropeHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
                leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
                rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                if (leftUpperArm != null && leftLowerArm != null && leftHand != null)
                {
                    if (leftShoulder != null)
                    {
                        leftShoulderBaseLocalRot = leftShoulder.localRotation;
                        leftShoulderBaseLocalPos = leftShoulder.localPosition;
                    }
                    leftArmBaseLocalRot = leftUpperArm.localRotation;
                    leftLowerArmBaseLocalRot = leftLowerArm.localRotation;
                    leftHandBaseLocalRot = leftHand.localRotation;
                    hasLeftArmBaseRot = true;
                }
                carryBone = animator.GetBoneTransform(HumanBodyBones.UpperChest);
                if (carryBone == null) carryBone = animator.GetBoneTransform(HumanBodyBones.Chest);
                if (carryBone == null) carryBone = animator.GetBoneTransform(HumanBodyBones.Spine);
                if (carryBone != null)
                {
                    carryBoneRestLocalOffset = animator.transform.InverseTransformPoint(carryBone.position);
                    carryBoneRestLocalRot = Quaternion.Inverse(animator.transform.rotation) * carryBone.rotation;
                    hasCarryBone = true;
                }
                if (ropeHand != null)
                {
                    var gripGo = new GameObject("RopeGrip");
                    gripGo.transform.SetParent(ropeHand, false);
                    gripGo.transform.localPosition = new Vector3(0f, 0.09f, 0f);
                    ropeGrip = gripGo.transform;
                }
                if (rightUpperArm != null && rightLowerArm != null && rightUpperArm.parent != null)
                {
                    if (rightShoulder != null)
                    {
                        lassoShoulderBaseLocalRot = rightShoulder.localRotation;
                        lassoShoulderBaseLocalPos = rightShoulder.localPosition;
                    }
                    lassoArmBaseLocalRot = rightUpperArm.localRotation;
                    lassoLowerArmBaseLocalRot = rightLowerArm.localRotation;
                    if (ropeHand != null) lassoHandBaseLocalRot = ropeHand.localRotation;
                    hasLassoBaseRot = true;
                }
                leftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
                rightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
                leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                if (leftFoot != null) leftFootRestRot = leftFoot.localRotation;
                if (rightFoot != null) rightFootRestRot = rightFoot.localRotation;
            }
        }
        startPosition = transform.position;
    }

    void Start()
    {
        camRig = FindObjectOfType<CameraRig>();
        runZone = FindObjectOfType<RunZone>();
        mount = MountController.Instance;
        spawner = FindObjectOfType<DinoSpawner>();
        CacheAnimatorParams();
        if (runZone == null)
            Debug.LogWarning("PlayerController: không thấy RunZone, chế độ bắt Dino sẽ luôn bật.");
    }

    void Update()
    {
        if (State == PlayerState.Dead)
        {
            UpdateDead();
            return;
        }

        bool sellPopupOpen = SellDinosPopup.Instance != null && SellDinosPopup.Instance.Visible;
        if (sellPopupOpen)
        {
            GameInput.ConsumeJump();
            jumpPressed = false;
            moveInput = Vector3.zero;
            velocity.x = 0f;
            velocity.z = 0f;
        }
        else
        {
            jumpPressed = GameInput.ConsumeJump();
            if (jumpPressed) lastJumpPressedTime = Time.time;
            ReadMoveInput();
        }
        UpdateRunActive();

        if (State != PlayerState.Sitting && transform.position.y < fallDeathY)
        {
            Die(DeathReason.Fell);
            return;
        }

        switch (State)
        {
            case PlayerState.Walking: UpdateWalking(); break;
            case PlayerState.Jumping: UpdateJumping(); break;
            case PlayerState.Catching: UpdateCatching(); break;
            case PlayerState.Riding: UpdateRiding(); break;
            case PlayerState.Sitting: UpdateSitting(); break;
        }

        UpdateVisualAndAnimation();
    }

    // ---------- Helpers ----------
    Vector3 CamForwardFlat()
    {
        return Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
    }

    Vector3 RunForwardFlat()
    {
        if (runZone != null)
        {
            Vector3 rf = Vector3.ProjectOnPlane(runZone.transform.forward, Vector3.up);
            if (rf.sqrMagnitude > 0.001f) return rf.normalized;
        }
        return Vector3.forward;
    }

    void ReadMoveInput()
    {
        Vector2 m = GameInput.Move;
        Vector3 right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
        moveInput = CamForwardFlat() * m.y + right * m.x;
        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();
        if (moveInput.sqrMagnitude > 0.01f) lastMoveInputTime = Time.time;
    }

    // Mặt đất = layer Ground + mọi collider rắn ở layer Default (sàn Pen, bục, vật cản...).
    // Loại trừ Ignore Raycast / Water / UI / Player / Dino và mọi Trigger.
    int GroundCheckMask()
    {
        const int excluded = (1 << 2) | (1 << 4) | (1 << 5) | (1 << 6) | (1 << 7);
        return (groundMask.value | 1) & ~excluded;
    }

    bool CheckGroundRaw()
    {
        int mask = GroundCheckMask();
        float toBottom = cc.height * 0.5f - cc.center.y + cc.skinWidth;
        Vector3 origin = transform.position + cc.center;
        float r = cc.radius * 0.9f;
        float dist = toBottom - r + 0.15f;

        if (Physics.SphereCast(origin, r, Vector3.down, out RaycastHit hit, dist,
                               mask, QueryTriggerInteraction.Ignore))
            return true;
        return Physics.Raycast(origin, Vector3.down, toBottom + 0.15f, mask, QueryTriggerInteraction.Ignore);
    }

    bool IsGrounded()
    {
        if ((cc.enabled && cc.isGrounded) || CheckGroundRaw())
        {
            lastGroundedTime = Time.time;
            return true;
        }
        return Time.time - lastGroundedTime <= coyoteTime;
    }
    void UpdateRunActive()
    {
        Bounds b = new Bounds(transform.position + cc.center,
                              new Vector3(cc.radius * 2f, cc.height, cc.radius * 2f));
        bool inZone = runZone == null || runZone.ContainsBody(b);
        if (!inZone && State == PlayerState.Jumping)
        {
            // Bay ngược ra ngoài đường chạy -> hủy trạng thái chạy, đáp đất bình thường không chết
            diesOnGround = false;
            steerWithCamera = false;
            SetTarget(null);
            if (preview != null) preview.Hide();
        }
        // Nếu đang nhảy từ ngoài vùng chạy vào (RunActive đang false và đang ở trạng thái Jumping),
        // giữ nguyên RunActive = false cho tới khi tiếp đất thành công mới kích hoạt RunZone.
        bool canActivateInState = State != PlayerState.Jumping || RunActive;
        RunActive = (inZone && canActivateInState) || diesOnGround ||
                    State == PlayerState.Riding || State == PlayerState.Catching;
    }

    void CancelRunToNormal()
    {
        bool keepPetMounted = false;
        if (currentDino != null && currentDino.IsPetOwned)
        {
            Vector3 exitPos = currentDino.transform.position;
            if (Physics.Raycast(exitPos + Vector3.up * 3f, Vector3.down, out RaycastHit gHit, 12f,
                                GroundCheckMask(), QueryTriggerInteraction.Ignore))
                exitPos.y = gHit.point.y + 1.02f;
            else
                exitPos.y += 1.02f;
            float exitYaw = currentDino.transform.eulerAngles.y;

            transform.SetParent(null);
            transform.SetPositionAndRotation(exitPos, Quaternion.Euler(0f, exitYaw, 0f));
            SetBodyActive(true);
            lastGroundedTime = Time.time;

            if (!petAutoMountedOnRunEntry && mount != null)
            {
                // Đã cưỡi sẵn Pet đi vào rồi đi ra -> giữ nguyên đang ngồi trên Pet đi lại bình thường
                keepPetMounted = true;
                currentDino.SuppressPetReturnOnDestroy();
                Destroy(currentDino.gameObject);
                currentDino = null;
                mount.ResumeMountedFromRun();
            }
            else
            {
                // Tự triệu hồi nút Q khi vào vùng chạy -> đi ra thì trả Dino về Pen ngay
                Destroy(currentDino.gameObject);
                currentDino = null;
            }
        }
        else if (currentDino != null)
        {
            ReleaseDino();
        }

        petAutoMountedOnRunEntry = false;
        petRising = false;
        carnivoreKillPending = false;
        carnivoreKillTimer = 0f;
        ride.End();
        SetTarget(null);
        if (preview != null)
        {
            preview.Hide();
            preview.SetRope(null, null);
        }
        lastDino = null;
        diesOnGround = false;
        steerWithCamera = false;
        glideExpired = false;
        lastMoveInputTime = -999f;
        velocity = Vector3.zero;
        RunActive = false;
        State = PlayerState.Walking;
        if (animator != null)
        {
            animator.transform.localPosition = visualBaseLocalPos;
            animator.transform.localRotation = Quaternion.identity;
            if (hasIsRiding && !keepPetMounted) animator.SetBool("IsRiding", false);
            if (hasIsJumping) animator.SetBool("IsJumping", false);
            if (hasIsFalling) animator.SetBool("IsFalling", false);
            if (hasIsLassoing) animator.SetBool("IsLassoing", false);
        }
        HudController.Say("Exited run zone", 1.5f);
    }

    Vector3 RespawnPos()
    {
        GameManager gm = GameManager.Instance;
        Vector3 p = (gm != null && gm.respawnPoint != null) ? gm.respawnPoint.position : startPosition;

        // Dò xuống để đặt nhân vật đứng ĐÚNG trên mặt đất (PlayerSpawn có thể đặt lơ lửng)
        RaycastHit hit;
        if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out hit, 40f,
                            GroundCheckMask(), QueryTriggerInteraction.Ignore))
            p = hit.point + Vector3.up * 1.02f;
        return p;
    }

    // Cưỡi Dino / ngồi thú nhún / đang kéo dây: tắt CharacterController để tự di chuyển transform
    void SetBodyActive(bool on)
    {
        cc.enabled = on;
    }

    void TeleportTo(Vector3 p)
    {
        transform.SetParent(null);
        cc.enabled = false;                                 // CharacterController chỉ nhận vị trí mới khi tắt rồi bật lại
        transform.SetPositionAndRotation(p, Quaternion.identity);
        cc.enabled = true;
        velocity = Vector3.zero;
        currentUpGravity = GetUpwardGravity(false);
        steerWithCamera = false;
        lastGroundedTime = Time.time;
        if (camRig != null) camRig.Snap();
    }

    void DismountPet()
    {
        if (mount == null) mount = MountController.Instance;
        if (mount != null) mount.ForceDismount();
    }

    public void OnDismountFromPetSeat(Vector3 seatWorldPos, Quaternion exitRot)
    {
        if (State != PlayerState.Walking || RunActive) return;

        float targetRootY = Mathf.Max(transform.position.y, seatWorldPos.y - visualBaseLocalPos.y);
        if (targetRootY > transform.position.y + 0.05f)
        {
            bool wasEnabled = cc != null && cc.enabled;
            if (cc != null) cc.enabled = false;
            transform.position = new Vector3(seatWorldPos.x, targetRootY, seatWorldPos.z);
            if (cc != null) cc.enabled = wasEnabled;
        }

        if (animator != null)
        {
            animator.transform.localPosition = visualBaseLocalPos;
            Vector3 e = exitRot.eulerAngles;
            animator.transform.rotation = Quaternion.Euler(0f, e.y, 0f);
            if (hasIsRiding) animator.SetBool("IsRiding", false);
            if (hasIsGrounded) animator.SetBool("IsGrounded", false);
            if (hasIsFalling) animator.SetBool("IsFalling", true);
        }

        lastGroundedTime = -999f;
        jumpedThisAirborne = false;
        velocity.y = 0f;
        currentUpGravity = GetUpwardGravity(false);
        steerWithCamera = false;
        diesOnGround = false;
        glideExpired = false;
        airTime = 0f;
        landDipTimer = 0f;
        State = PlayerState.Jumping;
    }

    void PlayJumpTakeoffAnim()
    {
        if (mount != null && mount.Mounted) return;
        airLegBlend = 0f;
        airFootBlend = 0f;
        if (animator == null) return;
        if (hasIsJumping) animator.SetBool("IsJumping", true);
        if (hasIsFalling) animator.SetBool("IsFalling", false);
        if (hasIsGrounded) animator.SetBool("IsGrounded", false);
        if (hasJumpStart) animator.SetTrigger("JumpStart");
        int jumpUpHash = Animator.StringToHash("Base Layer.JumpUp");
        if (animator.HasState(0, jumpUpHash))
            animator.CrossFadeInFixedTime(jumpUpHash, 0.04f, 0, 0f);
    }

    // ---------- Walking ----------
    void EnterRunWithPetDino(bool autoSummoned)
    {
        if (mount == null || mount.SlotDino == null || mount.SlotDino.data == null) return;
        if (!autoSummoned && !mount.Mounted) return;
        if (autoSummoned && (mount.Mounted || mount.InRunAsDino)) return;

        if (spawner == null) spawner = FindObjectOfType<DinoSpawner>();
        Dino prefab = spawner != null ? spawner.dinoPrefab : null;
        if (prefab == null)
        {
            Dino[] found = Resources.FindObjectsOfTypeAll<Dino>();
            if (found != null && found.Length > 0) prefab = found[0];
        }
        if (prefab == null) return;

        Vector3 playerWorldBeforeMount = transform.position;
        OwnedDino petOwned = mount.SlotDino;
        float petSpeed = petOwned.data.runSpeed * mount.petSpeedMultiplier;
        float runYaw = spawner != null ? spawner.transform.eulerAngles.y : transform.eulerAngles.y;

        if (autoSummoned) mount.HandOffAutoSummonedToRunDino();
        else mount.HandOffToRunDino();

        Vector3 spawnPos = transform.position;
        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out RaycastHit gHit, 10f,
                            GroundCheckMask(), QueryTriggerInteraction.Ignore))
            spawnPos.y = gHit.point.y;

        Dino d = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, runYaw, 0f));
        d.Setup(petOwned, petSpeed, runYaw, runZone);
        d.MarkAsPetOwned();
        if (spawner != null && spawner.useActiveWindow)
            d.SetWindow(transform, spawner.transform.position, spawner.transform.forward, spawner.despawnBehind, spawner.despawnAhead);

        petAutoMountedOnRunEntry = autoSummoned;
        d.IsRidden = true;
        currentDino = d;
        velocity = Vector3.zero;
        SetBodyActive(false);

        if (autoSummoned)
            d.PlaySummonRise(petSummonRiseDuration);

        MountDino();

        if (autoSummoned && currentDino != null && currentDino.seat != null)
        {
            petRising = true;
            petRiseTimer = 0f;
            petRiseStartLocal = currentDino.seat.InverseTransformPoint(playerWorldBeforeMount);
            transform.localPosition = petRiseStartLocal;
        }
        else
        {
            petRising = false;
        }
    }

    void UpdateWalking()
    {
        if (RunActive && mount != null)
        {
            if (mount.Mounted)
            {
                EnterRunWithPetDino(false);
                if (State == PlayerState.Riding) return;
            }
            else if (!mount.InRunAsDino && mount.SlotDino != null)
            {
                EnterRunWithPetDino(true);
                if (State == PlayerState.Riding) return;
            }
        }

        float dt = Time.deltaTime;
        float moveSpeed = (mount != null && mount.Mounted) ? mount.CurrentMoveSpeed : walkSpeed;
        Vector3 moveDir = moveInput.sqrMagnitude > 0.01f
            ? moveInput.normalized
            : (RunActive ? (cameraSteerEnabled ? CamForwardFlat() : RunForwardFlat()) : Vector3.zero);
        Vector3 horiz = moveDir * moveSpeed;
        bool grounded = IsGrounded();

        if (grounded && velocity.y < 0f) velocity.y = -2f;      // dính xuống đất, đi dốc không bị nảy

        if (HasBufferedJump() && grounded)
        {
            ConsumeJumpBuffer();
            bool jumpedOffPet = RunActive && mount != null && mount.Mounted;
            if (jumpedOffPet) DismountPet();
            lastGroundedTime = -999f;
            jumpedThisAirborne = true;
            velocity.y = GetJumpVelocity(RunActive);
            currentUpGravity = GetUpwardGravity(RunActive);
            steerWithCamera = RunActive;
            diesOnGround = false;
            glideExpired = false;
            airTime = 0f;
            landDipTimer = 0f;
            State = PlayerState.Jumping;
            PlayJumpTakeoffAnim();

            // Tích phân Velocity Verlet cho frame nhảy đầu tiên: y += (v0 + 0.5 * g * dt) * dt
            velocity.x = horiz.x;
            velocity.z = horiz.z;
            float prevVy = velocity.y;
            velocity.y += currentUpGravity * dt;
            float avgVy = 0.5f * (prevVy + velocity.y);
            lastFlags = cc.Move(new Vector3(velocity.x, avgVy, velocity.z) * dt);
            if ((lastFlags & CollisionFlags.Above) != 0 && velocity.y > 0f) velocity.y = 0f;
            return;
        }
        else if (!grounded)
        {
            jumpedThisAirborne = false;
            currentUpGravity = GetUpwardGravity(RunActive);
            steerWithCamera = RunActive;
            diesOnGround = false;
            glideExpired = false;
            airTime = 0f;
            State = PlayerState.Jumping;
        }

        velocity.x = horiz.x;
        velocity.z = horiz.z;
        float walkGravity = GetUpwardGravity(false) * Mathf.Max(1f, fallGravityMultiplier);
        float oldVy = velocity.y;
        velocity.y += walkGravity * dt;
        float verletWalkVy = grounded ? velocity.y : 0.5f * (oldVy + velocity.y);
        lastFlags = cc.Move(new Vector3(velocity.x, verletWalkVy, velocity.z) * dt);
        if ((lastFlags & CollisionFlags.Above) != 0 && velocity.y > 0f) velocity.y = 0f;
    }

    // ---------- Jumping ----------
    void UpdateJumping()
    {
        float dt = Time.deltaTime;
        bool gliding = steerWithCamera && !glideExpired && airGlideDuration > 0f;
        if (gliding) airTime += dt;

        if (RunActive && !glideExpired)
        {
            UpdateCatchTarget();
            if (targetDino == null && preview != null)
                preview.Show(transform, transform.position, true, GroundCheckMask());
        }
        else
        {
            SetTarget(null);
            if (preview != null) preview.Hide();
        }
        bool canCatch = RunActive && !glideExpired && Time.time >= catchLockUntil;

        // Bấm nút nhảy / Space trên không = quăng dây bắt NGAY LẬP TỨC con đang được nhắm
        if (canCatch && jumpPressed)
        {
            Dino pick = targetDino != null ? targetDino : FindBestByCameraAim();
            if (pick != null) { ConsumeJumpBuffer(); BeginCatch(pick); return; }
        }

        // Coyote Jump: nếu vừa bước hụt khỏi mép sàn (chưa nhảy) và bấm nhảy trong khoảng coyoteTime -> cho nhảy luôn
        if (!jumpedThisAirborne && !diesOnGround && HasBufferedJump() && (Time.time - lastGroundedTime <= coyoteTime))
        {
            ConsumeJumpBuffer();
            lastGroundedTime = -999f;
            jumpedThisAirborne = true;
            velocity.y = GetJumpVelocity(RunActive);
            currentUpGravity = GetUpwardGravity(RunActive);
            steerWithCamera = RunActive;
            glideExpired = false;
            airTime = 0f;
            landDipTimer = 0f;
            PlayJumpTakeoffAnim();
        }

        // Hết thời gian cố định trên không: có Dino trong vòng target thì tự bắt, không có thì rơi xuống đất chết
        if (gliding && airTime >= airGlideDuration)
        {
            Dino auto = canCatch ? (targetDino != null ? targetDino : FindBestByCameraAim()) : null;
            if (auto != null) { BeginCatch(auto); return; }

            gliding = false;
            glideExpired = true;
            diesOnGround = true;
            currentUpGravity = GetUpwardGravity(false);
            SetTarget(null);
            if (preview != null) preview.Hide();
        }

        // --- Bay: xoay camera kết hợp nút di chuyển + tích phân Velocity Verlet & trọng lực bất đối xứng
        AirSteer(dt);
        float prevVy = velocity.y;
        float dispVy;

        if (gliding && prevVy <= 0f)
        {
            velocity.y += Physics.gravity.y * airGlideGravityScale * dt;
            if (velocity.y < -airGlideMaxFallSpeed) velocity.y = -airGlideMaxFallSpeed;
            dispVy = 0.5f * (prevVy + velocity.y);

            if (airGlideMinHeight > 0f && Physics.Raycast(transform.position, Vector3.down, out RaycastHit gHit,
                airGlideMinHeight + 2f, GroundCheckMask(), QueryTriggerInteraction.Ignore))
            {
                float minY = gHit.point.y + airGlideMinHeight;
                if (transform.position.y + dispVy * dt < minY)
                {
                    dispVy = Mathf.Max(0f, (minY - transform.position.y) / Mathf.Max(dt, 0.0001f));
                    velocity.y = dispVy;
                }
            }
        }
        else
        {
            // Apex Hang (Tarodev): làm dịu nhẹ trọng lực ngay gần đỉnh parabol (|vy| < apexThreshold) để đỉnh nhảy tròn, mượt
            float apexPoint = GetApexPoint(prevVy);
            float apexMul = Mathf.Lerp(1f, apexGravityMultiplier, apexPoint);
            float gUp = currentUpGravity * apexMul;
            float gDown = currentUpGravity * Mathf.Max(1f, fallGravityMultiplier) * apexMul;

            if (prevVy > 0f)
            {
                float nextVy = prevVy + gUp * dt;
                if (nextVy >= 0f)
                {
                    velocity.y = nextVy;
                    dispVy = 0.5f * (prevVy + velocity.y);
                }
                else
                {
                    // Vượt qua đỉnh parabol giữa frame: chia chính xác thời gian lên đỉnh (tApex) và thời gian rơi (tFall)
                    float tApex = Mathf.Clamp(-prevVy / gUp, 0f, dt);
                    float tFall = Mathf.Max(0f, dt - tApex);
                    velocity.y = gDown * tFall;
                    dispVy = (0.5f * prevVy * tApex + 0.5f * velocity.y * tFall) / Mathf.Max(dt, 0.0001f);
                }
            }
            else
            {
                velocity.y = prevVy + gDown * dt;
                if (maxFallSpeed > 0f && velocity.y < -maxFallSpeed) velocity.y = -maxFallSpeed;
                dispVy = 0.5f * (prevVy + velocity.y);
            }
        }

        if (maxJumpHeight > 0f && dispVy > 0f &&
            Physics.Raycast(transform.position, Vector3.down, out RaycastHit maxHit,
                            maxJumpHeight + 10f, GroundCheckMask(), QueryTriggerInteraction.Ignore))
        {
            float maxY = maxHit.point.y + maxJumpHeight;
            if (transform.position.y >= maxY)
            {
                velocity.y = 0f;
                dispVy = 0f;
            }
            else if (transform.position.y + dispVy * dt > maxY)
            {
                dispVy = Mathf.Max(0f, (maxY - transform.position.y) / Mathf.Max(dt, 0.0001f));
                velocity.y = 0f;
            }
        }

        lastFlags = cc.Move(new Vector3(velocity.x, dispVy, velocity.z) * dt);
        if ((lastFlags & CollisionFlags.Above) != 0 && velocity.y > 0f) velocity.y = 0f;

        if (!gliding && velocity.y <= 0.1f)
        {
            bool nearGround = Physics.Raycast(transform.position, Vector3.down,
                                              1f + autoCatchHeight, GroundCheckMask(), QueryTriggerInteraction.Ignore);
            if (nearGround && canCatch)
            {
                Dino auto = targetDino != null ? targetDino : FindNearest(true);
                if (auto != null) { BeginCatch(auto); return; }
            }
        }

        bool touchedGround = (cc.enabled && cc.isGrounded) || CheckGroundRaw() || (lastFlags & CollisionFlags.Below) != 0;
        if (!gliding && velocity.y <= 0.5f && touchedGround)
        {
            lastGroundedTime = Time.time;
            currentUpGravity = GetUpwardGravity(false);
            steerWithCamera = false;
            glideExpired = false;
            SetTarget(null);
            if (preview != null) preview.Hide();
            if (diesOnGround) { velocity.y = -2f; Die(DeathReason.MissedJump); return; }
            lastDino = null;

            // Jump Buffering: bấm nhảy sớm trước khi chạm đất (trong khoảng jumpBufferTime) -> bật nhảy tiếp luôn, không qua 1 frame Idle
            if (HasBufferedJump())
            {
                ConsumeJumpBuffer();
                if (!RunActive)
                {
                    Bounds b = new Bounds(transform.position + cc.center,
                                          new Vector3(cc.radius * 2f, cc.height, cc.radius * 2f));
                    if (runZone == null || runZone.ContainsBody(b))
                    {
                        RunActive = true;
                        steerWithCamera = true;
                    }
                }
                lastGroundedTime = -999f;
                jumpedThisAirborne = true;
                airTime = 0f;
                landDipTimer = 0f;
                velocity.y = GetJumpVelocity(RunActive);
                currentUpGravity = GetUpwardGravity(RunActive);
                PlayJumpTakeoffAnim();
                return;
            }

            jumpedThisAirborne = false;
            velocity.y = -2f;
            landDipTimer = landDipDuration;
            lastMoveInputTime = -999f;
            State = PlayerState.Walking;
            if (animator != null && hasLand) animator.SetTrigger("Land");
        }
    }

    // Sau khi nhảy khỏi Dino / trong đường chạy: xoay camera kết hợp nút di chuyển để bẻ hướng bay.
    void AirSteer(float dt)
    {
        Vector3 h = new Vector3(velocity.x, 0f, velocity.z);
        float apexPoint = GetApexPoint(velocity.y);
        float bonusSpd = apexBonusSpeed * apexPoint;
        float accel = airControl * (1f + 0.35f * apexPoint);

        if (steerWithCamera)
        {
            Vector3 wishDir;
            if (cameraSteerEnabled)
            {
                wishDir = moveInput.sqrMagnitude > 0.01f
                    ? (CamForwardFlat() * 0.6f + moveInput)
                    : CamForwardFlat();
                if (wishDir.sqrMagnitude < 0.001f) wishDir = CamForwardFlat();
            }
            else
            {
                Vector3 curDir = h.sqrMagnitude > 0.001f ? h.normalized : RunForwardFlat();
                wishDir = moveInput.sqrMagnitude > 0.01f ? moveInput : curDir;
            }
            wishDir.Normalize();

            float targetSpd = Mathf.Clamp(Mathf.Max(h.magnitude, walkSpeed + bonusSpd), walkSpeed, maxAirSpeed);
            Vector3 want = wishDir * targetSpd;
            if (airTurnRate > 0f && h.sqrMagnitude > 0.25f)
            {
                h = Vector3.RotateTowards(h, want, airTurnRate * Mathf.Deg2Rad * dt, accel * dt);
            }
            else
            {
                h = Vector3.MoveTowards(h, want, accel * dt);
            }
        }
        else
        {
            // Nhảy từ mặt đất ở khu Pen: tăng tốc & bẻ hướng theo joystick + Apex Bonus ở đỉnh nhảy
            float spd = (walkSpeed + bonusSpd) * (mount != null ? mount.SpeedMultiplier : 1f);
            h = Vector3.MoveTowards(h, moveInput * spd, accel * dt);
        }

        velocity.x = h.x;
        velocity.z = h.z;
    }

    // ---------- Chọn mục tiêu ----------
    bool IsSelectable(Dino d)
    {
        return d != null && !d.IsRidden && !d.Claimed && d != lastDino;
    }

    float DistTo(Dino d) { return Vector3.Distance(transform.position, d.seat.position); }

    Dino FindNearest(bool frontOnly)
    {
        Vector3 fwd = CamForwardFlat();
        int n = Physics.OverlapSphereNonAlloc(transform.position, scanRadius, hitBuf,
                                              dinoMask, QueryTriggerInteraction.Collide);
        Dino best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < n; i++)
        {
            Dino d = hitBuf[i].GetComponentInParent<Dino>();
            if (!IsSelectable(d)) continue;

            Vector3 to = d.seat.position - transform.position;
            if (frontOnly && !IsInFanSector(d.seat.position, scanRadius)) continue;

            float dist = to.magnitude;
            if (dist < bestDist) { bestDist = dist; best = d; }
        }
        return best;
    }

    // Kiểm tra mục tiêu có nằm trong góc quạt phía trước mặt người chơi không (không quét ra sau lưng)
    bool IsInFanSector(Vector3 worldPos, float maxDist)
    {
        Vector3 flatP = Vector3.ProjectOnPlane(worldPos - transform.position, Vector3.up);
        if (flatP.magnitude > maxDist) return false;
        if (flatP.sqrMagnitude <= 0.01f) return true;

        Vector3 fwd = CamForwardFlat();
        if (Vector3.Dot(flatP.normalized, fwd) < frontDotMin) return false;
        return Vector3.Angle(fwd, flatP) <= aimMaxAngle;
    }

    Vector3 AimDirection()
    {
        Vector3 fwd = CamForwardFlat();
        if (moveInput.sqrMagnitude <= 0.05f) return fwd;
        Vector3 dir = fwd + moveInput * airAimSteerWeight;
        return dir.sqrMagnitude > 0.001f ? dir.normalized : fwd;
    }

    // Chọn con Dino gần hướng nhắm (camera + kéo nút di chuyển) nhất trong góc quạt phía trước
    float AimScore(Dino d)
    {
        if (!IsInFanSector(d.seat.position, scanRadius * 1.15f)) return float.MaxValue;

        Vector3 flatP = Vector3.ProjectOnPlane(d.seat.position - transform.position, Vector3.up);
        Vector3 aimDir = AimDirection();
        float ang;
        if (moveInput.sqrMagnitude > 0.05f)
        {
            ang = flatP.sqrMagnitude > 0.01f ? Vector3.Angle(aimDir, flatP) : 0f;
        }
        else
        {
            Vector3 flatC = Vector3.ProjectOnPlane(d.seat.position - cam.position, Vector3.up);
            ang = flatC.sqrMagnitude > 0.01f ? Vector3.Angle(aimDir, flatC) : 0f;
        }
        return ang + flatP.magnitude * aimDistanceWeight;
    }

    Dino FindBestByCameraAim()
    {
        int n = Physics.OverlapSphereNonAlloc(transform.position, scanRadius, hitBuf,
                                              dinoMask, QueryTriggerInteraction.Collide);
        Dino best = null;
        float bestScore = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            Dino d = hitBuf[i].GetComponentInParent<Dino>();
            if (!IsSelectable(d)) continue;
            float s = AimScore(d);
            if (s < bestScore) { bestScore = s; best = d; }
        }
        return best;
    }

    void UpdateCatchTarget()
    {
        Dino best = FindBestByCameraAim();
        if (best == null || best == targetDino) { SetTarget(best); return; }

        bool oldValid = targetDino != null && IsSelectable(targetDino);
        float oldScore = oldValid ? AimScore(targetDino) : float.MaxValue;
        float switchMargin = moveInput.sqrMagnitude > 0.05f ? 0f : aimSwitchDegrees;
        if (oldScore == float.MaxValue || AimScore(best) + switchMargin < oldScore)
            SetTarget(best);
    }

    void SetTarget(Dino d)
    {
        if (targetDino == d) return;
        Dino old = targetDino;
        if (targetDino != null) targetDino.SetHighlight(false);
        targetDino = d;

        if (d != null)
        {
            d.SetHighlight(true);
            if (preview != null)
            {
                // Đổi mục tiêu: vòng tròn bay từ con cũ sang con mới; lần đầu thì bay ra từ người chơi
                Vector3 from = old != null ? old.transform.position
                             : new Vector3(transform.position.x, d.transform.position.y, transform.position.z);
                preview.Show(d.transform, from);
            }
        }
        else if (preview != null)
        {
            if (State == PlayerState.Jumping && RunActive && !glideExpired)
                preview.Show(transform, transform.position, true, GroundCheckMask());
            else
                preview.Hide();
        }
    }

    // ---------- Catching ----------
    void BeginCatch(Dino d)
    {
        ConsumeJumpBuffer();
        DismountPet();
        SetTarget(d);
        currentDino = d;
        d.IsRidden = true;

        velocity = Vector3.zero;
        SetBodyActive(false);

        catchStart = transform.position;
        catchTimer = 0f;
        float dist = Vector3.Distance(catchStart, d.seat != null ? d.seat.position : d.transform.position);
        activeCatchDuration = Mathf.Max(0.04f, dist / Mathf.Max(1f, catchPullSpeed));
        State = PlayerState.Catching;
        FaceCatchTarget(1f);

        if (preview != null)
        {
            preview.Hide();
            preview.SetRope(ropeGrip != null ? ropeGrip : (ropeHand != null ? ropeHand : transform), d.transform, d.bodyCenter);
        }
    }

    void UpdateCatching()
    {
        if (runZone != null && !runZone.ContainsPoint(transform.position, 0f))
        {
            CancelRunToNormal();
            return;
        }
        if (currentDino == null)
        {
            SetTarget(null);
            if (preview != null) preview.Hide();
            if (preview != null) preview.SetRope(null, null);
            SetBodyActive(true);
            glideExpired = false;
            airTime = 0f;
            State = PlayerState.Jumping;
            return;
        }

        catchTimer += Time.deltaTime;
        float duration = Mathf.Max(0.04f, activeCatchDuration);
        float t = Mathf.Clamp01(catchTimer / duration);
        float e = t * t * (3f - 2f * t);
        transform.position = Vector3.Lerp(catchStart, currentDino.seat.position, e);
        FaceCatchTarget(Mathf.Clamp01(20f * Time.deltaTime));

        if (t >= 1f) MountDino();
    }

    void FaceCatchTarget(float lerpT)
    {
        if (currentDino == null || animator == null) return;
        Vector3 dir = currentDino.seat.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
            animator.transform.rotation = lerpT >= 1f
                ? targetRot
                : Quaternion.Slerp(animator.transform.rotation, targetRot, lerpT);
        }
    }

    void MountDino()
    {
        SetTarget(null);
        if (preview != null) preview.SetRope(null, null);

        transform.SetParent(currentDino.seat);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        lastDino = null;
        diesOnGround = false;
        carnivoreKillPending = false;
        carnivoreKillTimer = 0f;
        currentDino.Boost();
        steerEnableTime = Time.time + steerDelayAfterMount;

        float stamina = GameManager.Instance != null ? GameManager.Instance.Stamina : 10f;
        ride.Begin(currentDino, stamina);
        State = PlayerState.Riding;

        if (animator != null)
        {
            animator.transform.localPosition = visualBaseLocalPos;
            if (hasIsJumping) animator.SetBool("IsJumping", false);
            if (hasIsFalling) animator.SetBool("IsFalling", false);
            if (hasIsLassoing) animator.SetBool("IsLassoing", false);
            if (hasIsRiding) animator.SetBool("IsRiding", true);
            int riding = Animator.StringToHash("Base Layer.Riding");
            if (animator.HasState(0, riding)) animator.CrossFadeInFixedTime(riding, 0.08f, 0);
        }
    }

    // ---------- Riding ----------
    bool carnivoreKillPending;
    float carnivoreKillTimer;

    void UpdateRiding()
    {
        // Chạy ngược ra khỏi đường chạy (về phía vạch xuất phát) -> hủy chạy, trả về bình thường
        Vector3 checkPos = currentDino != null ? currentDino.transform.position : transform.position;
        if (runZone != null && !runZone.ContainsPoint(checkPos, 1f))
        {
            CancelRunToNormal();
            return;
        }

        if (carnivoreKillPending)
        {
            if (currentDino != null) currentDino.SetLowStaminaState(true, 1f, true);
            carnivoreKillTimer -= Time.deltaTime;
            if (carnivoreKillTimer <= 0f)
            {
                carnivoreKillPending = false;
                TriggerCarnivoreKill(currentDino);
            }
            return;
        }

        // Húc tường = chết ngay (tường là object có script Wall)
        if (currentDino.TouchingWall) { Die(DeathReason.WallHit); return; }

        if (petRising)
        {
            petRiseTimer += Time.deltaTime;
            float dur = Mathf.Max(0.05f, petSummonRiseDuration);
            float t = Mathf.Clamp01(petRiseTimer / dur);
            float ease = t * t * (3f - 2f * t);
            transform.localPosition = Vector3.Lerp(petRiseStartLocal, Vector3.zero, ease);
            if (t >= 1f)
            {
                transform.localPosition = Vector3.zero;
                petRising = false;
            }
        }

        currentDino.limitTurnAngle = limitDinoTurnAngle;
        currentDino.maxSteerAngle = dinoMaxTurnAngle;

        if (Time.time >= steerEnableTime)
        {
            if (moveInput.sqrMagnitude > 0.01f)
            {
                float targetYaw = Quaternion.LookRotation(moveInput, Vector3.up).eulerAngles.y;
                currentDino.SetHeading(targetYaw);
                currentDino.SetSteer(0f);
            }
            else
            {
                if (cameraSteerEnabled)
                    currentDino.SetHeading(cam.eulerAngles.y);         // không bấm di chuyển -> chạy theo hướng camera
                else
                    currentDino.ResetHeading();
                currentDino.SetSteer(0f);
            }
        }

        switch (ride.Tick(Time.deltaTime))
        {
            case RideEvent.TameCompleted:
                OnTameCompleted();
                break;
            case RideEvent.StaminaEmptyDead:
                Die(DeathReason.StaminaEmpty);
                return;
            case RideEvent.StaminaEmptyThrow:
                ThrowFromDino();
                return;
            case RideEvent.StaminaEmptyCarnivoreKill:
                if (carnivoreStopHoldDuration > 0.01f)
                {
                    carnivoreKillPending = true;
                    carnivoreKillTimer = carnivoreStopHoldDuration;
                    if (currentDino != null) currentDino.SetLowStaminaState(true, 1f, true);
                    return;
                }
                TriggerCarnivoreKill(currentDino);
                return;
        }

        if (currentDino != null) currentDino.SetLowStaminaState(ride.IsLowStamina, ride.LowStamina01, false);

        if (jumpPressed)
        {
            ConsumeJumpBuffer();
            jumpedThisAirborne = true;
            // Giữ tốc độ của Dino đang cưỡi + cộng thêm dinoJumpForward để lao vượt lên phía trước
            float launchSpeed = Mathf.Min(maxAirSpeed, Mathf.Max(walkSpeed, currentDino.CurrentSpeed) + dinoJumpForward);
            Vector3 jumpFwd = cameraSteerEnabled
                ? CamForwardFlat()
                : Vector3.ProjectOnPlane(currentDino.transform.forward, Vector3.up).normalized;
            if (jumpFwd.sqrMagnitude < 0.001f) jumpFwd = RunForwardFlat();
            Vector3 vel = jumpFwd * launchSpeed
                        + Vector3.up * GetJumpVelocity(true);
            ReleaseDino();
            if (animator != null && hasJumpFromDino) animator.SetTrigger("JumpFromDino");
            if (animator != null && hasJumpStart) animator.SetTrigger("JumpStart");
            velocity = vel;
            currentUpGravity = GetUpwardGravity(true);
            steerWithCamera = true;
            diesOnGround = true;
            glideExpired = false;
            airTime = 0f;
            State = PlayerState.Jumping;
        }
    }

    void OnTameCompleted()
    {
        OwnedDino owned = currentDino.Owned;
        currentDino.Claim();
        if (GameManager.Instance != null) GameManager.Instance.Inventory.Add(owned);
        if (TameSecuredUI.Instance != null)
        {
            TameSecuredUI.Instance.NotifyTamed(owned);
            if (!TameSecuredUI.Instance.hideDefaultToastOnTame)
                HudController.Say("Tamed " + owned.Title + "!  $" + NumberFormat.Short(owned.MoneyPerSecond) + "/s", 2.5f);
        }
        else
        {
            HudController.Say("Tamed " + owned.Title + "!  $" + NumberFormat.Short(owned.MoneyPerSecond) + "/s", 2.5f);
        }
    }

    void ThrowFromDino()
    {
        Vector3 vel = currentDino.transform.forward * (currentDino.baseSpeed + throwForward)
                    + Vector3.up * throwUp;
        ReleaseDino();
        if (animator != null && hasJumpFromDino) animator.SetTrigger("JumpFromDino");
        if (animator != null && hasJumpStart) animator.SetTrigger("JumpStart");
        velocity = vel;
        currentUpGravity = GetUpwardGravity(true);
        steerWithCamera = true;
        diesOnGround = false;
        glideExpired = false;
        airTime = 0f;
        State = PlayerState.Jumping;
    }

    void ReleaseDino()
    {
        transform.SetParent(null);
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        SetBodyActive(true);
        lastGroundedTime = -1f;

        currentDino.ClearLowStaminaEffect();
        currentDino.SetSteer(0f);
        currentDino.ResetHeading();
        currentDino.IsRidden = false;

        lastDino = currentDino;
        currentDino = null;
        petAutoMountedOnRunEntry = false;
        petRising = false;
        carnivoreKillPending = false;
        carnivoreKillTimer = 0f;
        catchLockUntil = Time.time + catchLockoutAfterJump;
        ride.End();
    }

    void TriggerCarnivoreKill(Dino killer)
    {
        if (onCarnivoreKill != null) onCarnivoreKill.Invoke();
        if (useCustomCarnivoreKillCutscene) return;
        // Hiện tại dùng tính năng hất xuống đất chết; về sau bật useCustomCarnivoreKillCutscene để chạy cutscene riêng.
        Die(DeathReason.CarnivoreKill);
    }

    // ---------- Die / Respawn ----------
    public void Kill(DeathReason reason) { Die(reason); }

    void Die(DeathReason reason)
    {
        if (State == PlayerState.Dead) return;
        carnivoreKillPending = false;
        carnivoreKillTimer = 0f;

        Dino diedOnDino = currentDino;
        if (currentDino != null) ReleaseDino();
        ride.End();
        SetTarget(null);
        if (preview != null)
        {
            preview.Hide();
            preview.SetRope(null, null);
        }
        DismountPet();

        if (State == PlayerState.Sitting && machine != null)
        {
            machine.SetOccupied(false);
            machine = null;
        }

        diesOnGround = false;
        lastDino = null;
        catchLockUntil = 0f;
        landDipTimer = 0f;
        waitMoveReleaseToStandUp = false;
        lastMoveInputTime = -999f;
        State = PlayerState.Dead;

        if (animator != null)
        {
            animator.transform.localPosition = visualBaseLocalPos;
            if (hasIsJumping) animator.SetBool("IsJumping", false);
            if (hasIsFalling) animator.SetBool("IsFalling", false);
            if (hasIsRunning) animator.SetBool("IsRunning", false);
            if (hasIsRiding) animator.SetBool("IsRiding", false);
            if (hasIsLassoing) animator.SetBool("IsLassoing", false);
        }

        if (diedOnDino != null && reason != DeathReason.Fell)
        {
            Vector3 fwd = Vector3.ProjectOnPlane(diedOnDino.transform.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.001f) fwd = transform.forward;
            bool wallCrash = reason == DeathReason.WallHit;
            bool carnivoreKill = reason == DeathReason.CarnivoreKill;
            velocity = wallCrash
                ? Vector3.zero
                : (carnivoreKill
                    ? fwd * deathThrowForward + Vector3.up * deathThrowUp
                    : fwd * (diedOnDino.baseSpeed + deathThrowForward) + Vector3.up * deathThrowUp);
            if (animator != null)
            {
                animator.transform.rotation = Quaternion.LookRotation(fwd);
                if (hasIsGrounded) animator.SetBool("IsGrounded", false);
                if (hasIsJumping) animator.SetBool("IsJumping", !wallCrash);
                if (hasIsFalling) animator.SetBool("IsFalling", true);
                if (hasVerticalSpeed) animator.SetFloat("VerticalSpeed", velocity.y);
                int jumpUp = Animator.StringToHash("Base Layer.JumpUp");
                int freeFall = Animator.StringToHash("Base Layer.FreeFall");
                if (!wallCrash && animator.HasState(0, jumpUp)) animator.CrossFadeInFixedTime(jumpUp, 0.06f, 0);
                else if (animator.HasState(0, freeFall)) animator.CrossFadeInFixedTime(freeFall, 0.06f, 0);
            }
            deadFalling = true;
            SetBodyActive(true);
        }
        else
        {
            deadFalling = false;
            velocity = Vector3.zero;
            SetBodyActive(false);
            if (animator != null)
            {
                animator.transform.localPosition = visualBaseLocalPos;
                if (hasDie) animator.SetTrigger("Die");
            }
        }

        string msg;
        switch (reason)
        {
            case DeathReason.StaminaEmpty: msg = "You ran out of stamina before taming!"; break;
            case DeathReason.CarnivoreKill: msg = "The carnivore stopped and killed you!"; break;
            case DeathReason.WallHit: msg = "You crashed into the wall!"; break;
            case DeathReason.Fell: msg = "You fell off the map!"; break;
            default: msg = "You missed the jump and fell!"; break;
        }
        HudController.Say(msg, 3f);

        StartCoroutine(RespawnAfterDeathRoutine());
    }

    void UpdateDead()
    {
        if (!deadFalling) return;

        float dt = Time.deltaTime;
        velocity.y += Physics.gravity.y * deathFallGravityScale * dt;
        CollisionFlags flags = cc.Move(velocity * dt);
        if (animator != null)
        {
            if (hasIsJumping) animator.SetBool("IsJumping", true);
            if (hasIsFalling) animator.SetBool("IsFalling", true);
            if (hasVerticalSpeed) animator.SetFloat("VerticalSpeed", velocity.y);
        }

        bool grounded = cc.isGrounded || (flags & CollisionFlags.Below) != 0;
        if ((grounded && velocity.y <= 0f) || transform.position.y < fallDeathY)
            FinishDeadFall();
    }

    void FinishDeadFall()
    {
        deadFalling = false;
        velocity = Vector3.zero;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 10f,
                            GroundCheckMask(), QueryTriggerInteraction.Ignore))
        {
            float standOffset = cc != null ? (cc.height * 0.5f - cc.center.y) : 1f;
            transform.position = new Vector3(transform.position.x, hit.point.y + standOffset, transform.position.z);
        }
        SetBodyActive(false);
        if (animator != null)
        {
            animator.transform.localPosition = visualBaseLocalPos;
            if (hasIsJumping) animator.SetBool("IsJumping", false);
            if (hasIsFalling) animator.SetBool("IsFalling", false);
            if (hasIsGrounded) animator.SetBool("IsGrounded", true);
            if (hasVerticalSpeed) animator.SetFloat("VerticalSpeed", 0f);
            if (hasDie) animator.SetTrigger("Die");
        }
    }

    IEnumerator RespawnAfterDeathRoutine()
    {
        float waitFall = 0f;
        while (deadFalling && waitFall < 2f)
        {
            waitFall += Time.deltaTime;
            yield return null;
        }
        if (deadFalling) FinishDeadFall();

        yield return new WaitForSeconds(deathRespawnDelay);
        TeleportTo(RespawnPos());
        SetBodyActive(true);
        State = PlayerState.Walking;
        ResetAnimatorToIdle();
    }

    // ---------- Dino Machine ----------
    Coroutine sitMountRoutine;
    bool isMountingMachine;

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (State != PlayerState.Walking || isMountingMachine) return;
        DinoMachine m = hit.collider.GetComponentInParent<DinoMachine>();
        if (m != null) m.TryTouchMount(this);
    }

    public bool SitOn(DinoMachine m)
    {
        if (State != PlayerState.Walking || isMountingMachine || m == null || m.seat == null) return false;
        // Chỉ được ngồi lên Dino Machine khi tay không cầm gì và không cưỡi gì
        if (GameManager.Instance != null && GameManager.Instance.Inventory.Held != null) return false;
        if (mount != null && mount.Mounted) return false;
        machine = m;
        m.SetOccupied(true);
        waitMoveReleaseToStandUp = moveInput.sqrMagnitude > 0.05f;
        velocity = Vector3.zero;
        SetBodyActive(false);
        State = PlayerState.Sitting;
        if (sitMountRoutine != null) StopCoroutine(sitMountRoutine);
        sitMountRoutine = StartCoroutine(SitMountJumpRoutine(m));
        return true;
    }

    IEnumerator SitMountJumpRoutine(DinoMachine m)
    {
        isMountingMachine = true;
        Vector3 startPos = transform.position;
        Quaternion startRootRot = transform.rotation;
        Quaternion startVisualRot = animator != null ? animator.transform.localRotation : Quaternion.identity;

        if (animator != null)
        {
            if (hasIsRunning) animator.SetBool("IsRunning", false);
            if (hasIsRiding) animator.SetBool("IsRiding", false);
            if (hasIsJumping) animator.SetBool("IsJumping", true);
            if (hasJumpStart) animator.SetTrigger("JumpStart");
        }

        const float duration = 0.28f;
        const float jumpArc = 1.1f;
        float t = 0f;
        while (t < 1f)
        {
            if (m == null || m.seat == null || State != PlayerState.Sitting)
            {
                isMountingMachine = false;
                yield break;
            }
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            float smooth = t * t * (3f - 2f * t);
            Vector3 pos = Vector3.Lerp(startPos, m.seat.position, smooth);
            pos.y += Mathf.Sin(t * Mathf.PI) * jumpArc;
            transform.position = pos;
            transform.rotation = Quaternion.Slerp(startRootRot, m.seat.rotation, smooth);
            if (animator != null)
                animator.transform.localRotation = Quaternion.Slerp(startVisualRot, Quaternion.identity, smooth);
            yield return null;
        }

        transform.SetParent(m.seat);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        if (animator != null)
        {
            animator.transform.localRotation = Quaternion.identity;
            if (hasIsJumping) animator.SetBool("IsJumping", false);
            if (hasIsRiding) animator.SetBool("IsRiding", true);
            int riding = Animator.StringToHash("Base Layer.Riding");
            if (animator.HasState(0, riding)) animator.CrossFadeInFixedTime(riding, 0.08f, 0);
        }
        isMountingMachine = false;
        sitMountRoutine = null;
    }

    void UpdateSitting()
    {
        if (waitMoveReleaseToStandUp && moveInput.sqrMagnitude <= 0.05f)
            waitMoveReleaseToStandUp = false;
        if (isMountingMachine) return;
        if (jumpPressed || (!waitMoveReleaseToStandUp && moveInput.sqrMagnitude > 0.25f)) StandUp();
    }

    void StandUp()
    {
        if (sitMountRoutine != null) { StopCoroutine(sitMountRoutine); sitMountRoutine = null; }
        isMountingMachine = false;
        waitMoveReleaseToStandUp = false;
        Vector3 exit = machine != null ? machine.ExitPosition : transform.position + Vector3.up;
        transform.SetParent(null);
        transform.SetPositionAndRotation(exit, Quaternion.identity);
        SetBodyActive(true);
        velocity = Vector3.zero;
        if (machine != null) machine.SetOccupied(false);
        machine = null;
        State = PlayerState.Walking;
    }

    // Sau khi hồi sinh: xoá mọi trạng thái nhảy/rơi trên Animator, về thẳng Idle
    void ResetAnimatorToIdle()
    {
        if (animator == null) return;
        if (hasIsJumping) animator.SetBool("IsJumping", false);
        if (hasIsFalling) animator.SetBool("IsFalling", false);
        if (hasIsRunning) animator.SetBool("IsRunning", false);
        if (hasIsRiding) animator.SetBool("IsRiding", false);
        if (hasIsLassoing) animator.SetBool("IsLassoing", false);
        if (hasIsGrounded) animator.SetBool("IsGrounded", true);
        if (hasVerticalSpeed) animator.SetFloat("VerticalSpeed", 0f);
        if (hasJumpStart) animator.ResetTrigger("JumpStart");
        if (hasLand) animator.ResetTrigger("Land");
        if (hasJumpFromDino) animator.ResetTrigger("JumpFromDino");
        if (hasDie) animator.ResetTrigger("Die");
        int idle = Animator.StringToHash("Base Layer.Breathing Idle");
        if (animator.HasState(0, idle)) animator.Play(idle, 0, 0f);
    }

    // ---------- Model & Animation ----------
    void CacheAnimatorParams()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            switch (p.name)
            {
                case "IsRunning": hasIsRunning = true; break;
                case "IsJumping": hasIsJumping = true; break;
                case "IsGrounded": hasIsGrounded = true; break;
                case "IsFalling": hasIsFalling = true; break;
                case "VerticalSpeed": hasVerticalSpeed = true; break;
                case "JumpStart": hasJumpStart = true; break;
                case "Land": hasLand = true; break;
                case "IsRiding": hasIsRiding = true; break;
                case "IsHolding": hasIsHolding = true; break;
                case "IsLassoing": hasIsLassoing = true; break;
                case "JumpFromDino": hasJumpFromDino = true; break;
                case "Die": hasDie = true; break;
                case "IsRunActive": hasIsRunActive = true; break;
            }
        }
    }

    void ComputeRideBounce(float speedRatio, out Vector3 bobOffset, out Quaternion tiltRot)
    {
        rideBouncePhase += Time.deltaTime * rideBounceBaseFreq * speedRatio;
        // |sin(phase)|^sharpness tạo đáy nhọn (gia tốc đạp nảy nhanh) và đỉnh tròn (lơ lửng nhẹ) tự nhiên hơn sin thuần
        float stepWave = Mathf.Pow(Mathf.Abs(Mathf.Sin(rideBouncePhase)), Mathf.Clamp(rideBounceSharpness, 0.3f, 1.5f));
        float bobY = (stepWave * 2f - 1f) * rideBounceHeight;
        float bobX = Mathf.Sin(rideBouncePhase) * rideSwayX;
        float pitch = Mathf.Cos(rideBouncePhase * 2f) * rideBounceTilt;
        float roll = Mathf.Sin(rideBouncePhase) * (rideBounceTilt * 0.75f);

        RideBounceOffset = new Vector2(bobX, bobY);
        bobOffset = new Vector3(bobX, bobY, 0f);
        tiltRot = Quaternion.Euler(pitch, 0f, roll);
    }

    void UpdateVisualAndAnimation()
    {
        if (animator == null) return;
        Transform visual = animator.transform;

        bool mountedOnPet = mount != null && mount.Mounted;
        bool ridingOrSitting = State == PlayerState.Riding || (State == PlayerState.Sitting && !isMountingMachine) || mountedOnPet;

        // 1. Vị trí & góc xoay của Model
        if (State == PlayerState.Riding && currentDino != null)
        {
            if (currentDino.CurrentSpeed > 0.1f)
            {
                float speedRatio = Mathf.Clamp(currentDino.CurrentSpeed / Mathf.Max(1f, currentDino.animReferenceSpeed), 0.4f, 3f);
                ComputeRideBounce(speedRatio, out Vector3 bobOffset, out Quaternion tiltRot);
                visual.localPosition = visualBaseLocalPos + ridingModelOffset + bobOffset;
                visual.localRotation = tiltRot;
            }
            else
            {
                RideBounceOffset = Vector2.zero;
                visual.localPosition = visualBaseLocalPos + ridingModelOffset;
                visual.localRotation = Quaternion.identity;
            }
        }
        else if (State == PlayerState.Riding || State == PlayerState.Sitting)
        {
            RideBounceOffset = Vector2.zero;
            visual.localPosition = visualBaseLocalPos + ridingModelOffset;
            if (!isMountingMachine) visual.localRotation = Quaternion.identity;
        }
        else if (State == PlayerState.Catching)
        {
            RideBounceOffset = Vector2.zero;
            visual.localPosition = visualBaseLocalPos;
        }
        else
        {
            float dipY = 0f;
            if (!mountedOnPet && landDipTimer > 0f && landDipDuration > 0.001f)
            {
                landDipTimer = Mathf.Max(0f, landDipTimer - Time.deltaTime);
                float t = 1f - (landDipTimer / landDipDuration);
                dipY -= Mathf.Sin(t * Mathf.PI) * landDipDepth;
            }
            if (mountedOnPet)
            {
                float petSpeed = new Vector2(velocity.x, velocity.z).magnitude;
                Quaternion tiltRot = Quaternion.identity;
                if (State == PlayerState.Walking && petSpeed > 0.5f)
                {
                    float speedRatio = Mathf.Clamp(petSpeed / 15f, 0.4f, 3f);
                    ComputeRideBounce(speedRatio, out _, out tiltRot);
                }
                else
                {
                    RideBounceOffset = Vector2.zero;
                }
                visual.localPosition = visualBaseLocalPos + ridingModelOffset;
                visual.rotation = mount.VisualRotation * tiltRot;
            }
            else
            {
                RideBounceOffset = Vector2.zero;
                visual.localPosition = visualBaseLocalPos + new Vector3(0f, dipY, 0f);
                Vector3 flatMove = (State == PlayerState.Jumping && steerWithCamera) || (State == PlayerState.Walking && RunActive)
                    ? new Vector3(velocity.x, 0f, velocity.z)
                    : new Vector3(moveInput.x, 0f, moveInput.z);
                if (flatMove.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(flatMove);
                    visual.rotation = Quaternion.Slerp(visual.rotation, targetRot, modelTurnSpeed * Time.deltaTime);
                }
            }
        }

        // 2. Cập nhật tham số Animator (chỉ gửi các tham số đã tạo trong Animator Controller)
        bool hasMoveAnim = moveInput.sqrMagnitude > 0.01f || (Time.time - lastMoveInputTime) <= runStopGrace;
        bool isRunning = State == PlayerState.Walking && (hasMoveAnim || RunActive) && !mountedOnPet;
        bool isJumping = !mountedOnPet && (State == PlayerState.Jumping || State == PlayerState.Catching || isMountingMachine);
        bool isGrounded = State == PlayerState.Walking || State == PlayerState.Riding || (State == PlayerState.Sitting && !isMountingMachine) || mountedOnPet;
        float fallThreshold = GetJumpVelocity(RunActive) * 0.35f;
        bool isFalling = !mountedOnPet && State == PlayerState.Jumping && velocity.y <= fallThreshold;
        OwnedDino heldDino = GameManager.Instance != null ? GameManager.Instance.Inventory.Held : null;
        bool inWildRideOrSit = State == PlayerState.Riding || (State == PlayerState.Sitting && !isMountingMachine);
        bool isHolding = heldDino != null && !RunActive && !inWildRideOrSit && State != PlayerState.Catching && State != PlayerState.Dead;
        bool isLassoing = State == PlayerState.Catching;

        UpdateHoldVisual(heldDino, isHolding);

        if (hasIsRunning) animator.SetBool("IsRunning", isRunning);
        if (hasIsJumping) animator.SetBool("IsJumping", isJumping);
        if (hasIsGrounded) animator.SetBool("IsGrounded", isGrounded);
        if (hasIsFalling) animator.SetBool("IsFalling", isFalling);
        if (hasVerticalSpeed) animator.SetFloat("VerticalSpeed", velocity.y);
        if (hasIsRiding) animator.SetBool("IsRiding", ridingOrSitting);
        if (hasIsHolding) animator.SetBool("IsHolding", isHolding);
        if (hasIsLassoing) animator.SetBool("IsLassoing", isLassoing);
        if (hasIsRunActive) animator.SetBool("IsRunActive", RunActive);
    }

    void LateUpdate()
    {
        bool mountedOnPet = mount != null && mount.Mounted;
        if (State != PlayerState.Riding && State != PlayerState.Catching && State != PlayerState.Dead &&
            animator != null && mount != null && mount.TryGetSeatWorldPosition(out Vector3 seatWorldPos))
        {
            Vector3 bob = new Vector3(RideBounceOffset.x, RideBounceOffset.y, 0f);
            animator.transform.position = seatWorldPos + transform.TransformVector(visualBaseLocalPos + ridingModelOffset + bob);
        }

        bool inAir = !mountedOnPet && (
                     (State == PlayerState.Jumping && velocity.y <= GetJumpVelocity(RunActive) * 0.5f) ||
                     State == PlayerState.Catching ||
                     (State == PlayerState.Dead && deadFalling));
        float dt = Time.deltaTime;
        float rate = inAir ? 8f : 14f;
        airLegBlend = Mathf.MoveTowards(airLegBlend, inAir ? airLegRelax : 0f, rate * dt);
        airFootBlend = Mathf.MoveTowards(airFootBlend, inAir ? airFootSync : 0f, rate * dt);

        if (airLegBlend > 0.001f)
        {
            if (leftLowerLeg != null)
                leftLowerLeg.localRotation = Quaternion.Slerp(leftLowerLeg.localRotation, Quaternion.identity, airLegBlend);
            if (rightLowerLeg != null)
                rightLowerLeg.localRotation = Quaternion.Slerp(rightLowerLeg.localRotation, Quaternion.identity, airLegBlend);
        }

        if (airFootBlend > 0.001f)
        {
            Quaternion pitchRot = Quaternion.Euler(airFootPitch, 0f, 0f);
            if (leftFoot != null)
                leftFoot.localRotation = Quaternion.Slerp(leftFoot.localRotation, leftFootRestRot * pitchRot, airFootBlend);
            if (rightFoot != null)
                rightFoot.localRotation = Quaternion.Slerp(rightFoot.localRotation, rightFootRestRot * pitchRot, airFootBlend);
        }

        // Tay phải quay dây thòng lọng khi ở trong đường chạy (kể cả lúc đang cưỡi Dino); chỉ tắt khi ra khỏi RunZone hoặc chết/ngồi máy
        bool twirlLasso = RunActive && State != PlayerState.Dead && State != PlayerState.Sitting && State != PlayerState.Catching;
        bool aimCatch = State == PlayerState.Catching && currentDino != null && currentDino.seat != null;
        if (twirlLasso) lassoSpinPhase += dt * lassoSpinSpeed;
        lassoArmBlend = Mathf.MoveTowards(lassoArmBlend, (twirlLasso || aimCatch) ? 1f : 0f, 10f * dt);

        if (lassoArmBlend > 0.001f && animator != null && rightUpperArm != null && rightLowerArm != null && ropeHand != null)
        {
            if (aimCatch && hasLassoBaseRot)
            {
                Quaternion animShoulder = rightShoulder != null ? rightShoulder.rotation : Quaternion.identity;
                Quaternion animUpper = rightUpperArm.rotation;
                Quaternion animLower = rightLowerArm.rotation;
                Quaternion animHand = ropeHand.rotation;

                if (rightShoulder != null) rightShoulder.localRotation = lassoShoulderBaseLocalRot;
                rightUpperArm.localRotation = lassoArmBaseLocalRot;
                rightLowerArm.localRotation = lassoLowerArmBaseLocalRot;
                ropeHand.localRotation = lassoHandBaseLocalRot;

                Transform root = animator.transform;
                Vector3 catchTarget = currentDino.seat != null
                    ? currentDino.seat.position
                    : currentDino.transform.TransformPoint(currentDino.bodyCenter);
                Vector3 toTarget = catchTarget - rightUpperArm.position;
                toTarget = Vector3.Dot(toTarget, root.forward) > 0.1f ? toTarget.normalized : root.forward;

                Vector3 upperGoal = (toTarget + root.right * catchArmOutward + root.up * catchArmUpward).normalized;
                Vector3 lowerGoal = (toTarget + root.right * (catchArmOutward * 0.45f) + root.up * (catchArmUpward * 0.5f)).normalized;

                if (rightShoulder != null)
                {
                    Vector3 shDir = (rightUpperArm.position - rightShoulder.position).normalized;
                    if (shDir.sqrMagnitude > 0.0001f)
                        rightShoulder.rotation = Quaternion.Slerp(rightShoulder.rotation,
                            Quaternion.FromToRotation(shDir, upperGoal) * rightShoulder.rotation, lassoShoulderLiftWeight);
                    rightShoulder.localRotation = rightShoulder.localRotation * Quaternion.Euler(lassoShoulderOffset);
                    rightShoulder.localPosition = Vector3.Lerp(lassoShoulderBaseLocalPos,
                        lassoShoulderBaseLocalPos + lassoShoulderPosOffset, lassoArmBlend);
                }

                Vector3 curUpper = (rightLowerArm.position - rightUpperArm.position).normalized;
                if (curUpper.sqrMagnitude > 0.0001f)
                {
                    rightUpperArm.rotation = Quaternion.FromToRotation(curUpper, upperGoal) * rightUpperArm.rotation;
                    rightUpperArm.rotation = Quaternion.AngleAxis(lassoUpperArmTwist, upperGoal) * rightUpperArm.rotation;
                }

                Vector3 curLower = (ropeHand.position - rightLowerArm.position).normalized;
                if (curLower.sqrMagnitude > 0.0001f)
                {
                    rightLowerArm.rotation = Quaternion.FromToRotation(curLower, lowerGoal) * rightLowerArm.rotation;
                    rightLowerArm.rotation = Quaternion.AngleAxis(lassoForearmTwist, lowerGoal) * rightLowerArm.rotation;
                }

                ropeHand.localRotation = ropeHand.localRotation * Quaternion.Euler(lassoHandAngles);

                if (lassoArmBlend < 0.999f)
                {
                    if (rightShoulder != null) rightShoulder.rotation = Quaternion.Slerp(animShoulder, rightShoulder.rotation, lassoArmBlend);
                    rightUpperArm.rotation = Quaternion.Slerp(animUpper, rightUpperArm.rotation, lassoArmBlend);
                    rightLowerArm.rotation = Quaternion.Slerp(animLower, rightLowerArm.rotation, lassoArmBlend);
                    ropeHand.rotation = Quaternion.Slerp(animHand, ropeHand.rotation, lassoArmBlend);
                }
            }
            else if (aimCatch)
            {
                Transform root = animator.transform;
                Vector3 catchTarget = currentDino.seat != null
                    ? currentDino.seat.position
                    : currentDino.transform.TransformPoint(currentDino.bodyCenter);
                Vector3 toTarget = catchTarget - rightUpperArm.position;
                toTarget = Vector3.Dot(toTarget, root.forward) > 0.1f ? toTarget.normalized : root.forward;
                Vector3 armDir = (toTarget + root.right * catchArmOutward + root.up * catchArmUpward).normalized;
                Vector3 curUpper = (rightLowerArm.position - rightUpperArm.position).normalized;
                if (curUpper.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetUpperRot = Quaternion.FromToRotation(curUpper, armDir) * rightUpperArm.rotation;
                    rightUpperArm.rotation = Quaternion.Slerp(rightUpperArm.rotation, targetUpperRot, lassoArmBlend);
                }
            }
            else if (hasLassoBaseRot)
            {
                Quaternion animShoulder = rightShoulder != null ? rightShoulder.rotation : Quaternion.identity;
                Quaternion animUpper = rightUpperArm.rotation;
                Quaternion animLower = rightLowerArm.rotation;
                Quaternion animHand = ropeHand.rotation;

                // Đưa về dáng T-pose gốc để tính khớp chuẩn xác mỗi frame (chỉnh Inspector ăn ngay trong Play Mode)
                if (rightShoulder != null) rightShoulder.localRotation = lassoShoulderBaseLocalRot;
                rightUpperArm.localRotation = lassoArmBaseLocalRot;
                rightLowerArm.localRotation = lassoLowerArmBaseLocalRot;
                ropeHand.localRotation = lassoHandBaseLocalRot;

                Transform root = animator.transform;
                Vector3 upperGoal = root.TransformDirection(lassoUpperArmDir.sqrMagnitude > 0.001f ? lassoUpperArmDir.normalized : new Vector3(0.8f, 0.25f, 0.35f));
                float liftBob = Mathf.Sin(lassoSpinPhase) * lassoBobAmplitude;
                Vector3 lowerGoal = Quaternion.AngleAxis(liftBob, root.forward)
                                  * root.TransformDirection(lassoForearmDir.sqrMagnitude > 0.001f ? lassoForearmDir.normalized : new Vector3(0.3f, 0.9f, 0.2f));

                // 1. Bả vai (RightShoulder): nâng nhẹ theo hướng bắp tay + góc bù + dịch vị trí làm phồng vai
                if (rightShoulder != null)
                {
                    Vector3 shDir = (rightUpperArm.position - rightShoulder.position).normalized;
                    if (shDir.sqrMagnitude > 0.0001f)
                        rightShoulder.rotation = Quaternion.Slerp(rightShoulder.rotation,
                            Quaternion.FromToRotation(shDir, upperGoal) * rightShoulder.rotation, lassoShoulderLiftWeight);
                    rightShoulder.localRotation = rightShoulder.localRotation * Quaternion.Euler(lassoShoulderOffset);
                    rightShoulder.localPosition = Vector3.Lerp(lassoShoulderBaseLocalPos,
                        lassoShoulderBaseLocalPos + lassoShoulderPosOffset, lassoArmBlend);
                }

                // 2. Bắp tay trên (RightUpperArm): đưa sang phải-trước vừa phải + xoay trục để cùi chỏ hướng xuống
                Vector3 curUpper = (rightLowerArm.position - rightUpperArm.position).normalized;
                if (curUpper.sqrMagnitude > 0.0001f)
                {
                    rightUpperArm.rotation = Quaternion.FromToRotation(curUpper, upperGoal) * rightUpperArm.rotation;
                    rightUpperArm.rotation = Quaternion.AngleAxis(lassoUpperArmTwist, upperGoal) * rightUpperArm.rotation;
                }

                // 3. Cẳng tay / Khuỷu tay (RightLowerArm): gập khuỷu tay hướng lên trên (dáng chữ L tự nhiên) + xoay trục hướng lòng bàn tay vào trong
                Vector3 curLower = (ropeHand.position - rightLowerArm.position).normalized;
                if (curLower.sqrMagnitude > 0.0001f)
                {
                    rightLowerArm.rotation = Quaternion.FromToRotation(curLower, lowerGoal) * rightLowerArm.rotation;
                    rightLowerArm.rotation = Quaternion.AngleAxis(lassoForearmTwist, lowerGoal) * rightLowerArm.rotation;
                }

                // 4. Bàn tay / Cổ tay (RightHand): xoay tinh chỉnh 3 trục
                ropeHand.localRotation = ropeHand.localRotation * Quaternion.Euler(lassoHandAngles);

                if (lassoArmBlend < 0.999f)
                {
                    if (rightShoulder != null) rightShoulder.rotation = Quaternion.Slerp(animShoulder, rightShoulder.rotation, lassoArmBlend);
                    rightUpperArm.rotation = Quaternion.Slerp(animUpper, rightUpperArm.rotation, lassoArmBlend);
                    rightLowerArm.rotation = Quaternion.Slerp(animLower, rightLowerArm.rotation, lassoArmBlend);
                    ropeHand.rotation = Quaternion.Slerp(animHand, ropeHand.rotation, lassoArmBlend);
                }
            }

            if (ropeGrip != null)
                ropeGrip.localPosition = lassoGripLocalOffset;
        }
        else if (hasLassoBaseRot && rightShoulder != null)
        {
            rightShoulder.localPosition = lassoShoulderBaseLocalPos;
        }

        if (preview != null)
            preview.SetTwirlingLasso(ropeGrip != null ? ropeGrip : (ropeHand != null ? ropeHand : transform),
                                     twirlLasso, lassoSpinPhase, animator != null ? animator.transform : transform);

        // 2 tay giơ lên vác Dino trên đầu (dùng chính dáng tay quăng dây đối xứng trái/phải)
        bool carrying = !twirlLasso && !aimCatch && currentHoldVisualDino != null && holdVisualRoot != null;
        holdArmBlend = Mathf.MoveTowards(holdArmBlend, carrying ? 1f : 0f, 12f * dt);
        if (holdArmBlend > 0.001f && lassoArmBlend <= 0.01f && animator != null)
        {
            Transform root = animator.transform;
            if (hasLeftArmBaseRot)
                PoseCarryArmAsLasso(leftShoulder, leftUpperArm, leftLowerArm, leftHand,
                                    leftShoulderBaseLocalRot, leftShoulderBaseLocalPos, leftArmBaseLocalRot, leftLowerArmBaseLocalRot, leftHandBaseLocalRot,
                                    -1f, root, holdArmBlend);
            if (hasLassoBaseRot && ropeHand != null)
                PoseCarryArmAsLasso(rightShoulder, rightUpperArm, rightLowerArm, ropeHand,
                                    lassoShoulderBaseLocalRot, lassoShoulderBaseLocalPos, lassoArmBaseLocalRot, lassoLowerArmBaseLocalRot, lassoHandBaseLocalRot,
                                    1f, root, holdArmBlend);
        }
        else if (hasLeftArmBaseRot && leftShoulder != null)
        {
            leftShoulder.localPosition = leftShoulderBaseLocalPos;
        }

        // Bám Dino đang vác cân bằng giữa 2 tay + rung lắc tự nhiên theo nhịp di chuyển
        if (holdVisualRoot != null && animator != null)
        {
            Transform root = animator.transform;
            Vector3 perDinoPos = (currentHoldVisualDino != null && currentHoldVisualDino.data != null)
                ? currentHoldVisualDino.data.holdPositionOffset : Vector3.zero;
            Vector3 perDinoRot = (currentHoldVisualDino != null && currentHoldVisualDino.data != null)
                ? currentHoldVisualDino.data.holdRotationOffset : Vector3.zero;
            Vector3 baseHoldPos = holdDinoOffset + perDinoPos;
            Quaternion baseHoldRot = Quaternion.Euler(holdDinoEuler) * Quaternion.Euler(perDinoRot);

            if (leftHand != null && ropeHand != null)
            {
                Vector3 handsMidLocal = root.InverseTransformPoint((leftHand.position + ropeHand.position) * 0.5f);
                if (!hasCarryIdleRef && holdArmBlend >= 0.99f && State == PlayerState.Walking && moveInput.sqrMagnitude < 0.01f)
                {
                    carryHandsRestLocal = handsMidLocal;
                    if (carryBone != null)
                        carryBoneRestLocalRot = Quaternion.Inverse(root.rotation) * carryBone.rotation;
                    hasCarryIdleRef = true;
                }

                Vector3 posDelta = hasCarryIdleRef ? (handsMidLocal - carryHandsRestLocal) : Vector3.zero;
                posDelta.x = Mathf.Lerp(posDelta.x, 0f, 0.35f);
                holdVisualRoot.transform.localPosition = baseHoldPos + posDelta * holdBoneSwayWeight;

                Vector3 spanLocal = root.InverseTransformDirection(ropeHand.position - leftHand.position);
                float rollDeg = Mathf.Clamp(Mathf.Atan2(spanLocal.y, Mathf.Max(0.1f, spanLocal.x)) * Mathf.Rad2Deg * holdBoneSwayWeight, -12f, 12f);
                float pitchDeg = 0f;
                if (holdFollowPitch && hasCarryIdleRef && carryBone != null)
                {
                    Quaternion boneDelta = (Quaternion.Inverse(root.rotation) * carryBone.rotation) * Quaternion.Inverse(carryBoneRestLocalRot);
                    pitchDeg = Mathf.Clamp(Mathf.DeltaAngle(0f, boneDelta.eulerAngles.x) * holdBoneSwayWeight, -holdMaxPitchAngle, holdMaxPitchAngle);
                }
                holdVisualRoot.transform.localRotation = Quaternion.Euler(pitchDeg, 0f, rollDeg) * baseHoldRot;
            }
            else if (hasCarryBone && carryBone != null)
            {
                Vector3 curLocalBonePos = root.InverseTransformPoint(carryBone.position);
                Vector3 boneDeltaLocal = curLocalBonePos - carryBoneRestLocalOffset;
                holdVisualRoot.transform.localPosition = baseHoldPos + boneDeltaLocal * holdBoneSwayWeight;
                holdVisualRoot.transform.localRotation = baseHoldRot;
            }
            else
            {
                holdVisualRoot.transform.localPosition = baseHoldPos;
                holdVisualRoot.transform.localRotation = baseHoldRot;
            }
        }
    }

    void UpdateHoldVisual(OwnedDino held, bool shouldCarry)
    {
        if (!shouldCarry || held == null || held.data == null || animator == null)
        {
            if (holdVisualRoot != null)
            {
                Destroy(holdVisualRoot);
                holdVisualRoot = null;
            }
            currentHoldVisualDino = null;
            hasCarryIdleRef = false;
            return;
        }

        if (currentHoldVisualDino == held && holdVisualRoot != null) return;

        if (holdVisualRoot != null) Destroy(holdVisualRoot);
        currentHoldVisualDino = held;
        hasCarryIdleRef = false;

        holdVisualRoot = new GameObject("HoldDinoVisual");
        holdVisualRoot.transform.SetParent(animator.transform, false);
        holdVisualRoot.transform.localPosition = holdDinoOffset + held.data.holdPositionOffset;
        holdVisualRoot.transform.localRotation = Quaternion.Euler(holdDinoEuler) * Quaternion.Euler(held.data.holdRotationOffset);

        GameObject model = DinoView.BuildModel(holdVisualRoot.transform, held, new Vector3(1.1f, 1.4f, 2.2f));
        float labelTopY = holdLabelHeight;
        if (model != null)
        {
            Collider[] cols = model.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
                if (cols[i] != null) cols[i].enabled = false;

            Animator mAnim = model.GetComponentInChildren<Animator>();
            if (mAnim != null)
            {
                mAnim.SetBool("IsMoving", false);
                mAnim.Update(0f); // Ép áp dụng pose Idle ngay trước khi đo Bounds (tránh đo nhầm Bind Pose)
                if (freezeHoldDinoAnimator) mAnim.enabled = false;
            }

            // Giữ đúng 100% kích thước thật của model Dino (chỉ nhân holdDinoScaleMultiplier nếu bạn chỉnh khác 1)
            model.transform.localScale *= Mathf.Max(0.1f, holdDinoScaleMultiplier);

            Renderer[] rends = model.GetComponentsInChildren<Renderer>(true);
            if (rends != null && rends.Length > 0)
            {
                bool hasBounds = false;
                Bounds b = default(Bounds);
                for (int i = 0; i < rends.Length; i++)
                {
                    if (rends[i] == null || rends[i] is ParticleSystemRenderer) continue;
                    if (!hasBounds) { b = rends[i].bounds; hasBounds = true; }
                    else b.Encapsulate(rends[i].bounds);
                }

                if (hasBounds)
                {
                    // Đặt đáy chân/bụng Dino chạm đúng tầm 2 bàn tay nâng trên đầu
                    float bottomOffset = holdVisualRoot.transform.position.y - b.min.y;
                    model.transform.position += Vector3.up * bottomOffset;
                    labelTopY = b.size.y + holdLabelHeight;
                }
            }
        }

        if (GameManager.Instance != null)
        {
            string lbl = DinoView.BuildWorldLabelText(held, false, 0f);
            GameManager.Instance.CreateLabel(holdVisualRoot.transform, new Vector3(0f, labelTopY, 0f), lbl);
        }
    }

    void PoseCarryArmAsLasso(Transform shoulder, Transform upper, Transform lower, Transform hand,
                             Quaternion baseSh, Vector3 baseShPos, Quaternion baseUp, Quaternion baseLow, Quaternion baseHand,
                             float sideSign, Transform root, float blend)
    {
        if (upper == null || lower == null || hand == null) return;

        Quaternion animSh = shoulder != null ? shoulder.rotation : Quaternion.identity;
        Quaternion animUp = upper.rotation;
        Quaternion animLow = lower.rotation;
        Quaternion animHand = hand.rotation;

        if (shoulder != null) shoulder.localRotation = baseSh;
        upper.localRotation = baseUp;
        lower.localRotation = baseLow;
        hand.localRotation = baseHand;

        Vector3 rawUp = lassoUpperArmDir.sqrMagnitude > 0.001f ? lassoUpperArmDir.normalized : new Vector3(0.8f, 0.25f, 0.35f);
        Vector3 rawLow = lassoForearmDir.sqrMagnitude > 0.001f ? lassoForearmDir.normalized : new Vector3(0.3f, 0.9f, 0.2f);
        Vector3 upperGoal = root.TransformDirection(new Vector3(rawUp.x * sideSign, rawUp.y, rawUp.z).normalized);
        Vector3 lowerGoal = root.TransformDirection(new Vector3(rawLow.x * sideSign, rawLow.y, rawLow.z).normalized);

        if (shoulder != null)
        {
            Vector3 shDir = (upper.position - shoulder.position).normalized;
            if (shDir.sqrMagnitude > 0.0001f)
                shoulder.rotation = Quaternion.Slerp(shoulder.rotation,
                    Quaternion.FromToRotation(shDir, upperGoal) * shoulder.rotation, lassoShoulderLiftWeight);
            Vector3 shEuler = new Vector3(lassoShoulderOffset.x, lassoShoulderOffset.y * sideSign, lassoShoulderOffset.z * sideSign);
            shoulder.localRotation = shoulder.localRotation * Quaternion.Euler(shEuler);
            Vector3 shPosOff = new Vector3(lassoShoulderPosOffset.x * sideSign, lassoShoulderPosOffset.y, lassoShoulderPosOffset.z);
            shoulder.localPosition = Vector3.Lerp(baseShPos, baseShPos + shPosOff, blend);
        }

        Vector3 curUpper = (lower.position - upper.position).normalized;
        if (curUpper.sqrMagnitude > 0.0001f)
        {
            upper.rotation = Quaternion.FromToRotation(curUpper, upperGoal) * upper.rotation;
            upper.rotation = Quaternion.AngleAxis(lassoUpperArmTwist * sideSign, upperGoal) * upper.rotation;
        }

        Vector3 curLower = (hand.position - lower.position).normalized;
        if (curLower.sqrMagnitude > 0.0001f)
        {
            lower.rotation = Quaternion.FromToRotation(curLower, lowerGoal) * lower.rotation;
            lower.rotation = Quaternion.AngleAxis(lassoForearmTwist * sideSign, lowerGoal) * lower.rotation;
        }

        Vector3 handEuler = new Vector3(lassoHandAngles.x, lassoHandAngles.y * sideSign, lassoHandAngles.z * sideSign);
        hand.localRotation = hand.localRotation * Quaternion.Euler(handEuler);

        if (blend < 0.999f)
        {
            if (shoulder != null) shoulder.rotation = Quaternion.Slerp(animSh, shoulder.rotation, blend);
            upper.rotation = Quaternion.Slerp(animUp, upper.rotation, blend);
            lower.rotation = Quaternion.Slerp(animLow, lower.rotation, blend);
            hand.rotation = Quaternion.Slerp(animHand, hand.rotation, blend);
        }
    }
}