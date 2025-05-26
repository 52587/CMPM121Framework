// Assets/Scripts/Data/CharacterClassData.cs
using Newtonsoft.Json;
using System.Collections.Generic;

// Ensure this namespace matches your project structure if you use namespaces
// namespace YourProject.Data 
// {

[System.Serializable]
public class CharacterClassData
{
    public int sprite; // Index for PlayerSpriteManager
    public string health; // RPN string
    public string mana; // RPN string
    public string mana_regeneration; // RPN string
    public string spellpower; // RPN string
    public string speed; // RPN string
}

// } // End namespace if used
