namespace Assets.Scripts.Craft.Parts.Modifiers {
    using System.Collections;
    using Assets.Scripts.DroonComLinks.Interfaces;
    using ModApi.Craft.Parts;
    using ModApi.Ui.Inspector;
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
        private GameObject[] segments;
        private float[] segmentsOffsets = new float[] { 0, -0.78f, -0.72f, -0.59f, -0.53f, -0.45f, -0.4f };
        private int segmentCount = 7;
        private bool Opened = true;
        private bool animating = false;

        protected override void OnInitialized () {
            base.OnInitialized ();
            segments = new GameObject[segmentCount];
            segments[0] = transform.Find ("Antenna").gameObject;

            for (int i = 1; i < segmentCount; i++) {
                segments[i] = segments[i - 1].transform.Find ("Segment" + i).gameObject;
            }
        }
        public void Initialize (DCLAntennaData AntennaData, float size) {
            _antennaData = AntennaData;
            Data.OnSizeChanged (size);
        }

        public override void OnGenerateInspectorModel (PartInspectorModel model) {
            model.Add (new TextButtonModel ("Open/Close Antenna", delegate {
                if (!animating) StartCoroutine (TogglePosition (!Opened, Data.deploymentDuration));
            }));
        }

        public void UpdateAntenna () {
            segments[0].transform.localScale = new Vector3 (Data.size, Data.size, Data.size);
            if (Data.startOpen != Opened && !animating) StartCoroutine (TogglePosition (Data.startOpen, Data.deploymentDuration));
        }

        private IEnumerator TogglePosition (bool open, float animationDuration) {
            float currentAnimTime = 0;
            int direction = open? - 1 : 1;
            float[] speed = new float[segmentCount];
            animating = true;

            for (int i = 1; i < segmentCount; i++) {
                speed[i] = segmentsOffsets[i] / animationDuration * direction;
            }

            while (currentAnimTime <= animationDuration) {
                for (int i = 1; i < segmentCount; i++) {
                    Vector3 pos = segments[i].transform.localPosition;
                    pos.z += speed[i] * Time.deltaTime;
                    segments[i].transform.localPosition = pos;
                }

                currentAnimTime += Time.deltaTime;
                yield return 0;
            }

            Opened = Data.startOpen = open;
            animating = false;
        }
    }
}