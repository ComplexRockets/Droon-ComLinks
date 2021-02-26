namespace Assets.Scripts.Craft.Parts.Modifiers {
    using System;
    using Assets.Scripts.Design;
    using Assets.Scripts.DroonComLinks;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Craft.Parts;
    using ModApi.Design.PartProperties;
    using ModApi;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier ("DCL Antenna")]
    [PartModifierTypeId ("DroonComLinks.DCLAntenna")]
    public class DCLAntennaData : PartModifierData<DCLAntennaScript> {
        public double minReceivablePower => AntennaMath.GetMinReceivedPower (Script.antennaData.type.sensitivityConstant, efficiency, Script.antennaData.gain);
        public float waveLength => AntennaMath.FrequencyToWaveLength (frequency);
        private IDesignerPartPropertiesModifierInterface _designerPartProperties;

        [SerializeField]
        [DesignerPropertySlider (Label = "Size", MinValue = 0, MaxValue = 5, Order = 1, Tooltip = "Change the Size of the antenna", NumberOfSteps = 1000)]
        private float _size = 0;
        public float size => _size;

        [SerializeField]
        [DesignerPropertySlider (Label = "Max power consumption", MinValue = 1, MaxValue = 100, Order = 2, Tooltip = "Changes the maximum power the antenna can draw", NumberOfSteps = 1000)]
        private float _maxPowerConsumption = 0;
        public float maxTransmittedPower => _maxPowerConsumption / Script.antennaData.type.txPowerRatio;

        [SerializeField]
        [DesignerPropertySlider (Label = "Frequency", MinValue = 1, MaxValue = 50, Order = 3, Tooltip = "Change the frequency of the antenna", NumberOfSteps = 1000)]
        private float _frenquency = 0;
        public float frequency => Mathf.Round (_frenquency * 100f) / 100f;

        [SerializeField]
        [DesignerPropertySlider (Label = "Efficiency", MinValue = 0.1f, MaxValue = 0.9f, Order = 3, Tooltip = "Change the Efficiency of the antenna", NumberOfSteps = 1000)]
        private float _efficiency = 0;
        public float efficiency => _efficiency;

        [SerializeField]
        [DesignerPropertySlider (Label = "Bit per Cycle", MinValue = 0.1f, MaxValue = 0.9f, Order = 3, Tooltip = "Set the number of bit sent each cycle (The frequency gives the number of cycles per second)", NumberOfSteps = 1000)]
        private float _bitPerCycle = 0;
        public float bitPerCycle => _efficiency;

        protected override void OnDesignerInitialization (IDesignerPartPropertiesModifierInterface d) {
            d.OnAnyPropertyChanged (() => UpdateAntenna ());
            d.OnPropertyChanged (() => _size, (newVal, oldVal) => Script.antennaData?.OnSizeChanged (newVal));

            d.OnValueLabelRequested (() => _size, (float x) => Utilities.FormatPercentage (x));
            d.OnValueLabelRequested (() => _efficiency, (float x) => Utilities.FormatPercentage (x));
            d.OnValueLabelRequested (() => _maxPowerConsumption, (float x) => _maxPowerConsumption.ToString ("n0") + " W");
            d.OnValueLabelRequested (() => _frenquency, (float x) => _frenquency.ToString ("n2") + " GHz");

            _designerPartProperties.GetSliderProperty (() => _frenquency).UpdateSliderSettings (Script.antennaData.type.minFrenquency, Script.antennaData.type.maxFrenquency, 1000);
            _designerPartProperties.GetSliderProperty (() => _maxPowerConsumption).UpdateSliderSettings (Script.antennaData.type.minPower, Script.antennaData.type.maxPower, 1000);

            _size = Script.antennaData.type.defaultSize;
            _frenquency = Script.antennaData.type.defaultFrequency;
            _efficiency = Script.antennaData.type.defaultEfficency;
            _maxPowerConsumption = Script.antennaData.type.defaultMaxPowerConsumption;

            _designerPartProperties = d;
        }

        private void UpdateAntenna () {
            if (Game.InDesignerScene) {
                //_designerPartProperties.Manager.RefreshUI ();
                Symmetry.SynchronizePartModifiers (Script.PartScript);
            }
            Script.UpdateAntenna ();
        }
    }
}