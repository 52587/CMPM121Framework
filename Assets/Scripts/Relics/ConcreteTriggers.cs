using UnityEngine;
using Relics;

namespace Relics
{
    // Concrete trigger implementations
    
    public class TakeDamageTrigger : RelicTrigger
    {
        public override void Activate()
        {
            // Subscribe to EventBus damage events and check if it's the player
            EventBus.Instance.OnDamage += OnDamage;
            Debug.Log($"TakeDamageTrigger activated for {ownerRelic?.Name}");
        }

        public override void Deactivate()
        {
            EventBus.Instance.OnDamage -= OnDamage;
            Debug.Log($"TakeDamageTrigger deactivated for {ownerRelic?.Name}");
        }

        private void OnDamage(Vector3 where, Damage damage, Hittable target)
        {
            // Check if the damage target is our player
            if (target == player?.hp)
            {
                CheckConditionAndFireEffect(damage);
            }
        }

        protected override void CheckConditionAndFireEffect(params object[] args)
        {
            if (args.Length > 0 && args[0] is Damage damage)
            {
                Debug.Log($"TakeDamageTrigger fired for {ownerRelic?.Name}: took {damage.amount} damage");
                ownerRelic?.Effect?.ApplyEffect(damage);
            }
        }
    }

    public class OnKillTrigger : RelicTrigger
    {
        public override void Activate()
        {
            EventBus.Instance.OnEnemyKilled += OnEnemyKilled;
            Debug.Log($"OnKillTrigger activated for {ownerRelic?.Name}");
        }

        public override void Deactivate()
        {
            EventBus.Instance.OnEnemyKilled -= OnEnemyKilled;
            Debug.Log($"OnKillTrigger deactivated for {ownerRelic?.Name}");
        }

        private void OnEnemyKilled(GameObject killedEnemy, GameObject killer)
        {
            // Check if the player was the killer
            if (killer == player?.gameObject || killer == GameManager.Instance?.player)
            {
                CheckConditionAndFireEffect(killedEnemy, killer);
            }
        }

        protected override void CheckConditionAndFireEffect(params object[] args)
        {
            Debug.Log($"OnKillTrigger fired for {ownerRelic?.Name}: enemy killed");
            ownerRelic?.Effect?.ApplyEffect(args);
        }
    }

    public class PlayerDealDamageTrigger : RelicTrigger
    {
        public override void Activate()
        {
            EventBus.Instance.OnPlayerDealtDamage += OnPlayerDealtDamage;
            Debug.Log($"PlayerDealDamageTrigger activated for {ownerRelic?.Name}");
        }

        public override void Deactivate()
        {
            EventBus.Instance.OnPlayerDealtDamage -= OnPlayerDealtDamage;
            Debug.Log($"PlayerDealDamageTrigger deactivated for {ownerRelic?.Name}");
        }

        private void OnPlayerDealtDamage(Hittable target, Damage damage)
        {
            CheckConditionAndFireEffect(target, damage);
        }

        protected override void CheckConditionAndFireEffect(params object[] args)
        {
            Debug.Log($"PlayerDealDamageTrigger fired for {ownerRelic?.Name}: dealt damage");
            ownerRelic?.Effect?.ApplyEffect(args);
        }
    }

    public class StandStillTrigger : RelicTrigger
    {
        private float requiredStillTime = 3f; // Default 3 seconds
        private float stillStartTime = -1f;
        private bool isPlayerStill = false;
        private bool effectActive = false;
        private RelicTriggerUpdater triggerUpdater;

        public override void Initialize(Relic relic, PlayerController owner, RelicJsonData.TriggerData triggerData)
        {
            base.Initialize(relic, owner, triggerData);
            
            // Parse the required still time from trigger data
            if (!string.IsNullOrEmpty(triggerData.amount) && float.TryParse(triggerData.amount, out float time))
            {
                requiredStillTime = time;
            }

            // Get or create the trigger updater
            triggerUpdater = owner.GetComponent<RelicTriggerUpdater>();
            if (triggerUpdater == null)
            {
                triggerUpdater = owner.gameObject.AddComponent<RelicTriggerUpdater>();
            }
        }

        public override void Activate()
        {
            EventBus.Instance.OnPlayerMoved += OnPlayerMoved;
            EventBus.Instance.OnPlayerStopped += OnPlayerStopped;
            
            // Register for continuous updates
            if (triggerUpdater != null)
            {
                triggerUpdater.RegisterStandStillTrigger(this);
            }
            
            Debug.Log($"StandStillTrigger activated for {ownerRelic?.Name} (requires {requiredStillTime}s still)");
        }

        public override void Deactivate()
        {
            EventBus.Instance.OnPlayerMoved -= OnPlayerMoved;
            EventBus.Instance.OnPlayerStopped -= OnPlayerStopped;
            
            // Unregister from continuous updates
            if (triggerUpdater != null)
            {
                triggerUpdater.UnregisterStandStillTrigger(this);
            }
            
            // Remove effect if active
            if (effectActive && ownerRelic?.Effect is IRelicEffect relicEffect)
            {
                relicEffect.RemoveEffect();
                effectActive = false;
            }
            Debug.Log($"StandStillTrigger deactivated for {ownerRelic?.Name}");
        }

        private void OnPlayerMoved(PlayerController playerController)
        {
            if (playerController == player)
            {
                isPlayerStill = false;
                stillStartTime = -1f;
                
                // Remove effect if it was active
                if (effectActive && ownerRelic?.Effect is IRelicEffect relicEffect)
                {
                    relicEffect.RemoveEffect();
                    effectActive = false;
                    Debug.Log($"StandStillTrigger: Player moved, effect removed for {ownerRelic?.Name}");
                }
            }
        }

        private void OnPlayerStopped(PlayerController playerController)
        {
            if (playerController == player)
            {
                isPlayerStill = true;
                stillStartTime = Time.time;
                Debug.Log($"StandStillTrigger: Player stopped, starting timer for {ownerRelic?.Name}");
            }
        }

        protected override void CheckConditionAndFireEffect(params object[] args)
        {
            if (isPlayerStill && !effectActive && stillStartTime > 0 && Time.time - stillStartTime >= requiredStillTime)
            {
                Debug.Log($"StandStillTrigger fired for {ownerRelic?.Name}: stood still for {requiredStillTime}s");
                ownerRelic?.Effect?.ApplyEffect();
                effectActive = true;
            }
        }

        // This trigger needs to be checked continuously
        public void Update()
        {
            if (isPlayerStill && !effectActive)
            {
                CheckConditionAndFireEffect();
            }
        }
    }

    public class WaveStartTrigger : RelicTrigger
    {
        public override void Activate()
        {
            EventBus.Instance.OnWaveStarted += OnWaveStarted;
            Debug.Log($"WaveStartTrigger activated for {ownerRelic?.Name}");
        }

        public override void Deactivate()
        {
            EventBus.Instance.OnWaveStarted -= OnWaveStarted;
            Debug.Log($"WaveStartTrigger deactivated for {ownerRelic?.Name}");
        }

        private void OnWaveStarted(int waveNumber)
        {
            CheckConditionAndFireEffect(waveNumber);
        }

        protected override void CheckConditionAndFireEffect(params object[] args)
        {
            Debug.Log($"WaveStartTrigger fired for {ownerRelic?.Name}: new wave started");
            ownerRelic?.Effect?.ApplyEffect(args);
        }
    }

    public class SpellMissTrigger : RelicTrigger
    {
        public override void Activate()
        {
            // Note: This would need a spell miss event to be implemented in the spell system
            Debug.Log($"SpellMissTrigger activated for {ownerRelic?.Name} (requires spell miss event implementation)");
        }

        public override void Deactivate()
        {
            Debug.Log($"SpellMissTrigger deactivated for {ownerRelic?.Name}");
        }

        protected override void CheckConditionAndFireEffect(params object[] args)
        {
            Debug.Log($"SpellMissTrigger fired for {ownerRelic?.Name}: spell missed");
            ownerRelic?.Effect?.ApplyEffect(args);
        }
    }
}