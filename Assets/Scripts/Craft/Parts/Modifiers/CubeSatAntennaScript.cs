namespace Assets.Scripts.Craft.Parts.Modifiers {
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System;
    using Assets.Scripts.DroonComLinks.Interfaces;
    using ModApi.Craft.Parts;
    using UnityEngine;

    public class CubeSatAntennaScript : PartModifierScript<CubeSatAntennaData>, IDCLAntennaScript {
        public IDCLAntennaData data => Data;
        private DCLAntennaData _antennaData;
        public DCLAntennaData antennaData {
            get {
                if (_antennaData == null) _antennaData = base.PartScript.GetModifier<DCLAntennaScript> ().Data;
                return _antennaData;
            }
        }
        private GameObject antennaObject;
        protected override void OnInitialized () {
            base.OnInitialized ();
            antennaObject = transform.Find ("Antenna").gameObject;
        }

        public void Initialize (DCLAntennaData AntennaData, float size) {
            _antennaData = AntennaData;
            Data.OnSizeChanged (size);
        }

        public void UpdateAntenna () {
            antennaObject.transform.localScale = new Vector3 (Data.size, Data.size, Data.size);
        }
    }
}