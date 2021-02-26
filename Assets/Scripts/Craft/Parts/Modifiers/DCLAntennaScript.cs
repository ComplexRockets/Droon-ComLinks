namespace Assets.Scripts.Craft.Parts.Modifiers {
    using System;
    using Assets.Scripts.DroonComLinks.Interfaces;
    using Assets.Scripts.DroonComLinks.Objects;
    using ModApi.Craft.Parts;
    using ModApi.Ui.Inspector;
    using UnityEngine;

    public class DCLAntennaScript : PartModifierScript<DCLAntennaData> {
        public IDCLAntennaScript antennaScript {
            get {
                if (_antennaScript == null) _antennaScript = PartScript.GetModifierWithInterface<IDCLAntennaScript> ();
                return _antennaScript;
            }
        }
        public IDCLAntennaData antennaData {
            get {
                if (_antennaData == null) _antennaData = _antennaScript.data;
                return _antennaData;
            }
        }
        private IDCLAntennaScript _antennaScript;
        private IDCLAntennaData _antennaData;
        public float gain => antennaData.gain;

        protected override void OnInitialized () {
            base.OnInitialized ();
        }

        public override void OnModifiersCreated () {
            base.OnModifiersCreated ();
            Debug.Log ("modifier initialized antenna script: " + antennaScript == null);
            antennaScript.Initialize (Data, Data.size);
            //Data.saveData ();
        }

        public override void OnGenerateInspectorModel (PartInspectorModel model) {
            model.Add (new TextModel ("Frequency", () => Data.frequency.ToString ("n2") + " GHz"));
            model.Add (new TextModel ("Gain", () => antennaData.gain.ToString ("n0") + " nat"));
            model.Add (new TextModel ("Gain", () => (10 * Math.Log10 (antennaData.gain)).ToString ("n0") + " dBi"));
            model.Add (new TextModel ("Min Rx Power", () => Data.minReceivablePower.ToString ("n2") + " W"));
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
            //Data.saveData ();
        }
    }
}