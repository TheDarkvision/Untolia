namespace Untolia.Core.RPG;

public static class CharacterFactory
{
    // Creates a PartyMember from a CharacterDef by evaluating growth curves at the target level.
    public static PartyMember CreateFromDef(CharacterDef def, int? levelOverride = null)
    {
        var level = levelOverride ?? def.StartingLevel;
        if (level < 1) level = 1;

        // Evaluate primary stats at level
        var stats = new PrimaryStats
        {
            Force = CurveEval.EvalAtLevel(def.Growth.Force, level),
            Focus = CurveEval.EvalAtLevel(def.Growth.Focus, level),
            Resolve = CurveEval.EvalAtLevel(def.Growth.Resolve, level),
            Agility = CurveEval.EvalAtLevel(def.Growth.Agility, level)
        };

        // Evaluate vitals at level
        var maxHp = CurveEval.EvalAtLevel(def.Growth.Hp, level);
        var maxMp = CurveEval.EvalAtLevel(def.Growth.Mp, level);
        if (maxHp <= 0) maxHp = 1;
        if (maxMp < 0) maxMp = 0;

        // Build the runtime PartyMember
        var member = new PartyMember(def.Id, def.Name, stats, maxHp, maxMp, level)
        {
            Portrait = def.Portrait
        };

        // Copy tags
        if (def.Tags != null && def.Tags.Count > 0)
            member.Tags.AddRange(def.Tags);

        // Copy elements (resist/weak/immune)
        if (def.Elements != null)
        {
            if (def.Elements.Resist != null) member.Resist.UnionWith(def.Elements.Resist);
            if (def.Elements.Weak != null) member.Weak.UnionWith(def.Elements.Weak);
            if (def.Elements.Immune != null) member.Immune.UnionWith(def.Elements.Immune);
        }

        // Copy initial equipment ids
        if (def.Equipment != null)
        {
            member.WeaponId = def.Equipment.Weapon;
            member.ShieldId = def.Equipment.Shield;
            member.ArmorId = def.Equipment.Armor;
            member.TrinketId = def.Equipment.Trinket;
        }

        // If you have an ability system, unlock learnset up to current level here.
        // foreach (var learn in def.Learnset.Where(l => l.Level <= level)) { /* add ability learn.Ability */ }

        return member;
    }
}