using UnityEngine;
using System.Collections.Generic;
using Relics;

public class RelicTriggerUpdater : MonoBehaviour
{
    private List<Relics.StandStillTrigger> standStillTriggers = new List<Relics.StandStillTrigger>();

    public void RegisterStandStillTrigger(Relics.StandStillTrigger trigger)
    {
        if (!standStillTriggers.Contains(trigger))
        {
            standStillTriggers.Add(trigger);
            Debug.Log("Registered StandStillTrigger for continuous updates");
        }
    }

    public void UnregisterStandStillTrigger(Relics.StandStillTrigger trigger)
    {
        if (standStillTriggers.Contains(trigger))
        {
            standStillTriggers.Remove(trigger);
            Debug.Log("Unregistered StandStillTrigger from continuous updates");
        }
    }

    void Update()
    {
        // Update all StandStillTriggers
        for (int i = standStillTriggers.Count - 1; i >= 0; i--)
        {
            if (standStillTriggers[i] != null)
            {
                standStillTriggers[i].Update();
            }
            else
            {
                // Clean up null references
                standStillTriggers.RemoveAt(i);
            }
        }
    }
}