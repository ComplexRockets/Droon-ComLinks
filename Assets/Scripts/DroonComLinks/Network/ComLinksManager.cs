﻿//This mod uses Harmony https://github.com/pardeike/Harmony Copyright (c) 2017 Andreas Pardeike
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.DroonComlinks.Ui;
using Assets.Scripts.DroonComLinks.Antennas;
using Assets.Scripts.DroonComLinks.Controls;
using Assets.Scripts.DroonComLinks.Ui.Flight;
using Assets.Scripts.DroonComLinks.Ui.ListItems;
using Assets.Scripts.Flight;
using Assets.Scripts.Flight.MapView.Interfaces;
using Assets.Scripts.Flight.Sim;
using Assets.Scripts.State;
using Assets.Scripts.Web;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Planet;
using ModApi.Scenes.Events;
using ModApi.Ui;
using ModApi.Ui.Inspector;
using UnityEngine;
using static Assets.Scripts.DroonComLinks.Antennas.AntennaTypes;

namespace Assets.Scripts.DroonComLinks.Network
{
    public class ComLinksManager : MonoBehaviour, IDisplayable
    {
        public static ComLinksManager Instance;
        public string id => "ComLinksManager";
        public event EmptyEventHandler NetworkNodesUpdated;
        public event StringEventHandler NetworkConnectionAdded, NetworkConnectionRemoved;
        public event BoolEventHandler RemoteControlChanged, DroonControlChanged;
        public delegate void StringEventHandler(string data);
        public delegate void BoolEventHandler(bool data);
        public delegate void EmptyEventHandler();
        public bool OnEva { get; private set; }
        private FlightState _flightState;
        public ControlsPatches controlManager;
        public IMapViewCoordinateConverter coordinateConverter;
        public DCLFlightUIScript FlightUI => DCLUIManager.flightUI;
        public NetworkInfofView InfoView => DCLUIManager.infoView;
        private NetworkNode _player;
        public NetworkNode Player => _player;
        public CraftNode PlayerCraftNode => (CraftNode)FlightSceneScript.Instance.CraftNode;
        public IFuelSource PlayerBattery => PlayerCraftNode.CraftScript.ActiveCommandPod.BatteryFuelSource;
        public List<NetworkNode> NetworkNodes { get; private set; } = new();
        private readonly Dictionary<string, NetworkNode> _networkNodeFromId = new();
        private readonly Dictionary<int, NetworkNode> _networkNodeFromCraftNodeId = new();
        private readonly List<NetworkNode> _fixedNetworkNodes = new();
        private readonly Dictionary<NetworkNode, List<NetworkNode>> _pathsToGS = new();
        public List<NetworkConnection> NetworkConnections { get; private set; } = new();
        private readonly Dictionary<string, NetworkConnection> _networkConnectionFromId = new();
        private readonly Dictionary<string, float> comRequests = new();
        private readonly Dictionary<NetworkNode, float> _lastPathsFinding = new();
        public DrawModes DrawMode => Mod.Instance.drawMode;
        public List<PlanetNode> planets = new();
        public bool PlayerIsNetworkNode => _player != null;
        public bool PlayerHasDroonControl => CheckPlayerDroonControl();
        public bool PlayerHasRemoteControl => CheckPlayerRemoteControl();
        public bool PlayerHasControl => PlayerHasDroonControl || PlayerHasRemoteControl;
        private bool _playerHasDroonControl, _playerHasRemoteControl;
        public bool StartMethodCalled { get; set; }
        public bool FlightActive { get; private set; } = false;
        private bool _requestFlightStateUpdate;
        public float UpdatePeriod => 1 / Mod.Instance.updateFrequency;
        private readonly float _comRequestsPeriod = 0.5f;
        private float _lastDroonControlCheck = 0;
        private float _lastError = 0;
        private readonly float _lastFlightStateUpdate = 0;
        private float _lastNetworkUpdate = 0;

        public int Fps => (int)(1 / Time.deltaTime);
        public int averageFps = 0;
        private int _registeredCraftNodeCount;
        public const float minPower = 1;

        protected virtual void Awake()
        {
            if (ModSettings.Instance.DebugMode) Debug.Log("ComLinks Awake");
            Instance = this;

            Game.Instance.SceneManager.SceneLoaded += OnSceneloaded;
            Game.Instance.SceneManager.SceneUnloading += OnSceneUnloading;


            try
            {
                controlManager = new ControlsPatches();
                controlManager.PatchControls();
            }
            catch
            {
                MessageDialogScript messageDialogScript = Game.Instance.UserInterface.CreateMessageDialog(MessageDialogType.Okay);
                messageDialogScript.MessageText = string.Format(DCLUtilities.errorColor + "Harmony seems to be missing. The dependency needs to be installed and enabled for DroonComLinks to work");
                messageDialogScript.OkayClicked += delegate (MessageDialogScript d)
                {
                    d.Close();
                    WebUtility.OpenUrl(Game.SimpleRocketsWebsiteUrl + "/Mods/View/234638/Juno-Harmony");
                };
            }
        }

        private void OnSceneloaded(object sender, SceneEventArgs e)
        {
            if (e.Scene == "Flight")
            {
                if (ModSettings.Instance.DebugMode) Debug.Log("GAME STARTED");
                StartCoroutine(DCLUtilities.CallNextFrame(InitialiseFlight));
            }
            if (e.Scene == "Design")
            {
                //Tech tree test things
                // Debug.Log("DCL designer Parts:");
                // foreach (DesignerPart part in Game.Instance.CachedDesignerParts.Parts)
                // {
                //     Debug.Log("-  part:" + part.Name);
                //     if (part.Mod != null)
                //     {
                //         Debug.Log("-  part:" + part.Name + " mod:" + part.Mod.ModInfo.Name);
                //         if (part.Mod.ModInfo.Name.Contains("Droon Comlinks"))
                //         {
                //             Debug.Log("Showed");
                //             part.ShowInDesigner = true;
                //         }
                //     }
                // }

                // Dictionary<string, TechItem> items = (Dictionary<string, TechItem>)Traverse.Create(Game.Instance.GameState.Career.TechTree).Field("_items").GetValue();
                // foreach (string item in items.Keys)
                // {
                //     Debug.Log("Item : " + item + " " + items[item].Id);
                // }
            }
        }

        private void InitialiseFlight()
        {
            FlightActive = true;
            _flightState = FlightSceneScript.Instance.FlightState;
            coordinateConverter = (IMapViewCoordinateConverter)Game.Instance.FlightScene.ViewManager.MapViewManager.MapView;
            comRequests.Clear();

            try { DCLUIManager.InitializeFlightUI(); } catch (Exception E) { Debug.LogError("Initialize DCL UI error " + E); }
            try { InitialisePlanets(); } catch (Exception E) { Debug.LogError("GetPlanets error " + E); }
            try { InitialiseFixedNodes(); } catch (Exception E) { Debug.LogError("Initialise FixedNodes error " + E); }
            try { FlightStateUpdate(); } catch (Exception E) { Debug.LogError("FlightState Update error " + E); }
            //StartCoroutine(ManageUpdateFrequency());

            Game.Instance.FlightScene.ViewManager.MapViewManager.ForegroundStateChanged += OnMapViewStateChanged;
            Game.Instance.FlightScene.CraftChanged += OnPlayerCraftChanged;
            Game.Instance.FlightScene.ActiveCommandPodChanged += OnPlayerCraftChanged;
        }

        private void InitialiseFixedNodes()
        {
            _fixedNetworkNodes.Clear();
            foreach (PlanetNode planet in planets)
            {
                bool flag = false;
                if (!planet.IsTerrainDataLoaded) { planet.LoadTerrainData(); flag = true; }
                foreach (StructureNodeData structure in planet.PlanetData.StructureNodes)
                {
                    try
                    {
                        Vector3d pos = planet.GetSurfacePosition(structure.Latitude * 0.01745329238474369, structure.Longitude * 0.01745329238474369, structure.ElevationType, structure.Elevation > 1 ? structure.Elevation : 1);
                        if (structure.PrefabPath.Contains("PrimaryLaunchSite") || structure.PrefabPath.Contains("SatelliteDish"))
                        {
                            _fixedNetworkNodes.Add(new NetworkNode(structure.Name + " id: " + _fixedNetworkNodes.Count, pos, planet, this));
                            if (ModSettings.Instance.DebugMode) Debug.Log("ADDED GS at height " + structure.Elevation + " prefab " + structure.PrefabPath);
                        }

                        if (structure.SubStructures.Count > 0) InitialiseSubStructures(structure.SubStructures, planet, pos);
                    }
                    catch (Exception e)
                    {
                        if (ModSettings.Instance.DebugMode) Debug.LogErrorFormat("Error in fixed nodes initialisation for planet: {0}, structure: {1}, latitude: {2}, longitude: {3}, elevation type: {4}, elevation: {5}, {6}",
                            planet.Name ?? "null",
                            structure.Name ?? "null",
                            structure.Latitude,
                            structure.Longitude,
                            structure.ElevationType,
                            structure.Elevation,
                            e);
                    }
                }
                if (flag) planet.UnloadTerrainData();
            }
        }

        private void InitialiseSubStructures(List<SubStructure> subStructures, PlanetNode planet, Vector3d parentPos)
        {
            foreach (SubStructure subStructure in subStructures)
            {
                Vector3d pos = parentPos + subStructure.LocalPosition;
                if (subStructure.PrefabPath.Contains("PrimaryLaunchSite") || subStructure.PrefabPath.Contains("SatelliteDish"))
                {
                    _fixedNetworkNodes.Add(new NetworkNode(subStructure.Name + " id: " + _fixedNetworkNodes.Count, pos, planet, this));
                    if (ModSettings.Instance.DebugMode) Debug.Log("ADDED GS prefab path : " + subStructure.PrefabPath);
                }

                if (subStructure.SubStructures.Count > 0) InitialiseSubStructures((List<SubStructure>)subStructure.SubStructures, planet, pos);
            }
        }

        public void InitialisePlanets()
        {
            planets = new List<PlanetNode> { (PlanetNode)_flightState.RootNode };
            GetPlanetsChildren((PlanetNode)_flightState.RootNode);
        }

        private void GetPlanetsChildren(PlanetNode parent)
        {
            foreach (PlanetNode child in parent.ChildPlanets)
            {
                planets.Add(child);
                if (child.ChildPlanets.Count > 0) GetPlanetsChildren(child);
            }
        }

        private void OnSceneUnloading(object sender, SceneEventArgs e)
        {
            if (e.Scene == "Flight")
            {
                if (ModSettings.Instance.DebugMode) Debug.Log("GAME ENDED");
                DCLUIManager.CloseFlightUI();
                FlightActive = false;
            }
        }

        private void OnMapViewStateChanged(bool mapViewActive)
        {
            if (!mapViewActive) DestroyConnections();
        }

        private void OnPlayerCraftChanged(ICraftNode craftNode) => FlightStateUpdate();

        private void OnCraftNodeChange(CraftNode craftNode) => RequestFlightStateUpdate("On CraftNodeChange");

        private void OnDocked(string craftNodeNameA, string craftNodeNameB) => RequestFlightStateUpdate("On Docked");

        private void OnCraftStructureChanged() => RequestFlightStateUpdate("On CraftStructureChanged");

        private void OnCraftSplited(ICraftScript craftScript) => RequestFlightStateUpdate("On CraftSplited");

        public void RequestFlightStateUpdate(string reason)
        {
            if (!FlightActive) return;
            if (ModSettings.Instance.DebugMode) Debug.Log(reason + " requestFlightStateUpdate");
            _requestFlightStateUpdate = true;
        }

        public void ForceRefresh()
        {
            if (FlightActive) StartCoroutine(DCLUtilities.CallNextFrame(Refresh));
        }

        private void Refresh()
        {
            Mod.Instance.maxSignalStrength = 0;
            Mod.Instance.minSignalStrength = float.MaxValue;
            _pathsToGS.Clear();
            _lastPathsFinding.Clear();
            DestroyConnections();
            foreach (NetworkNode node in NetworkNodes) node.ForceRefresh();
            UpdateNetwork();
            UpdateConnections();
        }

        private void Update()
        {
            if (FlightActive)
            {
                try
                {
                    if (_registeredCraftNodeCount != _flightState.CraftNodes.Count()) RequestFlightStateUpdate("CraftNode count inconsistency");

                    if (_requestFlightStateUpdate && _lastFlightStateUpdate + UpdatePeriod < Time.time) FlightStateUpdate(); //Mega update
                    else if (_lastNetworkUpdate + UpdatePeriod < Time.time) UpdateNetwork(); //Medium update
                    else LightNetworkUpdate(); //Light update

                    //Things that need to be updated anyway
                    if (Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.Visible) DrawNetwork();
                    DCLUIManager.Update();
                }
                catch (Exception e) { Debug.LogError("Error On Update : " + e); ManageError(); }
            }
        }

        private void LightNetworkUpdate(bool networkUpdate = false)
        {
            foreach (NetworkNode node in NetworkNodes) node.StartLightNetworkUpdate();
            if (!networkUpdate) foreach (NetworkNode node in NetworkNodes) node.CheckConnectionsOcclusion();
            foreach (NetworkNode node in NetworkNodes) { node.GetNodesInRelayRange(null); node.TransfertNodesInRelayRange(); }

            UpdateConnections();
            CheckPlayerRemoteControl();
            if (PlayerIsNetworkNode) GetNodePathToGS(_player);
        }

        private void UpdateNetwork()
        {
            _lastNetworkUpdate = Time.time;

            foreach (NetworkNode node in NetworkNodes) node.StartNetworkUpdate();
            foreach (NetworkNode node in NetworkNodes) node.GetNodesInDirectRange();
            LightNetworkUpdate(networkUpdate: true);
        }

        public bool ManageComRequest(string id, bool needsPower = true)
        {
            try
            {
                if (FlightActive)
                {
                    if (Game.Instance.FlightScene.TimeManager.CurrentMode.WarpMode) needsPower = false;
                    if (comRequests.ContainsKey(id) && comRequests[id] + _comRequestsPeriod > Time.time) needsPower = false;

                    //if (id != "FlightControlsUpdate" && id != "LockHeading") Debug.Log("[ComRequest] type: " + id + " needs power: " + needsPower);
                    if (PlayerHasDroonControl) return true;
                    if (PlayerHasRemoteControl && ModSettings.Instance.NeedPower)
                    {
                        double comPowerConsumption = 0;
                        if (needsPower)
                        {
                            comRequests[id] = Time.time;
                            NetworkNode firstNode = GetNodePathToGS(_player)[0];
                            NetworkConnection firstConnection = ConnectionOf(_player, firstNode);
                            comPowerConsumption = firstConnection.TxPower / NetworkConnection.PlayerAntennaConnectingTo(firstNode).AntennaType.txPowerRatio;
                            //Debug.Log("-   power consumption: " + powerConsumption + " Tx power: " + firstConnection.TxPowerFrom(_player) + " power ratio: " + NetworkConnection.InAntenna(_player, firstNode).antennaType.txPowerRatio);
                        }
                        bool enoughPower = PlayerBattery.TotalFuel >= comPowerConsumption;
                        if (needsPower && enoughPower) { PlayerBattery.RemoveFuel(comPowerConsumption); return true; }

                        return enoughPower;
                    }

                    return !ModSettings.Instance.BlockControls;
                }
            }
            catch (Exception e) { Debug.LogError("ManageComRequest Error, id [" + id + "] : " + e); }
            return true;
        }

        private void ManageError()
        {
            if (_lastError + 2 < Time.time)
            {
                ForceRefresh();
                try { FlightStateUpdate(); } catch (Exception E) { Debug.LogError("FlightStateUpdate error " + E); }
                _lastError = Time.time;
            }
        }

        public void FlightStateUpdate()
        {
            _requestFlightStateUpdate = false;
            if (!FlightActive) return;

            bool log = ModSettings.Instance.DebugMode;
            int craftsWithAntenna = 0;
            int craftsWithoutAntenna = 0;
            int craftsWithoutCommandPods = 0;
            _registeredCraftNodeCount = _flightState.CraftNodes.Count();
            NetworkNodes.Clear();
            _networkNodeFromId.Clear();
            _networkNodeFromCraftNodeId.Clear();
            _pathsToGS.Clear();
            _lastPathsFinding.Clear();
            _player = null;
            _flightState.ProcessDestroyedCraftNodes();
            DestroyConnections();


            if (log)
            {
                Debug.Log("");
                Debug.LogWarning("------------- START -------------");
            }

            foreach (CraftNode craftNode in _flightState.CraftNodes)
            {
                if (craftNode.Name == null) { if (log) Debug.LogError("-    CraftNode content null error"); continue; }
                if (log) Debug.Log("Craft : " + craftNode.Name + "   Id : " + craftNode.NodeId);
                XElement craftXml = null;
                List<Antenna> antennas = new();

                if (!craftNode.HasCommandPod) { craftsWithoutCommandPods++; continue; }
                double unloadedCraftBattery = 0;
                try
                {
                    if (craftNode.CraftScript != null)
                    {

                        CraftData craftData = craftNode.CraftScript.Data;
                        foreach (PartData part in craftData.Assembly.Parts)
                        {
                            DCLAntennaScript antennaScript = part.PartScript.GetModifier<DCLAntennaScript>();
                            if (antennaScript != null)
                            {
                                if (log) Debug.Log("-    Antenna found type: " + antennaScript.AntennaData.type.id + ", part id: " + part.Id);
                                antennas.Add(new Antenna(
                                    antennaScript.AntennaData.type.id,
                                    part.GenerateXml(craftNode.GameObject.transform, false),
                                    antennaScript.Data.GenerateStateXml(false),
                                    antennaScript.AntennaData.GenerateStateXml(false),
                                    part.PartScript));
                            }
                            else if (part.Name.StartsWith(AntennaTypes.dclAntennaPrefix))
                            {
                                CustomAntennaResult antennaData = AntennaTypes.ParseCustomAntennaName(part.Name);
                                if (antennaData.error != "") { if (log) Debug.LogError("Error when parsing custom antenna data: " + antennaData.error); continue; }

                                if (log) Debug.Log("Custom antenna found, type : " + antennaData.type.id);
                                XElement antennaModifer, typeModifer;
                                CreateAntennaModifers(antennaData, out antennaModifer, out typeModifer);

                                PartModifierData antennaModiferObj = PartModifierData.CreateFromStateXml(antennaModifer, part, craftData.XmlVersion);
                                PartModifierData typeModiferObj = PartModifierData.CreateFromStateXml(typeModifer, part, craftData.XmlVersion);
                                antennaModiferObj.CreateScript();
                                typeModiferObj.CreateScript();

                                antennas.Add(new Antenna(
                                    antennaData.type.id,
                                    part.GenerateXml(craftNode.GameObject.transform, false),
                                    antennaModifer,
                                    typeModifer,
                                    part.PartScript));
                            }
                        }
                    }
                    else
                    {
                        if (log) Debug.Log("-    Craft script not found searching for craft xml");
                        craftXml = _flightState.LoadCraftXml(craftNode.NodeId);
                        if (craftXml != null)
                        {
                            foreach (XElement partXml in craftXml.Element("Assembly").Element("Parts").Elements())
                            {
                                XElement[] modifers = new XElement[2];
                                foreach (XElement modifer in partXml.Elements())
                                {
                                    if (modifer.Name.ToString().Contains("DroonComLinks."))
                                    {
                                        if (modifer.Name == "DroonComLinks.DCLAntenna") modifers[0] = modifer;
                                        else modifers[1] = modifer;
                                    }
                                    else if (modifer.Name == "FuelTank")
                                    {
                                        if (modifer.Attribute("fuelType")?.Value == "Battery")
                                        {
                                            if (modifer.Attribute("fuel") != null) unloadedCraftBattery += double.Parse(modifer.Attribute("fuel").Value);
                                        }
                                    }
                                }
                                if (modifers[0] != null && modifers[1] != null)
                                {
                                    string type = partXml.Attribute("partType").Value;
                                    if (AntennaTypes.TypeIsValid(type))
                                    {
                                        if (log) Debug.Log("-    Antenna found, type: " + type + ", part id: " + partXml.Attribute("id").Value);
                                        antennas.Add(new Antenna(
                                            type,
                                            partXml,
                                            modifers[0],
                                            modifers[1]));
                                    }
                                }

                                string partName = partXml.Attribute("name")?.Value;
                                if (partName != null && partName.StartsWith(AntennaTypes.dclAntennaPrefix))
                                {
                                    CustomAntennaResult antennaData = AntennaTypes.ParseCustomAntennaName(partName);

                                    if (antennaData.error != "") { if (log) Debug.LogError("Error when parsing custom antenna data: " + antennaData.error); continue; }

                                    if (log) Debug.Log("-    Custom antenna found, type : " + antennaData.type.id + ", part id: " + partXml.Attribute("id").Value);
                                    XElement antennaModifer, typeModifer;
                                    CreateAntennaModifers(antennaData, out antennaModifer, out typeModifer);

                                    partXml.Add(antennaModifer);
                                    partXml.Add(typeModifer);

                                    antennas.Add(new Antenna(
                                        antennaData.type.id,
                                        partXml,
                                        antennaModifer,
                                        typeModifer));
                                }
                            }
                            if (log) Debug.Log("    unloaded craft battery: " + unloadedCraftBattery);
                        }
                    }
                }
                catch (Exception e) { if (log) Debug.LogError("Error on craft " + e); }

                if (antennas.Count == 0) { craftsWithoutAntenna++; continue; }
                craftsWithAntenna++;
                NetworkNodes.Add(new NetworkNode(craftNode, antennas, this, unloadedCraftBattery));
                _networkNodeFromCraftNodeId.Add(craftNode.NodeId, NetworkNodes.Last());
                _networkNodeFromId.Add(NetworkNodes.Last().id, NetworkNodes.Last());
                if (craftNode.IsPlayer)
                {
                    _player = NetworkNodes.Last();
                    _player.isPlayer = true;
                }
            }
            foreach (NetworkNode node in _fixedNetworkNodes)
            {
                NetworkNodes.Add(node);
                _networkNodeFromId.Add(node.id, node);
            }
            if (PlayerCraftNode.CraftScript.Data.Assembly.Parts[0].PartType.Name == "Eva") OnEva = true;
            else OnEva = false;
            CheckPlayerDroonControl(imediate: true);

            if (log)
            {
                Debug.LogWarning("------------- Result -------------");
                Debug.Log(craftsWithAntenna + " Crafts with antenna");
                Debug.Log(craftsWithoutAntenna + " Crafts without antenna");
                Debug.Log(craftsWithoutCommandPods + " Crafts without command pod");
                if (PlayerIsNetworkNode) Debug.Log("Player : " + _player.CraftNode.Name + " part id : " + _player.CraftNode.NodeId);
                else if (OnEva) Debug.Log("Player on eva");
                else Debug.Log("Player not found");
                Debug.LogWarning("-------------- END --------------");
            }

            NetworkNodesUpdated?.Invoke();
            UpdateNetwork();
        }

        private void CreateAntennaModifers(CustomAntennaResult data, out XElement antennaModifer, out XElement typeModifer)
        {
            antennaModifer = new XElement(AntennaTypes.defaultDCLModiferXml);
            typeModifer = new XElement(data.type.defaultModfierXml);

            foreach (string property in data.properties.Keys)
            {
                if (ModSettings.Instance.DebugMode) Debug.Log("Adding property: " + property + " value: " + data.properties[property]);
                typeModifer.Attribute(property)?.SetValue(data.properties[property]);
                antennaModifer.Attribute(property)?.SetValue(data.properties[property]);
            }
        }

        private void UpdateConnections()
        {
            List<NetworkConnection> copy = new(NetworkConnections);
            foreach (NetworkConnection connection in copy)
            {
                if (!connection.NodeA.stringNodesInDirectRange.Contains(connection.NodeB.id))
                {
                    connection.Destroy();
                    NetworkConnections.Remove(connection);
                    _networkConnectionFromId.Remove(connection.id);
                    NetworkConnectionRemoved?.Invoke(connection.id);
                }
            }

            List<NetworkNode> checkedNodes = new();
            foreach (NetworkNode nodeA in NetworkNodes)
            {
                checkedNodes.Add(nodeA);
                //foreach (NetworkNode nodeB in nodeA.GetOptimisedNodesInDirectRange(checkedNodes))
                foreach (NetworkNode nodeB in nodeA.objectNodesInDirectRange)
                {
                    if (!ConnectionExists(nodeA, nodeB))
                    {
                        NetworkConnections.Add(new NetworkConnection(nodeA, nodeB, coordinateConverter));
                        string id = NetworkConnections.Last().id;
                        _networkConnectionFromId.Add(id, NetworkConnections.Last());
                        NetworkConnectionAdded?.Invoke(id);
                    }
                }
            }
        }

        public bool ConnectionExists(NetworkNode nodeA, NetworkNode nodeB) => _networkConnectionFromId.ContainsKey(NetworkConnection.ConnectionID(nodeA.id, nodeB.id));
        public bool ConnectionExists(string connectionId) => _networkConnectionFromId.ContainsKey(connectionId);

        public NetworkConnection ConnectionOf(NetworkNode nodeA, NetworkNode nodeB)
        {
            string connectionId = NetworkConnection.ConnectionID(nodeA.id, nodeB.id);
            if (ConnectionExists(connectionId)) return _networkConnectionFromId[connectionId];
            Debug.LogError("Connection between [" + nodeA.id + "] and [" + nodeB.id + "] doesn't exist");
            return null;
        }

        private void DrawNetwork()
        {
            List<NetworkConnection> notUpdatedConnections = new(NetworkConnections);

            switch (DrawMode)
            {
                case DrawModes.All:
                    NetworkConnections.ForEach(c => c.UpdateConnection());
                    notUpdatedConnections.Clear();
                    break;

                case DrawModes.GsPaths:
                    UpdatePathsToGS(NetworkNodes);
                    foreach (NetworkNode node in _pathsToGS.Keys)
                        if (_pathsToGS[node].Count != 0 && ConnectionExists(node, _pathsToGS[node][0]))
                        {
                            NetworkConnection connection = ConnectionOf(node, _pathsToGS[node][0]);
                            connection.UpdateConnection();
                            notUpdatedConnections.Remove(connection);
                        }
                    break;

                case DrawModes.Player:
                    if (!PlayerIsNetworkNode) break;
                    List<NetworkConnection> copy1 = new(NetworkConnections);
                    foreach (NetworkConnection connection in copy1)
                        if (connection.Connects(_player))
                        {
                            connection.UpdateConnection();
                            notUpdatedConnections.Remove(connection);
                        }
                    break;

                case DrawModes.SelectedNode:
                    if (DCLUIManager.infoView == null || !DCLUIManager.infoView.NodeSelected) { DCLUtilities.ShowMapViewMessage("No selected node\nUse the Info View to select one", error: true); ; break; }
                    List<NetworkConnection> copy2 = new(NetworkConnections);
                    foreach (NetworkConnection connection in copy2)
                        if (connection.Connects(DCLUIManager.infoView.SelectedNode))
                        {
                            connection.UpdateConnection();
                            notUpdatedConnections.Remove(connection);
                        }
                    break;

                case DrawModes.GsPath:
                    if (!PlayerIsNetworkNode) break;
                    List<NetworkNode> path = GetNodePathToGS(_player);
                    NetworkNode prevNode = _player;
                    foreach (NetworkNode node in path)
                    {
                        if (ConnectionExists(prevNode, node))
                        {
                            NetworkConnection connection = ConnectionOf(prevNode, node);
                            connection.UpdateConnection();
                            notUpdatedConnections.Remove(connection);
                        }
                        else Debug.LogError("Connection between [" + prevNode.id + "] and [" + node.id + "] doesn't exist");
                        prevNode = node;
                    }
                    break;
            }

            foreach (NetworkConnection connection in notUpdatedConnections)
            {
                connection.Destroy();
            }
        }

        private void DestroyConnections()
        {
            foreach (NetworkConnection connection in NetworkConnections) connection.Destroy();
            NetworkConnections.Clear();
            _networkConnectionFromId.Clear();
        }

        public List<NetworkNode> GetNodePathToGS(NetworkNode node)
        {
            UpdatePathsToGS(new List<NetworkNode> { node });
            if (_pathsToGS.ContainsKey(node)) return _pathsToGS[node];
            else return new List<NetworkNode>();
        }

        private void UpdatePathsToGS(List<NetworkNode> nodes)
        {
            Dictionary<NetworkNode, List<NetworkNode>> paths = new();

            foreach (NetworkNode node in nodes)
            {
                if (node == null)
                {
                    if (ModSettings.Instance.DebugMode) Debug.LogError("Update paths to GS error, node null");
                    continue;
                }
                if (node.IsCraft)
                {
                    List<NetworkNode> list = paths.Values.ToList().Find(l => l.Contains(node));
                    if (list == null)
                    {
                        paths.Add(node, GetPathToGS(node));
                    }
                    else
                    {
                        List<NetworkNode> newList = new(list);
                        newList.RemoveRange(0, list.IndexOf(node) + 1);
                        paths.Add(node, newList);
                    }
                }
            }
            foreach (NetworkNode node in paths.Keys)
            {
                if (_pathsToGS.ContainsKey(node)) _pathsToGS[node] = paths[node];
                else _pathsToGS.Add(node, paths[node]);
            }
        }

        private List<NetworkNode> GetPathToGS(NetworkNode initialNode)
        {
            if (_lastPathsFinding.ContainsKey(initialNode) && _lastPathsFinding[initialNode] + UpdatePeriod > Time.time && _pathsToGS.ContainsKey(initialNode))
                return _pathsToGS[initialNode];
            _lastPathsFinding[initialNode] = Time.time;

            if (!initialNode.GsInRange) return new List<NetworkNode>();

            List<NetworkNode> _nodesInRelayRange = new(initialNode.nodesInRelayRange);
            List<NetworkNode> _nodesInDirectRange = new(initialNode.objectNodesInDirectRange);
            List<NetworkNode> _nodesToCheck = new(_nodesInDirectRange);

            //If there is a ground station in direct range, return the closest one
            if (initialNode.GsInDirectRange) return new List<NetworkNode>() { initialNode.nodesInDirectRange.FindAll(n => !n.node.IsCraft).OrderBy(n => n.distance).First().node };
            //Debug.Log ("GS is not in direct range");

            NetworkNode[] nodes = _nodesInRelayRange.ToArray();
            //Debug.Log ("_nodesInRelayRange: " + _nodesInRelayRange.Count);
            float[] distances = Enumerable.Repeat(float.PositiveInfinity, nodes.Count()).ToArray(); ;
            NetworkNode[] prevNodes = new NetworkNode[nodes.Count()];

            foreach (InRangeNode node in initialNode.nodesInDirectRange)
            {
                int nodeIndex = Array.IndexOf(nodes, node.node);
                distances[nodeIndex] = (float)node.distance;
                prevNodes[nodeIndex] = initialNode;

            }
            //Debug.Log ("Done with directrange");

            while (_nodesToCheck.Count > 0)
            {
                NetworkNode currentNode = _nodesToCheck[0];
                int currentNodeIndex = Array.IndexOf(nodes, currentNode);

                foreach (InRangeNode inRangeNode in currentNode.nodesInDirectRange)
                {
                    NetworkNode node = inRangeNode.node;

                    if (_nodesInRelayRange.Contains(node))
                    {
                        if (!_nodesToCheck.Contains(node)) { _nodesToCheck.Add(node); }
                        int nodeIndex = Array.IndexOf(nodes, node);
                        float newDistance = (float)(distances[currentNodeIndex] + inRangeNode.distance);
                        if (distances[nodeIndex] > newDistance)
                        {
                            distances[nodeIndex] = newDistance;
                            prevNodes[nodeIndex] = currentNode;
                        }
                    }
                }

                _nodesInRelayRange.Remove(currentNode);
                _nodesToCheck.RemoveAt(0);
            }
            //Debug.Log ("EndLoop");

            NetworkNode[] sorted = new NetworkNode[nodes.Length];
            Array.Copy(nodes, sorted, nodes.Length);
            Array.Sort(distances, sorted);
            NetworkNode closestGS = Array.Find(sorted, node => !node.IsCraft);

            //Debug.Log ("Building path");
            List<NetworkNode> path = new();
            NetworkNode prevNode = closestGS;
            while (prevNode != initialNode)
            {
                path.Add(prevNode);
                //Debug.Log ("PrevNode: " + prevNode.name);
                try { prevNode = prevNodes[Array.IndexOf(nodes, prevNode)]; } catch (Exception e) { if (ModSettings.Instance.DebugMode) Debug.Log("PrevNode not found at index " + Array.IndexOf(nodes, prevNode) + ", Nodes Count: " + nodes.Count() + ", PrevNodes Count: " + prevNodes.Count() + ", error: " + e); path.Reverse(); break; }
                if (prevNode == null)
                {
                    //Debug.LogError ("PrevNodeNull");
                    break;
                }
            }
            //Debug.Log ("Path count : " + path.Count);

            path.Reverse();
            // Debug.LogWarning ("---- Path Found ----");
            // Debug.Log ("Initial Node : " + initialNode.name);
            // foreach (NetworkNode node in path) {
            //     Debug.Log ("-  Node : " + node.name);
            // }
            return path;
        }

        private bool CheckPlayerDroonControl(bool imediate = false)
        {
            if (!imediate && _lastDroonControlCheck + UpdatePeriod > Time.time) return _playerHasDroonControl;
            _lastDroonControlCheck = Time.time;

            if (OnEva)
            {
                if (_playerHasDroonControl == false)
                {
                    _playerHasDroonControl = true;
                    DroonControlChanged?.Invoke(true);
                }
                return true;
            }

            foreach (CrewCompartmentData partData in PlayerCraftNode.CraftScript.Data.Assembly.GetModifiers<CrewCompartmentData>())
            {
                if (partData.Script.Crew.Count > 0)
                {
                    if (_playerHasDroonControl == false)
                    {
                        _playerHasDroonControl = true;
                        DroonControlChanged?.Invoke(true);
                    }
                    return true;
                }
            }

            if (_playerHasDroonControl == true)
            {
                _playerHasDroonControl = false;
                DroonControlChanged?.Invoke(false);
            }
            return false;
        }

        private bool CheckPlayerRemoteControl()
        {
            if (_player == null)
            {
                if (_playerHasRemoteControl != false)
                {
                    _playerHasRemoteControl = false;
                    RemoteControlChanged?.Invoke(false);
                }
                return false;
            }

            if (_playerHasRemoteControl != _player.GsInRange)
            {
                _playerHasRemoteControl = _player.GsInRange;
                RemoteControlChanged?.Invoke(_player.GsInRange);
            }
            return _playerHasRemoteControl;
        }

        public NetworkNode GetNetworkNodeFromId(string nodeId)
        {
            NetworkNode node = _networkNodeFromId[nodeId];
            if (node == null)
            {
                Debug.LogError("NetworkNode" + nodeId + " not found,     (GetNetworkNodeFromId)");
                return null;
            }
            return node;
        }

        public NetworkConnection GetNetworkConnectionFromId(string connectionId)
        {
            NetworkConnection node = _networkConnectionFromId[connectionId];
            if (node == null)
            {
                Debug.LogError("NetworkConnection" + connectionId + " not found,     (GetNetworkConnectionFromId)");
                return null;
            }
            return node;
        }

        public NetworkNode GetNetworkNodeFromCraftId(int craftId)
        {
            NetworkNode node = _networkNodeFromCraftNodeId[craftId];
            if (node == null)
            {
                Debug.LogError("NetworkNode for craft node " + craftId + " not found,     (GetNetworkNodeFromCraftId)");
                return null;
            }
            return node;
        }

        public Antenna GetAntennaFromId(int craftId, int partId)
        {

            NetworkNode node = GetNetworkNodeFromCraftId(craftId);
            if (node == null) return null;

            Antenna antenna = node.GetAntennaFromPartId(partId);
            if (antenna == null) return null;

            return antenna;
        }

        public void CreateInfoPanel(InspectorModel i)
        {
            i.Add(new TextModel("Network Nodes", () => NetworkNodes.Count.ToString()));
            i.Add(new TextModel("Network Connections", () => NetworkConnections.Count.ToString()));
            i.Add(new TextModel("Player Has Remote Control", () => DCLUtilities.YesNo(PlayerHasRemoteControl)));
            i.Add(new TextModel("Player Has Droon Control", () => DCLUtilities.YesNo(PlayerHasDroonControl)));
        }
        private IUIListItem[,] info;
        public IUIListItem[,] GetInfo()
        {
            if (info == null)
            {
                info = new IUIListItem[4, 4];
                info[0, 0] = new UIListTextValue<int>("Network Nodes", () => NetworkNodes.Count, null);
                info[0, 1] = new UIListTextValue<int>("Network Connections", () => NetworkConnections.Count, null);
                info[1, 0] = PlayerIsNetworkNode ? new UIListLabelButton<string>("Player", () => _player.id, null) : new UIListTextValue<string>("Player", () => PlayerCraftNode.Name, null);
                info[1, 1] = new UIListTextValue<bool>("Has Remote Control", () => PlayerHasRemoteControl, null);
                info[1, 2] = new UIListTextValue<bool>("Has Droon Control", () => PlayerHasDroonControl, null);
                info[3, 0] = new UIListTextValue<string>("Refresh Rate", () => $"{1 / UpdatePeriod:n2} Hz", null);
                info[3, 1] = new UIListTextValue<int>("Average Fps", () => averageFps, null);
            }
            return info;
        }
    }
}