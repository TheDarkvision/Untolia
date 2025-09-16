using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Untolia.Core.RPG;

public sealed class PrimaryStats
{
    public int Force { get; set; }
    public int Focus { get; set; }
    public int Resolve { get; set; }
    public int Agility { get; set; }

    public PrimaryStats() { }

    public PrimaryStats(int force, int focus, int resolve, int agility)
    {
        Force = force;
        Focus = focus;
        Resolve = resolve;
        Agility = agility;
    }

    public PrimaryStats Clone() => new(Force, Focus, Resolve, Agility);
}

public sealed class Progression
{
    public int Level { get; private set; } = 1;
    public int Exp { get; private set; } = 0;

    public int ExpToNext => 100 + (Level - 1) * 50;

    public bool GainExp(int amount)
    {
        if (amount <= 0) return false;
        Exp += amount;
        var leveled = false;
        while (Exp >= ExpToNext)
        {
            Exp -= ExpToNext;
            Level++;
            leveled = true;
        }
        return leveled;
    }

    internal void ForceLevelSet(int level) => Level = level;
}

public sealed class Vital
{
    public int Max { get; private set; }
    public int Current { get; private set; }

    public Vital(int max)
    {
        Max = System.Math.Max(1, max);
        Current = Max;
    }

    public void Heal(int amount) => Current = System.Math.Min(Max, Current + System.Math.Max(0, amount));
    public void Damage(int amount) => Current = System.Math.Max(0, Current - System.Math.Max(0, amount));
    public bool IsZero => Current <= 0;

    public void SetMax(int max, bool refill)
    {
        Max = System.Math.Max(1, max);
        if (refill) Current = Max;
        else Current = System.Math.Min(Current, Max);
    }
}

public sealed class PartyMember
{
    public string Id { get; }
    public string Name { get; set; }

    public PrimaryStats BaseStats { get; }
    public Progression Progress { get; }
    public Vital HP { get; }
    public Vital MP { get; }

    public bool InParty { get; internal set; }

    // New: extra metadata carried from CharacterDef
    public string? Portrait { get; set; }
    public List<string> Tags { get; } = new();
    public HashSet<string> Resist { get; } = new();
    public HashSet<string> Weak { get; } = new();
    public HashSet<string> Immune { get; } = new();

    public string? WeaponId { get; set; }
    public string? ShieldId { get; set; }
    public string? ArmorId { get; set; }
    public string? TrinketId { get; set; }

    public PartyMember(string id, string name, PrimaryStats stats, int baseHp, int baseMp, int startLevel = 1)
    {
        Id = id;
        Name = name;
        BaseStats = stats.Clone();
        Progress = new Progression();

        // initialize vitals based on provided base values
        HP = new Vital(baseHp);
        MP = new Vital(baseMp);

        if (startLevel > 1)
        {
            // set level directly; caller should have evaluated stats/HP/MP at target level already
            Progress.ForceLevelSet(startLevel);
        }
    }

// Returns how many levels were gained (0 if none)
    public int GainExp(int amount)
    {
        int before = Progress.Level;
        var leveled = Progress.GainExp(amount);
        int gained = Progress.Level - before;
        if (gained > 0) OnLevelUp(gained);
        return gained;
    }


    private void OnLevelUp(int levelsGained)
    {
        // Hook point: visuals/SFX can be triggered by caller via a callback if needed.
        // Recalculation of stats/vitals is handled via RecomputeDerivedFromDef when a CharacterDef is available.
    }

    // Recompute current stats and vitals from a CharacterDef at the current level
    public void RecomputeDerivedFromDef(CharacterDef def)
    {
        int lvl = Progress.Level;

        // Update primary stats from growth curves (if present), otherwise keep current
        if (def.Growth != null)
        {
            BaseStats.Force   = CurveEval.EvalAtLevel(def.Growth.Force, lvl);
            BaseStats.Focus   = CurveEval.EvalAtLevel(def.Growth.Focus, lvl);
            BaseStats.Resolve = CurveEval.EvalAtLevel(def.Growth.Resolve, lvl);
            BaseStats.Agility = CurveEval.EvalAtLevel(def.Growth.Agility, lvl);

            var newMaxHp = CurveEval.EvalAtLevel(def.Growth.Hp, lvl);
            var newMaxMp = CurveEval.EvalAtLevel(def.Growth.Mp, lvl);
            if (newMaxHp <= 0) newMaxHp = 1;
            if (newMaxMp < 0) newMaxMp = 0;

            // Keep current HP/MP within new maxima; optionally heal a bit here if you want
            HP.SetMax(newMaxHp, refill: false);
            MP.SetMax(newMaxMp, refill: false);
        }
    }

    
}
