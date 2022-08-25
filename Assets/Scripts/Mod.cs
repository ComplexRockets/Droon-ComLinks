//The mod uses Harmony https://github.com/pardeike/Harmony Copyright (c) 2017 Andreas Pardeike

namespace Assets.Scripts
{
    using System.Collections.Generic;
    using Assets.Packages.DevConsole;
    using Assets.Scripts.DroonComLinks;
    using Assets.Scripts.Ui;
    using ModApi.Settings.Core.Events;
    using ModApi.Ui.Inspector;
    using UnityEngine;

    public enum LineColorModes { Default, Frequency, LinkDirection, SignalStrength }
    public enum DrawModes { None, GsPath, GsPaths, CraftOnly, All }

    public class Mod : ModApi.Mods.GameMod
    {
        public ComLinksManager comLinksManager;
        public DataManager dataManager = new DataManager();
        public string saveFilePath = Application.persistentDataPath + "/UserData/DroonComLinks/";
        public Mod(float linesScaleFactor, float sphereScaleFactor, float conesScaleFactor, float antennaDelay, float radiusScale, float heightScale, float minFPS, bool directivity, bool autoFrequency, LineColorModes lineColorMode, DrawModes drawMode)
        {
            this._linesScaleFactor = linesScaleFactor;
            this.sphereScaleFactor = sphereScaleFactor;
            this.conesScaleFactor = conesScaleFactor;
            this.radiusScale = radiusScale;
            this.heightScale = heightScale;
            this.updateFrequency = minFPS;
            this.directivity = directivity;
            this.autoFrequency = autoFrequency;
            this.lineColorMode = lineColorMode;
            this.drawMode = drawMode;
        }
        private float _linesScaleFactor = 1f;
        public float linesScaleFactor => _linesScaleFactor * 3;
        public float sphereScaleFactor { get; private set; } = 0f;
        public float conesScaleFactor { get; private set; } = 1f;
        public float radiusScale { get; private set; } = 1.006f;
        public float heightScale { get; private set; } = 1f;
        public float updateFrequency { get; private set; } = 5f;
        public bool directivity { get; private set; } = true;
        public bool autoFrequency { get; private set; } = true;
        private bool commandRegistered = false;
        private int _signalStrengthFactor = 10000;
        public int signalStrengthFactor => _signalStrengthFactor;
        public float antennaDelayFactor => (ModSettings.Instance.delayOverride >= 0 ? ModSettings.Instance.delayOverride : (int)ModSettings.Instance.delay.Value) / 100;
        public Color defaultLineColor = new Color(0.30f, 0.75f, 0.40f);
        public Color[] defaultLineColors = new Color[2] { new Color(0f, 0.35f, 0.75f), new Color(0.8f, 0.8f, 0.2f) };
        public LineColorModes lineColorMode { get; private set; } = LineColorModes.Default;
        public DrawModes drawMode { get; private set; } = DrawModes.GsPaths;
        private Mod() : base() { }
        public static Mod Instance { get; } = GetModInstance<Mod>();
        public double minStrength = float.PositiveInfinity;
        public double maxStrength = float.NegativeInfinity;
        public double averageStrength = 0;
        public int nbrOfStrength = 0;
        public event FloatEventHandler LineScaleFactorChanged;
        public delegate void FloatEventHandler(float value);

        protected override void OnModInitialized()
        {
            base.OnModInitialized();
            dataManager.Initialise();
            DCLUIManager.InitialiseDesignerUI();
            Game.Instance.UserInterface.AddBuildInspectorPanelAction(ModApi.Ui.Inspector.InspectorIds.FlightView, OnBuildFlightViewInspectorPanel);
            Game.Instance.UserInterface.AddBuildInspectorPanelAction(ModApi.Ui.Inspector.InspectorIds.MapView, OnBuildMapViewInspectorPanel);
            ModSettings.Instance.Changed += SettingChanged;
            RegisterDevComands();
        }

        private void SettingChanged(object sender, SettingsChangedEventArgs<ModSettings> e)
        {
            comLinksManager.ForceRefresh();
            RegisterDevComands();
        }

        private void RegisterDevComands()
        {
            if (ModSettings.Instance.debugMode && !commandRegistered)
            {
                DevConsoleApi.RegisterCommand("DCLDebug_PrintFrequencyColors", delegate ()
                {
                    foreach (float f in frequencyColors.Keys) Debug.Log("<color=#" + ColorUtility.ToHtmlStringRGB(frequencyColors[f]) + ">- " + f + " Hz");
                });

                DevConsoleApi.RegisterCommand("DCLDebug_PrintSignalStrength", delegate ()
                {
                    Debug.Log("Signal strength info:   min: " + minStrength + "   average: " + averageStrength + "   max: " + maxStrength);
                });

                DevConsoleApi.RegisterCommand("DCLDebug_SetSensitivity", delegate (string type, float value)
                {
                    AntennaTypes.GetType(type).sensitivityConstant = value;
                    comLinksManager.ForceRefresh();
                });

                DevConsoleApi.RegisterCommand("DCLDebug_SetPowerRatio", delegate (string type, float value)
                {
                    AntennaTypes.GetType(type).txPowerRatio = value;
                    comLinksManager.ForceRefresh();
                });

                DevConsoleApi.RegisterCommand("DCLDebug_BenchmarkAntennas", delegate ()
                {
                    // foreach (IAntennaType type in AntennaTypes.types)
                    // {
                    //     float txPower = type.defaultMaxPowerConsumption * type.txPowerRatio;
                    //     float wl = 1 / type.defaultFrequency;
                    //     float gain;

                    //     if (type == AntennaTypes.groundStation) gain = 30;
                    //     else if (type == AntennaTypes.parametricalParabolic) gain = AntennaMath.GetParabolicGain(AntennaMath.GetParabolicFocalLength(2f, 0.1f), 2f, wl);
                    //     else gain = AntennaMath.GetGain(type, wl, 1, type.defaultEfficiency);

                    //     Debug.Log("Type " + type.id + " raiting to GS: " + (int)(AntennaMath.GetReceivedPower(txPower, 30, gain, wl, 10000) * 100000000000000) + " raiting to identical antenna: " + (int)(AntennaMath.GetReceivedPower(txPower, gain, gain, wl, 10000) * 1000000000000));
                    // }
                    comLinksManager.BenchmarkNetwork();
                });
                commandRegistered = true;
            }
            else if (!ModSettings.Instance.debugMode && commandRegistered)
            {
                DevConsoleApi.UnregisterCommand("DCLDebug_PrintFrequencyColors");
                DevConsoleApi.UnregisterCommand("DCLDebug_PrintSignalStrength");
                DevConsoleApi.UnregisterCommand("DCLDebug_SetSensitivity");
                DevConsoleApi.UnregisterCommand("DCLDebug_SetPowerRatio");
                DevConsoleApi.UnregisterCommand("DCLDebug_BenchmarkAntennas");
                commandRegistered = false;
            }
        }

        private void OnBuildFlightViewInspectorPanel(BuildInspectorPanelRequest request)
        {
            var g = new GroupModel("Droon ComLinks");
            request.Model.AddGroup(g);
            //g.Collapsed = true;

            var textButtonModel = new TextButtonModel("Open Status Panel", b => DCLUIManager.OnOpenPanelButtonClicked());
            g.Add(textButtonModel);
        }

        private void OnBuildMapViewInspectorPanel(BuildInspectorPanelRequest request)
        {
            GroupModel g = new GroupModel("DCL Map Options");
            request.Model.AddGroup(g);
            g.Collapsed = true;

            TextButtonModel textButtonModel = new TextButtonModel("Open Status Panel", b => DCLUIManager.OnOpenPanelButtonClicked());
            g.Add(textButtonModel);

            EnumDropdownModel<DrawModes> drawModeDropdown = new EnumDropdownModel<DrawModes>("Draw Mode", () => drawMode, "Determines what connection to show when drawing the network");
            drawModeDropdown.ValueChanged += delegate (DrawModes newVal, DrawModes oldVal)
            {
                drawMode = newVal;
            };
            g.Add(drawModeDropdown);

            EnumDropdownModel<LineColorModes> lineColorModeDropdown = new EnumDropdownModel<LineColorModes>("Line Color Mode", () => lineColorMode, "Determines how the connection lines should be colored");
            lineColorModeDropdown.ValueChanged += delegate (LineColorModes newVal, LineColorModes oldVal)
            {
                lineColorMode = newVal;
            };
            g.Add(lineColorModeDropdown);

            TextButtonModel colorButton = new TextButtonModel("Reset Freq Colors", b => frequencyColors.Clear());
            g.Add(colorButton);
            colorButton.DetermineVisibility = () => lineColorMode == LineColorModes.Frequency;

            SliderModel lineWidthSlider = new SliderModel("Line Width", () => _linesScaleFactor, delegate (float x)
            {
                _linesScaleFactor = x;
                LineScaleFactorChanged?.Invoke(_linesScaleFactor);
            }, 0.01f, 5);
            g.Add(lineWidthSlider);

            SliderModel updatefrenquencySlider = new SliderModel("DCL Update Frequency", () => updateFrequency, delegate (float x)
                {
                    updateFrequency = (float)x;
                }, 0, 60);
            g.Add(updatefrenquencySlider);
            updatefrenquencySlider.ValueFormatter = ((float x) => $"{updatefrenquencySlider.Value:n2} Hz");

            SliderModel sphereScaleSlider = new SliderModel("(Debug) Sphere Scale", () => sphereScaleFactor, delegate (float x)
            {
                sphereScaleFactor = x;
            }, 0.01f, 5f);
            g.Add(sphereScaleSlider);
            sphereScaleSlider.DetermineVisibility = () => ModSettings.Instance.debugMode;

            ColorModel colorSelector = new ColorModel("(Debug) Color", () => defaultLineColor, delegate (Color c)
            {
                defaultLineColor = c;
            }, false, true, true);
            g.Add(colorSelector);
            colorSelector.DetermineVisibility = () => ModSettings.Instance.debugMode;

            SliderModel signalStrengthFactorSlider = new SliderModel("(Debug) Strength Factor", () => _signalStrengthFactor, delegate (float x)
            {
                _signalStrengthFactor = (int)x;
            }, 100, 1000000);
            g.Add(signalStrengthFactorSlider);
            signalStrengthFactorSlider.ValueFormatter = ((float x) => $"{signalStrengthFactorSlider.Value:n2}");
            signalStrengthFactorSlider.DetermineVisibility = () => ModSettings.Instance.debugMode;

            SliderModel radiusScaleSlider = new SliderModel("(Debug) Radius Scale", () => radiusScale, delegate (float x)
            {
                radiusScale = (float)x;
            }, 0.8f, 1.2f);
            g.Add(radiusScaleSlider);
            radiusScaleSlider.ValueFormatter = ((float x) => $"{(radiusScaleSlider.Value * 100):n2}%");
            radiusScaleSlider.DetermineVisibility = () => ModSettings.Instance.debugMode;

            SliderModel heightScaleSlider = new SliderModel("(Debug) GS Height Scale", () => heightScale, delegate (float x)
            {
                heightScale = (float)x;
            }, 1f, 1.05f);
            g.Add(heightScaleSlider);
            heightScaleSlider.ValueFormatter = ((float x) => $"{(heightScaleSlider.Value * 100):n2}%");
            heightScaleSlider.DetermineVisibility = () => ModSettings.Instance.debugMode;

            updatefrenquencySlider.OnSliderAdjustmentEnded += delegate { comLinksManager.ForceRefresh(); };
            signalStrengthFactorSlider.OnSliderAdjustmentEnded += delegate { comLinksManager.ForceRefresh(); };
            radiusScaleSlider.OnSliderAdjustmentEnded += delegate { comLinksManager.ForceRefresh(); };
            heightScaleSlider.OnSliderAdjustmentEnded += delegate { comLinksManager.ForceRefresh(); };
            drawModeDropdown.ValueChangedByUserInput += delegate { comLinksManager.ForceRefresh(); };
        }

        public Dictionary<float, Color> frequencyColors = new Dictionary<float, Color>();

        public Color GetFrequencyColor(float frequency)
        {
            if (frequencyColors.ContainsKey(frequency)) return frequencyColors[frequency];
            Color newColor = new Color();
            while (newColor == new Color() || frequencyColors.ContainsValue(newColor))
            {
                newColor = new Color(UnityEngine.Random.Range(1, 5) * 0.25f, UnityEngine.Random.Range(1, 5) * 0.25f, UnityEngine.Random.Range(1, 5) * 0.25f);
            }
            frequencyColors.Add(frequency, newColor);
            return newColor;
        }
    }
}