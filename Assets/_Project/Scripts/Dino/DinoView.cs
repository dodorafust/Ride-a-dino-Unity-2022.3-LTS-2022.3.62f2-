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
