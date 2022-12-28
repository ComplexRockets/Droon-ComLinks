namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Craft.Parts;
    using Assets.Scripts.Design;
    using Assets.Scripts.DroonComLinks.Antennas;

    [Serializable]
    [DesignerPartModifier("CubeSatAntenna")]
    [PartModifierTypeId("DroonComLinks.CubeSatAntenna")]
    public class CubeSatAntennaData : PartModifierData<CubeSatAntennaScript>, IDCLAntennaData
    {
        public IAntennaType type => AntennaTypes.cubeSatSBand;
        public float size
        {
            get;
            private set;
        }
        //public float gain => 10 * Mathf.Log (4100 / (azAngle * eqAngle)) * size;
        public float gain => AntennaMath.GetGain(type, Script.antennaData.waveLength, size, Script.antennaData.efficiency);
        public int price => 0;
        public float mass => 0.02f;

        public void OnSizeChanged(float _size)
        {
             Symmetry.ExecuteOnSymmetricPartModifiers(this, true, delegate(CubeSatAntennaData d)
             {
                d.size = _size;
                 d.Script.UpdateAntenna();
            });
        }
    }
}