using System;
public interface IUpgradable
{
    string UpgradeName { get; }
    int Level { get; }
    bool IsMaxLevel { get; }
    double UpgradeCost { get; }
    string NextLevelInfo { get; }
    event Action Changed;
    void TryUpgrade();
}
