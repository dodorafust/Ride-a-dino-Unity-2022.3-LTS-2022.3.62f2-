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

    public OwnedDino SlotDino { get; private set; }
    public bool Mounted { get; private set; }
    public float SpeedMultiplier { get { return Mounted ? speedMultiplier : 1f; } }

    public event Action Changed;

    PlayerController player;
    Pen pen;
    WorldPromptUI prompt;
    GameObject visual;
    OwnedDino promptDino;

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

        if (Mounted && visual != null && player != null)
        {
            Vector3 v = player.Velocity;
            v.y = 0f;
            if (v.sqrMagnitude > 0.5f)
                visual.transform.rotation = Quaternion.Slerp(visual.transform.rotation,
                                                             Quaternion.LookRotation(v), 10f * Time.deltaTime);
        }
    }

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
        if (d == null || pen == null || !pen.Contains(d)) return;
        if (player != null && player.State != PlayerState.Walking) return;

        if (Mounted) Dismount();
        SlotDino = d;
        Mounted = true;
        pen.SetRidden(d, true);

        visual = new GameObject("MountVisual");
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = visualOffset;
        DinoView.BuildModel(visual.transform, d, new Vector3(1.1f, 1.4f, 2.2f));

        promptDino = null;
        if (prompt != null) prompt.Hide();
        Fire();
    }

    public void Dismount()
    {
        if (!Mounted) return;
        Mounted = false;
        if (visual != null) Destroy(visual);
        if (pen != null && SlotDino != null) pen.SetRidden(SlotDino, false);
        Fire();
    }

    public void ForceDismount() { Dismount(); }

    // Bấm ô cưỡi: gọi cưỡi ngay / xuống
    public void ToggleRide()
    {
        if (SlotDino == null)
        {
            HudController.Say("Ride a dino in your pen first");
            return;
        }
        if (player != null && player.State != PlayerState.Walking) return;
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
            if (visual != null) Destroy(visual);
        }
        SlotDino = null;
        Fire();
    }

    void Fire() { if (Changed != null) Changed(); }
}
