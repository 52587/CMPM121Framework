using UnityEngine;
using UnityEngine.UI;

public class IconManager : MonoBehaviour
{
    [SerializeField]
    protected Sprite[] sprites;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }    public void PlaceSprite(int which, Image target)
    {
        if (sprites == null)
        {
            Debug.LogError("IconManager: sprites array is null!");
            return;
        }
        
        if (which < 0 || which >= sprites.Length)
        {
            Debug.LogError($"IconManager: Invalid sprite index {which}. Array length: {sprites.Length}");
            return;
        }
        
        if (target == null)
        {
            Debug.LogError("IconManager: Target Image is null!");
            return;
        }
        
        if (sprites[which] == null)
        {
            Debug.LogError($"IconManager: Sprite at index {which} is null!");
            return;
        }
          target.sprite = sprites[which];
    }

    public Sprite Get(int index)
    {
        return sprites[index];
    }

    public int GetCount()
    {
        return sprites.Length;
    }


}
