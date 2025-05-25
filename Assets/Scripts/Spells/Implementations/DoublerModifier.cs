using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections;

public class DoublerModifier : SpellDecorator
{
    private float delay;
    private float manaMultiplier;
    private float cooldownMultiplier;

    public DoublerModifier(Spell wrappedSpell, SpellCaster owner, JObject spellData) : base(wrappedSpell, owner, spellData)
    {
        float.TryParse(spellData?["delay"]?.Value<string>() ?? "0.5", out delay);
        float.TryParse(spellData?["mana_multiplier"]?.Value<string>() ?? "1", out manaMultiplier);
        float.TryParse(spellData?["cooldown_multiplier"]?.Value<string>() ?? "1", out cooldownMultiplier);
    }

    public override int GetManaCost()
    {
        return Mathf.RoundToInt(base.GetManaCost() * manaMultiplier);
    }

    public override float GetCooldown()
    {
        return base.GetCooldown() * cooldownMultiplier;
    }

    public override IEnumerator Cast(Vector3 where, Vector3 target, Hittable.Team team)
    {
        this.team = team;
        this.last_cast = Time.time;

        // Debug.Log($"[DoublerModifier.Cast] First cast of {wrappedSpell.GetName()}");
        // First cast
        yield return owner.StartCoroutineFromOwner(wrappedSpell.Cast(where, target, team));

        // Wait for the delay
        // Debug.Log($"[DoublerModifier.Cast] Waiting for delay: {delay}s");
        float startTime = Time.time;
        while (Time.time < startTime + delay)
        {
            yield return null; // Wait for the next frame
        }

        // Debug.Log($"[DoublerModifier.Cast] Second cast of {wrappedSpell.GetName()}");
        // Second cast
        yield return owner.StartCoroutineFromOwner(wrappedSpell.Cast(where, target, team));
        // Debug.Log($"[DoublerModifier.Cast] {GetName()} finished.");
    }

    // Other overrides (GetName, GetDescription, GetManaCost, GetCooldown, etc.)
}