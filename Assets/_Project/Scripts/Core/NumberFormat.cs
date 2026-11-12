using System;
using System.Globalization;

public static class NumberFormat
{
    static readonly string[] Suffix = { "", "K", "M", "B", "T", "Qa", "Qi" };

    public static string Short(double v)
    {
        if (v < 0) v = 0;
        if (v < 1000) return Math.Floor(v).ToString("0", CultureInfo.InvariantCulture);
        int i = 0;
        while (v >= 1000 && i < Suffix.Length - 1) { v /= 1000; i++; }
        string f = v >= 100 ? "0" : (v >= 10 ? "0.#" : "0.##");
        return v.ToString(f, CultureInfo.InvariantCulture) + Suffix[i];
    }
    public static string Small(double v)
    {
        if (v < 0) v = 0;
        if (v < 1000) return v.ToString("0.#", CultureInfo.InvariantCulture);
        return Short(v);
    }
}
