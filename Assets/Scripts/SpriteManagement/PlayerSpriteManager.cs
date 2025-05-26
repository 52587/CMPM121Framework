using UnityEngine;
using UnityEngine.UI;

public class PlayerSpriteManager : IconManager
{
    public Sprite GetSpriteAtIndex(int index)
    {
        if (sprites != null && index >= 0 && index < sprites.Length)
        {
            return sprites[index];
        }
        Debug.LogWarning($"[PlayerSpriteManager] Invalid sprite index {index} or sprites array not initialized.");
        return null;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager.Instance.playerSpriteManager = this;
    }

}
