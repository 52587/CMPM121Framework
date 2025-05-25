using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Relics;

public class RelicChoiceUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject relicIcon; // GameObject containing the Image component for the relic's icon
    public TextMeshProUGUI relicNameText; // TextMeshProUGUI for the relic's name
    public TextMeshProUGUI relicDescriptionText; // TextMeshProUGUI for the relic's description
    public Button selectButton; // Button to select this relic

    private RelicJsonData currentRelic;
    private int choiceIndex;
    private IRewardManager rewardManager;

    /// <summary>
    /// Sets up the RelicChoiceUI with relic data and manager reference
    /// </summary>
    /// <param name="relic">The relic data to display</param>
    /// <param name="index">The index of this choice in the reward list</param>
    /// <param name="manager">The reward manager to notify when selected</param>
    public void Setup(RelicJsonData relic, int index, IRewardManager manager)
    {
        currentRelic = relic;
        choiceIndex = index;
        rewardManager = manager;

        // Set relic icon using RelicIconManager
        if (relicIcon != null && GameManager.Instance != null && GameManager.Instance.relicIconManager != null)
        {
            Image iconImage = relicIcon.GetComponent<Image>();
            if (iconImage != null)
            {
                // Use the icon_id from the relic data
                GameManager.Instance.relicIconManager.PlaceSprite(relic.icon_id, iconImage);
            }
            else
            {
                Debug.LogWarning("RelicChoiceUI: relicIcon GameObject does not have an Image component.");
            }
        }
        else if (relicIcon == null)
        {
            Debug.LogWarning("RelicChoiceUI: relicIcon is not assigned in the Inspector.");
        }
        else
        {
            Debug.LogWarning("RelicChoiceUI: GameManager.Instance or relicIconManager is null. Cannot set relic icon.");
        }

        // Set relic name
        if (relicNameText != null)
        {
            relicNameText.text = relic.name;
        }

        // Set relic description (combine trigger and effect descriptions)
        if (relicDescriptionText != null)
        {
            string fullDescription = "";
            
            // Use the main description if available, otherwise combine trigger and effect
            if (!string.IsNullOrEmpty(relic.description))
            {
                fullDescription = relic.description;
            }
            else
            {
                // Build description from trigger type and effect description
                string triggerDesc = "";
                if (relic.trigger != null)
                {
                    // Create a basic trigger description from type
                    triggerDesc = $"Trigger: {relic.trigger.type}";
                    if (!string.IsNullOrEmpty(relic.trigger.amount))
                    {
                        triggerDesc += $" ({relic.trigger.amount})";
                    }
                }
                
                string effectDesc = relic.effect?.description ?? "";
                
                if (!string.IsNullOrEmpty(triggerDesc) && !string.IsNullOrEmpty(effectDesc))
                {
                    fullDescription = $"{triggerDesc} - {effectDesc}";
                }
                else if (!string.IsNullOrEmpty(effectDesc))
                {
                    fullDescription = effectDesc;
                }
                else if (!string.IsNullOrEmpty(triggerDesc))
                {
                    fullDescription = triggerDesc;
                }
                else
                {
                    fullDescription = "No description available";
                }
            }
            
            relicDescriptionText.text = fullDescription;
        }

        // Configure the select button
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnSelectButtonClicked);
        }
        else
        {
            Debug.LogError("RelicChoiceUI: Select Button is not assigned in the Inspector.");
        }
    }

    void OnSelectButtonClicked()
    {
        if (rewardManager != null)
        {
            rewardManager.SelectRelicReward(choiceIndex);
        }
        else
        {
            Debug.LogError("RelicChoiceUI: RewardManager reference is null.");
        }
    }

    void Start()
    {
        if (selectButton == null)
        {
            Debug.LogError("RelicChoiceUI: Select Button is not assigned in the Inspector.");
            return;
        }
    }
}