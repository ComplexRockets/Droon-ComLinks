using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.DroonComLinks.Antennas;
using Assets.Scripts.DroonComLinks.Network;
using Assets.Scripts.Flight.MapView.UI.Inspector;
using HarmonyLib;
using ModApi.State.MapView;
using ModApi.Ui.Inspector;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Ui.Flight
{
    public class NetworkInfofView
    {
        private Mod Mod => Mod.Instance;
        private IInspectorPanel _inspectorPanel;
        private Dictionary<string, DCLSphere> _spheres = new();
        private DCLSphere _hoveredSphere = null;
        public NetworkNode SelectedNode { get; private set; }
        private ItemVisibilityModel ItemVisibilityModel => Traverse.Create((MapViewInspectorScript)Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.MapViewInspector).Field("_itemVisibilityModel").GetValue<ItemVisibilityModel>();
        private MapItemDataDefaults CraftDefaults => Traverse.Create(ItemVisibilityModel).Field("_craftDefaults").GetValue<MapItemDataDefaults>();
        private MapItemDataDefaults StructureDefaults => Traverse.Create(ItemVisibilityModel).Field("_structureDefaults").GetValue<MapItemDataDefaults>();
        private float _markerScale = 1;
        public bool infoViewActive = false;
        private bool showNodeMarkers = true, connectionMarkers = true;
        private bool craftIconVisible, structureIconVisible;
        public bool InspectoPpanelOpen => _inspectorPanel != null;
        public bool NodeSelected => SelectedNode != null;
        private bool SphereHovered => _hoveredSphere != null;

        public NetworkInfofView()
        {
            ComLinksManager.Instance.NetworkNodesUpdated += OnNetworkNodesUpdated;
        }

        private void OnNetworkNodesUpdated()
        {
            try { SelectedNode.id.ToString(); }
            catch { SelectedNode = null; }
            RefreshNetworkInfoPanel();
        }

        public void RefreshNetworkInfoPanel()
        {
            if (!InspectoPpanelOpen) return;
            CloseInfoView();
            OnToggleNetworkInfoPanel();
        }

        private void CloseInfoView()
        {
            _inspectorPanel.Close();
            _inspectorPanel = null;
        }

        public void OnToggleNetworkInfoPanel()
        {
            if (InspectoPpanelOpen)
            {
                ToggleInfoView(false);
                CloseInfoView();
                return;
            }

            InspectorPanelCreationInfo inspectorPanelCreationInfo = new()
            {
                StartPosition = InspectorPanelCreationInfo.InspectorStartPosition.UpperRight,
                Resizable = true,
                StartOffset = new Vector2(-270f, -90f)
            };
            InspectorModel inspectorModel = new("DCLNetInfo", "Network Info");

            CreateFixedPanel(inspectorModel);
            if (infoViewActive) CreateInfoViewPanel(inspectorModel);
            if (NodeSelected) SelectedNode.CreateInfoPanel(inspectorModel);
            if (ModSettings.Instance.DebugMode) CreateDebugPanel(inspectorModel);

            _inspectorPanel = Game.Instance.UserInterface.CreateInspectorPanel(inspectorModel, inspectorPanelCreationInfo);
            _inspectorPanel.CloseButtonClicked += (IInspectorPanel panel) => OnToggleNetworkInfoPanel();
        }

        private void CreateInfoViewPanel(InspectorModel inspectorModel)
        {
            GroupModel g = inspectorModel.Add(new GroupModel("Info view"));

            g.Add(new ToggleModel("Node Markers", () => showNodeMarkers, v => showNodeMarkers = v));
            g.Add(new ToggleModel("Connection Markers", () => connectionMarkers, v => connectionMarkers = v));
            g.Add(new SliderModel("Marker Scale", () => _markerScale, delegate (float x)
            {
                _markerScale = x;
                DestroySpheres();
                Update();
            }, 0.01f, 5f));
        }

        private void CreateFixedPanel(InspectorModel inspectorModel)
        {
            ComLinksManager.Instance.CreateInfoPanel(inspectorModel);

            inspectorModel.Add(new SpacerModel());

            EnumDropdownModel<DrawModes> drawModeDropdown = new("Draw Mode", () => Mod.Instance.drawMode, "Determines what connection to show when drawing the network");
            drawModeDropdown.ValueChanged += delegate (DrawModes newVal, DrawModes oldVal)
            {
                Mod.Instance.drawMode = newVal;
            };
            inspectorModel.Add(drawModeDropdown);
            drawModeDropdown.ValueChangedByUserInput += delegate { ComLinksManager.Instance.ForceRefresh(); };

            EnumDropdownModel<LineColorModes> lineColorModeDropdown = new("Line Color Mode", () => Mod.Instance.lineColorMode, "Determines how the connection lines should be colored");
            lineColorModeDropdown.ValueChanged += delegate (LineColorModes newVal, LineColorModes oldVal)
            {
                Mod.Instance.lineColorMode = newVal;
            };
            inspectorModel.Add(lineColorModeDropdown);

            TextButtonModel colorButton = new("Reset Freq Colors", b => Mod.Instance.frequencyColors.Clear());
            inspectorModel.Add(colorButton);
            colorButton.DetermineVisibility = () => Mod.Instance.lineColorMode == LineColorModes.Frequency;

            SliderModel lineWidthSlider = new("Line Width", () => Mod.Instance.linesScaleFactor, delegate (float x)
            {
                Mod.Instance.linesScaleFactor = x;
                Mod.Instance.InvokeLineScaleFactorChanged();
            }, 0.01f, 5);
            inspectorModel.Add(lineWidthSlider);

            SliderModel updatefrequencySlider = new("DCL Update Frequency", () => Mod.Instance.updateFrequency, delegate (float x)
                {
                    Mod.Instance.updateFrequency = x;
                }, 0, 60);
            inspectorModel.Add(updatefrequencySlider);
            updatefrequencySlider.ValueFormatter = (float x) => $"{updatefrequencySlider.Value:n2} Hz";
            updatefrequencySlider.OnSliderAdjustmentEnded += delegate { ComLinksManager.Instance.ForceRefresh(); };

            inspectorModel.Add(new SpacerModel());

            inspectorModel.Add(new ToggleModel("Toggle Info view", () => infoViewActive, v => { ToggleInfoView(v); RefreshNetworkInfoPanel(); }));
        }

        private void CreateDebugPanel(InspectorModel inspectorModel)
        {
            GroupModel g = inspectorModel.Add(new GroupModel("Debug Panel"));

            g.Add(new ColorModel("Line Color", () => Mod.defaultLineColor, delegate (Color c)
            {
                Mod.defaultLineColor = c;
            }, false, true, true));

            SliderModel signalStrengthFactorSlider = g.Add(new SliderModel("Strength Factor", () => Mod.signalStrengthFactor, delegate (float x)
            {
                Mod.signalStrengthFactor = x;
            }, 0.0001f, 1f));
            signalStrengthFactorSlider.ValueFormatter = (float x) => $"{signalStrengthFactorSlider.Value:n4}";
            signalStrengthFactorSlider.OnSliderAdjustmentEnded += delegate { ComLinksManager.Instance.ForceRefresh(); };

            SliderModel parabolicTxRatio = g.Add(new SliderModel("Parabolic Tx Ratio", () => AntennaTypes.parabolic.txPowerRatio, delegate (float x)
            {
                AntennaTypes.parabolic.txPowerRatio = (float)x;
            }, 0f, 0.1f));
            parabolicTxRatio.ValueFormatter = (float x) => $"{parabolicTxRatio.Value * 100:n2}%";
            parabolicTxRatio.OnSliderAdjustmentEnded += delegate { ComLinksManager.Instance.ForceRefresh(); };

            SliderModel whipTxRatio = g.Add(new SliderModel("Whip Tx Ratio", () => AntennaTypes.whip.txPowerRatio, delegate (float x)
            {
                AntennaTypes.whip.txPowerRatio = (float)x;
            }, 0f, 0.1f));
            whipTxRatio.ValueFormatter = (float x) => $"{whipTxRatio.Value * 100:n2}%";
            whipTxRatio.OnSliderAdjustmentEnded += delegate { ComLinksManager.Instance.ForceRefresh(); };

            SliderModel dipoleTxRatio = g.Add(new SliderModel("Dipole Tx Ratio", () => AntennaTypes.dipole.txPowerRatio, delegate (float x)
            {
                AntennaTypes.dipole.txPowerRatio = (float)x;
            }, 0f, 0.1f));
            dipoleTxRatio.ValueFormatter = (float x) => $"{dipoleTxRatio.Value * 100:n2}%";
            dipoleTxRatio.OnSliderAdjustmentEnded += delegate { ComLinksManager.Instance.ForceRefresh(); };

            SliderModel patchTxRatio = g.Add(new SliderModel("Patch Tx Ratio", () => AntennaTypes.patch.txPowerRatio, delegate (float x)
            {
                AntennaTypes.patch.txPowerRatio = (float)x;
            }, 0f, 0.1f));
            patchTxRatio.ValueFormatter = (float x) => $"{patchTxRatio.Value * 100:n2}%";
            patchTxRatio.OnSliderAdjustmentEnded += delegate { ComLinksManager.Instance.ForceRefresh(); };

            g.Add(new TextModel("Max Signal Strength", () => Mod.Instance.maxSignalStrength.ToString()));
            g.Add(new TextModel("Min Signal Strength", () => Mod.Instance.minSignalStrength.ToString()));

            g.Add(new TextModel("Average Fps", () => ComLinksManager.Instance.averageFps.ToString()));
            g.Add(new TextModel("Refresh Rate", () => $"{1 / ComLinksManager.Instance.UpdatePeriod:n2} Hz"));
        }

        private void ToggleInfoView(bool active)
        {
            infoViewActive = active;

            if (active)
            {
                craftIconVisible = CraftDefaults.ShowIcons;
                structureIconVisible = StructureDefaults.ShowIcons;
                CraftDefaults.ShowIcons = false;
                StructureDefaults.ShowIcons = false;
            }
            else
            {
                CraftDefaults.ShowIcons = craftIconVisible;
                StructureDefaults.ShowIcons = structureIconVisible;
                SelectedNode = null;
            }
        }

        public void Update()
        {
            if (Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.Visible && infoViewActive && showNodeMarkers) { OnMapViewUpdate(); return; }
            DestroySpheres();
        }

        public void OnMapViewUpdate()
        {
            List<string> spheresCopy = new Dictionary<string, DCLSphere>(_spheres).Keys.ToList();
            foreach (string sphereID in spheresCopy)
                if (ComLinksManager.Instance.GetNetworkNodeFromId(sphereID) == null) { _spheres[sphereID].DestroySphere(); _spheres.Remove(sphereID); }

            foreach (NetworkNode node in ComLinksManager.Instance.NetworkNodes)
                if (_spheres.ContainsKey(node.id)) _spheres[node.id].UpdateSphere(node.Position);
                else _spheres[node.id] = DCLSphere.CreateNew(node.id, node.Position, _markerScale);

            Ray ray = Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 1 << LayerMask.NameToLayer("MapView")) || !hit.transform.name.StartsWith(DCLSphere.prefix))
            {
                if (SphereHovered) _hoveredSphere.SetHilighted(false);
                _hoveredSphere = null;
                if (NodeSelected) _spheres[SelectedNode.id].SetHilighted(true);
                return;
            }

            string hitId = hit.transform.name.Remove(0, DCLSphere.prefix.Length);
            if (!SphereHovered || hitId != _hoveredSphere.id)
            {
                if (SphereHovered) _hoveredSphere.SetHilighted(false);
                _hoveredSphere = _spheres[hitId];
                _hoveredSphere.SetHilighted(true);
            }
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                if (NodeSelected) _spheres[SelectedNode.id].SetHilighted(false);
                SelectedNode = ComLinksManager.Instance.GetNetworkNodeFromId(hitId);
                _spheres[SelectedNode.id].SetHilighted(true);
                RefreshNetworkInfoPanel();
            }
        }

        public void Close()
        {
            ToggleInfoView(false);
            DestroySpheres();
        }

        private void DestroySpheres()
        {
            if (_spheres.Count != 0)
            {
                foreach (DCLSphere sphere in _spheres.Values) sphere.DestroySphere();
                _spheres.Clear();
            }
        }
    }
}