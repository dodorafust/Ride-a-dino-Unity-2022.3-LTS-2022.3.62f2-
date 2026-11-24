using System;
using UnityEngine;

// "Ô cưỡi" ngoài cùng bên trái hotbar: Dino trong chuồng mà bạn chọn để cưỡi đi lại (chạy nhanh hơn).
// - Lại gần Dino trong chuồng -> hiện ô "Ride" (WorldPromptUI) -> chạm / phím E để cưỡi, Dino đó vào ô cưỡi.
// - Bấm ô cưỡi (hoặc phím Q) = gọi cưỡi ngay / xuống.
// - Dino bị Unequip khỏi chuồng thì ô cưỡi tự gỡ.
public class MountController : MonoBehaviour
{
    public static MountController Instance { get; private set; }

    public float speedMultiplier = 1.8f;
    public float promptRange = 4.5f;
    public Vector3 visualOffset = new Vector3(0f, -0.9f, 0f);

    [Header("Tốc độ & Boost khi cưỡi Pet")]
    public float petSpeedMultiplier = 1.2f;  // nhân thêm vào runSpeed của Dino khi cưỡi ngoài đường chạy (1.2 = nhanh hơn 20%)
    public float boostMultiplier = 1.6f;     // hệ số vọt tốc độ ngay lúc cưỡi vào đường chạy
    public float boostDuration = 2.5f;       // thời gian giảm dần từ Boost về tốc độ Dino bình thường
    public float runSteadyMultiplier = 1f;   // hệ số tốc độ giữ lại sau khi hết Boost

    public OwnedDino SlotDino { get; private set; }
    public bool Mounted { get; private set; }
    public bool InRunAsDino { get; private set; }
    public float SpeedMultiplier { get { return Mounted ? speedMultiplier : 1f; } }

    public float CurrentMoveSpeed
    {
        get
        {
            if (!Mounted || SlotDino == null || SlotDino.data == null) return 0f;
            float baseSpd = Mathf.Max(0.5f, SlotDino.data.runSpeed) * Mathf.Max(0.1f, petSpeedMultiplier);
            if (player != null && player.RunActive)
            {
                float steady = Mathf.Max(0.1f, runSteadyMultiplier);
                float k = boostTimer > 0f
                    ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(boostTimer / Mathf.Max(0.01f, boostDuration)))
                    : 0f;
                float boostK = Mathf.Lerp(steady, Mathf.Max(steady, boostMultiplier), k);
                return baseSpd * boostK;
            }
            return baseSpd;
        }
    }

    public event Action Changed;

    PlayerController player;
    Pen pen;
    WorldPromptUI prompt;
    GameObject visual;
    Animator visualAnim;
    Transform seatBone;
    Vector3 seatOffsetScaled;
    float baseVisualScale = 1f;
    OwnedDino promptDino;
    float boostTimer;
    bool wasRunActive;

    void Awake() { Instance = this; }

    void Start()
    {
        player = GetComponent<PlayerController>();
        pen = FindObjectOfType<Pen>();
        prompt = FindObjectOfType<WorldPromptUI>(true);
        if (pen != null) pen.Changed += OnPenChanged;
    }

    void OnDestroy()
    {
        if (pen != null) pen.Changed -= OnPenChanged;
    }

    void Update()
    {
        UpdatePrompt();
        if (Input.GetKeyDown(KeyCode.Q)) ToggleRide();

        bool inRun = player != null && player.RunActive;
        if (Mounted && inRun && !wasRunActive)
            boostTimer = boostDuration;
        else if (!Mounted || !inRun)
            boostTimer = 0f;
        wasRunActive = inRun;
        if (boostTimer > 0f)
            boostTimer = Mathf.Max(0f, boostTimer - Time.deltaTime);

        if (Mounted && visual != null && player != null)
        {
            Vector3 v = player.Velocity;
            v.y = 0f;
            bool moving = v.sqrMagnitude > 0.5f;
            bool inAir = player.State == PlayerState.Jumping;
            if (moving)
                visual.transform.rotation = Quaternion.Slerp(visual.transform.rotation,
                                                             Quaternion.LookRotation(v), 14f * Time.deltaTime);
            if (visualAnim != null)
            {
                bool runAnim = moving && !inAir;
                visualAnim.SetBool("IsMoving", runAnim);
                visualAnim.speed = runAnim ? Mathf.Clamp(v.magnitude / 15f, 0.4f, 3f) : 1f;
            }
        }
    }

    public bool TryGetSeatWorldPosition(out Vector3 pos)
    {
        if (!Mounted || visual == null)
        {
            pos = Vector3.zero;
            return false;
        }
        if (SlotDino != null && SlotDino.data != null)
        {
            float seatSizeMul = SlotDino.SizeSeatOffsetMultiplier(SlotDino.data.seatSizeScaleWeight);
            seatOffsetScaled = SlotDino.data.seatOffset * (baseVisualScale * seatSizeMul);
        }
        if (seatBone != null)
            pos = seatBone.position + visual.transform.TransformVector(seatOffsetScaled);
        else
            pos = visual.transform.TransformPoint(new Vector3(0f, 1.8f, 0f) + seatOffsetScaled);
        return true;
    }

    public Quaternion VisualRotation { get { return visual != null ? visual.transform.rotation : transform.rotation; } }

    void UpdatePrompt()
    {
        if (prompt == null || pen == null || player == null) return;

        OwnedDino near = null;
        if (player.State == PlayerState.Walking)
            near = pen.GetNearest(transform.position, promptRange, Mounted ? SlotDino : null);

        if (near == null)
        {
            if (promptDino != null) { promptDino = null; prompt.Hide(); }
            return;
        }

        if (near != promptDino)
        {
            promptDino = near;
            PenDino pd;
            if (pen.TryGetVisual(near, out pd))
            {
                OwnedDino captured = near;
                prompt.Show(pd.transform, near.Title, "Ride", delegate { Ride(captured); });
            }
        }

        if (Input.GetKeyDown(KeyCode.E)) Ride(promptDino);
    }

    public void Ride(OwnedDino d)
    {
        if (d == null || InRunAsDino) return;
        bool inPen = pen != null && pen.Contains(d);
        if (!inPen && d != SlotDino) return;
        if (player != null && (player.State != PlayerState.Walking || player.RunActive)) return;

        if (Mounted) Dismount();
        SlotDino = d;
        Mounted = true;
        if (inPen) pen.SetRidden(d, true);

        BuildMountVisual(d);

        promptDino = null;
        if (prompt != null) prompt.Hide();
        Fire();
    }

    void BuildMountVisual(OwnedDino d)
    {
        if (visual != null) Destroy(visual);
        visual = new GameObject("MountVisual");
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = visualOffset;
        GameObject built = DinoView.BuildModel(visual.transform, d, new Vector3(1.1f, 1.4f, 2.2f));
        visualAnim = built != null ? built.GetComponentInChildren<Animator>() : null;
        seatBone = DinoView.FindSeatBone(built != null ? built.transform : null, d.data.seatBoneName);
        float visualScale = built != null ? Mathf.Max(0.1f, built.transform.localScale.y) : Mathf.Max(0.1f, d.data.modelScale * d.SizeScaleMultiplier);
        float sizeMul = Mathf.Max(0.1f, d.SizeScaleMultiplier);
        baseVisualScale = visualScale / sizeMul;
        float seatSizeMul = d.SizeSeatOffsetMultiplier(d.data.seatSizeScaleWeight);
        seatOffsetScaled = d.data.seatOffset * (baseVisualScale * seatSizeMul);
    }

    public void Dismount()
    {
        if (!Mounted) return;
        bool hasSeatPos = TryGetSeatWorldPosition(out Vector3 seatWorldPos);
        Vector3 exitPos = visual != null ? visual.transform.position : transform.position;
        Quaternion exitRot = visual != null ? visual.transform.rotation : transform.rotation;
        Mounted = false;
        visualAnim = null;
        seatBone = null;
        if (visual != null) Destroy(visual);
        if (player != null && hasSeatPos && player.State == PlayerState.Walking && !player.RunActive)
            player.OnDismountFromPetSeat(seatWorldPos, exitRot);
        if (pen != null && SlotDino != null) pen.SetRidden(SlotDino, false, exitPos, exitRot);
        Fire();
    }

    public void ForceDismount() { Dismount(); }

    public void HandOffToRunDino()
    {
        if (!Mounted) return;
        Mounted = false;
        InRunAsDino = true;
        visualAnim = null;
        seatBone = null;
        if (visual != null) Destroy(visual);
        Fire();
    }

    public void HandOffAutoSummonedToRunDino()
    {
        if (SlotDino == null || SlotDino.data == null) return;
        Mounted = false;
        InRunAsDino = true;
        visualAnim = null;
        seatBone = null;
        if (visual != null) Destroy(visual);
        if (pen != null && pen.Contains(SlotDino))
            pen.SetRidden(SlotDino, true);
        promptDino = null;
        if (prompt != null) prompt.Hide();
        Fire();
    }

    public void ResumeMountedFromRun()
    {
        if (SlotDino == null || SlotDino.data == null)
        {
            InRunAsDino = false;
            Fire();
            return;
        }
        InRunAsDino = false;
        Mounted = true;
        BuildMountVisual(SlotDino);
        if (pen != null && pen.Contains(SlotDino))
            pen.SetRidden(SlotDino, true);
        promptDino = null;
        if (prompt != null) prompt.Hide();
        Fire();
    }

    public void OnPetRunDinoReturned(OwnedDino d)
    {
        if (!InRunAsDino && (SlotDino == null || d != SlotDino)) return;
        InRunAsDino = false;
        if (pen != null && SlotDino != null && pen.Contains(SlotDino))
            pen.SetRidden(SlotDino, false);
        Fire();
    }

    public void DebugSetSlotAndRide(OwnedDino d)
    {
        if (d == null || d.data == null) return;
        InRunAsDino = false;
        if (Mounted) Dismount();
        SlotDino = d;
        Fire();
        if (player == null || (player.State == PlayerState.Walking && !player.RunActive))
            Ride(d);
    }

    // Bấm ô cưỡi: gọi cưỡi ngay / xuống
    public void ToggleRide()
    {
        if (SlotDino == null)
        {
            HudController.Say("Ride a dino in your pen first");
            return;
        }
        if (player != null && (player.State != PlayerState.Walking || player.RunActive)) return;
        if (Mounted) Dismount(); else Ride(SlotDino);
    }

    void OnPenChanged()
    {
        if (SlotDino == null) return;
        if (pen != null && pen.Contains(SlotDino)) return;

        // Dino của ô cưỡi không còn trong chuồng -> gỡ
        if (Mounted)
        {
            Mounted = false;
            visualAnim = null;
            seatBone = null;
            if (visual != null) Destroy(visual);
        }
        SlotDino = null;
        Fire();
    }

    void Fire() { if (Changed != null) Changed(); }
}
