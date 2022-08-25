namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System.Collections.Generic;
    using System;
    using Assets.Scripts.DroonComLinks.Interfaces;
    using Assets.Scripts.DroonComLinks.Objects.Antennas.ParabolicAntenna;
    using Assets.Scripts.DroonComLinks;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Craft.Parts;
    using ModApi.Design.PartProperties;
    using ModApi;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("Parametric Antenna")]
    [PartModifierTypeId("DroonComLinks.ParametricalParabolicAntenna")]
    public class ParametricalParabolicAntennaData : PartModifierData<ParametricalParabolicAntennaScript>, IDCLAntennaData
    {
        public IAntennaType type => AntennaTypes.parametricalParabolic;
        public float size => _size;
        private float _size = 1f;
        public float focalLength => AntennaMath.GetParabolicFocalLength(diameter, depth);
        public float gain => AntennaMath.GetParabolicGain(focalLength, diameter, Script.antennaData.waveLength);
        public int price => 0;
        public float mass => 0;
        public float diameter => _diameter;
        public float radius => diameter / 2;
        public float depth => _depth;
        public float sideDepth => _sideDepth;
        public float bottomDepth => _bottomDepth;
        public float bottomDiameter => _bottomDiameter;
        public float bottomRadius => _bottomDiameter / 2;
        public float bottomOffset => _bottomOffset;
        public float bottom => -_bottomDepth - _bottomOffset;
        public float supportArmPos => _supportArmPos;
        public float doubleSupportArmOffset => _doubleSupportArmOffset;
        public float supportArmDiameter => _supportArmDiameter;
        public float crossStructureBending => _crossStructureBending;
        private float _aperture => Mathf.PI * diameter * diameter / 4;
        public int sideStructureCount => _sideStructureCount;
        public int supportArmCount => _supportArmCount;
        public string supportArmShape => _supportArmShape;
        public string upperElement => _upperElement;
        public string lowerElement => _lowerElement;
        public bool secondaryReflector => _secondaryReflector;
        public bool square => _supportArmShape == "Square";
        public bool straightSide => _straightSide;
        public bool bottomRidge => _bottomRidge;
        public bool topRidge => _topRidge;
        public bool crossStructure => _crossStructure;

        [SerializeField]
        [DesignerPropertySlider(Label = "Diameter", Order = 1, MinValue = 0.1f, MaxValue = 5f, Tooltip = "The diameter of the antenna. What did you expect?", NumberOfSteps = 1000)]
        private float _diameter = AntennaTypes.Defaults.parabolicDiameter;

        [SerializeField]
        [DesignerPropertySlider(Label = "Depth", Order = 2, MinValue = 0.1f, MaxValue = 1f, Tooltip = "", NumberOfSteps = 1000)]
        private float _depth = AntennaTypes.Defaults.parabolicDepth;

        [SerializeField]
        [DesignerPropertySlider(Label = "Side Depth", Order = 3, MinValue = 0f, MaxValue = 0.5f, Tooltip = "", NumberOfSteps = 1000)]
        private float _sideDepth = 0.05f;

        [SerializeField]
        [DesignerPropertySlider(Label = "Bottom Depth", Order = 4, MinValue = 0f, MaxValue = 0.5f, Tooltip = "", NumberOfSteps = 1000)]
        private float _bottomDepth = 0f;

        [SerializeField]
        [DesignerPropertySlider(Label = "Bottom Diameter", Order = 5, MinValue = 0.1f, MaxValue = 20f, Tooltip = "", NumberOfSteps = 1000)]
        private float _bottomDiameter = 1f;

        [SerializeField]
        [DesignerPropertySlider(Label = "Bottom Offset", Order = 6, MinValue = 0f, MaxValue = 5f, Tooltip = "", NumberOfSteps = 1000)]
        private float _bottomOffset = 0f;

        [SerializeField]
        [DesignerPropertySlider(Label = "Support Arm count", Order = 7, MinValue = 1, MaxValue = 6, Tooltip = "", NumberOfSteps = 6)]
        private int _supportArmCount = 3;

        [SerializeField]
        [DesignerPropertySlider(Label = "Side Structure count", Order = 8, MinValue = 0, MaxValue = 20, Tooltip = "", NumberOfSteps = 20)]
        private int _sideStructureCount = 6;

        [SerializeField]
        [DesignerPropertySlider(Label = "Support Arm Position", Order = 9, MinValue = 0f, MaxValue = 1f, Tooltip = "", NumberOfSteps = 1000)]
        private float _supportArmPos = 0.8f;

        [SerializeField]
        [DesignerPropertySlider(Label = "Double Support Arm Offset", Order = 10, MinValue = 0f, MaxValue = 1f, Tooltip = "", NumberOfSteps = 1000)]
        private float _doubleSupportArmOffset = 0f;

        [SerializeField]
        [DesignerPropertySlider(Label = "Support Arm Diameter", Order = 11, MinValue = 0.01f, MaxValue = 0.2f, Tooltip = "", NumberOfSteps = 1000)]
        private float _supportArmDiameter = 0.05f;

        [SerializeField]
        [DesignerPropertySpinner("Round", "Square", Label = "Support Arm", Order = 12, Tooltip = "")]
        private string _supportArmShape = "Round";

        [SerializeField]
        [DesignerPropertySpinner(Label = "Upper Element", Order = 13, Tooltip = "")]
        private string _upperElement = "Basic";

        [SerializeField]
        [DesignerPropertySpinner(Label = "Lower Element", Order = 14, Tooltip = "")]
        private string _lowerElement = "Basic";

        [SerializeField]
        [DesignerPropertyToggleButton(Label = "Secondary Reflector", Order = 15, Tooltip = "")]
        private bool _secondaryReflector = true;

        [SerializeField]
        [DesignerPropertyToggleButton(Label = "Straight Side", Order = 16, Tooltip = "")]
        private bool _straightSide = false;

        [SerializeField]
        [DesignerPropertyToggleButton(Label = "Top Ridge", Order = 17, Tooltip = "")]
        private bool _topRidge = true;

        [SerializeField]
        [DesignerPropertyToggleButton(Label = "Bottom Ridge", Order = 18, Tooltip = "")]
        private bool _bottomRidge = true;

        [SerializeField]
        [DesignerPropertyToggleButton(Label = "Cross Structure", Order = 19, Tooltip = "")]
        private bool _crossStructure = false;

        [SerializeField]
        [DesignerPropertySlider(Label = "Cross Structure Bending", Order = 20, MinValue = 0.9f, MaxValue = 1.1f, Tooltip = "", NumberOfSteps = 200)]
        private float _crossStructureBending = 1f;

        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            d.OnAnyPropertyChanged(() => Script.CreateAntenna());

            d.OnPropertyChanged(() => _secondaryReflector, (newVal, oldVal) =>
            {
                _upperElement = "Basic";
                d.GetSpinnerProperty(() => _upperElement).UpdateValues();
            });
            d.OnPropertyChanged(() => _depth, (newVal, oldVal) =>
            {
                if (sideDepth > depth) _sideDepth = depth;
                if (diameter < 4 * depth) _diameter = 4 * depth;
                d.GetSliderProperty(() => _sideDepth).UpdateSliderSettings(0f, depth, 1000);
                d.GetSliderProperty(() => _diameter).UpdateSliderSettings(4 * depth, 5f, 1000);
            });
            d.OnPropertyChanged(() => _diameter, (newVal, oldVal) =>
            {
                if (depth > diameter / 4) _depth = diameter / 4;
                d.GetSliderProperty(() => _depth).UpdateSliderSettings(0.1f, diameter / 4, 1000);
            });

            d.OnSpinnerValuesRequested(() => _upperElement, delegate (List<string> x)
            {
                x.Clear();
                x.AddRange(_secondaryReflector ? UpperElement.Reflectors : UpperElement.Receivers);
            });
            d.OnSpinnerValuesRequested(() => _lowerElement, delegate (List<string> x)
            {
                x.Clear();
                x.AddRange(LowerElement.Receivers);
            });

            d.OnValueLabelRequested(() => _diameter, (float x) => diameter.ToString("n1") + "m");
            d.OnValueLabelRequested(() => _depth, (float x) => depth.ToString("n1") + "m");
            d.OnValueLabelRequested(() => _sideDepth, (float x) => sideDepth.ToString("n1") + "m");
            d.OnValueLabelRequested(() => _supportArmPos, (float x) => Utilities.FormatPercentage(x));
            d.OnValueLabelRequested(() => _doubleSupportArmOffset, (float x) => Utilities.FormatPercentage(x));
            d.OnValueLabelRequested(() => _supportArmDiameter, (float x) => supportArmDiameter.ToString("n1") + "");

            d.OnVisibilityRequested(() => _lowerElement, (bool x) => _secondaryReflector);
        }

        public void OnSizeChanged(float size)
        {
            _size = size;
            Script.CreateAntenna();
        }
    }
}