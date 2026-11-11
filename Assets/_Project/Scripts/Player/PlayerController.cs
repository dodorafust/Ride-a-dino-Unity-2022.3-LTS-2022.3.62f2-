using UnityEngine;

public enum PlayerState { Walking, Jumping, Riding }

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Walk")]
    public float walkSpeed = 9f;
    public float jumpForce = 10f;
    public float gravityScale = 2f;
    public float airControl = 15f;

    [Header("Dino jump")]
    public float dinoJumpUp = 9f;
    public float dinoJumpForward = 6f;

    [Header("Detect")]
    public float detectRadius = 14f;
    public LayerMask groundMask;
    public LayerMask dinoMask;
    public Transform cam;

    public PlayerState State { get; private set; } = PlayerState.Walking;
    public Dino CurrentDino => currentDino;

    Rigidbody rb;
    Dino currentDino;
    Dino hoverDino;
    Vector3 moveInput;
    float mountBlockUntil;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
    }

    void Update()
    {
        ReadMoveInput();

        switch (State)
        {
            case PlayerState.Walking: UpdateWalking(); break;
            case PlayerState.Jumping: UpdateJumping(); break;
            case PlayerState.Riding:  UpdateRiding();  break;
        }

        UpdateDinoRing();
    }

    void FixedUpdate()
    {
        if (rb.isKinematic) return;

        // Trọng lực mạnh hơn cho cảm giác nhảy gọn
        rb.AddForce(Physics.gravity * (gravityScale - 1f), ForceMode.Acceleration);

        // Điều hướng trên không
        if (State == PlayerState.Jumping)
            rb.AddForce(moveInput * airControl, ForceMode.Acceleration);
    }

    void ReadMoveInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
        moveInput = (fwd * v + right * h);
        if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();
    }

    bool IsGrounded() =>
        Physics.Raycast(transform.position, Vector3.down, 1.1f, groundMask);

    void UpdateWalking()
    {
        Vector3 vel = moveInput * walkSpeed;
        vel.y = rb.velocity.y;
        rb.velocity = vel;

        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            rb.velocity = new Vector3(vel.x, jumpForce, vel.z);
            State = PlayerState.Jumping;
        }
        else if (!IsGrounded())
        {
            State = PlayerState.Jumping;
        }
    }

    void UpdateJumping()
    {
        if (IsGrounded() && rb.velocity.y <= 0.1f)
            State = PlayerState.Walking;
    }

    void UpdateRiding()
    {
        currentDino.SetSteer(Input.GetAxisRaw("Horizontal"));

        if (Input.GetButtonDown("Jump"))
        {
            Vector3 vel = Vector3.forward * (currentDino.speed + dinoJumpForward)
                        + Vector3.up * dinoJumpUp;
            ReleaseDino();
            rb.velocity = vel;
            State = PlayerState.Jumping;
        }
    }

    // Vùng đáp là Trigger nằm trên lưng Dino
    void OnTriggerEnter(Collider other) => TryLand(other);
    void OnTriggerStay(Collider other)  => TryLand(other);

    void TryLand(Collider other)
    {
        if (State != PlayerState.Jumping) return;
        if (Time.time < mountBlockUntil) return;
        if (rb.velocity.y > 0f) return;   // chỉ đáp khi đang rơi

        Dino dino = other.GetComponentInParent<Dino>();
        if (dino == null || dino.IsRidden) return;
        MountDino(dino);
    }

    void MountDino(Dino d)
    {
        currentDino = d;
        d.IsRidden = true;
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;
        transform.SetParent(d.seat);
        transform.localPosition = Vector3.zero;
        State = PlayerState.Riding;
    }

    void ReleaseDino()
    {
        transform.SetParent(null);
        rb.isKinematic = false;
        currentDino.SetSteer(0f);
        currentDino.IsRidden = false;
        currentDino = null;
        mountBlockUntil = Time.time + 0.3f;
    }

    // Vòng tròn nhận diện: hiện trên con Dino gần nhất ở phía trước
    void UpdateDinoRing()
    {
        Dino best = null;
        float bestDist = detectRadius;

        foreach (var c in Physics.OverlapSphere(transform.position, detectRadius, dinoMask))
        {
            Dino d = c.GetComponentInParent<Dino>();
            if (d == null || d.IsRidden) continue;

            Vector3 to = d.transform.position - transform.position;
            if (to.z < -2f) continue;   // bỏ qua Dino đã chạy qua phía sau

            float dist = to.magnitude;
            if (dist < bestDist) { bestDist = dist; best = d; }
        }

        if (hoverDino != best)
        {
            if (hoverDino) hoverDino.ShowRing(false);
            hoverDino = best;
            if (hoverDino) hoverDino.ShowRing(true);
        }
    }
}