using UnityEngine;

// Dino đi lang thang trong chuồng + số "+$" bay lên mỗi giây.
public class PenDino : MonoBehaviour
{
    public float speed = 1.6f;
    public float popupInterval = 1f;

    public OwnedDino Owned { get; private set; }
    public bool IsRidden { get; private set; }

    Pen pen;
    GameObject model;
    WorldLabel label;
    Vector3 target;
    float wait, popupTimer;

    public void Init(OwnedDino owned, Pen owner)
    {
        Owned = owned;
        pen = owner;
        model = DinoView.BuildModel(transform, owned, new Vector3(1.1f, 1.4f, 2.2f));

        if (GameManager.Instance != null)
            label = GameManager.Instance.CreateLabel(transform, new Vector3(0f, 2.6f, 0f), LabelText());

        popupTimer = Random.Range(0f, popupInterval);
        transform.position = RandomPoint();
        PickTarget();
    }

    string LabelText()
    {
        return "<color=#" + DinoColors.Hex(DinoColors.Of(Owned.data.rarity)) + ">" + Owned.Title + "</color>\n" +
               "<color=#55FF55>$" + NumberFormat.Short(Owned.MoneyPerSecond) + "/s</color>";
    }

    // Đang được người chơi cưỡi đi: ẩn khỏi chuồng
    public void SetRidden(bool v)
    {
        IsRidden = v;
        if (model != null) model.SetActive(!v);
        if (label != null) label.hidden = v;
        if (!v)
        {
            transform.position = RandomPoint();   // xuống xe thì về lại trong chuồng
            PickTarget();
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

        popupTimer -= Time.deltaTime;
        if (popupTimer <= 0f)
        {
            popupTimer += popupInterval;
            if (GameManager.Instance != null)
                GameManager.Instance.ShowPopup(transform.position + Vector3.up * 2.2f,
                                               "+$" + NumberFormat.Short(Owned.MoneyPerSecond * popupInterval),
                                               PopupKind.Cash);
        }

        if (wait > 0f)
        {
            wait -= Time.deltaTime;
            if (wait <= 0f) PickTarget();
            return;
        }

        Vector3 to = target - transform.position;
        to.y = 0f;
        if (to.magnitude < 0.2f)
        {
            wait = Random.Range(0.5f, 2f);
            return;
        }

        Vector3 dir = to.normalized;
        transform.position += dir * speed * Time.deltaTime;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
    }
}
