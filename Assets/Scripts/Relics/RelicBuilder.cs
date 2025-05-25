using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Relics;

public class RelicBuilder
{
    private JArray allRelicsJson;
    private List<RelicJsonData> availableRelics;

    public RelicBuilder()
    {
        LoadRelics();
        ParseRelics();
    }

    private void LoadRelics()
    {
        TextAsset relicsFile = Resources.Load<TextAsset>("relics");
        if (relicsFile == null)
        {
            Debug.LogError("Failed to load relics.json from Resources folder.");
            allRelicsJson = new JArray();
            return;
        }
        allRelicsJson = JArray.Parse(relicsFile.text);
    }

    private void ParseRelics()
    {
        availableRelics = new List<RelicJsonData>();
        
        if (allRelicsJson == null) return;

        foreach (JObject relicJson in allRelicsJson)
        {
            try
            {
                RelicJsonData relicData = ParseRelicFromJson(relicJson);
                if (relicData != null)
                {
                    availableRelics.Add(relicData);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to parse relic from JSON: {e.Message}");
            }
        }

        if (availableRelics.Count == 0)
        {
            Debug.LogWarning("RelicBuilder: No relics found/parsed from JSON.");
        }
        else
        {
            Debug.Log($"RelicBuilder: Loaded {availableRelics.Count} relics.");
        }
    }

    private RelicJsonData ParseRelicFromJson(JObject relicJson)
    {
        RelicJsonData relicData = new RelicJsonData();
        
        // Parse basic properties
        relicData.name = relicJson["name"]?.Value<string>() ?? "Unknown Relic";
        relicData.icon_id = relicJson["sprite"]?.Value<int>() ?? 0;
        
        // Parse trigger data
        JObject triggerJson = relicJson["trigger"] as JObject;
        if (triggerJson != null)
        {
            relicData.trigger = new RelicJsonData.TriggerData();
            relicData.trigger.type = triggerJson["type"]?.Value<string>();
            relicData.trigger.amount = triggerJson["amount"]?.Value<string>();
            relicData.trigger.condition = triggerJson;
        }

        // Parse effect data
        JObject effectJson = relicJson["effect"] as JObject;
        if (effectJson != null)
        {
            relicData.effect = new RelicJsonData.EffectData();
            relicData.effect.type = effectJson["type"]?.Value<string>();
            relicData.effect.amount = effectJson["amount"]?.Value<string>();
            relicData.effect.until = effectJson["until"]?.Value<string>();
            relicData.effect.description = effectJson["description"]?.Value<string>();
            relicData.effect.condition = effectJson;
        }

        return relicData;
    }

    /// <summary>
    /// Get a random relic data for the reward system.
    /// </summary>
    public RelicJsonData GetRandomRelicData()
    {
        return Build(); // Delegate to the existing Build method
    }

    /// <summary>
    /// Builds a randomly selected relic using uniform distribution (same rate as spells).
    /// This is the method to call for player rewards.
    /// </summary>
    public RelicJsonData Build()
    {
        if (availableRelics == null || availableRelics.Count == 0)
        {
            Debug.LogError("RelicBuilder: No relics available to build from.");
            return null;
        }

        // Select a random relic using uniform distribution (same as spell system)
        int randomIndex = Random.Range(0, availableRelics.Count);
        RelicJsonData selectedRelic = availableRelics[randomIndex];
        
        Debug.Log($"RelicBuilder: Selected relic '{selectedRelic.name}' (index {randomIndex}/{availableRelics.Count})");
        return selectedRelic;
    }

    /// <summary>
    /// Builds a specific relic by name.
    /// </summary>
    public RelicJsonData BuildSpecificRelic(string relicName)
    {
        if (availableRelics == null) return null;

        foreach (RelicJsonData relic in availableRelics)
        {
            if (relic.name.Equals(relicName, System.StringComparison.OrdinalIgnoreCase))
            {
                return relic;
            }
        }

        Debug.LogWarning($"RelicBuilder: Relic '{relicName}' not found.");
        return null;
    }

    /// <summary>
    /// Get the count of available relics for debugging purposes.
    /// </summary>
    public int GetAvailableRelicCount()
    {
        return availableRelics?.Count ?? 0;
    }
}