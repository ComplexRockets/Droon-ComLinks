namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using Assets.Scripts.DroonComLinks.Interfaces;
    using Assets.Scripts.DroonComLinks;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Craft.Parts;
    using ModApi.Design.PartProperties;
    using UnityEngine;
    using System.Collections.Generic;
    using Assets.Scripts.Design;

    public enum WhipAntennaTypes { None, Deployable, Fixed }
    public enum WhipAntennaStyles { None, Simple, Thin }

    [Serializable]
    [DesignerPartModifier("WhipAntenna")]
    [PartModifierTypeId("DroonComLinks.WhipAntenna")]
    public class WhipAntennaData : PartModifierData<WhipAntennaScript>, IDCLAntennaData
    {
        public IAntennaType type => AntennaTypes.whip;
        //public float gain => 10 * Mathf.Log (4100 / (azAngle * eqAngle)) * size;
        public float gain => AntennaMath.GetGain(type, Script.antennaData.waveLength, size, Script.antennaData.efficiency);
        public float mass => 0.6f;
        public int price => 0;
        public float size
        {
            get;
            private set;
        }
        public readonly Dictionary<WhipAntennaTypes, WhipAntennaStyles[]> typeStyles = new Dictionary<WhipAntennaTypes, WhipAntennaStyles[]> {
            {WhipAntennaTypes.Deployable, new WhipAntennaStyles[2] {WhipAntennaStyles.Simple, WhipAntennaStyles.Thin}},
            { WhipAntennaTypes.Fixed, new WhipAntennaStyles[1] { WhipAntennaStyles.Simple}} };

        [SerializeField]
        [DesignerPropertySpinner(Label = "Type", Order = 1, Tooltip = "")]
        private string _antennaType = WhipAntennaTypes.Deployable.ToString();
        public WhipAntennaTypes antennaType => (WhipAntennaTypes)Enum.Parse(typeof(WhipAntennaTypes), _antennaType);

        [SerializeField]
        [DesignerPropertySpinner(Label = "Style", Order = 1, Tooltip = "")]
        private string _antennaStyle = WhipAntennaStyles.Simple.ToString();
        public WhipAntennaStyles antennaStyle => (WhipAntennaStyles)Enum.Parse(typeof(WhipAntennaStyles), _antennaStyle);

        [SerializeField]
        [DesignerPropertyToggleButton(Label = "Deployed in the designer", Order = 2, Tooltip = "Open or close the antenna in the designer")]
        private bool _startOpen = true;
        public bool startOpen
        {
            get => _startOpen;
            set
            {
                _startOpen = value;
                if (Game.InDesignerScene) DesignerPartProperties?.Manager?.RefreshUI();
            }
        }

        [SerializeField]
        [DesignerPropertySlider(Label = "Deployment duration", Order = 3, MinValue = 0.2f, MaxValue = 10f, Tooltip = "", NumberOfSteps = 1000)]
        private float _deploymentDuration = 2;
        public float deploymentDuration => _deploymentDuration;

        [SerializeField]
        [DesignerPropertySlider(Label = "Length", Order = 2, MinValue = 0.2f, MaxValue = 5f, Tooltip = "", NumberOfSteps = 1000)]
        private float _length = 0.7f;
        public float length => _length;

        [SerializeField]
        [DesignerPropertySlider(Label = "Thickness", Order = 3, MinValue = 0.01f, MaxValue = 0.15f, Tooltip = "", NumberOfSteps = 1000)]
        private float _thickness = 0.015f;
        public float thickness => _thickness;

        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            d.OnAnyPropertyChanged(delegate
            {
                if (!Array.Exists(typeStyles[antennaType], style => style == antennaStyle)) _antennaStyle = typeStyles[antennaType][0].ToString();
                Script.antennaData?.Script.UpdateAntenna();
                d.GetSpinnerProperty(() => _antennaStyle).UpdateValues();
                d.Manager.RefreshUI();
            });
            d.OnPropertyChanged(() => _antennaType, (string oldType, string newType) => _antennaStyle = typeStyles[antennaType][0].ToString());

            d.OnValueLabelRequested(() => _deploymentDuration, (float x) => deploymentDuration.ToString("n1") + "s");
            d.OnValueLabelRequested(() => _length, (float x) => (_length * size).ToString("n2") + "m");
            d.OnValueLabelRequested(() => _thickness, (float x) => (_thickness * 100 * size).ToString("n2") + "cm");

            d.OnSpinnerValuesRequested(() => _antennaType, delegate (List<string> x)
            {
                x.Clear();
                x.Add(WhipAntennaTypes.Deployable.ToString());
                x.Add(WhipAntennaTypes.Fixed.ToString());
            });

            d.OnSpinnerValuesRequested(() => _antennaStyle, delegate (List<string> x)
            {
                x.Clear();
                foreach (WhipAntennaStyles style in typeStyles[antennaType]) x.Add(style.ToString());
            });

            d.OnVisibilityRequested(() => _deploymentDuration, (bool x) => _antennaType == WhipAntennaTypes.Deployable.ToString());
            d.OnVisibilityRequested(() => _startOpen, (bool x) => _antennaType == WhipAntennaTypes.Deployable.ToString());
            d.OnVisibilityRequested(() => _length, (bool x) => _antennaType == WhipAntennaTypes.Fixed.ToString());
            d.OnVisibilityRequested(() => _thickness, (bool x) => _antennaType == WhipAntennaTypes.Fixed.ToString());
        }

        public void OnSizeChanged(float _size)
        {
            Symmetry.ExecuteOnSymmetricPartModifiers(this, true, delegate (WhipAntennaData d)
            {
                d.size = _size;
                d.Script.UpdateAntenna();
            });
        }
    }
}