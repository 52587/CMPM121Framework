using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpellCaster 
{
    public int mana;
    public int max_mana;
    public int mana_reg;
    public Hittable.Team team;
    private Spell currentSpell; 
    public int spellPower; 
    private MonoBehaviour coroutineRunner; // Added: Reference to a MonoBehaviour

    public bool IsOwnerActive => coroutineRunner != null && coroutineRunner.gameObject.activeInHierarchy; // Added

    public IEnumerator ManaRegeneration()
    {
        while (true)
        {
            if (coroutineRunner == null) yield break; // Stop if no runner
            mana += mana_reg;
            mana = Mathf.Min(mana, max_mana);
            yield return new WaitForSeconds(1);
        }
    }

    // Constructor updated
    public SpellCaster(int mana, int mana_reg, Hittable.Team team, int spellPower, MonoBehaviour coroutineRunner)
    {
        this.mana = mana;
        this.max_mana = mana;
        this.mana_reg = mana_reg;
        this.team = team;
        this.spellPower = spellPower;
        this.coroutineRunner = coroutineRunner; // Added
        if (this.coroutineRunner == null)
        {
            Debug.LogError("SpellCaster created with a null coroutineRunner!");
        }
    }

    // Added method to set the current spell
    public void SetCurrentSpell(Spell spellToSet)
    {
        this.currentSpell = spellToSet;
    }

    public IEnumerator Cast(Vector3 where, Vector3 target)
    {        
        Debug.Log($"[SpellCaster.Cast] Attempting to cast {currentSpell?.GetName()}. Mana: {mana}, Cost: {currentSpell?.GetManaCost()}, Ready: {currentSpell?.IsReady()}");
        if (currentSpell == null) 
        {
            Debug.LogWarning("[SpellCaster.Cast] currentSpell is null.");
            yield break;
        }
        if (coroutineRunner == null)
        {
            Debug.LogError("[SpellCaster.Cast] coroutineRunner is null.");
            yield break;
        }

        // Use currentSpell
        if (mana >= currentSpell.GetManaCost() && currentSpell.IsReady())
        {
            Debug.Log("[SpellCaster.Cast] Conditions met. Casting spell.");
            mana -= currentSpell.GetManaCost();
            // Spells themselves return IEnumerator, so the SpellCaster's coroutineRunner should run it.
            yield return coroutineRunner.StartCoroutine(currentSpell.Cast(where, target, team));
            Debug.Log($"[SpellCaster.Cast] Spell {currentSpell.GetName()} coroutine finished.");
        }
        else
        {
            Debug.LogWarning($"[SpellCaster.Cast] Cannot cast spell. Mana sufficient: {mana >= currentSpell.GetManaCost()} (Mana: {mana}, Cost: {currentSpell.GetManaCost()}). IsReady: {currentSpell.IsReady()} (Last Cast: {currentSpell.last_cast}, Cooldown: {currentSpell.GetCooldown()}, Time: {Time.time})");
        }
        // Removed yield break here as the above yield return already handles it.
    }

    // Added: Method to start coroutines using the owner's context
    public Coroutine StartCoroutineFromOwner(IEnumerator coroutine)
    {
        if (coroutineRunner != null && coroutineRunner.gameObject.activeInHierarchy)
        {
            return coroutineRunner.StartCoroutine(coroutine);
        }
        else
        {
            if (coroutineRunner == null) Debug.LogWarning("SpellCaster: coroutineRunner is null. Cannot start coroutine.");
            else Debug.LogWarning("SpellCaster: coroutineRunner's GameObject is inactive. Cannot start coroutine.");
            return null;
        }
    }
}
