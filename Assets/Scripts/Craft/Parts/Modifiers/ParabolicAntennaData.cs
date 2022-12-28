namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System.Collections.Generic;
    using System;
    using Assets.Scripts.DroonComLinks.Antennas.ParabolicAntenna;
    using Assets.Scripts.DroonComLinks.Antennas;
    using Assets.Scripts.DroonComLinks;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Craft.Parts;
    using ModApi.Design.PartProperties;
    using ModApi;
    using UnityEngine;
    using Assets.Scripts.Design;

    [Serializable]
    [DesignerPartModifier("ParabolicAntenna")]
    [PartModifierTypeId("DroonComLinks.ParabolicAntenna")]

    public class ParabolicAntennaData : PartModifierData<ParabolicAntennaScript>, IDCLAntennaData
    {
        public string antennaType => "Parabolic Antenna";
        public IAntennaType type => AntennaTypes.parametricalParabolic;
        public float size { get; private set; } = 1f;
        public float focalLength => AntennaMath.GetParabolicFocalLength(diameter, depth);
        public float gain => AntennaMath.GetParabolicGain(focalLength, diameter, Script.antennaData.waveLength);
        public float mass
        {
            get
            {
                float bottomDiameterSurface = DCLUtilities.CircleSurface(bottomDiameter);
                float volume = (DCLUtilities.SphereVolume(radius + thickness) - DCLUtilities.SphereVolume(radius)) * depth; // Parabola volume
                volume += (bottomDiameterSurface - DCLUtilities.CircleSurface(bottomDiameter - thickness)) * bottomOffset; // Bottom offset volume
                volume += (bottomDiameterSurface - DCLUtilities.CircleSurface(bottomDiameter - thickness)) * bottomDepth + 2 * bottomDiameterSurface; // Bottom Depth volume
                volume += DCLUtilities.CircleSurface(supportArmDiameter / 2) * DCLUtilities.Pythagore(focalLength, radius) * supportArmCount * doubleSupportArm * 2; // SupportArms volume
                return volume * 2.7f / size; // Volume * aluminium volumic mass
            }
        }
        public int price
        {
            get
            {
                float p = mass * size * 7; //mass * (massic price of metal (5) + price of manufacturing (2))
                p += Script.upperElement.elementData.price * diameter; //Upper Element Price
                if (secondaryReflector) p += Script.lowerElement.elementData.price * diameter; //Lower Element Price
                return (int)(p);
            }
        }
        private float _aperture => Mathf.PI * diameter * diameter / 4;
        private int doubleSupportArm => doubleSupportArmOffset > 0 ? 2 : 1;

        [SerializeField]
        [DesignerPropertySlider(Label = "Diameter", Order = 1, MinValue = 0.2f, MaxValue = 5f, Tooltip = "", NumberOfSteps = 1000)]
        private float _diameter = AntennaTypes.Defaults.parabolicDiameter;
        public float diameter => DCLUtilities.RoundN2(_diameter) * size;
        public float radius => diameter / 2;

        [SerializeField]
        [DesignerPropertySlider(Label = "Depth", Order = 2, MinValue = 0.1f, MaxValue = 1f, Tooltip = "", NumberOfSteps = 1000)]
        private float _depth = AntennaTypes.Defaults.parabolicDepth;
        public float depth => DCLUtilities.RoundN2(_depth) * size;

        [SerializeField]
        [DesignerPropertySlider(Label = "Thickness", Order = 3, MinValue = 0.01f, MaxValue = 0.5f, Tooltip = "", NumberOfSteps = 1000)]
        private float _thickness = 0.05f;
        public float thickness => DCLUtilities.RoundN2(_thickness) * size;

        [SerializeField]
        [DesignerPropertySlider(Label = "Support Arm count", Order = 7, MinValue = 1, MaxValue = 8, Tooltip = "", NumberOfSteps = 8)]
        private int _supportArmCount = 3;
        public int supportArmCount => _supportArmCount;

        [SerializeField]
        [DesignerPropertySlider(Label = "Support Arm Position", Order = 9, MinValue = 0.1f, MaxValue = 1f, Tooltip = "", NumberOfSteps = 1000)]
        private float _supportArmPos = 0.75f;
        public float supportArmPos => Mathf.Min(_supportArmPos, 1 - supportArmRadius / radius);

        [SerializeField]
        [DesignerPropertySlider(Label = "Double Support Arm Offset", Order = 10, MinValue = 0f, MaxValue = 1f, Tooltip = "", NumberOfSteps = 1000)]
        private float _doubleSupportArmOffset = 0f;
        public float doubleSupportArmOffset => DCLUtilities.RoundN2(_doubleSupportArmOffset);

        [SerializeField]
        [DesignerPropertySlider(Label = "Support Arm Diameter", Order = 11, MinValue = 0.01f, MaxValue = 0.1f, Tooltip = "", NumberOfSteps = 1000)]
        private float _supportArmDiameter = 0.04f;
        public float supportArmDiameter => DCLUtilities.Round(_supportArmDiameter, 4) * size;
        public float supportArmRadius => supportArmDiameter / 2;

        [SerializeField]
        [DesignerPropertySlider(Label = "Ridge Length", Order = 11, MinValue = 0f, MaxValue = 0.1f, Tooltip = "", NumberOfSteps = 1000)]
        private float _ridgeWidth = 0.035f;
        public float ridgeWidth => DCLUtilities.Round(_ridgeWidth, 4) * size;

        [SerializeField]
        [DesignerPropertySlider(Label = "Ridge A", Order = 11, MinValue = 0.01f, MaxValue = 0.05f, Tooltip = "", NumberOfSteps = 1000)]
        private float _ridgeA = 0.01f;
        public float ridgeA => DCLUtilities.Round(_ridgeA, 4);

        [SerializeField]
        [DesignerPropertySlider(Label = "Ridge B", Order = 11, MinValue = -0.02f, MaxValue = 0.02f, Tooltip = "", NumberOfSteps = 1000)]
        private float _ridgeB = 0.002f;
        public float ridgeB => DCLUtilities.Round(_ridgeB, 4);

        [SerializeField]
        [DesignerPropertyToggleButton(Label = "Straight Side", Order = 15, Tooltip = "")]
        private bool _straightSide = false;
        public bool straightSide => _straightSide;

        [SerializeField]
        [DesignerPropertySlider(Label = "Bottom Depth", Order = 11, MinValue = 0f, MaxValue = 0.75f, Tooltip = "", NumberOfSteps = 1000)]
        private float _bottomDepth = 0f;
        public float bottomDepth => DCLUtilities.RoundN2(_bottomDepth) * size;

        [SerializeField]
        [DesignerPropertySlider(Label = "Bottom Offset", Order = 11, MinValue = 0, MaxValue = 0.5f, Tooltip = "", NumberOfSteps = 1000)]
        private float _bottomOffset = 0f;
        public float bottomOffset => DCLUtilities.RoundN2(_bottomOffset) * size;

        [SerializeField]
        [DesignerPropertySlider(Label = "Bottom Diameter", Order = 11, MinValue = 0f, MaxValue = 4f, Tooltip = "", NumberOfSteps = 1000)]
        private float _bottomDiameter = 0f;
        public float bottomDiameter => DCLUtilities.RoundN2(_bottomDiameter) * size;
        public float bottomRadius => bottomDiameter / 2;

        [SerializeField]
        [DesignerPropertySpinner(Label = "Upper Element", Order = 13, Tooltip = "")]
        private string _upperElement = "Basic";
        public string upperElement => _upperElement;

        [SerializeField]
        [DesignerPropertySpinner(Label = "Lower Element", Order = 14, Tooltip = "")]
        private string _lowerElement = "Basic";
        public string lowerElement => _lowerElement;

        [SerializeField]
        [DesignerPropertyToggleButton(Label = "Secondary Reflector", Order = 15, Tooltip = "")]
        private bool _secondaryReflector = true;
        public bool secondaryReflector => _secondaryReflector;

        [SerializeField]
        [PartModifierProperty(true, false)]
        private int _parabolaResolution = 64;
        public int parabolaResolution => _parabolaResolution;

        [SerializeField]
        [PartModifierProperty(true, false)]
        private int _parabolaDivCount = 16;
        public int parabolaDivCount => _parabolaDivCount;

        [SerializeField]
        [PartModifierProperty(true, false)]
        private int _ridgeDivCount = 8;
        public int ridgeDivCount => _ridgeDivCount;

        [SerializeField]
        [PartModifierProperty(true, false)]
        private int _structureResolution = 8;
        public int structureResolution => _structureResolution;

        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            d.OnAnyPropertyChanged(() =>
            {
                DCLUtilities.SetDesignerSliderMinMax(d.GetSliderProperty(() => _depth), ref _depth, _diameter / 50, _diameter / 4);
                DCLUtilities.SetDesignerSliderMinMax(d.GetSliderProperty(() => _bottomDiameter), ref _bottomDiameter, (_straightSide || bottomOffset > 0) ? Mathf.Max(_diameter / 2 - 10 * _bottomDepth, 0) : 0, _diameter);
                DCLUtilities.SetDesignerSliderMinMax(d.GetSliderProperty(() => _bottomDepth), ref _bottomDepth, _thickness, 0.75f);

                Script.antennaData?.Script.UpdateAntenna();
            });

            d.OnPropertyChanged(() => _secondaryReflector, (newVal, oldVal) =>
            {
                _upperElement = "Basic";
                d.GetSpinnerProperty(() => _upperElement).UpdateValues();
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

            d.OnValueLabelRequested(() => _diameter, (float x) => diameter.ToString("n2") + "m");
            d.OnValueLabelRequested(() => _depth, (float x) => depth.ToString("n2") + "m");
            d.OnValueLabelRequested(() => _thickness, (float x) => (thickness * 100).ToString("n2") + "cm");
            d.OnValueLabelRequested(() => _supportArmPos, (float x) => Utilities.FormatPercentage(x));
            d.OnValueLabelRequested(() => _doubleSupportArmOffset, (float x) => Utilities.FormatPercentage(x));
            d.OnValueLabelRequested(() => _supportArmDiameter, (float x) => (supportArmDiameter * 100).ToString("n2") + "cm");
            d.OnValueLabelRequested(() => _ridgeWidth, (float x) => x.ToString("n2"));
            d.OnValueLabelRequested(() => _ridgeA, (float x) => (x * 100).ToString("n2"));
            d.OnValueLabelRequested(() => _ridgeB, (float x) => (x * 100).ToString("n2"));
            d.OnValueLabelRequested(() => _bottomDiameter, (float x) => bottomDiameter.ToString("n2") + "m");
            d.OnValueLabelRequested(() => _bottomDepth, (float x) => bottomDepth.ToString("n2") + "m");
            d.OnValueLabelRequested(() => _bottomOffset, (float x) => bottomOffset.ToString("n2") + "m");

            d.OnVisibilityRequested(() => _lowerElement, (bool x) => _secondaryReflector);
            d.OnVisibilityRequested(() => _bottomDepth, (bool x) => _straightSide);
            d.OnVisibilityRequested(() => _bottomDiameter, (bool x) => _straightSide || bottomOffset > 0);
        }

        public void OnSizeChanged(float _size)
        {
            Symmetry.ExecuteOnSymmetricPartModifiers(this, true, delegate (ParabolicAntennaData d)
            {
                if (d._upperElement == "Cassinni") d._upperElement = "Saturn";
                if (d._lowerElement == "Cassinni") d._lowerElement = "Saturn";

                d.size = _size;
                d.DesignerPartProperties?.Manager?.RefreshUI();
                d.Script.UpdateAntenna();
            });
        }
    }
}