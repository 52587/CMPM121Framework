# Relic Timing System Documentation

## Overview

The enhanced relic timing system provides comprehensive support for duration-based effects, temporary effects with various "until" conditions, and improved timing infrastructure for relic effects. This system allows relics to have sophisticated timing behaviors that can be triggered and expired based on different game events.

## Timing Types

### 1. Duration-Based Timing (`"duration X"`)
Effects that last for a specific number of seconds.

**Format:** `"duration 5"` (lasts for 5 seconds)

**Example:**
```json
{
    "effect": {
        "type": "gain-spellpower",
        "amount": "25",
        "until": "duration 15"
    }
}
```

**Implementation:** Uses Unity coroutines to automatically remove the effect after the specified duration.

### 2. Next-Spell Timing (`"next-spell"`)
Effects that last until the player casts the next spell and deals damage.

**Format:** `"next-spell"`

**Example:**
```json
{
    "effect": {
        "type": "gain-spellpower",
        "amount": "100",
        "until": "next-spell"
    }
}
```

**Implementation:** 
- Effect remains active until `OnSpellCast()` is called
- Subscribes to `EventBus.OnPlayerDealtDamage` to detect when spell damage is dealt
- Removes effect after first damage is dealt following spell cast

### 3. Movement-Based Timing (`"move"`)
Effects that last until the player moves a significant distance.

**Format:** `"move"`

**Example:**
```json
{
    "effect": {
        "type": "gain-spellpower",
        "amount": "10 5 wave * +",
        "until": "move"
    }
}
```

**Implementation:**
- Monitors player position every 0.1 seconds
- Removes effect when player moves more than 0.1 units from original position
- Uses coroutine for continuous position checking

### 4. Wave-Start Timing (`"wave-start"`)
Effects that trigger at the beginning of new waves.

**Format:** `"wave-start"`

**Example:**
```json
{
    "trigger": {
        "type": "wave-start"
    },
    "effect": {
        "type": "temporary-invulnerability",
        "amount": "1",
        "until": "duration 3"
    }
}
```

**Implementation:** Integrates with the wave system to trigger effects when new waves begin.

## Architecture

### Base RelicEffect Class

The `RelicEffect` base class provides timing infrastructure for all effects:

```csharp
public abstract class RelicEffect : MonoBehaviour, IRelicEffect
{
    // Timing system properties
    protected bool hasTimingConstraint = false;
    protected string timingType = null;
    protected float timingDuration = 0f;
    
    // Timing constraint parsing and management
    protected virtual void ParseTimingConstraints();
    protected virtual void StartTimingConstraint();
    protected virtual void CleanupTimingConstraints();
    
    // Spell cast notification for next-spell timing
    public virtual void OnSpellCast();
}
```

### Key Methods

#### `ParseTimingConstraints()`
- Automatically parses the `until` field from effect data
- Sets up timing type and duration
- Called during effect initialization

#### `StartTimingConstraint()`
- Initiates the appropriate timing mechanism
- Called when effect is activated
- Starts coroutines for duration/movement checking

#### `CleanupTimingConstraints()`
- Stops all timing-related coroutines
- Unsubscribes from events
- Called when effect is deactivated or destroyed

#### `OnSpellCast()`
- Handles next-spell timing logic
- Subscribes to damage events
- Called by RelicManager when spells are cast

## Usage Examples

### Creating a Duration-Based Effect

```csharp
public class TemporaryShieldEffect : RelicEffect
{
    private bool shieldActive = false;
    
    public override string GetEffectType() => "temporary-shield";
    
    public override void ApplyEffect(params object[] args)
    {
        shieldActive = true;
        isTemporaryEffectActive = true;
        // Shield logic here
    }
    
    public override void RemoveEffect()
    {
        if (isTemporaryEffectActive)
        {
            shieldActive = false;
            isTemporaryEffectActive = false;
            CleanupTimingConstraints();
        }
    }
}
```

### JSON Configuration

```json
{
    "name": "Temporal Shield",
    "trigger": {
        "type": "take-damage"
    },
    "effect": {
        "type": "temporary-shield",
        "amount": "1",
        "until": "duration 10"
    }
}
```

## Integration with RelicManager

The `RelicManager` has been enhanced to support the timing system:

```csharp
// Notify all RelicEffects about spell casts
if (conditionType == "cast-spell")
{
    if (relic.Effect is RelicEffect relicEffect)
    {
        relicEffect.OnSpellCast();
    }
}
```

## TimingManager (Optional)

A centralized `TimingManager` is available for complex timing scenarios:

```csharp
public class TimingManager : MonoBehaviour
{
    public void RegisterTimingConstraint(RelicEffect effect, string timingType, float duration);
    public void UnregisterTimingConstraint(RelicEffect effect);
    public void OnWaveStart(int waveNumber);
    public void OnPlayerMoved(Vector3 newPosition, Vector3 oldPosition);
}
```

## Testing

The `RelicTimingTest` script provides comprehensive testing for all timing mechanisms:

```csharp
// Run all timing tests
RelicTimingTest test = FindObjectOfType<RelicTimingTest>();
test.StartTimingTests();
```

## Performance Considerations

1. **Movement Checking:** Uses 0.1-second intervals to balance responsiveness and performance
2. **Coroutine Management:** Properly stops and cleans up coroutines to prevent memory leaks
3. **Event Subscription:** Automatically unsubscribes from events to prevent null references

## Error Handling

- Invalid timing formats are logged as warnings
- Missing components are handled gracefully
- Null checks prevent crashes during timing operations

## Future Extensions

The timing system is designed to be extensible:

1. **Custom Timing Types:** Add new timing conditions by extending `ParseTimingConstraints()`
2. **Complex Conditions:** Combine multiple timing conditions
3. **Event-Based Timing:** Add timing based on custom game events
4. **Conditional Timing:** Add timing that depends on game state

## Debugging

Enable debug logging to trace timing behavior:

```csharp
Debug.Log($"[{GetEffectType()}] Parsed timing: {timingType}");
Debug.Log($"[{GetEffectType()}] Duration {timingDuration}s expired - removing effect");
```

The timing system provides comprehensive logging for troubleshooting timing issues.
