using UnityEngine;
using Newtonsoft.Json.Linq;
using Relics;

public class WaveEndedTrigger : RelicTrigger
{
    public override void Initialize(Relic relic, PlayerController owner, RelicJsonData.TriggerData triggerData)
    {
        base.Initialize(relic, owner, triggerData);
    }

    public override void Activate()
    {
        EventBus.Instance.OnWaveEnded += OnWaveEndedHandler;
    }

    private void OnWaveEndedHandler()
    {
        CheckConditionAndFireEffect();
    }

    public override void Deactivate()
    {
        EventBus.Instance.OnWaveEnded -= OnWaveEndedHandler;
    }

    protected override void CheckConditionAndFireEffect(params object[] args)
    {
        ownerRelic.HandleTrigger();
    }
}
