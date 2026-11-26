using UnityEngine;

// Dino đi lang thang trong chuồng + số "+$" bay lên mỗi giây.
public class PenDino : MonoBehaviour
{
    public float speed = 1.6f;
    public float returnWalkSpeed = 4.5f;
    [Min(0.5f)] public float animReferenceSpeed = 4.5f;
    public float popupInterval = 1f;
    [Header("Vị trí chữ & tiền trên đầu Dino")]
    public float labelHeadPadding = 1.4f;
    public float moneyPopupHeadPadding = 0.35f;

    public OwnedDino Owned { get; private set; }
    public bool IsRidden { get; private set; }

    Pen pen;
    GameObject model;
    Animator anim;
    WorldLabel label;
    Vector3 target;
    float wait, popupTimer;
    float headTopY = 2.2f;
    bool returningToPen;

    public void Init(OwnedDino owned, Pen owner)
    {
        Owned = owned;
        pen = owner;
        model = DinoView.BuildModel(transform, owned, new Vector3(1.1f, 1.4f, 2.2f));
        anim = model != null ? model.GetComponentInChildren<Animator>() : null;
        headTopY = DinoView.GetModelTopOffset(model, transform, 0f, 2.2f);

        if (GameManager.Instance != null)
            label = GameManager.Instance.CreateLabel(transform, new Vector3(0f, headTopY + labelHeadPadding, 0f), LabelText());

        popupTimer = Random.Range(0f, popupInterval);
        transform.position = RandomPoint();
        PickTarget();
    }

    string LabelText()
    {
        return DinoView.BuildWorldLabelText(Owned, false, 0f);
    }

    // Đang được người chơi cưỡi đi: ẩn khỏi chuồng
    public void SetRidden(bool v, Vector3? walkBackFromPos = null, Quaternion? startRot = null)
    {
        IsRidden = v;
        if (model != null) model.SetActive(!v);
        if (label != null) label.hidden = v;
        if (!v)
        {
            if (walkBackFromPos.HasValue)
            {
                Vector3 p = walkBackFromPos.Value;
                p.y = pen != null ? pen.AreaBounds.min.y : p.y;
                if (Physics.Raycast(walkBackFromPos.Value + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f,
                                    ~((1 << 2) | (1 << 6) | (1 << 7)), QueryTriggerInteraction.Ignore))
                    p.y = hit.point.y;
                transform.position = p;
                if (startRot.HasValue)
                {
                    Vector3 e = startRot.Value.eulerAngles;
                    transform.rotation = Quaternion.Euler(0f, e.y, 0f);
                }
                returningToPen = true;
                PickTarget();
            }
            else
            {
                returningToPen = false;
                transform.position = RandomPoint();   // xuống xe từ RunZone thì về lại trong chuồng
                PickTarget();
            }
        }
        else
        {
            returningToPen = false;
        }
    }

    Vector3 RandomPoint()
    {
        Bounds a = pen.AreaBounds;
        float m = 1f;
        return new Vector3(
            Random.Range(a.min.x + m, a.max.x - m),
            a.min.y,
            Random.Range(a.min.z + m, a.max.z - m));
    }

    void PickTarget()
    {
        target = RandomPoint();
        wait = 0f;
    }

    void Update()
    {
        if (IsRidden) return;

        float curLabelPad = pen != null ? pen.dinoLabelHeadPadding : labelHeadPadding;
        float curMoneyPad = pen != null ? pen.dinoMoneyPopupHeadPadding : moneyPopupHeadPadding;
        if (label != null) label.SetOffset(new Vector3(0f, headTopY + curLabelPad, 0f));

        popupTimer -= Time.deltaTime;
        if (popupTimer <= 0f)
        {
            popupTimer += popupInterval;
            if (GameManager.Instance != null)
                GameManager.Instance.ShowPopup(transform.position + Vector3.up * (headTopY + curMoneyPad),
                                               "+$" + NumberFormat.Short(Owned.MoneyPerSecond * popupInterval),
                                               PopupKind.Cash);
        }

        if (wait > 0f)
        {
            if (anim != null)
            {
                anim.SetBool("IsMoving", false);
                anim.speed = 1f;
            }
            wait -= Time.deltaTime;
            if (wait <= 0f) PickTarget();
            return;
        }

        Vector3 to = target - transform.position;
        to.y = 0f;
        if (to.magnitude < 0.25f)
        {
            returningToPen = false;
            if (anim != null)
            {
                anim.SetBool("IsMoving", false);
                anim.speed = 1f;
            }
            wait = Random.Range(0.5f, 2f);
            return;
        }

        float moveSpd = returningToPen ? Mathf.Max(speed, returnWalkSpeed) : speed;
        if (anim != null)
        {
            anim.SetBool("IsMoving", true);
            anim.speed = Mathf.Clamp(moveSpd / Mathf.Max(0.5f, animReferenceSpeed), 0.25f, 1.5f);
        }

        Vector3 dir = to.normalized;
        transform.position += dir * moveSpd * Time.deltaTime;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
    }
}
