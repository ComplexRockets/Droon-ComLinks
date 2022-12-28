namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System.Collections;
    using System;
    using Assets.Scripts.DroonComLinks.Antennas;
    using ModApi.Craft.Parts;
    using ModApi.Ui.Inspector;
    using ModApi.Ui;
    using Assets.Scripts.Design;
    using System.Collections.Generic;
    using UnityEngine;
    using Assets.Scripts.DroonComLinks.Controls;
    using Assets.Scripts.Craft.Parts.Modifiers.Input;

    public class DCLAntennaScript : PartModifierScript<DCLAntennaData> //, IFlightUpdate, IGameLoopItem
    {
        public IDCLAntennaScript antennaScript
        {
            get
            {
                try
                {
                    if (_antennaScript == null) _antennaScript = PartScript.GetModifierWithInterface<IDCLAntennaScript>();
                    return _antennaScript;
                }
                catch { return null; }
            }
        }
        public IDCLAntennaData antennaData
        {
            get
            {
                if (antennaScript == null) return null;
                return _antennaScript.data;
            }
        }
        private IDCLAntennaScript _antennaScript;
        public Antenna antenna
        {
            get
            {
                Dictionary<int, Antenna> antennaFromPartId = Mod.Instance.comLinksManager?.networkNodeFromCraftNode[PartScript.CraftScript.CraftNode]?.antennaFromPartId;
                if (antennaFromPartId == null) return null;
                if (antennaFromPartId.ContainsKey(PartScript.Data.Id)) return antennaFromPartId[PartScript.Data.Id];
                return null;
            }
        }
        public float gain => antennaData.gain;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            StartCoroutine(InitialiseCoroutine());
        }

        public override void OnAddedToCraftInDesigner(bool subAssembly)
        {
            base.OnAddedToCraftInDesigner(subAssembly);
            if (!subAssembly) StartCoroutine(FirstInitialiseCoroutine());
        }

        private IEnumerator InitialiseCoroutine()
        {
            while (antennaScript == null) yield return 0;
            antennaScript.Initialize(Data, Data.size);
        }

        private IEnumerator FirstInitialiseCoroutine()
        {
            while (Data.manager == null || antennaData == null) yield return 0;
            Data.InitializeFields();
        }

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            if (!Data.customAntenna) model.Title = antennaData.type.id;

            SliderModel maxPowerSlider = new SliderModel(
                "Max Power Consumption",
                () => Data.maxPower,
                (float p) =>
                {
                    if (ControlsPatches.RegisterExternalCommand("AntennaMaxPowerConsumptionChanging", needsPower: false))
                    {
                        Data.maxPower = AntennaTypes.ClampFloatAtribute(antennaData.type, AntennaTypes.FloatAttributes.MaxPower, p);

                        if (Data.maxPower == 0) PartScript.Deactivate();
                        else PartScript.Activate();
                    }
                },
                0,
                antennaData.type.maxPower
            );
            maxPowerSlider.ValueChangedByUserInput += delegate
            {
                ControlsPatches.RegisterExternalCommand("UserInputChangeAntennaMaxPowerConsumption");
                Mod.Instance.comLinksManager?.ForceRefresh();
            };
            maxPowerSlider.OnSliderAdjustmentEnded += delegate
            {
                ControlsPatches.RegisterExternalCommand("ChangeAntennaMaxPowerConsumption");
                Mod.Instance.comLinksManager?.ForceRefresh();
            };
            maxPowerSlider.ValueFormatter = ((float x) => $"{(x):n2} W");

            model.Add(maxPowerSlider);
            model.Add(new TextModel("Max Tx Power", () => (Data.maxTransmittedPower.ToString("n2") + " W")));
            model.Add(new TextModel("Min Rx Power", () => (Data.minReceivablePower * Math.Pow(10, 19)).ToString("n2") + " E-19 W"));
            model.Add(new TextModel("Freq", () => antenna.frequencies[1].ToString("n2") + " GHz"));
            model.Add(new TextModel("Min / Max Freq", () => antenna.frequencies[0].ToString("n2") + " / " + antenna.frequencies[2].ToString("n2") + " GHz"));
            model.Add(new TextModel("Gain", () => (10 * Math.Log10(antennaData.gain)).ToString("n2  ") + " dB"));

            // InputControllerScript inputController = PartScript.GetModifier<InputControllerScript>();
            // if (inputController != null && inputController.InputId == "OpenCloseAntenna" && inputController.enabled)
            // {
            // }

            base.OnGenerateInspectorModel(model);
        }

        // public override void GetCraftWarnings(List<string> warnings)
        // {
        //     if (_battery?.IsEmpty ?? true)
        //     {
        //         warnings.Add(base.PartScript.Data.Name + " [ID " + base.PartScript.Data.Id + "] needs battery to broadcast data");
        //     }
        // }

        public override void OnActivated()
        {
            base.OnActivated();
            if (antenna != null)
            {
                antenna?.toggle(true);
                Mod.Instance.comLinksManager.ForceRefresh();
            }
            else
            {
                if (ModSettings.Instance.debugMode) Debug.Log("On antenna activated requestFlightStateUpdate");
                Mod.Instance.comLinksManager.requestFlightStateUpdate = true;
            }

            if (Data.maxPower == 0) Data.maxPower = antennaData.type.defaultMaxPowerConsumption;
        }
        public override void OnDeactivated()
        {
            if (!(bool)Mod.Instance.comLinksManager?.playerHasDroonControl && Mod.Instance.comLinksManager?.networkNodeFromCraftNode[PartScript.CraftScript.CraftNode]?.antennas.FindAll(a => a.activated).Count == 1)
            {
                MessageDialogScript dialog = Game.Instance.UserInterface.CreateMessageDialog(MessageDialogType.OkayCancel);
                dialog.MessageText = "This is the last activated antenna,\n disabling it might make the craft uncontrollable\n Do you want to proceed?";
                dialog.CancelButtonText = "Yes";
                dialog.OkayButtonText = "No";
                dialog.CancelClicked += delegate (MessageDialogScript d)
                {
                    Deactivate();
                    dialog.Close();
                };
                dialog.OkayClicked += delegate (MessageDialogScript d)
                {
                    dialog.Close();
                };
                PartScript.Activate();
            }
            else Deactivate();
        }

        private void Deactivate()
        {
            base.OnDeactivated();
            if (antenna != null)
            {
                antenna?.toggle(false);
                Mod.Instance.comLinksManager.ForceRefresh();
            }
            else
            {
                if (ModSettings.Instance.debugMode) Debug.Log("On antenna deactivated requestFlightStateUpdate");
                Mod.Instance.comLinksManager.requestFlightStateUpdate = true;
            }
        }

        // public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
        // {
        //     base.OnCraftLoaded(craftScript, movedToNewCraft);
        //     //_battery = base.PartScript.BatteryFuelSource;
        // }

        // public override void OnCraftStructureChanged(ICraftScript craftScript)
        // {
        //     base.OnCraftStructureChanged(craftScript);
        //     //_battery = base.PartScript.BatteryFuelSource;
        // }

        public void UpdateAntenna()
        {
            Symmetry.ExecuteOnSymmetricPartModifiers(this.Data, true, delegate (DCLAntennaData d)
            {
                d.Script.antennaScript.UpdateAntenna();
                d.UpdateAntenna();
            });
        }

        // void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        // {
        //     if (base.PartScript.Data.Activated && _battery.TotalFuel > 0.0)
        //     {
        //         _battery.RemoveFuel(base.Data.idle * Time.deltaTime * 0.001f);
        //     }
        // }

        private void OnDestroy()
        {
            if (ModSettings.Instance.debugMode) Debug.Log("On antenna part destroyed requestFlightStateUpdate");
            Mod.Instance.comLinksManager.requestFlightStateUpdate = true;
        }
    }
}