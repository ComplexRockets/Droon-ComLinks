namespace Assets.Scripts {
    using System.Collections.Generic;
    using Assets.Scripts.DroonComLinks;
    using Assets.Scripts.Ui.Flight;
    using ModApi.Ui.Inspector;
    using UnityEngine;

    public class Mod : ModApi.Mods.GameMod {
        public ComLinksManager ComLinksManager;
        private float _linesScaleFactor = 1f, _red = 0.30f, _green = 0.75f, _blue = 0.40f, _antennaDelay = 0f, _signalStrengthFactor = 1f, _radiusScale = 0.995f, _updatefrenquency = 0.02f, _gainConstant = 0.3f;
        public float lineScale => _linesScaleFactor;
        public float gainConstant => _gainConstant * 100000;
        public Color defaultLineColor => new Color (_red, _green, _blue);
        public float radiusScale => _radiusScale;
        public float updateFrequency => _updatefrenquency;
        public float signalStrengthFactor => _signalStrengthFactor;
        public float antennaDelay => _antennaDelay;
        private int _maxConnections = 0;
        public int maxConnections => _maxConnections;
        public string drawMode => _drawMode;
        private string _drawMode = "GS Path";
        public string[] drawModes = { "None", "GS Path", "GS Paths", "Craft Only", "All" };
        public string lineColorMode => _lineColorMode;
        private string _lineColorMode = "None";
        public string[] lineColorModes = { "None", "Signal Strength", "Frequency" };
        private bool _logaritmicValues = false;
        public bool logaritmicValues => _logaritmicValues;
        private Mod () : base () { }
        public static Mod Instance { get; } = GetModInstance<Mod> ();
        protected override void OnModInitialized () {
            base.OnModInitialized ();
            Game.Instance.UserInterface.AddBuildInspectorPanelAction (ModApi.Ui.Inspector.InspectorIds.FlightView, OnBuildFlightViewInspectorPanel);
            Game.Instance.UserInterface.AddBuildInspectorPanelAction (ModApi.Ui.Inspector.InspectorIds.MapView, OnBuildMapViewInspectorPanel);
        }

        private void OnBuildFlightViewInspectorPanel (BuildInspectorPanelRequest request) {
            var g = new GroupModel ("Droon ComLinks");
            request.Model.AddGroup (g);
            //g.Collapsed = true;

            var textButtonModel = new TextButtonModel ("Open Status Panel", b => droonComLinksUI.OnOpenPanelButtonClicked ());
            g.Add (textButtonModel);

            SliderModel delaySlider = new SliderModel ("(Debug) Delay", () => _antennaDelay, delegate (float x) {
                _antennaDelay = x;
            }, 0, 50);
            g.Add (delaySlider);
            delaySlider.ValueFormatter = ((float x) => $"{delaySlider.Value:n2}s");
        }

        private void OnBuildMapViewInspectorPanel (BuildInspectorPanelRequest request) {
            GroupModel g = new GroupModel ("DCL Map Options");
            request.Model.AddGroup (g);
            g.Collapsed = true;

            TextButtonModel textButtonModel = new TextButtonModel ("Open Status Panel", b => droonComLinksUI.OnOpenPanelButtonClicked ());
            g.Add (textButtonModel);

            TextButtonModel colorButton = new TextButtonModel ("Reset Colors", b => waveLengthColors.Clear ());
            g.Add (colorButton);

            // ToggleModel logaritmicButton = new ToggleModel ("Logaritmic", () => _logaritmicValues, delegate (bool x) {
            //     _logaritmicValues = x;
            // });
            // g.Add (logaritmicButton);

            SliderModel lineWidthSlider = new SliderModel ("Lines Width", () => _linesScaleFactor, delegate (float x) {
                _linesScaleFactor = x;
            }, 0.01f, 5);
            g.Add (lineWidthSlider);

            DropdownModel drawModeDropdown = new DropdownModel ("Draw Mode", () => _drawMode, value => _drawMode = value, drawModes);
            g.Add (drawModeDropdown);

            DropdownModel lineColorModeDropdown = new DropdownModel ("Color Filter", () => _lineColorMode, value => _lineColorMode = value, lineColorModes);
            g.Add (lineColorModeDropdown);

            SliderModel redSlider = new SliderModel ("(Debug) Red", () => _red, delegate (float x) {
                _red = x;
            });
            g.Add (redSlider);
            redSlider.ValueFormatter = ((float x) => $"{redSlider.Value:n2}");

            SliderModel greenSlider = new SliderModel ("(Debug) Green", () => _green, delegate (float x) {
                _green = x;
            });
            g.Add (greenSlider);
            greenSlider.ValueFormatter = ((float x) => $"{greenSlider.Value:n2}");

            SliderModel blueSlider = new SliderModel ("(Debug) Blue", () => _blue, delegate (float x) {
                _blue = x;
            });
            g.Add (blueSlider);
            blueSlider.ValueFormatter = ((float x) => $"{blueSlider.Value:n2}");

            SliderModel maxConnectionsSlider = new SliderModel ("(Debug) Max Connections", () => _maxConnections, delegate (float x) {
                _maxConnections = (int) x;
            }, 0, 10);
            g.Add (maxConnectionsSlider);
            maxConnectionsSlider.ValueFormatter = ((float x) => $"{maxConnectionsSlider.Value:n2}");

            SliderModel rangeSlider = new SliderModel ("(Debug) Range Factor", () => _signalStrengthFactor, delegate (float x) {
                _signalStrengthFactor = (float) x;
            }, 0, 10);
            g.Add (rangeSlider);

            SliderModel updatefrenquencySlider = new SliderModel ("(Debug) Update Period", () => _updatefrenquency, delegate (float x) {
                _updatefrenquency = (float) x;
            }, 0, 1);
            g.Add (updatefrenquencySlider);
            updatefrenquencySlider.ValueFormatter = ((float x) => $"{updatefrenquencySlider.Value:n2}s");

            SliderModel gainConstantSlider = new SliderModel ("(Debug) Gain Constant", () => _gainConstant, delegate (float x) {
                _gainConstant = (float) x;
            }, 0, 2);
            g.Add (gainConstantSlider);
            gainConstantSlider.ValueFormatter = ((float x) => $"{gainConstantSlider.Value:n2}");

            SliderModel radiusScaleSlider = new SliderModel ("(Debug) Radius scale", () => _radiusScale, delegate (float x) {
                _radiusScale = (float) x;
            }, 0.5f, 1);
            g.Add (radiusScaleSlider);
            radiusScaleSlider.ValueFormatter = ((float x) => $"{(radiusScaleSlider.Value*100):n2}%");

            drawModeDropdown.ValueChangedByUserInput += delegate {  ComLinksManager.ForceRefresh (); };
            maxConnectionsSlider.OnSliderAdjustmentEnded += delegate { ComLinksManager.ForceRefresh (); };
            rangeSlider.OnSliderAdjustmentEnded += delegate { ComLinksManager.ForceRefresh (); };
            updatefrenquencySlider.OnSliderAdjustmentEnded += delegate { ComLinksManager.ForceRefresh (); };
            gainConstantSlider.OnSliderAdjustmentEnded += delegate { ComLinksManager.ForceRefresh (); };
            radiusScaleSlider.OnSliderAdjustmentEnded += delegate { ComLinksManager.ForceRefresh (); };
        }

        public Dictionary<float, Color> waveLengthColors = new Dictionary<float, Color> ();

        public Color GetWaveLengthColor (float waveLength) {
            if (waveLengthColors.ContainsKey (waveLength)) return waveLengthColors[waveLength];
            Color newColor = new Color ();
            while (newColor == new Color () || waveLengthColors.ContainsValue (newColor)) {
                newColor = new Color (UnityEngine.Random.Range (1, 5) * 0.25f, UnityEngine.Random.Range (1, 5) * 0.25f, UnityEngine.Random.Range (1, 5) * 0.25f);
            }
            waveLengthColors.Add (waveLength, newColor);
            return newColor;
        }
    }
}