//The mod uses Harmony https://github.com/pardeike/Harmony Copyright (c) 2017 Andreas Pardeike

namespace Assets.Scripts
{
    using System.Collections.Generic;
    using System.Reflection;
    using Assets.Packages.DevConsole;
    using Assets.Scripts.DroonComlinks.Ui;
    using Assets.Scripts.DroonComLinks;
    using Assets.Scripts.DroonComLinks.Antennas;
    using Assets.Scripts.DroonComLinks.Network;
    using ModApi.Settings.Core.Events;
    using ModApi.Ui.Inspector;
    using UnityEngine;

    public enum LineColorModes { Default, Frequency, SignalStrength }
    public enum DrawModes { None, GsPath, GsPaths, Player, SelectedNode, All }

    public class Mod : ModApi.Mods.GameMod
    {
        public DataManager dataManager = new();
        public string saveFilePath = Application.persistentDataPath + "/UserData/DroonComLinks/";
        public Mod(float linesScaleFactor, float antennaDelay, float radiusScale, float minFPS, bool directivity, bool autoFrequency, LineColorModes lineColorMode, DrawModes drawMode)
        {
            this.linesScaleFactor = linesScaleFactor;
            this.radiusScale = radiusScale;
            this.updateFrequency = minFPS;
            this.Directivity = directivity;
            this.AutoFrequency = autoFrequency;
            this.lineColorMode = lineColorMode;
            this.drawMode = drawMode;
        }
        public float linesScaleFactor = 1f;
        public float radiusScale = 1.006f;
        //public float heightScale = 1f;
        public float updateFrequency = 5f;
        public bool Directivity { get; private set; } = true;
        public bool AutoFrequency { get; private set; } = true;
        private bool commandRegistered = false;
        public float signalStrengthFactor = 0.5f;
        public float maxSignalStrength = 0f;
        public float minSignalStrength = float.MaxValue;
        public float AntennaDelayFactor => (ModSettings.Instance.DelayOverride >= 0 ? ModSettings.Instance.DelayOverride : (int)ModSettings.Instance.Delay.Value) / 100;
        public Color defaultLineColor = new(0.30f, 0.75f, 0.40f);
        public Color[] defaultLineColors = new Color[2] { new(0f, 0.35f, 0.75f), new(0.8f, 0.8f, 0.2f) };
        public LineColorModes lineColorMode = LineColorModes.Default;
        public DrawModes drawMode = DrawModes.GsPaths;
        private Mod() : base() { }
        public static Mod Instance { get; } = GetModInstance<Mod>();
        public double minStrength = float.PositiveInfinity;
        public double maxStrength = float.NegativeInfinity;
        public double averageStrength = 0;
        public int nbrOfStrength = 0;
        public event FloatEventHandler LineScaleFactorChanged;
        public delegate void FloatEventHandler(float value);
        public void InvokeLineScaleFactorChanged() => LineScaleFactorChanged?.Invoke(linesScaleFactor);

        protected override void OnModInitialized()
        {
            base.OnModInitialized();
            DCLUIManager.InitialiseDesignerUI();
            Game.Instance.UserInterface.AddBuildInspectorPanelAction(InspectorIds.FlightView, OnBuildFlightViewInspectorPanel);
            Game.Instance.UserInterface.AddBuildInspectorPanelAction(InspectorIds.MapView, OnBuildMapViewInspectorPanel);
            ModSettings.Instance.Changed += SettingChanged;
            RegisterDevComands();
        }

        private void SettingChanged(object sender, SettingsChangedEventArgs<ModSettings> e)
        {
            ComLinksManager.Instance.ForceRefresh();
            RegisterDevComands();
        }

        private void RegisterDevComands()
        {
            if (ModSettings.Instance.DebugMode && !commandRegistered)
            {
                DevConsoleApi.RegisterCommand("DCLDebug_PrintFrequencyColors", delegate ()
                {
                    foreach (float f in frequencyColors.Keys) Debug.Log("<color=#" + ColorUtility.ToHtmlStringRGB(frequencyColors[f]) + ">- " + f + " Hz");
                });

                DevConsoleApi.RegisterCommand("DCLDebug_PrintSignalStrength", delegate ()
                {
                    Debug.Log("Signal strength info:   min: " + minStrength + "   average: " + averageStrength + "   max: " + maxStrength);
                });

                DevConsoleApi.RegisterCommand("DCLDebug_SetPowerRatio", delegate (string type, float value)
                {
                    AntennaTypes.GetType(type).txPowerRatio = value;
                    ComLinksManager.Instance.ForceRefresh();
                });

                DevConsoleApi.RegisterCommand("DCLDebug_GetPropertyValue", delegate (string propertyName)
                {
                    PropertyInfo property = ComLinksManager.Instance.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField | BindingFlags.Instance | BindingFlags.GetProperty);
                    if (property != null)
                    {
                        if (ModSettings.Instance.DebugMode) Debug.Log(propertyName + " = " + property.GetValue(this).ToString());
                    }
                    else
                    {
                        if (ModSettings.Instance.DebugMode) Debug.LogError("Property [" + propertyName + "] not found");
                    }
                });
                commandRegistered = true;
            }
            else if (!ModSettings.Instance.DebugMode && commandRegistered)
            {
                DevConsoleApi.UnregisterCommand("DCLDebug_PrintFrequencyColors");
                DevConsoleApi.UnregisterCommand("DCLDebug_PrintSignalStrength");
                DevConsoleApi.UnregisterCommand("DCLDebug_SetSensitivity");
                DevConsoleApi.UnregisterCommand("DCLDebug_SetPowerRatio");
                DevConsoleApi.UnregisterCommand("DCLDebug_BenchmarkAntennas");
                DevConsoleApi.UnregisterCommand("DCLDebug_GetPropertyValue");
                commandRegistered = false;
            }
        }

        private void OnBuildFlightViewInspectorPanel(BuildInspectorPanelRequest request)
        {
            var g = new GroupModel("Droon ComLinks");
            request.Model.AddGroup(g);
            //g.Collapsed = true;

            var textButtonModel = new TextButtonModel("Open Status Panel", b => DCLUIManager.OnOpenControlPanelButtonClicked());
            g.Add(textButtonModel);
        }

        private void OnBuildMapViewInspectorPanel(BuildInspectorPanelRequest request)
        {
            GroupModel g = new("Droon ComLinks");
            request.Model.AddGroup(g);
            g.Collapsed = true;

            TextButtonModel textButtonModel2 = new("Open Network Manager", b => DCLUIManager.OnOpenNetworkInfoButtonClicked());
            g.Add(textButtonModel2);
        }

        public Dictionary<float, Color> frequencyColors = new();

        public Color GetFrequencyColor(float frequency)
        {
            if (frequencyColors.ContainsKey(frequency)) return frequencyColors[frequency];
            Color newColor = new();
            while (newColor == new Color() || frequencyColors.ContainsValue(newColor))
            {
                newColor = new Color(UnityEngine.Random.Range(1, 5) * 0.25f, UnityEngine.Random.Range(1, 5) * 0.25f, UnityEngine.Random.Range(1, 5) * 0.25f);
            }
            frequencyColors.Add(frequency, newColor);
            return newColor;
        }
    }
}