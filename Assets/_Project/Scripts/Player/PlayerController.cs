using UnityEngine;

public enum PlayerState { Walking, Jumping, Catching, Riding, Sitting }
public enum DeathReason { StaminaEmpty, WallHit, MissedJump, Fell }

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Walk")]
    public float walkSpeed = 9f;
    public float jumpForce = 10f;
    public float gravityScale = 2f;
    public float airControl = 15f;

    [Header("Jump from Dino (nhảy chuyền)")]
    public float dinoJumpUp = 11f;                 // vận tốc bay lên ban đầu (cao hơn)
    public float dinoJumpForward = 4f;             // cộng thêm vào tốc độ của Dino vừa rời
    public float dinoJumpGravityScale = 1.5f;      // trọng lực nhẹ hơn = bay lâu hơn
    public float airTurnRate = 140f;               // độ/giây: xoay camera để bẻ hướng bay
    public float maxAirSpeed = 30f;
    public float catchLockoutAfterJump = 0.08f;    // chỉ để chống "ăn" 2 lần cùng 1 lần bấm

    [Header("Catch")]
    public float scanRadius = 20f;
    public float frontDotMin = 0f;
    public float aimMaxAngle = 65f;                // lệch tối đa so với tâm camera (độ, theo phương ngang)
    public float aimDistanceWeight = 1.2f;         // 1m xa hơn ~ lệch thêm 1.2 độ
    public float aimSwitchDegrees = 4f;            // chống nhấp nháy khi 2 con gần như thẳng hàng
    public float catchDuration = 0.18f;
    public float autoCatchHeight = 0.5f;
    public float steerDelayAfterMount = 0.6f;

    [Header("Throw (hết Stamina sau khi tame xong)")]
    public float throwForward = 6f;
    public float throwUp = 11f;

    [Header("Rơi khỏi map")]
    public float fallDeathY = -20f;            // thấp hơn mức này = chết (DeathZone là cách khác)

    [Header("References")]
    public LayerMask groundMask;
    public LayerMask dinoMask;
    public Transform cam;
    public CatchPreview preview;

    [Header("Model & Animation")]
    public Animator animator;
    public float modelTurnSpeed = 14f;
    public Vector3 ridingModelOffset = Vector3.zero;   // chỉnh độ cao/tiến lùi của nhân vật khi ngồi trên lưng Dino

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
    Vector3 moveInput, catchStart, startPosition, visualBaseLocalPos;
    float catchTimer, steerEnableTime, catchLockUntil, lastGroundedTime;
    bool jumpPressed;
    bool diesOnGround;
    Vector3 velocity;              // vận tốc tự quản (thay cho Rigidbody)
    CollisionFlags lastFlags;
    float airGravityScale;
    bool steerWithCamera;          // true sau khi nhảy khỏi Dino / bị hất: xoay camera = bẻ hướng bay
    bool hasIsRunning, hasIsJumping, hasIsGrounded, hasIsFalling, hasVerticalSpeed,
         hasJumpStart, hasLand, hasIsRiding, hasIsHolding, hasIsLassoing, hasJumpFromDino, hasDie;
    readonly Collider[] hitBuf = new Collider[32];

    void Awake()
    {
        Instance = this;
        cc = GetComponent<CharacterController>();
        airGravityScale = gravityScale;
        ride = GetComponent<RideSession>();
        if (ride == null) ride = gameObject.AddComponent<RideSession>();
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) visualBaseLocalPos = animator.transform.localPosition;
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
        jumpPressed = GameInput.ConsumeJump();
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
        return Time.time - lastGroundedTime <= 0.1f;
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
        airGravityScale = gravityScale;
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
        Vector3 horiz = moveInput * walkSpeed * speedMul;
        bool grounded = IsGrounded();

        if (grounded && velocity.y < 0f) velocity.y = -2f;      // dính xuống đất, đi dốc không bị nảy

        if (jumpPressed && grounded)
        {
            lastGroundedTime = -1f;
            velocity.y = jumpForce;
            airGravityScale = gravityScale;
            steerWithCamera = false;
            diesOnGround = false;
            State = PlayerState.Jumping;
            if (animator != null && hasJumpStart) animator.SetTrigger("JumpStart");
        }
        else if (!grounded)
        {
            airGravityScale = gravityScale;
            steerWithCamera = false;
            diesOnGround = false;
            State = PlayerState.Jumping;
        }

        velocity.x = horiz.x;
        velocity.z = horiz.z;
        velocity.y += Physics.gravity.y * gravityScale * dt;
        lastFlags = cc.Move(velocity * dt);
        if ((lastFlags & CollisionFlags.Above) != 0 && velocity.y > 0f) velocity.y = 0f;
    }

    // ---------- Jumping ----------
    void UpdateJumping()
    {
        float dt = Time.deltaTime;
        if (RunActive) UpdateCatchTarget(); else SetTarget(null);
        bool canCatch = RunActive && Time.time >= catchLockUntil;

        // Bấm nút nhảy / Space trên không = quăng dây bắt NGAY LẬP TỨC con đang được nhắm
        if (canCatch && jumpPressed)
        {
            Dino pick = targetDino != null ? targetDino : FindBestByCameraAim();
            if (pick != null) { BeginCatch(pick); return; }
        }

        // --- Bay: xoay camera = bẻ hướng bay, joystick = chỉnh nhẹ, rồi áp trọng lực và di chuyển
        AirSteer(dt);
        velocity.y += Physics.gravity.y * airGravityScale * dt;
        lastFlags = cc.Move(velocity * dt);
        if ((lastFlags & CollisionFlags.Above) != 0 && velocity.y > 0f) velocity.y = 0f;

        if (velocity.y > 0.1f) return;      // còn đang bay lên

        bool nearGround = Physics.Raycast(transform.position, Vector3.down,
                                          1f + autoCatchHeight, GroundCheckMask(), QueryTriggerInteraction.Ignore);
        if (nearGround && canCatch)
        {
            Dino auto = targetDino != null ? targetDino : FindNearest(false);
            if (auto != null) { BeginCatch(auto); return; }
        }

        if (CheckGroundRaw() || (lastFlags & CollisionFlags.Below) != 0)
        {
            lastGroundedTime = Time.time;
            velocity.y = -2f;
            airGravityScale = gravityScale;
            steerWithCamera = false;
            SetTarget(null);
            if (diesOnGround) { Die(DeathReason.MissedJump); return; }
            lastDino = null;
            State = PlayerState.Walking;
            if (animator != null && hasLand) animator.SetTrigger("Land");
        }
    }

    // Sau khi nhảy khỏi Dino / bị hất: vector bay xoay dần về hướng camera đang nhìn.
    // Joystick cộng thêm gia tốc nhẹ. Nhảy từ mặt đất thì chỉ có gia tốc joystick (như cũ).
    void AirSteer(float dt)
    {
        Vector3 h = new Vector3(velocity.x, 0f, velocity.z);

        if (steerWithCamera && airTurnRate > 0f && h.sqrMagnitude > 0.25f)
        {
            Vector3 want = CamForwardFlat() * h.magnitude;
            h = Vector3.RotateTowards(h, want, airTurnRate * Mathf.Deg2Rad * dt, 0f);
        }

        float before = h.magnitude;
        h += moveInput * airControl * dt;
        float cap = Mathf.Max(before, steerWithCamera ? maxAirSpeed : walkSpeed * 1.25f);
        if (h.magnitude > cap) h = h.normalized * cap;

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
            if (frontOnly)
            {
                Vector3 flat = Vector3.ProjectOnPlane(to, Vector3.up);
                if (flat.sqrMagnitude > 0.01f &&
                    Vector3.Dot(flat.normalized, fwd) < frontDotMin) continue;
            }

            float dist = to.magnitude;
            if (dist < bestDist) { bestDist = dist; best = d; }
        }
        return best;
    }

    // Chọn con Dino gần TÂM CAMERA nhất (theo phương ngang) -> xoay camera để đổi mục tiêu
    float AimScore(Dino d)
    {
        Vector3 toPlayer = d.seat.position - transform.position;
        Vector3 flatP = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
        if (flatP.magnitude > scanRadius * 1.15f) return float.MaxValue;
        Vector3 fwd = CamForwardFlat();
        if (flatP.sqrMagnitude > 0.01f && Vector3.Dot(flatP.normalized, fwd) < frontDotMin) return float.MaxValue;

        Vector3 flatC = Vector3.ProjectOnPlane(d.seat.position - cam.position, Vector3.up);
        float ang = flatC.sqrMagnitude > 0.01f ? Vector3.Angle(fwd, flatC) : 0f;
        if (ang > aimMaxAngle) return float.MaxValue;
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
        if (oldScore == float.MaxValue || AimScore(best) + aimSwitchDegrees < oldScore)
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
        else if (preview != null) preview.Hide();
    }

    // ---------- Catching ----------
    void BeginCatch(Dino d)
    {
        DismountPet();
        SetTarget(d);
        currentDino = d;
        d.IsRidden = true;

        velocity = Vector3.zero;
        SetBodyActive(false);

        catchStart = transform.position;
        catchTimer = 0f;
        State = PlayerState.Catching;

        if (preview != null) preview.SetRope(transform, d.seat);
    }

    void UpdateCatching()
    {
        if (currentDino == null)
        {
            SetBodyActive(true);
            State = PlayerState.Jumping;
            return;
        }

        catchTimer += Time.deltaTime;
        float t = Mathf.Clamp01(catchTimer / catchDuration);
        float e = t * t * (3f - 2f * t);
        transform.position = Vector3.Lerp(catchStart, currentDino.seat.position, e);

        if (t >= 1f) MountDino();
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
    }

    // ---------- Riding ----------
    void UpdateRiding()
    {
        // Húc tường = chết ngay (tường là object có script Wall)
        if (currentDino.TouchingWall) { Die(DeathReason.WallHit); return; }

        currentDino.SetSteer(GameInput.Move.x);                // joystick trái/phải hoặc A/D

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
            Vector3 vel = CamForwardFlat() * (currentDino.CurrentSpeed + dinoJumpForward)
                        + Vector3.up * dinoJumpUp;
            ReleaseDino();
            if (animator != null && hasJumpFromDino) animator.SetTrigger("JumpFromDino");
            if (animator != null && hasJumpStart) animator.SetTrigger("JumpStart");
            velocity = vel;
            airGravityScale = dinoJumpGravityScale;
            steerWithCamera = true;
            diesOnGround = true;
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
        Vector3 vel = currentDino.transform.forward * (currentDino.CurrentSpeed + throwForward)
                    + Vector3.up * throwUp;
        ReleaseDino();
        if (animator != null && hasJumpFromDino) animator.SetTrigger("JumpFromDino");
        if (animator != null && hasJumpStart) animator.SetTrigger("JumpStart");
        velocity = vel;
        airGravityScale = dinoJumpGravityScale;
        steerWithCamera = true;
        diesOnGround = false;
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
        if (currentDino != null) ReleaseDino();
        ride.End();
        SetTarget(null);
        if (preview != null) preview.SetRope(null, null);
        DismountPet();

        if (State == PlayerState.Sitting && machine != null)
        {
            machine.SetOccupied(false);
            machine = null;
        }

        TeleportTo(RespawnPos());
        if (animator != null && hasDie) animator.SetTrigger("Die");
        diesOnGround = false;
        lastDino = null;
        catchLockUntil = 0f;
        State = PlayerState.Walking;
        ResetAnimatorToIdle();

        string msg;
        switch (reason)
        {
            case DeathReason.StaminaEmpty: msg = "You ran out of stamina before taming!"; break;
            case DeathReason.WallHit: msg = "You crashed into the wall!"; break;
            case DeathReason.Fell: msg = "You fell off the map!"; break;
            default: msg = "You missed the jump and fell!"; break;
        }
        HudController.Say(msg, 3f);
    }

    // ---------- Dino Machine ----------
    public bool SitOn(DinoMachine m)
    {
        if (State != PlayerState.Walking) return false;
        DismountPet();
        machine = m;
        velocity = Vector3.zero;
        SetBodyActive(false);
        transform.SetParent(m.seat);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        State = PlayerState.Sitting;
        return true;
    }

    void UpdateSitting()
    {
        if (jumpPressed || moveInput.sqrMagnitude > 0.25f) StandUp();
    }

    void StandUp()
    {
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
        if (hasIsGrounded) animator.SetBool("IsGrounded", true);
        if (hasVerticalSpeed) animator.SetFloat("VerticalSpeed", 0f);
        if (hasJumpStart) animator.ResetTrigger("JumpStart");
        if (hasLand) animator.ResetTrigger("Land");
        if (hasJumpFromDino) animator.ResetTrigger("JumpFromDino");
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
            }
        }
    }

    void UpdateVisualAndAnimation()
    {
        if (animator == null) return;
        Transform visual = animator.transform;

        bool mountedOnPet = mount != null && mount.Mounted;
        bool ridingOrSitting = State == PlayerState.Riding || State == PlayerState.Sitting || mountedOnPet;

        // 1. Vị trí & góc xoay của Model
        if (State == PlayerState.Riding || State == PlayerState.Sitting)
        {
            visual.localPosition = visualBaseLocalPos + ridingModelOffset;
            visual.localRotation = Quaternion.identity;
        }
        else
        {
            visual.localPosition = mountedOnPet ? (visualBaseLocalPos + ridingModelOffset) : visualBaseLocalPos;
            Vector3 flatMove = new Vector3(moveInput.x, 0f, moveInput.z);
            if (flatMove.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(flatMove);
                visual.rotation = Quaternion.Slerp(visual.rotation, targetRot, modelTurnSpeed * Time.deltaTime);
            }
        }

        // 2. Cập nhật tham số Animator (chỉ gửi các tham số đã tạo trong Animator Controller)
        bool isRunning = State == PlayerState.Walking && moveInput.sqrMagnitude > 0.01f && !mountedOnPet;
        bool isJumping = State == PlayerState.Jumping || State == PlayerState.Catching;
        bool isGrounded = State == PlayerState.Walking || State == PlayerState.Riding || State == PlayerState.Sitting;
        bool isFalling = State == PlayerState.Jumping && velocity.y <= 1.0f;
        bool isHolding = GameManager.Instance != null && GameManager.Instance.Inventory.Held != null && !ridingOrSitting;
        bool isLassoing = (State == PlayerState.Riding && !ride.Tamed) ||
                          (RunActive && !ridingOrSitting && targetDino != null);

        if (hasIsRunning) animator.SetBool("IsRunning", isRunning);
        if (hasIsJumping) animator.SetBool("IsJumping", isJumping);
        if (hasIsGrounded) animator.SetBool("IsGrounded", isGrounded);
        if (hasIsFalling) animator.SetBool("IsFalling", isFalling);
        if (hasVerticalSpeed) animator.SetFloat("VerticalSpeed", velocity.y);
        if (hasIsRiding) animator.SetBool("IsRiding", ridingOrSitting);
        if (hasIsHolding) animator.SetBool("IsHolding", isHolding);
        if (hasIsLassoing) animator.SetBool("IsLassoing", isLassoing);
    }
}