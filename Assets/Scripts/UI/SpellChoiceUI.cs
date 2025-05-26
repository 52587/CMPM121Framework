using UnityEngine;
using UnityEngine.UI; // Required if you use standard UI elements like Button, Image
using TMPro; // Required if you use TextMeshPro elements

public class SpellChoiceUI : MonoBehaviour
{
    public GameObject spellIcon; // Assign in Inspector: The GameObject containing the Image component for the spell's icon
    public TextMeshProUGUI spellNameText; // Assign in Inspector: TextMeshProUGUI for the spell's name
    public TextMeshProUGUI spellDescriptionText; // Assign in Inspector: TextMeshProUGUI for the spell's description
    public Button selectButton; // Assign in Inspector: The Button to select this spell

    private Spell currentSpell;
    private int choiceIndex;
    private SpellRewardManager rewardManager;

    // Call this method from SpellRewardManager to set up the UI element
    public void Setup(Spell spell, int index, SpellRewardManager manager)
    {
        currentSpell = spell;
        choiceIndex = index;
        rewardManager = manager;

        if (spellIcon != null && GameManager.Instance != null && GameManager.Instance.spellIconManager != null)
        {
            Image iconImage = spellIcon.GetComponent<Image>();
            if (iconImage != null)
            {
                GameManager.Instance.spellIconManager.PlaceSprite(spell.GetIcon(), iconImage);
            }
            else
            {
                Debug.LogWarning("SpellChoiceUI: spellIcon GameObject does not have an Image component.");
            }
        }
        else if (spellIcon == null)
        {
            Debug.LogWarning("SpellChoiceUI: spellIcon is not assigned in the Inspector.");
        }
        else
        {
            Debug.LogWarning("SpellChoiceUI: GameManager.Instance or spellIconManager is null. Cannot set spell icon.");
        }

        if (spellNameText != null)
        {
            spellNameText.text = spell.GetName(); // Assuming Spell class has GetName()
        }

        if (spellDescriptionText != null)
        {
            // Assuming Spell class has GetDescription()
            // spellDescriptionText.text = spell.GetDescription();
            // If GetDescription() doesn't exist, you might display other info or leave it blank
            spellDescriptionText.text = "Spell Description Placeholder";
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners(); // Clear previous listeners
            selectButton.onClick.AddListener(OnSelectButtonClicked);
        }
        else
        {
            Debug.LogError("SpellChoiceUI: Select Button is not assigned in the Inspector.");
        }
    }

    void OnSelectButtonClicked()
    {
        if (rewardManager != null)
        {
            rewardManager.SelectSpellReward(choiceIndex);
        }
        else
        {
            Debug.LogError("SpellChoiceUI: RewardManager reference is null.");
        }
    }

    void Start()
    {
        if (selectButton == null)
        {
            Debug.LogError("SpellChoiceUI: Select Button is not assigned in the Inspector.");
            return; // Early exit if button is not assigned
        }

        // Additional initialization if needed
    }

    public void SetSpell(Spell spell, SpellRewardManager rewardManager)
    {
        this.currentSpell = spell;
        this.rewardManager = rewardManager; // Store the reference

        if (rewardManager == null)
        {
            Debug.LogError("SpellChoiceUI: RewardManager reference is null.");
            // Potentially disable the button or show an error state
        }

        if (GameManager.Instance != null && GameManager.Instance.spellIconManager != null && spellIcon != null)
        {
            Image iconImage = spellIcon.GetComponent<Image>();
            if (iconImage != null)
            {
                GameManager.Instance.spellIconManager.PlaceSprite(spell.GetIcon(), iconImage);
            }
            else
            {
                Debug.LogWarning("SpellChoiceUI: spellIcon GameObject does not have an Image component.");
            }
        }
        else if (spellIcon == null)
        {
            Debug.LogWarning("SpellChoiceUI: spellIcon is not assigned in the Inspector.");
        }
        else
        {
            Debug.LogWarning("SpellChoiceUI: GameManager.Instance or spellIconManager is null. Cannot set spell icon.");
        }

        // Update Spell Name Text
        if (spellNameText != null)
        {
            spellNameText.text = spell.GetName();
        }

        // Update Spell Description Text
        if (spellDescriptionText != null)
        {
            spellDescriptionText.text = spell.GetDescription();
        }

        // Configure the select button
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => OnSelectButtonClicked());
        }
    }
}
