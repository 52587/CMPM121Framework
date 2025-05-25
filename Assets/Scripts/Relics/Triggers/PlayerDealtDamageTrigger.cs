using UnityEngine;
using Newtonsoft.Json.Linq;
using Relics;

public class PlayerDealtDamageTrigger : RelicTrigger
{
    public override void Initialize(Relic relic, PlayerController owner, RelicJsonData.TriggerData triggerData)
    {
        base.Initialize(relic, owner, triggerData);
    }

    public override void Activate()
    {
        EventBus.Instance.OnPlayerDealtDamage += OnPlayerDealtDamageHandler;
    }

    private void OnPlayerDealtDamageHandler(Hittable targetEnemy, Damage damageDealt)
    {
        CheckConditionAndFireEffect(targetEnemy, damageDealt);
    }

    public override void Deactivate()
    {
        EventBus.Instance.OnPlayerDealtDamage -= OnPlayerDealtDamageHandler;
    }

    protected override void CheckConditionAndFireEffect(params object[] args)
    {
        // params: targetEnemy, damageDealt
        ownerRelic.HandleTrigger(args);
    }
}
