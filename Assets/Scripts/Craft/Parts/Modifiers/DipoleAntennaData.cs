namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
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
        public float mass
        {
            get
            {
                float volume = 0.1f; // volume of core
                volume += CommonMethods.SphereVolume(thickness) * length * 2; // Volume of rod
                if (antennaType == DipoleAntennaTypes.Double) volume *= 2; // Second rod if double

                return volume * 2.7f; // Volume * aluminium volumic mass
            }
        }
        public int price => (int)(mass * size * 7); // mass * (massic price of metal (5) + price of manufacturing (2))
        public float size
        {
            get;
            private set;
        }
        public enum DipoleAntennaTypes { Simple, Double }

        [SerializeField]
        [DesignerPropertySpinner(Label = "Type", Order = 1, Tooltip = "")]
        private DipoleAntennaTypes _antennaType = DipoleAntennaTypes.Simple;
        public DipoleAntennaTypes antennaType => _antennaType;

        [SerializeField]
        [DesignerPropertySlider(Label = "Length", Order = 2, MinValue = 0.20f, MaxValue = 5f, Tooltip = "", NumberOfSteps = 1000)]
        private float _length = 0.7f;
        public float length => _length;

        [SerializeField]
        [DesignerPropertySlider(Label = "Thickness", Order = 3, MinValue = 0.01f, MaxValue = 0.15f, Tooltip = "", NumberOfSteps = 1000)] //MaxValue = 0.09f
        private float _thickness = 0.04f;
        public float thickness => _thickness;

        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            d.OnAnyPropertyChanged(delegate
            {
                Script.antennaData?.Script.UpdateAntenna();
            });

            d.OnValueLabelRequested(() => _length, (float x) => (_length * size).ToString("n2") + "m");
            d.OnValueLabelRequested(() => _thickness, (float x) => (_thickness * 100 * size).ToString("n2") + "cm");

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