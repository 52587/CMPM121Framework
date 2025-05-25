using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Relics;

public class RelicRewardManager : MonoBehaviour, IRewardManager
{
    [Header("UI Components")]
    public GameObject relicRewardUIPanel;
    public GameObject relicChoicePrefab;
    public Transform relicChoiceParent;

    private RelicBuilder relicBuilder;
    private List<GameObject> currentChoices = new List<GameObject>();
    private List<RelicJsonData> currentRelicData = new List<RelicJsonData>();

    void Start()
    {
        relicBuilder = new RelicBuilder();
        
        if (relicRewardUIPanel != null)
        {
            relicRewardUIPanel.SetActive(false);
        }
    }

    public void OfferRelicRewards()
    {
        // Disabled - relics are now handled by SpellRewardManager
        Debug.Log("[RelicRewardManager] Relic rewards are handled by SpellRewardManager. Skipping.");
        return;
    }

    private void CreateRelicChoice(RelicJsonData relicData)
    {
        // Disabled - handled by SpellRewardManager
        return;
    }

    private void OnRelicSelected(RelicJsonData selectedRelic)
    {
        // Disabled - handled by SpellRewardManager
        return;
    }

    private void ApplyRelicToPlayer(RelicJsonData relicData)
    {
        // Disabled - handled by SpellRewardManager
        return;
    }

    private void HideRewardPanel()
    {
        if (relicRewardUIPanel != null)
        {
            relicRewardUIPanel.SetActive(false);
        }
        
        ClearCurrentChoices();
    }

    private void ClearCurrentChoices()
    {
        foreach (GameObject choice in currentChoices)
        {
            if (choice != null)
            {
                Destroy(choice);
            }
        }
        currentChoices.Clear();
        currentRelicData.Clear();
    }

    public void SelectRelicReward(int choiceIndex)
    {
        Debug.LogWarning("[RelicRewardManager] SelectRelicReward called, but relics are now handled by SpellRewardManager.");
    }

    public void SkipRelicReward()
    {
        Debug.LogWarning("[RelicRewardManager] SkipRelicReward called, but relics are now handled by SpellRewardManager.");
    }

    public void SkipRelicSelection()
    {
        Debug.LogWarning("[RelicRewardManager] SkipRelicSelection called, but relics are now handled by SpellRewardManager.");
    }

    public void SelectSpellReward(int spellIndex)
    {
        Debug.LogWarning("RelicRewardManager: SelectSpellReward called, but this manager only handles relics.");
    }

    public void SkipSpellReward()
    {
        Debug.LogWarning("RelicRewardManager: SkipSpellReward called, but this manager only handles relics.");
    }
}