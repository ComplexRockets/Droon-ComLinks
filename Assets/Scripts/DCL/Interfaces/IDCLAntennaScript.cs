namespace Assets.Scripts.DroonComLinks.Interfaces
{
    using Assets.Scripts.Craft.Parts.Modifiers;
    public interface IDCLAntennaScript
    {
        IDCLAntennaData data { get; }
        void Initialize(DCLAntennaData antennaData, float size);
        void UpdateAntenna();
    }
}