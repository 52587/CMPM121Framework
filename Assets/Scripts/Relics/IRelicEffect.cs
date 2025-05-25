namespace Relics
{
    public interface IRelicEffect
    {
        void Initialize(Relic relicOwner, PlayerController playerOwner, RelicJsonData.EffectData effectData); // Added Initialize
        void ApplyEffect(params object[] args);
        void Activate();
        void Deactivate();
        bool IsActive();
        void RemoveEffect(); // Added missing method
    }
}
