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

    public enum WhipAntennaTypes { Deployable }//Fixed
    public enum DeployableAntennaStyles { Simple, Thin }

    [Serializable]
    [DesignerPartModifier("WhipAntenna")]
    [PartModifierTypeId("DroonComLinks.WhipAntenna")]
    public class WhipAntennaData : PartModifierData<WhipAntennaScript>, IDCLAntennaData
    {
        public IAntennaType type => AntennaTypes.whip;
        //public float gain => 10 * Mathf.Log (4100 / (azAngle * eqAngle)) * size;
        public float gain => AntennaMath.GetGain(type, Script.antennaData.waveLength, size, Script.antennaData.efficiency);
        public float mass => size * 0.6f;
        public int price => 0;
        public float size
        {
            get;
            private set;
        }

        [SerializeField]
        [DesignerPropertySpinner(Label = "Type", Order = 1, Tooltip = "")]
        private string _antennaType = WhipAntennaTypes.Deployable.ToString();
        public string antennaType => _antennaType;

        [SerializeField]
        [DesignerPropertySpinner(Label = "Style", Order = 1, Tooltip = "")]
        private string _antennaStyle = DeployableAntennaStyles.Simple.ToString();
        public string antennaStyle => _antennaStyle;

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
        [DesignerPropertySlider(Label = "Deployment duration", Order = 3, MinValue = 0.1f, MaxValue = 10f, Tooltip = "", NumberOfSteps = 1000)]
        private float _deploymentDuration = 2;
        public float deploymentDuration => _deploymentDuration;

        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            d.OnAnyPropertyChanged(delegate
            {
                Script.antennaData?.Script.UpdateAntenna();
                d.Manager.RefreshUI();
            });

            d.OnValueLabelRequested(() => _deploymentDuration, (float x) => deploymentDuration.ToString("n1") + "s");

            d.OnSpinnerValuesRequested(() => _antennaType, delegate (List<string> x)
            {
                x.Clear();
                //x.Add(WhipAntennaTypes.Fixed.ToString());
                x.Add(WhipAntennaTypes.Deployable.ToString());
            });

            d.OnSpinnerValuesRequested(() => _antennaStyle, delegate (List<string> x)
            {
                x.Clear();
                if (_antennaType == WhipAntennaTypes.Deployable.ToString())
                {
                    x.Add(DeployableAntennaStyles.Simple.ToString());
                    x.Add(DeployableAntennaStyles.Thin.ToString());
                }
            });

            d.OnVisibilityRequested(() => _deploymentDuration, (bool x) => _antennaType == WhipAntennaTypes.Deployable.ToString());
            d.OnVisibilityRequested(() => _startOpen, (bool x) => _antennaType == WhipAntennaTypes.Deployable.ToString());
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