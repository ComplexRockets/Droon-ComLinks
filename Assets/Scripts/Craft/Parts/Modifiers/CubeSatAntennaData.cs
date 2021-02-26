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
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier ("CubeSatAntenna")]
    [PartModifierTypeId ("DroonComLinks.CubeSatAntenna")]
    public class CubeSatAntennaData : PartModifierData<CubeSatAntennaScript>, IDCLAntennaData {
        public string antennaType => "CubeSat SBand Antenna";
        public IAntennaType type => AntennaTypes.cubeSatSBand;
        public float size {
            get;
            private set;
        }
        //public float gain => 10 * Mathf.Log (4100 / (azAngle * eqAngle)) * size;
        public float gain => AntennaMath.GetGain (type, Script.antennaData.waveLength, size, Script.antennaData.efficiency);

        public void OnSizeChanged (float _size) {
            size = _size;
            Script.UpdateAntenna ();
        }
    }
}