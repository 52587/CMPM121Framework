using System.Collections.Generic;

namespace Relics
{
    [System.Serializable]
    public class RelicTriggerJsonData
    {
        public string description;
        public string type;
        public string amount; // Can be null or string
    }

    [System.Serializable]
    public class RelicEffectJsonData
    {
        public string description;
        public string type;
        public string amount;
        public string until; // Can be null or string
    }
}
