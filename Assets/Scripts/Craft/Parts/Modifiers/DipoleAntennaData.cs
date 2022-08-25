namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using Assets.Scripts.Design;
    using Assets.Scripts.DroonComLinks;
    using Assets.Scripts.DroonComLinks.Interfaces;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Design.PartProperties;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("DipoleAntenna")]
    [PartModifierTypeId("DroonComlinks.DipoleAntenna")]
    public class DipoleAntennaData : PartModifierData<DipoleAntennaScript>, IDCLAntennaData
    {
        public IAntennaType type => AntennaTypes.dipole;
        public float gain => AntennaMath.GetGain(type, Script.antennaData.waveLength, size, Script.antennaData.efficiency);
        public float mass => size * 0.3f;
        public int price => 0;
        public float size
        {
            get;
            private set;
        }
        public enum DipoleAntennaTypes { Simple, Double }

        [SerializeField]
        [DesignerPropertySlider(Label = "Length", Order = 1, MinValue = 0.23f, MaxValue = 5f, Tooltip = "", NumberOfSteps = 1000)]
        private float _length = 1.13f;
        public float length => _length + 0.17f;

        [SerializeField]
        [DesignerPropertySpinner(Label = "Type", Order = 2, Tooltip = "")]
        private DipoleAntennaTypes _antennaType = DipoleAntennaTypes.Simple;
        public DipoleAntennaTypes antennaType => _antennaType;

        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            d.OnAnyPropertyChanged(delegate
            {
                Script.antennaData?.Script.UpdateAntenna();
            });

            d.OnValueLabelRequested(() => _length, (float x) => length.ToString("n2") + "m");

            d.OnSpinnerValuesRequested(() => _antennaType, delegate (List<string> x)
            {
                x.Clear();
                x.Add(DipoleAntennaTypes.Simple.ToString());
                x.Add(DipoleAntennaTypes.Double.ToString());
            });
        }

        public void OnSizeChanged(float _size)
        {
            Symmetry.ExecuteOnSymmetricPartModifiers(this, true, delegate (DipoleAntennaData d)
            {
                d.size = _size;
                d.Script.UpdateAntenna();
            });
        }
    }
}