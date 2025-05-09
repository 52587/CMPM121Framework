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
        // Set the team and last_cast for this modifier instance itself
        this.team = team;
        this.last_cast = Time.time; // This specific cast action time

        Debug.Log($"[DoublerModifier.Cast] First cast of {wrappedSpell.GetName()}");
        // First cast - Call the base decorator's Cast, which calls the wrapped spell's Cast.
        // The wrappedSpell's last_cast will be updated by its own Cast method.
        yield return owner.StartCoroutineFromOwner(wrappedSpell.Cast(where, target, team));

        Debug.Log($"[DoublerModifier.Cast] Waiting for delay: {delay}s");
        yield return new WaitForSeconds(delay);

        // Second cast
        // Important: We need to ensure the second cast doesn't share the exact same last_cast time as the first one
        // for cooldown purposes of the *wrapped spell* if its cooldown is very short.
        // However, the DoublerModifier itself has one last_cast time for *its* activation.
        // The wrapped spell will manage its own cooldown based on its own last_cast updates.
        Debug.Log($"[DoublerModifier.Cast] Second cast of {wrappedSpell.GetName()}");
        yield return owner.StartCoroutineFromOwner(wrappedSpell.Cast(where, target, team));
        Debug.Log($"[DoublerModifier.Cast] {GetName()} finished.");
    }
}