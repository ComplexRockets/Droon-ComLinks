namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using Assets.Scripts.Design;
    using Assets.Scripts.DroonComLinks.Antennas;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Craft.Parts;
    using ModApi.Design.PartProperties;
    using ModApi;
    using UnityEngine;
    using Assets.Scripts.DroonComLinks;

    [Serializable]
    [DesignerPartModifier("DCL Antenna")]
    [PartModifierTypeId("DroonComLinks.DCLAntenna")]
    public class DCLAntennaData : PartModifierData<DCLAntennaScript>
    {
        public override float MassDry
        {
            get
            {
                float mass = 0;
                try { mass += Script.AntennaData.mass; } catch { }
                mass *= Size;
                return mass;
            }
        }
        public override long Price
        {
            get
            {
                long price = 1000;
                try
                {
                    price += AntennaMath.GetBasePrice(this) + Script.AntennaData.price;
                }
                catch { }
                return price;
            }
        }

        public float WaveLength => AntennaMath.FWLConversion(Frequency);
        public IDesignerPartProperties manager;

        [SerializeField]
        [DesignerPropertySlider(Label = "Size", MinValue = 0.25f, MaxValue = 5, Order = 1, Tooltip = "Change the Size of the antenna", NumberOfSteps = 1000, TechTreeIdForMaxValue = "DCL.MaxAntennaSize")]
        private float _size = 0;
        public float Size => DCLUtilities.RoundN2(_size);

        [SerializeField]
        [DesignerPropertySlider(Label = "Max Power", MinValue = 1, MaxValue = 100, Order = 2, Tooltip = "Changes the maximum power the antenna can draw", NumberOfSteps = 1000, TechTreeIdForMaxValue = "DCL.MaxAntennaPower")]
        private float _maxPower = 0;
        public float MaxPower
        {
            get => DCLUtilities.RoundN2(_maxPower);
            set => _maxPower = DCLUtilities.RoundN2(value);
        }
        public float MaxTransmittedPower
        {
            get => _maxPower * Script.AntennaData.type.txPowerRatio;
            set => _maxPower = DCLUtilities.RoundN2(value / Script.AntennaData.type.txPowerRatio);
        }

        [SerializeField]
        [DesignerPropertySlider(Label = "Frequency", MinValue = 1, MaxValue = 1, Order = 3, Tooltip = "Change the frequency of the antenna", NumberOfSteps = 1000)]
        private float _frequency = 0;
        public float Frequency
        {
            get => DCLUtilities.RoundN2(_frequency);
            set => _frequency = DCLUtilities.RoundN2(value);
        }

        [SerializeField]
        [DesignerPropertySlider(Label = "Fractional BandWidth", MinValue = 0, MaxValue = 2, Order = 3, Tooltip = "Change the band width of the antenna", NumberOfSteps = 1000)]
        private float _fractionalBandWidth = 0;
        public float FractionalBandWidth => _fractionalBandWidth;

        [SerializeField]
        [DesignerPropertySlider(Label = "Efficiency", MinValue = 0.1f, MaxValue = 0.9f, Order = 4, Tooltip = "Change the Efficiency of the antenna", NumberOfSteps = 1000, TechTreeIdForMaxValue = "DCL.MaxAntennaEfficiency")]
        private float _efficiency = 0;
        public float Efficiency => DCLUtilities.RoundN2(_efficiency);

        [SerializeField]
        [PartModifierProperty(true, false)]
        //[DesignerPropertySlider(Label = "Bit per Cycle", MinValue = 1, MaxValue = 10, Order = 5, Tooltip = "Set the number of bit sent each cycle (The frequency gives the number of cycles per second)", NumberOfSteps = 10)]
        private int _bitPerCycle = 1;
        public int BitPerCycle => _bitPerCycle;

        [SerializeField]
        [PartModifierProperty(true, false)]
        private bool _customAntenna = false;
        public bool CustomAntenna => _customAntenna;

        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            manager = d.Manager;
            d.OnAnyPropertyChanged(() => Script.UpdateAntenna());
            d.OnPropertyChanged(() => _size, (newVal, oldVal) => Script.AntennaData?.OnSizeChanged(newVal));

            d.OnValueLabelRequested(() => _size, (float x) => Utilities.FormatPercentage(x));
            d.OnValueLabelRequested(() => _efficiency, (float x) => Utilities.FormatPercentage(x));
            d.OnValueLabelRequested(() => _maxPower, (float x) => x.ToString("n2") + "W");
            d.OnValueLabelRequested(() => _frequency, (float x) => DCLUtilities.FormatFrequency(x));
            d.OnValueLabelRequested(() => _fractionalBandWidth, (float x) => Utilities.FormatPercentage(x));
        }

        private void UpdateFields()
        {
            DesignerPartProperties?.GetSliderProperty(() => _frequency)?.UpdateSliderSettings(Script.AntennaData.type.minFrequency, Script.AntennaData.type.maxFrequency, 1000);
            DesignerPartProperties?.GetSliderProperty(() => _fractionalBandWidth)?.UpdateSliderSettings(0.01f, Script.AntennaData.type.maxFractionalBandWidth, 1000);
            DesignerPartProperties?.GetSliderProperty(() => _maxPower)?.UpdateSliderSettings(0, Script.AntennaData.type.maxPower, 1000);
        }

        public void UpdateAntenna()
        {
            if (Game.InDesignerScene)
            {
                UpdateFields();
                if (Script.PartScript == null) Debug.Log("Script.PartScript null");
                else Symmetry.SynchronizePartModifiers(Script.PartScript);
                if (Script.PartScript.CraftScript == null) Debug.Log("Script.PartScript.CraftScript null");
                else Script.PartScript.CraftScript.RaiseDesignerCraftStructureChangedEvent();

                if (manager == null) Debug.Log("UpdateAntenna but manager null");
                else manager.RefreshUI();
            }
        }
    }
}