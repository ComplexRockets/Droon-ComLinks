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
                try { mass += Script.antennaData.mass; } catch { }
                mass *= size;
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
                    price += AntennaMath.GetBasePrice(this) + Script.antennaData.price;
                }
                catch { }
                return price;
            }
        }

        public double minReceivablePower => AntennaMath.GetMinReceivablePower(Script.antennaData.type.sensitivityConstant, efficiency);
        public float waveLength => AntennaMath.FWLConversion(frequency);
        public IDesignerPartProperties manager => DesignerPartProperties?.Manager;

        [SerializeField]
        [DesignerPropertySlider(Label = "Size", MinValue = 0.25f, MaxValue = 5, Order = 1, Tooltip = "Change the Size of the antenna", NumberOfSteps = 1000)]
        private float _size = 0;
        public float size => DCLUtilities.RoundN2(_size);

        [SerializeField]
        [DesignerPropertySlider(Label = "Max Power", MinValue = 1, MaxValue = 100, Order = 2, Tooltip = "Changes the maximum power the antenna can draw", NumberOfSteps = 1000)]
        private float _maxPower = 0;
        public float maxPower
        {
            get => DCLUtilities.RoundN2(_maxPower);
            set => _maxPower = DCLUtilities.RoundN2(value);
        }
        public float maxTransmittedPower
        {
            get => _maxPower * Script.antennaData.type.txPowerRatio;
            set => _maxPower = DCLUtilities.RoundN2(value / Script.antennaData.type.txPowerRatio);
        }

        [SerializeField]
        [DesignerPropertySlider(Label = "Center Frequency", MinValue = 1, MaxValue = 50, Order = 3, Tooltip = "Change the frequency of the antenna", NumberOfSteps = 1000)]
        private float _frequency = 0;
        public float frequency
        {
            get => DCLUtilities.RoundN2(_frequency);
            set => _frequency = DCLUtilities.RoundN2(value);
        }

        [SerializeField]
        [DesignerPropertySlider(Label = "Fractional BandWidth", MinValue = 0, MaxValue = 2, Order = 3, Tooltip = "Change the frequency of the antenna", NumberOfSteps = 1000)]
        private float _fractionalBandWidth = 0;
        public float fractionalBandWidth => _fractionalBandWidth;

        [SerializeField]
        [DesignerPropertySlider(Label = "Efficiency", MinValue = 0.1f, MaxValue = 0.9f, Order = 4, Tooltip = "Change the Efficiency of the antenna", NumberOfSteps = 1000)]
        private float _efficiency = 0;
        public float efficiency => DCLUtilities.RoundN2(_efficiency);

        [SerializeField]
        [PartModifierProperty(true, false)]
        //[DesignerPropertySlider(Label = "Bit per Cycle", MinValue = 1, MaxValue = 10, Order = 5, Tooltip = "Set the number of bit sent each cycle (The frequency gives the number of cycles per second)", NumberOfSteps = 10)]
        private int _bitPerCycle = 1;
        public int bitPerCycle => _bitPerCycle;

        [SerializeField]
        [PartModifierProperty(true, false)]
        private bool _customAntenna = false;
        public bool customAntenna => _customAntenna;

        // [SerializeField]
        // [PartModifierProperty(true, false)]
        // private float _idlePowerConsumption = 1f;
        // public float idlePowerConsumption => _idlePowerConsumption;

        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            d.OnAnyPropertyChanged(() => Script.UpdateAntenna());
            d.OnPropertyChanged(() => _size, (newVal, oldVal) => Script.antennaData?.OnSizeChanged(newVal));

            d.OnValueLabelRequested(() => _size, (float x) => Utilities.FormatPercentage(x));
            d.OnValueLabelRequested(() => _efficiency, (float x) => Utilities.FormatPercentage(x));
            d.OnValueLabelRequested(() => _maxPower, (float x) => x.ToString("n2") + "W");
            d.OnValueLabelRequested(() => _frequency, (float x) => x.ToString("n2") + "GHz");
            d.OnValueLabelRequested(() => _fractionalBandWidth, (float x) => Utilities.FormatPercentage(x));
        }

        public void InitializeFields()
        {
            _size = Script.antennaData.type.defaultSize;
            frequency = Script.antennaData.type.defaultFrequency;
            _fractionalBandWidth = Script.antennaData.type.defaultFractionalBandWidth;
            _efficiency = Script.antennaData.type.defaultEfficiency;
            maxPower = Script.antennaData.type.defaultMaxPowerConsumption;
            Script.UpdateAntenna();
        }

        private void UpdateFields()
        {
            DesignerPartProperties?.GetSliderProperty(() => _frequency)?.UpdateSliderSettings(Script.antennaData.type.minFrenquency, Script.antennaData.type.maxFrenquency, 1000);
            DesignerPartProperties?.GetSliderProperty(() => _fractionalBandWidth)?.UpdateSliderSettings(0.01f, Script.antennaData.type.maxFractionalBandWidth, 1000);
            DesignerPartProperties?.GetSliderProperty(() => _maxPower)?.UpdateSliderSettings(0, Script.antennaData.type.maxPower, 1000);
        }

        public void UpdateAntenna()
        {
            if (Game.InDesignerScene)
            {
                UpdateFields();
                DesignerPartProperties?.Manager?.RefreshUI();
                Symmetry.SynchronizePartModifiers(Script.PartScript);
                Script.PartScript.CraftScript.RaiseDesignerCraftStructureChangedEvent();
            }
        }
    }
}