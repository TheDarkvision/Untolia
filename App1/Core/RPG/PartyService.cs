namespace Untolia.Core.RPG;

public sealed class PartyService
{
    private readonly List<string> _active = new();

    // Add missing fields and properties
    private readonly Dictionary<string, PartyMember> _roster = new(StringComparer.OrdinalIgnoreCase);

    public int MaxPartySize { get; set; } = 3;

    public IReadOnlyList<PartyMember> Active => _active.Select(id => _roster[id]).ToList();
    public IReadOnlyCollection<PartyMember> Roster => _roster.Values;

    // Recruit by CharacterDef (creates runtime member, registers, and optionally adds to party)
    public PartyMember Recruit(CharacterDef def, int? levelOverride = null, bool addToParty = true,
        int? position = null)
    {
        // If already exists in roster, return it
        if (_roster.TryGetValue(def.Id, out var existing))
        {
            if (addToParty && !_active.Contains(def.Id) && _active.Count < MaxPartySize)
                AddToParty(def.Id, position);
            return existing;
        }

        var member = CharacterFactory.CreateFromDef(def, levelOverride);
        _roster[member.Id] = member;

        if (addToParty && _active.Count < MaxPartySize)
            AddToParty(member.Id, position);

        return member;
    }

    // ... existing code ...
    // Recruit by ID using registry
    public PartyMember? Recruit(CharacterRegistry registry, string id, int? levelOverride = null,
        bool addToParty = true, int? position = null)
    {
        var def = registry.Get(id);
        if (def == null) return null;
        return Recruit(def, levelOverride, addToParty, position);
    }

    // Define characters up-front or dynamically as the story progresses
    public bool Register(PartyMember member)
    {
        if (_roster.ContainsKey(member.Id)) return false;
        _roster[member.Id] = member;
        return true;
    }

    // Add to current party if in roster and space permits
    public bool AddToParty(string id, int? insertAt = null)
    {
        if (!_roster.ContainsKey(id)) return false;
        if (_active.Contains(id)) return false;
        if (_active.Count >= MaxPartySize) return false;

        if (insertAt.HasValue)
        {
            var idx = Math.Clamp(insertAt.Value, 0, _active.Count);
            _active.Insert(idx, id);
        }
        else
        {
            _active.Add(id);
        }

        _roster[id].InParty = true;
        return true;
    }

    // Remove from party (does not remove from roster)
    public bool RemoveFromParty(string id)
    {
        var removed = _active.Remove(id);
        if (removed) _roster[id].InParty = false;
        return removed;
    }

    // Temporarily depart and remember position? For now, just remove.
    public bool DepartTemporarily(string id)
    {
        return RemoveFromParty(id);
    }

    // Return to party (tries to reinsert at position or at end)
    public bool ReturnToParty(string id, int? position = null)
    {
        return AddToParty(id, position);
    }

    public bool Swap(int indexA, int indexB)
    {
        if (indexA < 0 || indexB < 0 || indexA >= _active.Count || indexB >= _active.Count) return false;
        (_active[indexA], _active[indexB]) = (_active[indexB], _active[indexA]);
        return true;
    }

    public PartyMember? Get(string id)
    {
        return _roster.TryGetValue(id, out var m) ? m : null;
    }

    // Example convenience APIs
    public void AwardExpAll(int amount)
    {
        AwardExpAll(amount, null, null);
    }


    public void ClearParty()
    {
        foreach (var id in _active.ToArray())
            _roster[id].InParty = false;
        _active.Clear();
    }

    // Returns a map of member -> levels gained (0 if none). Optionally recomputes stats from registry and invokes a callback per level-up.
    public Dictionary<PartyMember, int> AwardExpAll(int amount, CharacterRegistry? registry = null,
        Action<PartyMember, int>? onLevelUp = null)
    {
        var results = new Dictionary<PartyMember, int>();
        foreach (var id in _active.ToArray())
        {
            var m = _roster[id];
            var gained = m.GainExp(amount);
            results[m] = gained;

            if (gained > 0)
            {
                if (registry != null)
                {
                    var def = registry.Get(m.Id);
                    if (def != null) m.RecomputeDerivedFromDef(def);
                    // Optional: unlock learnset up to new level here if you wire an ability system.
                }

                onLevelUp?.Invoke(m, gained);
            }
        }

        return results;
    }
}