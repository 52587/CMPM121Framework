using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections;
using Relics;

public class StandStillTrigger : RelicTrigger
{
    private float duration = 3f;
    private Coroutine standStillCoroutine;
    private bool isStandingStill = false;

    public override void Activate()
    {
        EventBus.Instance.OnPlayerMoved += HandlePlayerMoved;
        EventBus.Instance.OnPlayerStopped += HandlePlayerStopped;
    }

    public override void Initialize(Relic relic, PlayerController owner, RelicJsonData.TriggerData triggerData)
    {
        base.Initialize(relic, owner, triggerData);
        duration = float.TryParse(triggerData?.amount, out var d) ? d : 3f;

        if (player != null)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null && rb.linearVelocity.sqrMagnitude < 0.01f)
                HandlePlayerStopped(player);
        }
    }

    private void HandlePlayerMoved(PlayerController pc)
    {
        if (pc != player) return;
        if (isStandingStill)
        {
            isStandingStill = false;
            RelicManager.Instance?.NotifyConditionMet("move");
        }
        if (standStillCoroutine != null)
        {
            player.StopCoroutine(standStillCoroutine);
            standStillCoroutine = null;
        }
    }

    private void HandlePlayerStopped(PlayerController pc)
    {
        if (pc != player) return;
        if (!isStandingStill && standStillCoroutine == null)
        {
            standStillCoroutine = player.StartCoroutine(StandStillTimer());
        }
    }

    private IEnumerator StandStillTimer()
    {
        yield return new WaitForSeconds(duration);
        if (player != null)
        {
            var rb2 = player.GetComponent<Rigidbody2D>();
            if (rb2 != null && rb2.linearVelocity.sqrMagnitude < 0.01f)
            {
                isStandingStill = true;
                CheckConditionAndFireEffect();
            }
        }
        standStillCoroutine = null;
    }

    public override void Deactivate()
    {
        EventBus.Instance.OnPlayerMoved -= HandlePlayerMoved;
        EventBus.Instance.OnPlayerStopped -= HandlePlayerStopped;
        if (standStillCoroutine != null)
        {
            player.StopCoroutine(standStillCoroutine);
            standStillCoroutine = null;
        }
        isStandingStill = false;
    }

    protected override void CheckConditionAndFireEffect(params object[] args)
    {
        ownerRelic.HandleTrigger();
    }
}
