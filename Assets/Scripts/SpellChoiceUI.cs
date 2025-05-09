using UnityEngine;
using UnityEngine.UI; // Required if you use standard UI elements like Button, Image
using TMPro; // Required if you use TextMeshPro elements
// Add any other necessary using directives

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

        // The main logic for setting the icon is now at the beginning of this Setup method.
        // The commented-out lines below are now redundant.
        /*
        if (spellIcon != null)
        {
            // Assuming Spell class has GetIcon() that returns a Sprite
            // and you have a way to load/assign it.
            // For example, if GetIcon() returns a Sprite directly:
            // spellIcon.sprite = spell.GetIcon();
            // If GetIcon() returns a string path to a resource, you'd load it:
            // spellIcon.sprite = Resources.Load<Sprite>(spell.GetIconPath());
            // Or, if you have a SpellIconManager as hinted in your original SpellRewardManager:
            // GameManager.Instance.spellIconManager.PlaceSprite(spell.GetIcon(), spellIcon);
            // For now, let's assume you'll set this up later or it's handled elsewhere.
        }
        */

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
}
