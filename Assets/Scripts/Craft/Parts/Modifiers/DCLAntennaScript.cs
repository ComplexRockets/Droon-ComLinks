namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System.Collections;
    using System;
    using Assets.Scripts.DroonComLinks.Antennas;
    using ModApi.Craft.Parts;
    using ModApi.Ui.Inspector;
    using ModApi.Ui;
    using Assets.Scripts.Design;
    using Assets.Scripts.DroonComLinks.Controls;
    using Assets.Scripts.DroonComLinks;
    using Assets.Scripts.DroonComLinks.Network;

    public class DCLAntennaScript : PartModifierScript<DCLAntennaData> //, IFlightUpdate, IGameLoopItem
    {
        public IDCLAntennaScript AntennaScript
        {
            get
            {
                try
                {
                    _antennaScript ??= PartScript.GetModifierWithInterface<IDCLAntennaScript>();
                    return _antennaScript;
                }
                catch { return null; }
            }
        }
        public IDCLAntennaData AntennaData
        {
            get
            {
                if (AntennaScript == null) return null;
                return _antennaScript.data;
            }
        }
        private IDCLAntennaScript _antennaScript;
        public Antenna Antenna => ComLinksManager.Instance.GetAntennaFromId(PartScript.CraftScript.CraftNode.NodeId, PartScript.Data.Id);

        protected override void OnInitialized()
        {
            base.OnInitialized();
            StartCoroutine(InitialiseCoroutine());
        }

        private IEnumerator InitialiseCoroutine()
        {
            while (AntennaScript == null) yield return 0;
            AntennaScript.Initialize(Data, Data.Size);
            Data.UpdateAntenna();

            if (Data.MaxPower == 0) PartScript.Deactivate();
            else PartScript.Activate();
        }

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            if (!Data.CustomAntenna) model.Title = AntennaData.type.id;

            SliderModel maxPowerSlider = new(
                "Max Power Consumption",
                () => Data.MaxPower,
                (float p) =>
                {
                    if (ControlsPatches.RegisterExternalCommand("AntennaMaxPowerConsumptionChanging", needsPower: false))
                    {
                        Data.MaxPower = AntennaTypes.ClampAntennaAtribute(AntennaData.type, AntennaAttributes.maxPower, p);

                        if (Data.MaxPower == 0) PartScript.Deactivate();
                        else PartScript.Activate();
                    }
                },
                0,
                AntennaData.type.maxPower
            );
            maxPowerSlider.ValueChangedByUserInput += delegate
            {
                ControlsPatches.RegisterExternalCommand("UserInputChangeAntennaMaxPowerConsumption");
                ComLinksManager.Instance.ForceRefresh();
            };
            maxPowerSlider.OnSliderAdjustmentEnded += delegate
            {
                ControlsPatches.RegisterExternalCommand("ChangeAntennaMaxPowerConsumption");
                ComLinksManager.Instance.ForceRefresh();
            };
            maxPowerSlider.ValueFormatter = (float x) => $"{x:n2} W";

            model.Add(maxPowerSlider);
            model.Add(new TextModel("Max Tx Power", () => Data.MaxTransmittedPower.ToString("n2") + " W"));
            model.Add(new TextModel("Min Rx Power", () => (AntennaMath.GetMinReceivablePower(Data.Efficiency) * Math.Pow(10, 15)).ToString("n2") + " E-15 W"));
            model.Add(new TextModel("Freq", () => DCLUtilities.FormatFrequency(Antenna.Frequencies[1])));
            model.Add(new TextModel("Min / Max Freq", () => Antenna.Frequencies[0].ToString("n2") + " / " + DCLUtilities.FormatFrequency(Antenna.Frequencies[2])));
            model.Add(new TextModel("Gain", () => (10 * Math.Log10(Antenna.CenterFrequencyGain)).ToString("n2") + " dB"));

            if (ModSettings.Instance.DebugMode) model.Add(new TextButtonModel("Show Xml", b => Antenna.OnPrintAntennaXML()));

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
            if (Antenna != null)
            {
                Antenna.toggle(true);
                ComLinksManager.Instance.ForceRefresh();
            }
            else ComLinksManager.Instance.RequestFlightStateUpdate("Antenna missing");

            if (Data.MaxPower == 0) Data.MaxPower = AntennaData.type.defaultMaxPowerConsumption;
        }
        public override void OnDeactivated()
        {
            if (!ComLinksManager.Instance.PlayerHasDroonControl && ComLinksManager.Instance.GetNetworkNodeFromCraftId(PartScript.CraftScript.CraftNode.NodeId)?.AvailableAntennas.Count == 1)
            {
                MessageDialogScript dialog = Game.Instance.UserInterface.CreateMessageDialog(MessageDialogType.OkayCancel);
                dialog.MessageText = "This is the last available antenna,\n disabling it might make the craft uncontrollable\n Do you want to proceed?";
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
            if (Antenna != null)
            {
                Antenna.toggle(false);
                ComLinksManager.Instance.ForceRefresh();
            }
            else ComLinksManager.Instance.RequestFlightStateUpdate("Antenna missing");
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
                d.Script.AntennaScript.UpdateAntenna();
                d.UpdateAntenna();
            });
        }

        private void OnDestroy() => ComLinksManager.Instance.RequestFlightStateUpdate("On antenna part destroyed");
    }
}