namespace Relics
{
    public interface IRelicTrigger
    {
        // Signature updated to use RelicJsonData.TriggerData
        void Initialize(Relic relic, global::PlayerController owner, RelicJsonData.TriggerData triggerData);
        void Activate(); // Subscribe to game events
        void Deactivate(); // Unsubscribe from game events
    }
}
