namespace Assets.Scripts.Craft.Parts.Modifiers {
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System;
    using Assets.Scripts.DroonComLinks.Interfaces;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class WhipAntennaScript : PartModifierScript<WhipAntennaData>, IDCLAntennaScript {
        public IDCLAntennaData data => Data;
        private DCLAntennaData _antennaData;
        public DCLAntennaData antennaData {
            get {
                if (_antennaData == null) _antennaData = base.PartScript.GetModifier<DCLAntennaScript> ().Data;
                return _antennaData;
            }
        }
        private GameObject[] segments = new GameObject[segmentCount];
        private float[] segmentsOffsets = new float[] { 0, -0.78f, -0.72f, -0.59f, -0.53f, -0.45f, -0.4f };
        private static int segmentCount = 7;

        protected override void OnInitialized () {
            base.OnInitialized ();
            segments[0] = transform.Find ("Antenna").gameObject;

            for (int i = 1; i < segmentCount; i++) {
                segments[i] = segments[i - 1].transform.Find ("Segment" + i).gameObject;
            }
        }
        public void Initialize (DCLAntennaData AntennaData, float size) {
            _antennaData = AntennaData;
            Data.OnSizeChanged (size);
        }

        public void UpdateAntenna () {
            segments[0].transform.localScale = new Vector3 (Data.size, Data.size, Data.size);
            for (int i = 1; i < segmentCount; i++) {
                if (Data.startOpen) segments[i].transform.localPosition = new Vector3 ();
                else segments[i].transform.localPosition = new Vector3 (0, 0, segmentsOffsets[i]);
            }
        }
    }
}