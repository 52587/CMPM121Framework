using System.Collections.Generic;
using Newtonsoft.Json;

[System.Serializable]
public class SpawnData
{
    public string enemy; // Type of enemy
    public string count = "1"; // RPN string, default to 1 if missing
    public string hp; // RPN string (uses enemy base HP if missing)
    public string damage; // RPN string (uses enemy base damage if missing)
    public string speed; // RPN string (uses enemy base speed if missing)
    public float delay = 2.0f; // Delay before this group spawns
    public string location = "random"; // Spawn point name or "random"
    public List<int> sequence = new List<int>(); // Sequence of spawn points (currently unused, consider for specific patterns)
    public string sequence_type = "sequential"; // Type of spawning: "sequential" or "simultaneous"
    public string rpn_variables; // JSON string of additional RPN variables if needed
}

[System.Serializable]
public class LevelData
{
    public string name;
    public int waves = -1; // -1 indicates endless
    public List<SpawnData> spawns;
}
