namespace Assets.Scripts.Craft.Parts.Modifiers {
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using System;
    using Assets.Scripts.DroonComLinks.Interfaces;
    using Assets.Scripts.DroonComLinks;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Craft.Parts;
    using ModApi.Design.PartProperties;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier ("WhipAntenna")]
    [PartModifierTypeId ("DroonComLinks.WhipAntenna")]
    public class WhipAntennaData : PartModifierData<WhipAntennaScript>, IDCLAntennaData {
        public string antennaType => "Whip Antenna";
        public IAntennaType type => AntennaTypes.whip;
        //public float gain => 10 * Mathf.Log (4100 / (azAngle * eqAngle)) * size;
        public float gain => AntennaMath.GetGain (type, Script.antennaData.waveLength, size, Script.antennaData.efficiency);
        public float size {
            get;
            private set;
        }

        [SerializeField]
        [DesignerPropertyToggleButton (Label = "Start Open", Order = 1, Tooltip = "")]
        private bool _startOpen = true;
        public bool startOpen => _startOpen;

        protected override void OnDesignerInitialization (IDesignerPartPropertiesModifierInterface d) {
            d.OnAnyPropertyChanged (() => Script.UpdateAntenna ());
        }

        public void OnSizeChanged (float _size) {
            size = _size;
            Script.UpdateAntenna ();
        }
    }
}