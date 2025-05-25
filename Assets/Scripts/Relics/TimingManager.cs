using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Relics
{
    /// <summary>
    /// Centralized timing management system for relic effects.
    /// Handles complex timing scenarios and provides wave-based timing support.
    /// </summary>
    public class TimingManager : MonoBehaviour
    {
        public static TimingManager Instance { get; private set; }
        
        private Dictionary<RelicEffect, TimingConstraint> activeTimings = new Dictionary<RelicEffect, TimingConstraint>();
        private List<RelicEffect> pendingRemovals = new List<RelicEffect>();
        
        // Wave management
        private int currentWave = 0;
        private List<RelicEffect> waveStartEffects = new List<RelicEffect>();
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[TimingManager] Instance created");
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Subscribe to wave events from EventBus
            if (EventBus.Instance != null)
            {
                EventBus.Instance.OnWaveStarted += OnWaveStart;
                EventBus.Instance.OnWaveEnded += OnWaveEnd;
                Debug.Log("[TimingManager] Subscribed to wave events");
            }
            else
            {
                Debug.LogWarning("[TimingManager] EventBus not found, wave timing will not work");
            }
        }
        
        private void OnWaveEnd()
        {
            Debug.Log($"[TimingManager] Wave {currentWave} ended");
        }
        
        public void RegisterTimingConstraint(RelicEffect effect, string timingType, float duration = 0f)
        {
            if (effect == null) return;
            
            var constraint = new TimingConstraint
            {
                effect = effect,
                timingType = timingType,
                duration = duration,
                startTime = Time.time
            };
            
            activeTimings[effect] = constraint;
            
            switch (timingType)
            {
                case "wave-start":
                    waveStartEffects.Add(effect);
                    break;
                    
                case "duration":
                    StartCoroutine(HandleDurationTiming(constraint));
                    break;
            }
            
            Debug.Log($"[TimingManager] Registered timing constraint: {timingType} for {effect.GetEffectType()}");
        }
        
        public void UnregisterTimingConstraint(RelicEffect effect)
        {
            if (activeTimings.ContainsKey(effect))
            {
                activeTimings.Remove(effect);
                waveStartEffects.Remove(effect);
                Debug.Log($"[TimingManager] Unregistered timing constraint for {effect.GetEffectType()}");
            }
        }
        
        private System.Collections.IEnumerator HandleDurationTiming(TimingConstraint constraint)
        {
            yield return new WaitForSeconds(constraint.duration);
            
            if (activeTimings.ContainsKey(constraint.effect) && constraint.effect.IsActive())
            {
                Debug.Log($"[TimingManager] Duration expired for {constraint.effect.GetEffectType()}");
                constraint.effect.RemoveEffect();
                UnregisterTimingConstraint(constraint.effect);
            }
        }
        
        public void OnWaveStart(int waveNumber)
        {
            currentWave = waveNumber;
            Debug.Log($"[TimingManager] Wave {waveNumber} started - triggering {waveStartEffects.Count} wave-start effects");
            
            // Trigger all wave-start effects
            foreach (var effect in waveStartEffects.ToArray()) // ToArray to avoid modification during iteration
            {
                if (effect != null && effect.IsActive())
                {
                    Debug.Log($"[TimingManager] Triggering wave-start effect: {effect.GetEffectType()}");
                    effect.ApplyEffect();
                }
            }
        }
        
        public void OnPlayerMoved(Vector3 newPosition, Vector3 oldPosition)
        {
            float distance = Vector3.Distance(newPosition, oldPosition);
            if (distance > 0.1f) // Threshold to avoid micro-movements
            {
                pendingRemovals.Clear();
                
                foreach (var kvp in activeTimings)
                {
                    if (kvp.Value.timingType == "move" && kvp.Key.IsActive())
                    {
                        Debug.Log($"[TimingManager] Player moved - removing effect: {kvp.Key.GetEffectType()}");
                        pendingRemovals.Add(kvp.Key);
                    }
                }
                
                foreach (var effect in pendingRemovals)
                {
                    effect.RemoveEffect();
                    UnregisterTimingConstraint(effect);
                }
            }
        }
        
        public void OnSpellCast()
        {
            Debug.Log($"[TimingManager] Spell cast detected - checking {activeTimings.Count} effects");
            pendingRemovals.Clear();
            
            foreach (var kvp in activeTimings)
            {
                if (kvp.Value.timingType == "next-spell" && kvp.Key.IsActive())
                {
                    Debug.Log($"[TimingManager] Spell cast - triggering next-spell timing for: {kvp.Key.GetEffectType()}");
                    if (kvp.Key is RelicEffect relicEffect)
                    {
                        relicEffect.OnSpellCast();
                    }
                }
            }
        }
        
        public int GetActiveTimingCount()
        {
            return activeTimings.Count;
        }
        
        public List<string> GetActiveTimingTypes()
        {
            var types = new List<string>();
            foreach (var constraint in activeTimings.Values)
            {
                types.Add($"{constraint.effect.GetEffectType()}: {constraint.timingType}");
            }
            return types;
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            if (EventBus.Instance != null)
            {
                EventBus.Instance.OnWaveStarted -= OnWaveStart;
                EventBus.Instance.OnWaveEnded -= OnWaveEnd;
            }
            
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
    
    [System.Serializable]
    public class TimingConstraint
    {
        public RelicEffect effect;
        public string timingType;
        public float duration;
        public float startTime;
    }
}
