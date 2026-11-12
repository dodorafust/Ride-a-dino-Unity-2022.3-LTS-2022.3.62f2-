using System;
using System.Collections.Generic;

public class Inventory
{
    public readonly List<OwnedDino> Items = new List<OwnedDino>();
    public OwnedDino Held { get; private set; }
    public event Action Changed;

    public void Add(OwnedDino d, bool autoHold = true)
    {
        Items.Add(d);
        if (autoHold && Held == null) Held = d;
        Fire();
    }

    public bool Remove(OwnedDino d)
    {
        if (!Items.Remove(d)) return false;
        if (Held == d) Held = null;
        Fire();
        return true;
    }

    public void ToggleHold(OwnedDino d) { Held = (Held == d) ? null : d; Fire(); }
    public void SetHeld(OwnedDino d) { Held = d; Fire(); }

    public List<OwnedDino> GetBest(int count)
    {
        var list = new List<OwnedDino>(Items);
        list.Sort((a, b) => b.MoneyPerSecond.CompareTo(a.MoneyPerSecond));
        if (list.Count > count) list.RemoveRange(count, list.Count - count);
        return list;
    }

    void Fire() { if (Changed != null) Changed(); }
}
