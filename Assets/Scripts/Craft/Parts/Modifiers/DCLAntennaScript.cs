namespace Assets.Scripts.Craft.Parts.Modifiers {
    using System.Collections;
    using System;
    using Assets.Scripts.DroonComLinks.Interfaces;
    using ModApi.Craft.Parts;
    using ModApi.Ui.Inspector;
    using UnityEngine;
    using static ModApi.Ui.Inspector.SliderModel;
    using Assets.Scripts.DroonComLinks;

    public class DCLAntennaScript : PartModifierScript<DCLAntennaData> {
        public IDCLAntennaScript antennaScript {
            get {
                try {
                    if (_antennaScript == null) _antennaScript = PartScript.GetModifierWithInterface<IDCLAntennaScript> ();
                    return _antennaScript;
                } catch { return null; }
            }
        }
        public IDCLAntennaData antennaData {
            get {
                if (antennaScript == null) return null;
                if (_antennaData == null) _antennaData = _antennaScript.data;
                return _antennaData;
            }
        }
        private IDCLAntennaScript _antennaScript;
        private IDCLAntennaData _antennaData;
        public float gain => antennaData.gain;

        protected override void OnInitialized () {
            base.OnInitialized ();
            StartCoroutine (InitialiseCoroutine ());
        }

        private IEnumerator InitialiseCoroutine () {
            while (antennaScript == null) yield return 0;
            antennaScript.Initialize (Data, Data.size);

            while (Data.manager == null) yield return 0;
            Data.OnUpdateFields ();
        }

        public override void OnGenerateInspectorModel (PartInspectorModel model) {
            SliderModel frequencySlider = new SliderModel ("Frequency", () => Data.frequency, (float f) => Data.frequency = f, antennaData.type.minFrenquency, antennaData.type.maxFrenquency);
            frequencySlider.ValueFormatter = ((float x) => $"{frequencySlider.Value} GHz");
            frequencySlider.OnSliderAdjustmentEnded += delegate { Mod.Instance.ComLinksManager.ForceRefresh (); };
            model.Add (frequencySlider);
            model.Add (new TextModel ("Gain", () => (10 * Math.Log10 (antennaData.gain)).ToString ("n0") + " dB"));
            model.Add (new TextModel ("Min Rx Power", () => (Data.minReceivablePower * Math.Pow (10, 20)).ToString ("n2") + "E-20 W"));
            model.Add (new TextModel ("Max Tx Power", () => Data.maxTransmittedPower.ToString ("n2") + "W"));
        }
        public override void OnActivated () {
            base.OnActivated ();
            Mod.Instance.ComLinksManager.GetNode (PartScript.CraftScript.CraftNode)?.antennas.Find (antenna => antenna.partId == PartScript.Data.Id)?.toggleAntenna (true);
            Mod.Instance.ComLinksManager.ForceRefresh ();
        }
        public override void OnDeactivated () {
            base.OnDeactivated ();
            Mod.Instance.ComLinksManager.GetNode (PartScript.CraftScript.CraftNode)?.antennas.Find (antenna => antenna.partId == PartScript.Data.Id)?.toggleAntenna (false);
            Mod.Instance.ComLinksManager.ForceRefresh ();
        }

        public void UpdateAntenna () {
            antennaScript.UpdateAntenna ();
        }
    }
}