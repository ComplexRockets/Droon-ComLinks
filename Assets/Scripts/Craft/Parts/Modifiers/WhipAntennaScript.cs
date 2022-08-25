namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using Assets.Scripts.Craft.Parts.Modifiers.Input;
    using Assets.Scripts.Design;
    using Assets.Scripts.DroonComLinks.Interfaces;
    using Assets.Scripts.DroonComLinks.Objects.Antennas.WhipAntennas;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Input;
    using ModApi.GameLoop;
    using ModApi.GameLoop.Interfaces;

    public class WhipAntennaScript : PartModifierScript<WhipAntennaData>, IDCLAntennaScript, IFlightUpdate, IGameLoopItem
    {
        public IDCLAntennaData data => Data;
        private DCLAntennaData _antennaData;
        public DCLAntennaData antennaData
        {
            get
            {
                if (_antennaData == null) _antennaData = PartScript.GetModifier<DCLAntennaScript>().Data;
                return _antennaData;
            }
        }
        public IWhipAntenna whipAntenna { get; private set; }
        public IInputController inputController;
        public InputControllerScript inputControllerScript;
        private string _lastAntennaType = "DFGSDGF";
        private string _lastAntennaStyle = "DFGDSFG";
        private string _deployableString = WhipAntennaTypes.Deployable.ToString();
        public bool deployable => Data.antennaType == _deployableString;
        private bool _positionInitialized = false;

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        public void Initialize(DCLAntennaData antennaData, float size)
        {
            _antennaData = antennaData;
            inputControllerScript = gameObject.GetComponent<InputControllerScript>();
            if (Game.InFlightScene) Data.startOpen = inputControllerScript.Value > 0;
            Data.OnSizeChanged(size);
        }

        public void FlightUpdate(in FlightFrameData frame)
        {
            if (deployable && whipAntenna != null)
            {
                if (Data.startOpen != (inputControllerScript.Value > 0)) ToggleAntenna(!Data.startOpen, Data.deploymentDuration);
            }
        }

        public void UpdateAntenna()
        {
            if (_lastAntennaType != Data.antennaType || _lastAntennaStyle != Data.antennaStyle)
            {
                if (whipAntenna != null) whipAntenna.Destroy();

                switch (Enum.Parse(typeof(WhipAntennaTypes), Data.antennaType))
                {
                    case WhipAntennaTypes.Deployable:
                        whipAntenna = new LargeDeployableWhipAntenna();
                        inputControllerScript.enabled = true;
                        inputControllerScript.Data.PartPropertiesEnabled = true;
                        break;
                    // case WhipAntennaData.WhipAntennaTypes.Fixed:
                    //     whipAntenna = new FixedWhipAntenna();
                    //     inputControllerScript.Data.PartPropertiesEnabled = false;
                    //     inputControllerScript.enabled = false;
                    //     break;
                    default:
                        break;
                }

                whipAntenna.Initialize(PartScript.Transform, _antennaData.customAntenna, Data);
                PartColliderScript[] componentsInChildren = PartScript.GameObject.GetComponentsInChildren<PartColliderScript>(includeInactive: true);
                foreach (PartColliderScript partColliderScript in componentsInChildren) partColliderScript.gameObject.layer = 31;
                PartScript.PartMaterialScript.UpdateRenderers();
                PartScript.OnModifiersCreated();

                _lastAntennaType = Data.antennaType;
                _lastAntennaStyle = Data.antennaStyle;
            }

            if (Game.InDesignerScene)
            {
                Symmetry.SynchronizePartModifiers(PartScript);
                PartScript.CraftScript.RaiseDesignerCraftStructureChangedEvent();
            }

            whipAntenna.Update(Data.size);
            if (Game.InDesignerScene && (whipAntenna.opened != Data.startOpen)) ToggleAntenna(Data.startOpen, Data.deploymentDuration);
        }

        private void ToggleAntenna(bool open, float animationduration)
        {
            if ((Game.InDesignerScene || (Game.InFlightScene && !Game.Instance.FlightScene.TimeManager.CurrentMode.WarpMode)) && !whipAntenna.animating)
                StartCoroutine(whipAntenna.Toggle(open, animationduration, Data));
        }
    }
}