using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections;

public class SplitterModifier : SpellDecorator
{
    private float angle;
    private float manaMultiplier;

    public SplitterModifier(Spell wrappedSpell, SpellCaster owner, JObject spellData) : base(wrappedSpell, owner, spellData)
    {
        float.TryParse(spellData?["angle"]?.Value<string>() ?? "10", out angle);
        float.TryParse(spellData?["mana_multiplier"]?.Value<string>() ?? "1", out manaMultiplier);
    }

    public override int GetManaCost()
    {
        return Mathf.RoundToInt(base.GetManaCost() * manaMultiplier);
    }

    public override IEnumerator Cast(Vector3 where, Vector3 target, Hittable.Team team)
    {
        this.team = team;
        this.last_cast = Time.time;

        Vector3 direction = (target - where).normalized;

        // First projectile (slightly angled)
        Vector3 dir1 = Quaternion.Euler(0, 0, angle / 2f) * direction;
        Debug.Log($"[SplitterModifier.Cast] First split cast of {wrappedSpell.GetName()} at angle {angle / 2f}");
        yield return owner.StartCoroutineFromOwner(wrappedSpell.Cast(where, where + dir1, team)); // Target is relative to 'where'

        // Second projectile (slightly angled the other way)
        Vector3 dir2 = Quaternion.Euler(0, 0, -angle / 2f) * direction;
        Debug.Log($"[SplitterModifier.Cast] Second split cast of {wrappedSpell.GetName()} at angle {-angle / 2f}");
        yield return owner.StartCoroutineFromOwner(wrappedSpell.Cast(where, where + dir2, team));
        Debug.Log($"[SplitterModifier.Cast] {GetName()} finished.");
    }
}