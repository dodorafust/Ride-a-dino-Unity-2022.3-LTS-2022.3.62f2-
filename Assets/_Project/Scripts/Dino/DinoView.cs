using UnityEngine;

// Dựng model cho 1 OwnedDino: model 3D nếu có, không thì khối hộp greybox. Áp màu Variant + VFX Variant.
public static class DinoView
{
    public static GameObject BuildModel(Transform parent, OwnedDino owned, Vector3 greyboxScale)
    {
        GameObject model;
        bool greybox = owned.data.modelPrefab == null;
        if (!greybox)
        {
            model = Object.Instantiate(owned.data.modelPrefab, parent);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale *= Mathf.Max(0.1f, owned.data.modelScale * owned.SizeScaleMultiplier);

            Animator anim = model.GetComponentInChildren<Animator>();
            if (anim == null)
            {
                GameObject animRoot = model.transform.childCount > 0 ? model.transform.GetChild(0).gameObject : model;
                anim = animRoot.AddComponent<Animator>();
            }
            if (owned.data.animatorController != null)
                anim.runtimeAnimatorController = owned.data.animatorController;
            else if (anim.runtimeAnimatorController == null)
                anim.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("DinoController");
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }
        else
        {
            model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(model.GetComponent<Collider>());
            model.transform.SetParent(parent, false);
            model.transform.localScale = greyboxScale;
            model.transform.localPosition = new Vector3(0f, greyboxScale.y * 0.5f, 0f);
        }
        ApplyLook(model, owned, greybox);
        return model;
    }

    public static float GetModelTopOffset(GameObject model, Transform root, float padding, float fallbackHeight = 2.2f)
    {
        if (model == null || root == null) return fallbackHeight + padding;
        Renderer[] rs = model.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds b = default(Bounds);
        for (int i = 0; i < rs.Length; i++)
        {
            if (rs[i] == null || rs[i] is ParticleSystemRenderer) continue;
            if (!hasBounds) { b = rs[i].bounds; hasBounds = true; }
            else b.Encapsulate(rs[i].bounds);
        }
        return (hasBounds ? Mathf.Max(0.2f, b.max.y - root.position.y) : fallbackHeight) + padding;
    }

    public static string BuildWorldLabelText(OwnedDino owned, bool isWild, float playerStamina)
    {
        if (owned == null || owned.data == null) return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        string tagsLine = "";
        if (owned.variant != null && !owned.variant.isNormal)
        {
            Color vCol = (owned.variant.useTint && owned.variant.tint.maxColorComponent > 0.15f)
                ? owned.variant.tint
                : new Color(1f, 0.85f, 0.29f);
            tagsLine = "<color=#" + DinoColors.Hex(vCol) + ">" + owned.variant.displayName.ToUpperInvariant() + "</color>";
        }
        if (!string.IsNullOrEmpty(owned.SizeTag))
        {
            string sTag = "<color=#" + DinoColors.Hex(DinoSizeModifiers.ColorOf(owned.sizeType)) + ">" + owned.SizeTag + "</color>";
            tagsLine = string.IsNullOrEmpty(tagsLine) ? sTag : tagsLine + "  " + sTag;
        }
        if (owned.trait != DinoTrait.None)
        {
            string tTag = "<color=#B89CFF>" + owned.trait.ToString().ToUpperInvariant() + "</color>";
            tagsLine = string.IsNullOrEmpty(tagsLine) ? tTag : tagsLine + "  " + tTag;
        }

        if (!string.IsNullOrEmpty(tagsLine))
            sb.Append(tagsLine).Append('\n');

        sb.Append("<color=#").Append(DinoColors.Hex(DinoColors.Of(owned.data.rarity))).Append('>')
          .Append(owned.data.rarity).Append("</color>\n");

        sb.Append(owned.data.displayName).Append('\n');

        if (isWild)
        {
            float reqStamina = owned.EffectiveRequiredStamina;
            string reqColor = playerStamina >= reqStamina ? "55FF55" : "FF5555";
            sb.Append("<color=#").Append(reqColor).Append(">Required: ")
              .Append(NumberFormat.Short(reqStamina)).Append("</color>");
        }
        else
        {
            sb.Append("<color=#55FF55>$").Append(NumberFormat.Short(owned.MoneyPerSecond)).Append("/s</color>");
        }

        return sb.ToString();
    }

    static readonly string[] DefaultSeatBones = { "spine", "chest", "hips", "pelvis", "back" };

    public static Transform FindSeatBone(Transform root, string preferredName)
    {
        if (root == null) return null;
        if (!string.IsNullOrEmpty(preferredName))
        {
            Transform exact = FindChildRecursive(root, preferredName);
            if (exact != null) return exact;
        }
        for (int i = 0; i < DefaultSeatBones.Length; i++)
        {
            Transform found = FindChildRecursive(root, DefaultSeatBones[i]);
            if (found != null) return found;
        }
        return null;
    }

    static Transform FindChildRecursive(Transform parent, string targetName)
    {
        if (string.Equals(parent.name, targetName, System.StringComparison.OrdinalIgnoreCase))
            return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform hit = FindChildRecursive(parent.GetChild(i), targetName);
            if (hit != null) return hit;
        }
        return null;
    }

    // greybox = true: không có màu thì lấy màu Rarity. Model thật chỉ bị nhuộm khi Variant bật useTint.
    public static bool TryGetTint(OwnedDino owned, bool greybox, out Color c)
    {
        DinoVariant v = owned.variant;
        if (v != null && v.useTint) { c = v.tint; return true; }
        if (greybox) { c = DinoColors.Of(owned.data.rarity); return true; }
        c = Color.white;
        return false;
    }

    public static void ApplyLook(GameObject model, OwnedDino owned, bool greybox)
    {
        Color c;
        if (TryGetTint(owned, greybox, out c)) SetColor(model, c);

        DinoVariant v = owned.variant;
        if (v != null && v.vfxPrefab != null) Object.Instantiate(v.vfxPrefab, model.transform);
    }

    public static void SetColor(GameObject root, Color c)
    {
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_Color", c);       // Built-in
        mpb.SetColor("_BaseColor", c);   // URP
        Renderer[] rs = root.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < rs.Length; i++)
        {
            if (rs[i] is ParticleSystemRenderer) continue;
            rs[i].SetPropertyBlock(mpb);
        }
    }
}
