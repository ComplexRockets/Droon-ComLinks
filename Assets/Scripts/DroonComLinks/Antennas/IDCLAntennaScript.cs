    using Assets.Scripts.Craft.Parts.Modifiers;

namespace Assets.Scripts.DroonComLinks.Antennas
{
    public interface IDCLAntennaScript
    {
        IDCLAntennaData data { get; }
        void Initialize(DCLAntennaData antennaData, float size);
        void UpdateAntenna();
    }
}