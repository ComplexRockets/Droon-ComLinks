namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts.Craft.Parts.Modifiers.Input;
    using Assets.Scripts.Design;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Input;
    using ModApi.GameLoop;
    using ModApi.GameLoop.Interfaces;
    using Assets.Scripts.DroonComLinks.Antennas.WhipAntennas;
    using Assets.Scripts.DroonComLinks.Antennas;
    using Assets.Scripts.DroonComLinks.Antennas.WhipAntennas.Deployable;
    using Assets.Scripts.DroonComLinks.Antennas.WhipAntennas.Fixed;

    public class WhipAntennaScript : PartModifierScript<WhipAntennaData>, IDCLAntennaScript, IFlightUpdate, IGameLoopItem
    {
        public IDCLAntennaData data => Data;
        private DCLAntennaData _antennaData;
        public DCLAntennaData antennaData
        {
            get
            {
                _antennaData ??= PartScript.GetModifier<DCLAntennaScript>().Data;
                return _antennaData;
            }
        }
        public IWhipAntenna whipAntenna { get; private set; }
        public IInputController inputController;
        public InputControllerScript inputControllerScript;
        private WhipAntennaTypes _lastAntennaType = WhipAntennaTypes.None;
        private WhipAntennaStyles _lastAntennaStyle = WhipAntennaStyles.None;
        public bool deployable => true; //Data.antennaType == WhipAntennaTypes.Deployable.ToString();

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
            if ((_lastAntennaType != Data.antennaType) || (_lastAntennaStyle != Data.antennaStyle))
            {
                if (whipAntenna != null) whipAntenna.Destroy();

                switch (Data.antennaType)
                {
                    case WhipAntennaTypes.Deployable:
                        whipAntenna = DeployableWhipAntenna.Create(Data.antennaStyle);
                        inputControllerScript.enabled = true;
                        inputControllerScript.Data.PartPropertiesEnabled = true;
                        break;
                    case WhipAntennaTypes.Fixed:
                        whipAntenna = FixedWhipAntenna.Create(Data.antennaStyle);
                        inputControllerScript.Data.PartPropertiesEnabled = false;
                        inputControllerScript.enabled = false;
                        break;
                }

                whipAntenna.Initialize(PartScript.Transform, _antennaData.CustomAntenna, Data);
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
            }

            whipAntenna.UpdateAntenna(Data);
            if (Game.InDesignerScene && (whipAntenna.opened != Data.startOpen)) ToggleAntenna(Data.startOpen, Data.deploymentDuration);
        }

        private void ToggleAntenna(bool open, float animationduration)
        {
            if ((Game.InDesignerScene || (Game.InFlightScene && !Game.Instance.FlightScene.TimeManager.CurrentMode.WarpMode)) && !whipAntenna.animating)
                StartCoroutine(whipAntenna.Toggle(open, animationduration, Data));
        }
    }
}