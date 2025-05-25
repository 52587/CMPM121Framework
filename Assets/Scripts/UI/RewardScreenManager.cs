using UnityEngine;

public class RewardScreenManager : MonoBehaviour
{
    public GameObject rewardUI;
    private SpellRewardManager spellRewardManager;
    private bool isRewardUIActive = false;
    
    void Start()
    {
        // Find the SpellRewardManager to coordinate with it
        spellRewardManager = Object.FindFirstObjectByType<SpellRewardManager>();
        if (spellRewardManager == null)
        {
            Debug.LogWarning("RewardScreenManager: SpellRewardManager not found in scene!");
        }
        
        // Initially hide the reward UI
        if (rewardUI != null)
        {
            rewardUI.SetActive(false);
        }
    }

    void Update()
    {
        // Only manage the reward UI if SpellRewardManager is not handling rewards
        if (GameManager.Instance.state == GameManager.GameState.WAVEEND)
        {
            // Check if SpellRewardManager is currently showing rewards
            bool spellRewardActive = IsSpellRewardManagerActive();
            
            if (!spellRewardActive && !isRewardUIActive)
            {
                // Only show our reward UI if SpellRewardManager isn't active
                if (rewardUI != null)
                {
                    rewardUI.SetActive(true);
                    isRewardUIActive = true;
                    Debug.Log("[RewardScreenManager] Showing reward UI");
                }
            }
            else if (spellRewardActive && isRewardUIActive)
            {
                // Hide our reward UI when SpellRewardManager takes over
                if (rewardUI != null)
                {
                    rewardUI.SetActive(false);
                    isRewardUIActive = false;
                    Debug.Log("[RewardScreenManager] Hiding reward UI - SpellRewardManager active");
                }
            }
        }
        else
        {
            // Hide reward UI when not in WAVEEND state
            if (isRewardUIActive)
            {
                if (rewardUI != null)
                {
                    rewardUI.SetActive(false);
                    isRewardUIActive = false;
                    Debug.Log("[RewardScreenManager] Hiding reward UI - not in WAVEEND state");
                }
            }
        }
    }
    
    private bool IsSpellRewardManagerActive()
    {
        if (spellRewardManager == null) return false;
        
        // Check if any of the SpellRewardManager's UI panels are active
        bool spellPanelActive = spellRewardManager.spellRewardUIPanel != null && 
                               spellRewardManager.spellRewardUIPanel.activeInHierarchy;
        bool relicPanelActive = spellRewardManager.relicRewardUIPanel != null && 
                               spellRewardManager.relicRewardUIPanel.activeInHierarchy;
        bool combinedPanelActive = spellRewardManager.combinedRewardUIPanel != null && 
                                  spellRewardManager.combinedRewardUIPanel.activeInHierarchy;
        
        return spellPanelActive || relicPanelActive || combinedPanelActive;
    }
}
