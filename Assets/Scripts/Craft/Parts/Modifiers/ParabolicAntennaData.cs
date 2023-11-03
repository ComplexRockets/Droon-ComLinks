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
        public string AntennaType => "Parabolic Antenna";
        public IAntennaType type => AntennaTypes.parabolic;
        public float Size { get; private set; } = 1f;
        public float FocalLength => AntennaMath.GetParabolicFocalLength(Diameter, Depth);
        public float mass
        {
            get
            {
                float volume = (DCLUtilities.SphereVolume(Radius + Thickness) - DCLUtilities.SphereVolume(Radius)) * Depth; // Parabola volume
                volume += (DCLUtilities.CircleSurface(BottomDiameter) - DCLUtilities.CircleSurface(BottomDiameter - 2 * Thickness)) * (BottomOffset + BottomDepth); // Bottom offset and Depth volume
                volume += DCLUtilities.CircleSurface(SupportArmDiameter / 2) * DCLUtilities.Pythagore(FocalLength, Radius) * SupportArmCount * DoubleSupportArm * 2; // SupportArms volume
                return volume * 2.7f / Size; // Volume * aluminium volumic mass
            }
        }
        public int price
        {
            get
            {
                float p = mass * Size * 7; //mass * (massic price of metal (5) + price of manufacturing (2))
                p += Script.upperElement.elementData.price * Diameter; //Upper Element Price
                if (SecondaryReflector) p += Script.lowerElement.elementData.price * Diameter; //Lower Element Price
                return (int)p;
            }
        }
        private float _aperture => Mathf.PI * Diameter * Diameter / 4;
        private int DoubleSupportArm => DoubleSupportArmOffset > 0 ? 2 : 1;

        [SerializeField]
        [DesignerPropertySlider(Label = "Diameter", Order = 1, MinValue = 0.2f, MaxValue = 5f, Tooltip = "", NumberOfSteps = 1000)]
        private  float _diameter = AntennaTypes.Defaults.parabolicDiameter;
        public float Diameter => DCLUtilities.RoundN2(_diameter) * Size;
        public float Radius => Diameter / 2;

        [SerializeField]
        [DesignerPropertySlider(Label = "Depth", Order = 2, MinValue = 0.1f, MaxValue = 1f, Tooltip = "", NumberOfSteps = 1000)]
        private float _depth = 0.25f;
        public float Depth => DCLUtilities.RoundN2(_depth) * Size;

        [SerializeField]
        [DesignerPropertySlider(Label = "Thickness", Order = 3, MinValue = 0.01f, MaxValue = 0.5f, Tooltip = "", NumberOfSteps = 1000)]
        private  float _thickness = 0.05f;
        public float Thickness => DCLUtilities.RoundN2(_thickness) * Size;

        [SerializeField]
        [DesignerPropertySlider(Label = "Support Arm count", Order = 4, MinValue = 1, MaxValue = 8, Tooltip = "", NumberOfSteps = 8)]
        private  int _supportArmCount = 3;
        public int SupportArmCount => _supportArmCount;

        [SerializeField]
        [DesignerPropertySlider(Label = "Support Arm Position", Order = 5, MinValue = 0.1f, MaxValue = 1f, Tooltip = "", NumberOfSteps = 1000)]
        private  float _supportArmPos = 0.75f;
        public float SupportArmPos => Mathf.Min(_supportArmPos, 1 - SupportArmRadius / Radius);

        [SerializeField]
        [DesignerPropertySlider(Label = "Double Support Arm Offset", Order = 6, MinValue = 0f, MaxValue = 1f, Tooltip = "", NumberOfSteps = 1000)]
        private  float _doubleSupportArmOffset = 0f;
        public float DoubleSupportArmOffset => DCLUtilities.RoundN2(_doubleSupportArmOffset);

        [SerializeField]
        [DesignerPropertySlider(Label = "Support Arm Diameter", Order = 7, MinValue = 0.01f, MaxValue = 0.1f, Tooltip = "", NumberOfSteps = 1000)]
        private  float _supportArmDiameter = 0.04f;
        public float SupportArmDiameter => DCLUtilities.Round(_supportArmDiameter, 4) * Size;
        public float SupportArmRadius => SupportArmDiameter / 2;

        [SerializeField]
        [DesignerPropertySlider(Label = "Ridge Length", Order = 8, MinValue = 0f, MaxValue = 0.1f, Tooltip = "", NumberOfSteps = 1000)]
        private  float _ridgeWidth = 0.035f;
        public float RidgeWidth => DCLUtilities.Round(_ridgeWidth, 4) * Size;

        [SerializeField]
        [DesignerPropertySlider(Label = "Ridge A", Order = 9, MinValue = 0.01f, MaxValue = 0.05f, Tooltip = "", NumberOfSteps = 1000)]
        private  float _ridgeA = 0.01f;
        public float RidgeA => DCLUtilities.Round(_ridgeA, 4);

        [SerializeField]
        [DesignerPropertySlider(Label = "Ridge B", Order = 10, MinValue = -0.02f, MaxValue = 0.02f, Tooltip = "", NumberOfSteps = 1000)]
        private  float _ridgeB = 0.002f;
        public float RidgeB => DCLUtilities.Round(_ridgeB, 4);

        [SerializeField]
        [DesignerPropertyToggleButton(Label = "Straight Side", Order = 11, Tooltip = "")]
        private  bool _straightSide = false;
        public bool straightSide => _straightSide;

        [SerializeField]
        [DesignerPropertySlider(Label = "Bottom Depth", Order = 12, MinValue = 0f, MaxValue = 0.75f, Tooltip = "", NumberOfSteps = 1000)]
        private float _bottomDepth = 0f;
        public float BottomDepth => DCLUtilities.RoundN2(_bottomDepth) * Size;

        [SerializeField]
        [DesignerPropertySlider(Label = "Bottom Offset", Order = 13, MinValue = 0, MaxValue = 0.5f, Tooltip = "", NumberOfSteps = 1000)]
        private  float _bottomOffset = 0f;
        public float BottomOffset => DCLUtilities.RoundN2(_bottomOffset) * Size;

        [SerializeField]
        [DesignerPropertySlider(Label = "Bottom Diameter", Order = 14, MinValue = 0f, MaxValue = 4f, Tooltip = "", NumberOfSteps = 1000)]
        private float _bottomDiameter = 0f;
        public float BottomDiameter => DCLUtilities.RoundN2(_bottomDiameter) * Size;
        public float BottomRadius => BottomDiameter / 2;

        [SerializeField]
        [DesignerPropertySpinner(Label = "Upper Element", Order = 15, Tooltip = "")]
        private string _upperElement = "Basic";
        public string UpperElement => _upperElement;

        [SerializeField]
        [DesignerPropertySpinner(Label = "Lower Element", Order = 16, Tooltip = "")]
        private  string _lowerElement = "Basic";
        public string LowerElement => _lowerElement;

        [SerializeField]
        [DesignerPropertyToggleButton(Label = "Secondary Reflector", Order = 17, Tooltip = "")]
        private  bool _secondaryReflector = true;
        public bool SecondaryReflector => _secondaryReflector;

        [SerializeField]
        [PartModifierProperty(true, false)]
        private  int _parabolaResolution = 64;
        public int ParabolaResolution => _parabolaResolution;

        [SerializeField]
        [PartModifierProperty(true, false)]
        private  int _parabolaDivCount = 16;
        public int ParabolaDivCount => _parabolaDivCount;

        [SerializeField]
        [PartModifierProperty(true, false)]
        private  int _ridgeDivCount = 8;
        public int RidgeDivCount => _ridgeDivCount;

        [SerializeField]
        [PartModifierProperty(true, false)]
        private  int _structureResolution = 8;
        public int StructureResolution => _structureResolution;

        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            d.OnAnyPropertyChanged(() =>
            {
                DCLUtilities.SetDesignerSliderMinMax(d.GetSliderProperty(() => _depth), ref _depth, _diameter / 50, _diameter / 4);
                DCLUtilities.SetDesignerSliderMinMax(d.GetSliderProperty(() => _bottomDiameter), ref _bottomDiameter, (_straightSide || BottomOffset > 0) ? Mathf.Max(_diameter / 2 - 10 * _bottomDepth, 0) : 0, _diameter);
                DCLUtilities.SetDesignerSliderMinMax(d.GetSliderProperty(() => _bottomDepth), ref _bottomDepth, _thickness, 0.75f);

                Script.AntennaData?.Script.UpdateAntenna();
            });

            d.OnPropertyChanged(() => _secondaryReflector, (newVal, oldVal) =>
            {
                _upperElement = "Basic";
                d.GetSpinnerProperty(() => _upperElement).UpdateValues();
            });

            d.OnSpinnerValuesRequested(() => _upperElement, delegate (List<string> x)
            {
                x.Clear();
                x.AddRange(_secondaryReflector ? DroonComLinks.Antennas.ParabolicAntenna.UpperElement.Reflectors : DroonComLinks.Antennas.ParabolicAntenna.UpperElement.Receivers);
            });
            d.OnSpinnerValuesRequested(() => _lowerElement, delegate (List<string> x)
            {
                x.Clear();
                x.AddRange(DroonComLinks.Antennas.ParabolicAntenna.LowerElement.Receivers);
            });

            d.OnValueLabelRequested(() => _diameter, (float x) => Diameter.ToString("n2") + "m");
            d.OnValueLabelRequested(() => _depth, (float x) => Depth.ToString("n2") + "m");
            d.OnValueLabelRequested(() => _thickness, (float x) => (Thickness * 100).ToString("n2") + "cm");
            d.OnValueLabelRequested(() => _supportArmPos, (float x) => Utilities.FormatPercentage(x));
            d.OnValueLabelRequested(() => _doubleSupportArmOffset, (float x) => Utilities.FormatPercentage(x));
            d.OnValueLabelRequested(() => _supportArmDiameter, (float x) => (SupportArmDiameter * 100).ToString("n2") + "cm");
            d.OnValueLabelRequested(() => _ridgeWidth, (float x) => x.ToString("n2"));
            d.OnValueLabelRequested(() => _ridgeA, (float x) => (x * 100).ToString("n2"));
            d.OnValueLabelRequested(() => _ridgeB, (float x) => (x * 100).ToString("n2"));
            d.OnValueLabelRequested(() => _bottomDiameter, (float x) => BottomDiameter.ToString("n2") + "m");
            d.OnValueLabelRequested(() => _bottomDepth, (float x) => BottomDepth.ToString("n2") + "m");
            d.OnValueLabelRequested(() => _bottomOffset, (float x) => BottomOffset.ToString("n2") + "m");

            d.OnVisibilityRequested(() => _lowerElement, (bool x) => _secondaryReflector);
            d.OnVisibilityRequested(() => _bottomDepth, (bool x) => _straightSide);
            d.OnVisibilityRequested(() => _bottomDiameter, (bool x) => _straightSide || BottomOffset > 0);
        }

        public void OnSizeChanged(float _size)
        {
            Symmetry.ExecuteOnSymmetricPartModifiers(this, true, delegate (ParabolicAntennaData d)
            {
                d.Size = _size;
                d.DesignerPartProperties?.Manager?.RefreshUI();
                d.Script.UpdateAntenna();
            });
        }
    }
}