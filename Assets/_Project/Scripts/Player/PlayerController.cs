using System.Collections;
using UnityEngine;

public enum PlayerState { Walking, Jumping, Catching, Riding, Sitting, Dead }
public enum DeathReason { StaminaEmpty, WallHit, MissedJump, Fell }

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
    public bool limitDinoTurnAngle = true;         // tắt = cho phép Dino quay đầu tự do theo camera (không giới hạn góc)
    public float dinoMaxTurnAngle = 60f;           // góc quay đầu tối đa (độ) khi bật giới hạn

    [Header("Throw (hết Stamina sau khi tame xong)")]
    public float throwForward = 6f;
    public float throwUp = 11f;

    [Header("Chết khi đang cưỡi Dino (hết Stamina / đâm tường)")]
    public float deathThrowForward = 4.2f;         // lực hất thêm về phía trước mặt Dino
    public float deathThrowUp = 5.5f;              // lực nảy lên trước khi rơi xuống đất
    public float deathFallGravityScale = 1.5f;     // trọng lực lúc bị quăng rơi xuống đất

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
    [Range(0f, 1f)] public float airLegRelax = 0.5f;   // độ duỗi bớt cẳng chân trên không (0 = co sát mông như gốc, 1 = duỗi thẳng)
    [Range(0f, 1f)] public float airFootSync = 0.75f;  // đồng bộ cổ chân trên không để không bị duỗi xuống rồi vểnh ngược lên
    public float airFootPitch = 12f;                   // góc mũi chân khi ở trên không (độ, dương = hơi duỗi tự nhiên, âm = vểnh lên)
    public float landDipDepth = 0.16f;                 // độ nhún đệm trọng tâm khi vừa tiếp đất
    public float landDipDuration = 0.18f;              // thời gian nhún đệm tiếp đất rồi đứng thẳng dậy (giây)
    public float deathRespawnDelay = 1.3f;             // thời gian chờ phát animation chết trước khi về điểm spawn

    public PlayerState State { get; private set; } = PlayerState.Walking;
    public Dino CurrentDino { get { return currentDino; } }
    public Vector3 Velocity { get { return velocity; } }
    public bool RunActive { get; private set; }     // đang trong chế độ bắt Dino
    // Đang ở trên không và có Dino được nhắm -> bấm nút nhảy là bắt ngay (UI đổi icon mũi tên xuống)
    public bool CatchReady { get { return State == PlayerState.Jumping && targetDino != null; } }

    CharacterController cc;
    RideSession ride;
    CameraRig camRig;
    RunZone runZone;
    DinoMachine machine;
    MountController mount;

    Dino currentDino, targetDino, lastDino;
    Transform leftLowerLeg, rightLowerLeg, leftFoot, rightFoot;
    Quaternion leftFootRestRot = Quaternion.identity, rightFootRestRot = Quaternion.identity;
    float airLegBlend, airFootBlend, landDipTimer;
    Vector3 moveInput, catchStart, startPosition, visualBaseLocalPos;
    float catchTimer, activeCatchDuration, steerEnableTime, catchLockUntil, lastGroundedTime = -999f, lastJumpPressedTime = -999f, airTime;
    bool jumpPressed;
    bool jumpedThisAirborne;
    bool diesOnGround;
    bool deadFalling;
    bool glideExpired;
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

        jumpPressed = GameInput.ConsumeJump();
        if (jumpPressed) lastJumpPressedTime = Time.time;
        ReadMoveInput();
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

    void ReadMoveInput()
    {
        Vector2 m = GameInput.Move;
        Vector3 right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
        moveInput = CamForwardFlat() * m.y + right * m.x;
        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();
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
        RunActive = inZone || diesOnGround ||
                    State == PlayerState.Riding || State == PlayerState.Catching;
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

    // ---------- Walking ----------
    void UpdateWalking()
    {
        float dt = Time.deltaTime;
        float speedMul = mount != null ? mount.SpeedMultiplier : 1f;
        Vector3 moveDir = moveInput.sqrMagnitude > 0.01f
            ? moveInput.normalized
            : (RunActive ? CamForwardFlat() : Vector3.zero);
        Vector3 horiz = moveDir * walkSpeed * speedMul;
        bool grounded = IsGrounded();

        if (grounded && velocity.y < 0f) velocity.y = -2f;      // dính xuống đất, đi dốc không bị nảy

        if (HasBufferedJump() && grounded)
        {
            ConsumeJumpBuffer();
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
            if (animator != null && hasJumpStart) animator.SetTrigger("JumpStart");

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
            if (animator != null && hasJumpStart) animator.SetTrigger("JumpStart");
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
                lastGroundedTime = -999f;
                jumpedThisAirborne = true;
                airTime = 0f;
                landDipTimer = 0f;
                velocity.y = GetJumpVelocity(RunActive);
                currentUpGravity = GetUpwardGravity(RunActive);
                if (animator != null && hasJumpStart) animator.SetTrigger("JumpStart");
                return;
            }

            jumpedThisAirborne = false;
            velocity.y = -2f;
            landDipTimer = landDipDuration;
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
            Vector3 wishDir = moveInput.sqrMagnitude > 0.01f
                ? (CamForwardFlat() * 0.6f + moveInput)
                : CamForwardFlat();
            if (wishDir.sqrMagnitude < 0.001f) wishDir = CamForwardFlat();
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
            preview.SetRope(ropeHand != null ? ropeHand : transform, d.seat);
    }

    void UpdateCatching()
    {
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
        currentDino.Boost();
        steerEnableTime = Time.time + steerDelayAfterMount;

        float stamina = GameManager.Instance != null ? GameManager.Instance.Stamina : 10f;
        ride.Begin(currentDino, stamina);
        State = PlayerState.Riding;

        if (animator != null)
        {
            if (hasIsJumping) animator.SetBool("IsJumping", false);
            if (hasIsFalling) animator.SetBool("IsFalling", false);
            if (hasIsLassoing) animator.SetBool("IsLassoing", false);
            if (hasIsRiding) animator.SetBool("IsRiding", true);
            int riding = Animator.StringToHash("Base Layer.Riding");
            if (animator.HasState(0, riding)) animator.CrossFadeInFixedTime(riding, 0.08f, 0);
        }
    }

    // ---------- Riding ----------
    void UpdateRiding()
    {
        // Húc tường = chết ngay (tường là object có script Wall)
        if (currentDino.TouchingWall) { Die(DeathReason.WallHit); return; }

        currentDino.SetSteer(GameInput.Move.x);                // joystick trái/phải hoặc A/D
        currentDino.limitTurnAngle = limitDinoTurnAngle;
        currentDino.maxSteerAngle = dinoMaxTurnAngle;

        if (Time.time >= steerEnableTime)
            currentDino.SetHeading(cam.eulerAngles.y);         // xoay camera = lái hướng Dino

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
        }

        if (jumpPressed)
        {
            ConsumeJumpBuffer();
            jumpedThisAirborne = true;
            // Giữ tốc độ của Dino đang cưỡi + cộng thêm dinoJumpForward để lao vượt lên phía trước
            float launchSpeed = Mathf.Min(maxAirSpeed, Mathf.Max(walkSpeed, currentDino.CurrentSpeed) + dinoJumpForward);
            Vector3 vel = CamForwardFlat() * launchSpeed
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
        HudController.Say("Tamed " + owned.Title + "!  $" + NumberFormat.Short(owned.MoneyPerSecond) + "/s", 2.5f);
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

        currentDino.SetSteer(0f);
        currentDino.ResetHeading();
        currentDino.IsRidden = false;

        lastDino = currentDino;
        currentDino = null;
        catchLockUntil = Time.time + catchLockoutAfterJump;
        ride.End();
    }

    // ---------- Die / Respawn ----------
    public void Kill(DeathReason reason) { Die(reason); }

    void Die(DeathReason reason)
    {
        if (State == PlayerState.Dead) return;

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
        State = PlayerState.Dead;

        if (animator != null)
        {
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
            velocity = fwd * (diedOnDino.baseSpeed + deathThrowForward) + Vector3.up * deathThrowUp;
            if (animator != null)
            {
                animator.transform.rotation = Quaternion.LookRotation(fwd);
                if (hasIsGrounded) animator.SetBool("IsGrounded", false);
                if (hasIsJumping) animator.SetBool("IsJumping", true);
                if (hasIsFalling) animator.SetBool("IsFalling", true);
                if (hasVerticalSpeed) animator.SetFloat("VerticalSpeed", velocity.y);
                int jumpUp = Animator.StringToHash("Base Layer.JumpUp");
                int freeFall = Animator.StringToHash("Base Layer.FreeFall");
                if (animator.HasState(0, jumpUp)) animator.CrossFadeInFixedTime(jumpUp, 0.06f, 0);
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
            if (animator != null && hasDie) animator.SetTrigger("Die");
        }

        string msg;
        switch (reason)
        {
            case DeathReason.StaminaEmpty: msg = "You ran out of stamina before taming!"; break;
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

        bool grounded = cc.isGrounded || (flags & CollisionFlags.Below) != 0 || CheckGroundRaw();
        if ((grounded && velocity.y <= 0f) || transform.position.y < fallDeathY)
            FinishDeadFall();
    }

    void FinishDeadFall()
    {
        deadFalling = false;
        velocity = Vector3.zero;
        SetBodyActive(false);
        if (animator != null)
        {
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
        DismountPet();
        machine = m;
        m.SetOccupied(true);
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
        if (isMountingMachine) return;
        if (jumpPressed || moveInput.sqrMagnitude > 0.25f) StandUp();
    }

    void StandUp()
    {
        if (sitMountRoutine != null) { StopCoroutine(sitMountRoutine); sitMountRoutine = null; }
        isMountingMachine = false;
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

    void UpdateVisualAndAnimation()
    {
        if (animator == null) return;
        Transform visual = animator.transform;

        bool mountedOnPet = mount != null && mount.Mounted;
        bool ridingOrSitting = State == PlayerState.Riding || (State == PlayerState.Sitting && !isMountingMachine) || mountedOnPet;

        // 1. Vị trí & góc xoay của Model
        if (State == PlayerState.Riding || State == PlayerState.Sitting)
        {
            visual.localPosition = visualBaseLocalPos + ridingModelOffset;
            if (!isMountingMachine) visual.localRotation = Quaternion.identity;
        }
        else if (State == PlayerState.Catching)
        {
            visual.localPosition = visualBaseLocalPos;
        }
        else
        {
            float dipY = 0f;
            if (landDipTimer > 0f && landDipDuration > 0.001f)
            {
                landDipTimer = Mathf.Max(0f, landDipTimer - Time.deltaTime);
                float t = 1f - (landDipTimer / landDipDuration);
                dipY -= Mathf.Sin(t * Mathf.PI) * landDipDepth;
            }
            Vector3 basePos = mountedOnPet ? (visualBaseLocalPos + ridingModelOffset) : visualBaseLocalPos;
            visual.localPosition = basePos + new Vector3(0f, dipY, 0f);
            Vector3 flatMove = (State == PlayerState.Jumping && steerWithCamera) || (State == PlayerState.Walking && RunActive)
                ? new Vector3(velocity.x, 0f, velocity.z)
                : new Vector3(moveInput.x, 0f, moveInput.z);
            if (flatMove.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(flatMove);
                visual.rotation = Quaternion.Slerp(visual.rotation, targetRot, modelTurnSpeed * Time.deltaTime);
            }
        }

        // 2. Cập nhật tham số Animator (chỉ gửi các tham số đã tạo trong Animator Controller)
        bool isRunning = State == PlayerState.Walking && (moveInput.sqrMagnitude > 0.01f || RunActive) && !mountedOnPet;
        bool isJumping = State == PlayerState.Jumping || State == PlayerState.Catching || isMountingMachine;
        bool isGrounded = State == PlayerState.Walking || State == PlayerState.Riding || (State == PlayerState.Sitting && !isMountingMachine);
        bool isFalling = State == PlayerState.Jumping && velocity.y <= 1.0f;
        bool isHolding = GameManager.Instance != null && GameManager.Instance.Inventory.Held != null && !ridingOrSitting;
        bool isLassoing = State == PlayerState.Catching;

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
        bool inAir = State == PlayerState.Jumping || State == PlayerState.Catching ||
                     (State == PlayerState.Dead && deadFalling);
        float dt = Time.deltaTime;
        airLegBlend = Mathf.MoveTowards(airLegBlend, inAir ? airLegRelax : 0f, 8f * dt);
        airFootBlend = Mathf.MoveTowards(airFootBlend, inAir ? airFootSync : 0f, 8f * dt);

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
    }
}