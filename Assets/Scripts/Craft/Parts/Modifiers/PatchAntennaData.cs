namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Craft.Parts;
    using Assets.Scripts.Design;
    using Assets.Scripts.DroonComLinks.Antennas;

    [Serializable]
    [DesignerPartModifier("PatchAntenna")]
    [PartModifierTypeId("DroonComLinks.PatchAntenna")]
    public class PatchAntennaData : PartModifierData<PatchAntennaScript>, IDCLAntennaData
    {
        public IAntennaType type => AntennaTypes.patch;
        public float Size
        {
            get;
            private set;
        }
        public int price => 0;
        public float mass => 0.02f;

        public void OnSizeChanged(float _size)
        {
            Symmetry.ExecuteOnSymmetricPartModifiers(this, true, delegate (PatchAntennaData d)
            {
                d.Size = _size;
                d.Script.UpdateAntenna();
            });
        }
    }
}