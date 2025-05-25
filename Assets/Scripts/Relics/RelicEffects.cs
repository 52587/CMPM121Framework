using UnityEngine;
using System.Collections.Generic;
using Relics;

namespace Relics
{
    // --- Base Effect Implementation ---
    public abstract class RelicEffect : MonoBehaviour, IRelicEffect
    {
        protected Relic currentRelic;
        protected PlayerController player;
        protected RelicJsonData.EffectData effectData;
        protected bool isTemporaryEffectActive = false;
        
        // Enhanced timing system properties
        protected bool hasTimingConstraint = false;
        protected string timingType = null; // "next-spell", "move", "duration", "wave-start"
        protected float timingDuration = 0f; // For duration-based timing
        protected bool waitingForSpellAfterCast = false;
        protected bool waitingForMovement = false;
        protected Vector3 lastPlayerPosition;
        protected Coroutine durationCoroutine = null;

        // Signature updated to match IRelicEffect and use RelicJsonData.EffectData
        public virtual void Initialize(Relic relicOwner, PlayerController playerOwner, RelicJsonData.EffectData effectData)
        {
            currentRelic = relicOwner;
            player = playerOwner;
            this.effectData = effectData;
            
            ParseTimingConstraints();
        }

        protected virtual void ParseTimingConstraints()
        {
            if (effectData != null && !string.IsNullOrEmpty(effectData.until))
            {
                hasTimingConstraint = true;
                string until = effectData.until.ToLower();
                
                if (until == "next-spell")
                {
                    timingType = "next-spell";
                    Debug.Log($"[{GetEffectType()}] Parsed timing: next-spell");
                }
                else if (until == "move")
                {
                    timingType = "move";
                    Debug.Log($"[{GetEffectType()}] Parsed timing: move");
                }
                else if (until.StartsWith("duration"))
                {
                    timingType = "duration";
                    // Parse "duration X" where X is the number of seconds
                    string[] parts = until.Split(' ');
                    if (parts.Length >= 2 && float.TryParse(parts[1], out float duration))
                    {
                        timingDuration = duration;
                        Debug.Log($"[{GetEffectType()}] Parsed timing: duration {timingDuration} seconds");
                    }
                    else
                    {
                        Debug.LogWarning($"[{GetEffectType()}] Failed to parse duration from: {until}");
                        hasTimingConstraint = false;
                    }
                }
                else if (until == "wave-start")
                {
                    timingType = "wave-start";
                    Debug.Log($"[{GetEffectType()}] Parsed timing: wave-start");
                }
                else
                {
                    Debug.LogWarning($"[{GetEffectType()}] Unknown timing constraint: {until}");
                    hasTimingConstraint = false;
                }
            }
        }

        protected virtual void StartTimingConstraint()
        {
            if (!hasTimingConstraint) return;
            
            switch (timingType)
            {
                case "next-spell":
                    // Effect will be removed when OnSpellCast() is called
                    break;
                    
                case "move":
                    lastPlayerPosition = player.transform.position;
                    waitingForMovement = true;
                    StartCoroutine(CheckForMovement());
                    break;
                    
                case "duration":
                    if (timingDuration > 0)
                    {
                        durationCoroutine = StartCoroutine(DurationTimer());
                    }
                    break;
                    
                case "wave-start":
                    // Wave-start timing - will be handled when integrated with wave system
                    Debug.Log($"[{GetEffectType()}] Wave-start timing prepared");
                    break;
            }
        }

        protected virtual System.Collections.IEnumerator CheckForMovement()
        {
            while (waitingForMovement && isTemporaryEffectActive)
            {
                yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
                
                if (Vector3.Distance(player.transform.position, lastPlayerPosition) > 0.1f)
                {
                    Debug.Log($"[{GetEffectType()}] Player moved - removing effect");
                    waitingForMovement = false;
                    RemoveEffect();
                    yield break;
                }
            }
        }

        protected virtual System.Collections.IEnumerator DurationTimer()
        {
            yield return new WaitForSeconds(timingDuration);
            
            if (isTemporaryEffectActive)
            {
                Debug.Log($"[{GetEffectType()}] Duration {timingDuration}s expired - removing effect");
                RemoveEffect();
            }
        }

        // Method to be called when a spell is cast (called from RelicManager)
        public virtual void OnSpellCast()
        {
            if (hasTimingConstraint && timingType == "next-spell" && isTemporaryEffectActive)
            {
                Debug.Log($"[{GetEffectType()}] Spell cast detected - subscribing to damage events to remove effect after damage is dealt");
                waitingForSpellAfterCast = true;
                
                // Subscribe to damage events to remove effect after first damage is dealt
                if (EventBus.Instance != null)
                {
                    EventBus.Instance.OnPlayerDealtDamage += OnPlayerDealtDamageHandler;
                    Debug.Log($"[{GetEffectType()}] Subscribed to OnPlayerDealtDamage event");
                }
            }
        }
        
        protected virtual void OnPlayerDealtDamageHandler(Hittable target, Damage damage)
        {
            if (waitingForSpellAfterCast && isTemporaryEffectActive)
            {
                Debug.Log($"[{GetEffectType()}] Player dealt damage after spell cast - removing temporary effect");
                
                // Unsubscribe from the event
                if (EventBus.Instance != null)
                {
                    EventBus.Instance.OnPlayerDealtDamage -= OnPlayerDealtDamageHandler;
                }
                
                waitingForSpellAfterCast = false;
                RemoveEffect();
            }
        }

        protected virtual void CleanupTimingConstraints()
        {
            waitingForSpellAfterCast = false;
            waitingForMovement = false;
            
            if (durationCoroutine != null)
            {
                StopCoroutine(durationCoroutine);
                durationCoroutine = null;
            }
            
            // Unsubscribe from damage events
            if (EventBus.Instance != null)
            {
                EventBus.Instance.OnPlayerDealtDamage -= OnPlayerDealtDamageHandler;
            }
        }

        public abstract void ApplyEffect(params object[] args);
        public abstract void RemoveEffect(); // For temporary effects or cleanup
        public abstract string GetEffectType();

        // Implementation for IRelicEffect
        public virtual void Activate() 
        {
            isTemporaryEffectActive = true;
            StartTimingConstraint();
            Debug.Log($"BaseRelicEffect Activated for {currentRelic?.Name}");
        }

        public virtual void Deactivate()
        {
            isTemporaryEffectActive = false;
            CleanupTimingConstraints();
            Debug.Log($"BaseRelicEffect Deactivated for {currentRelic?.Name}");
        }

        public virtual bool IsActive()
        {
            return isTemporaryEffectActive;
        }

        protected virtual void OnDestroy()
        {
            CleanupTimingConstraints();
        }

        protected float ParseAmountFromFormula(string formula)
        {
            int currentWave = 0;
            if (GameManager.Instance != null)
            {
                // currentWave = GameManager.Instance.GetCurrentWaveNumber(); // Placeholder
            }
            return RelicFormulaParser.ParseAmount(formula, currentWave);
        }
    }

    // --- Effect Implementations ---

    public class GainManaEffect : RelicEffect
    {
        public override string GetEffectType() => "gain-mana";

        public override void ApplyEffect(params object[] args)
        {
            Debug.Log($"[GainManaEffect] ApplyEffect called for relic: {currentRelic?.Name}");
            
            if (player == null)
            {
                Debug.LogError($"[GainManaEffect] Player is null for relic: {currentRelic?.Name}");
                return;
            }
            
            if (player.spellcaster == null)
            {
                Debug.LogError($"[GainManaEffect] Player.spellcaster is null for relic: {currentRelic?.Name}");
                return;
            }
            
            // Use this.effectData
            float manaToGain = ParseAmountFromFormula(this.effectData.amount);
            int oldMana = player.spellcaster.mana;
            player.spellcaster.mana = Mathf.Min(player.spellcaster.mana + (int)manaToGain, player.spellcaster.max_mana);
            int newMana = player.spellcaster.mana;
            
            Debug.Log($"[GainManaEffect] {currentRelic.Name} triggered: Gained {manaToGain} mana. Mana: {oldMana} -> {newMana} (Max: {player.spellcaster.max_mana})");
        }

        public override void RemoveEffect() { /* Not a temporary effect */ }
    }

    public class GainSpellpowerEffect : RelicEffect
    {
        private float spellpowerBonus = 0f;

        public override string GetEffectType() => "gain-spellpower";

        public override void ApplyEffect(params object[] args)
        {
            Debug.Log($"[GainSpellpowerEffect] ApplyEffect called for relic: {currentRelic?.Name}");
            
            if (player == null)
            {
                Debug.LogError($"[GainSpellpowerEffect] Player is null for relic: {currentRelic?.Name}");
                return;
            }
            
            if (player.spellcaster == null)
            {
                Debug.LogError($"[GainSpellpowerEffect] Player.spellcaster is null for relic: {currentRelic?.Name}");
                return;
            }

            spellpowerBonus = ParseAmountFromFormula(this.effectData.amount);
            int oldSpellPower = player.spellcaster.spellPower;
            
            // Apply the bonus to both PlayerController and SpellCaster to keep them in sync
            player.spellcaster.spellPower += (int)spellpowerBonus;
            player.spellPower = player.spellcaster.spellPower; // Sync PlayerController's spell power
            
            int newSpellPower = player.spellcaster.spellPower;
            isTemporaryEffectActive = true;
            
            Debug.Log($"[GainSpellpowerEffect] {currentRelic.Name} triggered: Gained {spellpowerBonus} spellpower. SpellPower: {oldSpellPower} -> {newSpellPower}");
            
            // If this effect has timing constraints, they will be handled by the base class
            if (hasTimingConstraint)
            {
                Debug.Log($"[GainSpellpowerEffect] Effect has timing constraint: {timingType} - will be managed by base class");
            }
        }

        public override void RemoveEffect()
        {
            if (isTemporaryEffectActive && player != null && player.spellcaster != null)
            {
                int oldSpellPower = player.spellcaster.spellPower;
                
                // Remove the bonus from both PlayerController and SpellCaster to keep them in sync
                player.spellcaster.spellPower -= (int)spellpowerBonus;
                player.spellPower = player.spellcaster.spellPower; // Sync PlayerController's spell power
                
                int newSpellPower = player.spellcaster.spellPower;
                Debug.Log($"[GainSpellpowerEffect] {currentRelic.Name}: Spellpower bonus of {spellpowerBonus} removed. SpellPower: {oldSpellPower} -> {newSpellPower}");
                spellpowerBonus = 0f;
                isTemporaryEffectActive = false;
                
                // Cleanup timing constraints handled by base class
                CleanupTimingConstraints();
            }
        }
    }

    public class GainMaxHpEffect : RelicEffect
    {
        public override string GetEffectType() => "gain-max-hp";

        public override void ApplyEffect(params object[] args)
        { 
            Debug.Log($"[GainMaxHpEffect] ApplyEffect called for relic: {currentRelic?.Name}");
            
            if (player == null)
            {
                Debug.LogError($"[GainMaxHpEffect] Player is null for relic: {currentRelic?.Name}");
                return;
            }
            
            if (player.hp == null)
            {
                Debug.LogError($"[GainMaxHpEffect] Player.hp is null for relic: {currentRelic?.Name}");
                return;
            }
            
            // Use this.effectData
            float maxHpGain = ParseAmountFromFormula(this.effectData.amount);
            int oldMaxHp = player.hp.max_hp;
            player.hp.SetMaxHP(player.hp.max_hp + (int)maxHpGain);
            int newMaxHp = player.hp.max_hp;
            
            Debug.Log($"[GainMaxHpEffect] {currentRelic.Name} triggered: Gained {maxHpGain} Max HP. MaxHP: {oldMaxHp} -> {newMaxHp}");
        }
        public override void RemoveEffect() { /* Permanent effect */ }
    }

    public class FreeSpellOnDamageEffect : RelicEffect
    {
        private float cooldown = 10f;
        private float lastActivationTime = -100f;

        public override string GetEffectType() => "free-spell-on-damage";

        // Signature updated
        public override void Initialize(Relic relic, PlayerController p, RelicJsonData.EffectData effectData)
        {
            base.Initialize(relic, p, effectData); // Pass RelicJsonData.EffectData
            // Use effectData directly
            if (!string.IsNullOrEmpty(effectData.amount) && float.TryParse(effectData.amount, out float cdValue))
            {
                cooldown = cdValue;
            }
        }

        public override void ApplyEffect(params object[] args)
        {
            if (player == null || Time.time < lastActivationTime + cooldown)
            {
                return;
            }

            if (args.Length > 0 && args[0] is Damage damageTaken)
            {
                // // player.Stats.NextSpellIsFree = true;
                // // player.Stats.FreeSpellAttackerDirection = (damageTaken.sourcePosition - player.transform.position).normalized;
                Debug.LogWarning($"[{GetEffectType()}] Effect to make next spell free needs SpellCaster/Player modification.");
                Debug.Log($"{currentRelic.Name} triggered: Next spell (slot 0) would be free towards attacker. Cooldown: {cooldown}s.");
                lastActivationTime = Time.time;
            }
        }
        public override void RemoveEffect() { /* One-time trigger, state managed by cooldown */ }
    }

    public class ReduceNextSpellCostEffect : RelicEffect
    {
        public override string GetEffectType() => "reduce-next-spell-cost";

        public override void ApplyEffect(params object[] args)
        {
            if (player == null) return;
            // Use this.effectData
            float costReduction = ParseAmountFromFormula(this.effectData.amount);
            // // PlayerStats would need a field like: player.Stats.NextSpellCostReduction += costReduction;
            Debug.LogWarning($"[{GetEffectType()}] Effect to reduce next spell cost needs SpellCaster/Player modification.");
            Debug.Log($"{currentRelic.Name} triggered: Next spell cost would be reduced by {costReduction}.");
        }
        public override void RemoveEffect() { /* Consumed on next spell cast */ }
    }

    public class HomingNextSpellEffect : RelicEffect
    {
        public override string GetEffectType() => "homing-next-spell";

        public override void ApplyEffect(params object[] args)
        {
            if (player == null) return;
            // // PlayerStats would need a flag: player.Stats.NextSpellIsHoming = true;
            // No use of effectData.amount in this specific ApplyEffect, but Initialize change is consistent
            Debug.LogWarning($"[{GetEffectType()}] Effect to make next spell homing needs SpellCaster/Player modification.");
            Debug.Log($"{currentRelic.Name} triggered: Next spell would be homing.");
        }
        public override void RemoveEffect() { /* Consumed on next spell cast */ }
    }

    // --- Additional Effect Implementations for Timing Demo ---

    public class TemporarySpeedBoostEffect : RelicEffect
    {
        private float speedBonus = 0f;
        private float originalSpeed = 0f;

        public override string GetEffectType() => "temporary-speed-boost";

        public override void ApplyEffect(params object[] args)
        {
            if (player == null) return;

            speedBonus = ParseAmountFromFormula(this.effectData.amount);
            
            // Store original speed and apply boost
            // This assumes PlayerController has a speed field - you may need to adjust based on actual implementation
            if (player.GetComponent<Rigidbody2D>() != null)
            {
                // If using Rigidbody2D for movement, this would need to be integrated differently
                Debug.Log($"[{GetEffectType()}] Speed boost of {speedBonus} applied");
                isTemporaryEffectActive = true;
            }
            else
            {
                Debug.LogWarning($"[{GetEffectType()}] No Rigidbody2D found on player - speed boost effect placeholder");
            }
        }

        public override void RemoveEffect()
        {
            if (isTemporaryEffectActive)
            {
                Debug.Log($"[{GetEffectType()}] Speed boost of {speedBonus} removed");
                speedBonus = 0f;
                isTemporaryEffectActive = false;
                CleanupTimingConstraints();
            }
        }
    }

    public class TemporaryDamageBoostEffect : RelicEffect
    {
        private float damageMultiplier = 1f;

        public override string GetEffectType() => "temporary-damage-boost";

        public override void ApplyEffect(params object[] args)
        {
            if (player == null) return;

            damageMultiplier = ParseAmountFromFormula(this.effectData.amount);
            Debug.Log($"[{GetEffectType()}] Damage multiplier of {damageMultiplier}x applied");
            isTemporaryEffectActive = true;
            
            // In a real implementation, this would modify the player's damage calculation
            // For now, this serves as a placeholder to demonstrate timing
        }

        public override void RemoveEffect()
        {
            if (isTemporaryEffectActive)
            {
                Debug.Log($"[{GetEffectType()}] Damage multiplier of {damageMultiplier}x removed");
                damageMultiplier = 1f;
                isTemporaryEffectActive = false;
                CleanupTimingConstraints();
            }
        }
    }

    public class TemporaryInvulnerabilityEffect : RelicEffect
    {
        public override string GetEffectType() => "temporary-invulnerability";

        public override void ApplyEffect(params object[] args)
        {
            if (player == null) return;

            Debug.Log($"[{GetEffectType()}] Temporary invulnerability activated");
            isTemporaryEffectActive = true;
            
            // In a real implementation, this would modify the player's damage reception
            // This could integrate with the player's HP system or add a temporary shield
        }

        public override void RemoveEffect()
        {
            if (isTemporaryEffectActive)
            {
                Debug.Log($"[{GetEffectType()}] Temporary invulnerability removed");
                isTemporaryEffectActive = false;
                CleanupTimingConstraints();
            }
        }
    }
}
