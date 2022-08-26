//The mod uses Harmony https://github.com/pardeike/Harmony Copyright (c) 2017 Andreas Pardeike


using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Assets.Packages.DevConsole;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.DroonComLinks.Interfaces;
using Assets.Scripts.DroonComLinks.Objects;
using Assets.Scripts.DroonComLinks.Objects.Antennas;
using Assets.Scripts.DroonComLinks.Objects.ListItems;
using Assets.Scripts.Flight;
using Assets.Scripts.Flight.MapView.Interfaces;
using Assets.Scripts.Flight.Sim;
using Assets.Scripts.State;
using Assets.Scripts.Ui;
using Assets.Scripts.Ui.Flight;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Planet;
using ModApi.Scenes.Events;
using UnityEngine;
using static Assets.Scripts.DroonComLinks.AntennaTypes;

namespace Assets.Scripts.DroonComLinks
{
    public class ComLinksManager : MonoBehaviour, IDisplayable
    {
        public string id => "ComLinksManager";
        public event EmptyEventHandler NetworkNodesUpdated;
        public event StringEventHandler NetworkConnectionAdded;
        public event StringEventHandler NetworkConnectionRemoved;
        public event BoolEventHandler RemoteControlChanged;
        public event BoolEventHandler DroonControlChanged;
        public delegate void StringEventHandler(string data);
        public delegate void BoolEventHandler(bool data);
        public delegate void EmptyEventHandler();
        public bool onEva { get; private set; }
        private FlightState _flightState;
        public ControlsPatches controlManager = new ControlsPatches();
        public IMapViewCoordinateConverter coordinateConverter;
        public DCLFlightUIScript flightUI => DCLUIManager.flightUI;
        private NetworkNode _player;
        public NetworkNode player => _player;
        private CraftNode _playerCraftNode => (CraftNode)FlightSceneScript.Instance.CraftNode;
        private IFuelSource _playerBattery => _playerCraftNode.CraftScript.ActiveCommandPod.BatteryFuelSource;
        public List<NetworkNode> networkNodes { get; private set; } = new List<NetworkNode>();
        public Dictionary<string, NetworkNode> networkNodeFromId { get; private set; } = new Dictionary<string, NetworkNode>();
        public Dictionary<ICraftNode, NetworkNode> networkNodeFromCraftNode { get; private set; } = new Dictionary<ICraftNode, NetworkNode>();
        private List<NetworkNode> _fixedNetworkNodes = new List<NetworkNode>();
        private Dictionary<NetworkNode, List<NetworkNode>> _pathsToGS = new Dictionary<NetworkNode, List<NetworkNode>>();
        public List<NetworkConnection> networkConnections { get; private set; } = new List<NetworkConnection>();
        public Dictionary<string, NetworkConnection> NetworkConnectionFromId { get; private set; } = new Dictionary<string, NetworkConnection>();
        private Dictionary<string, float> comRequests = new Dictionary<string, float>();
        private Dictionary<NetworkNode, float> _lastPathsFinding = new Dictionary<NetworkNode, float>();
        private List<DCLSphere> _spheres = new List<DCLSphere>();
        private Dictionary<string, GameObject> planetSpheres = new Dictionary<string, GameObject>();
        private DrawModes _drawMode => Mod.Instance.drawMode;
        public List<PlanetNode> planets = new List<PlanetNode>();
        public bool playerHasDroonControl => CheckPlayerDroonControl();
        public bool playerHasRemoteControl => CheckPlayerRemoteControl();
        public bool playerHasControl => playerHasDroonControl || playerHasRemoteControl;
        private bool _playerHasDroonControl, _playerHasRemoteControl;
        public bool StartMethodCalled { get; set; }
        private bool _flightActive = false;
        public bool requestFlightStateUpdate;
        public float updatePeriod => 1 / Mod.Instance.updateFrequency;//= 0.2f;
        private float comRequestsPeriod = 0.5f;
        private float _lastDroonControlCheck = 0, _lastError = 0, _lastFlightStateUpdate = 0, _lastNetworkUpdate = 0;
        private int _fps => (int)(1 / Time.deltaTime);
        public int averageFps = 0;
        private int _registeredCraftNodeCount;
        public const float minPower = 1;

        protected virtual void Awake()
        {
            if (ModSettings.Instance.debugMode) Debug.Log("ComLinks Awake");
            Mod.Instance.comLinksManager = this;

            Game.Instance.SceneManager.SceneLoaded += OnSceneloaded;
            Game.Instance.SceneManager.SceneUnloading += OnSceneUnloading;

            controlManager.PatchControls();
        }

        private void OnSceneloaded(object sender, SceneEventArgs e)
        {
            if (e.Scene == "Flight")
            {
                if (ModSettings.Instance.debugMode) Debug.Log("GAME STARTED");
                StartCoroutine(CommonMethods.CallNextFrame(Initialise));
            }
        }

        private void Initialise()
        {
            _flightActive = true;
            _flightState = FlightSceneScript.Instance.FlightState;
            coordinateConverter = (IMapViewCoordinateConverter)Game.Instance.FlightScene.ViewManager.MapViewManager.MapView;
            comRequests.Clear();

            try { DCLUIManager.InitializeFlightUI(); } catch (Exception E) { Debug.LogError("Initialize DCL UI error " + E); }
            try { InitialisePlanets(); } catch (Exception E) { Debug.LogError("GetPlanets error " + E); }
            try { InitialiseFixedNodes(); } catch (Exception E) { Debug.LogError("Initialise FixedNodes error " + E); }
            try { FlightStateUpdate(); } catch (Exception E) { Debug.LogError("FlightState Update error " + E); }
            //StartCoroutine(ManageUpdateFrequency());

            Game.Instance.FlightScene.ViewManager.MapViewManager.ForegroundStateChanged += OnForegroundStateChanged;
            Game.Instance.FlightScene.CraftChanged += OnPlayerCraftChanged;
            Game.Instance.FlightScene.ActiveCommandPodChanged += OnPlayerCraftChanged;
            // Game.Instance.FlightScene.CraftNode.CraftScript.CraftSplit += OnCraftSplited;
            // Game.Instance.FlightScene.CraftNode.CraftScript.CraftStructureChanged += OnCraftStructureChanged;
            // Game.Instance.FlightScene.CraftNode.CraftScript.DockComplete += OnDocked;

            DevConsoleApi.RegisterCommand("GetPropertyValue", delegate (string propertyName)
            {
                PropertyInfo property = this.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField | BindingFlags.Instance | BindingFlags.GetProperty);
                if (property != null)
                {
                    if (ModSettings.Instance.debugMode) Debug.Log(propertyName + " = " + property.GetValue(this).ToString());
                }
                else
                {
                    if (ModSettings.Instance.debugMode) Debug.LogError("Property [" + propertyName + "] not found");
                }
            });
        }

        private void InitialiseFixedNodes()
        {
            _fixedNetworkNodes.Clear();
            foreach (PlanetNode planet in planets)
            {
                bool flag = false;
                if (!planet.TerrainDataLoaded) { planet.LoadTerrainData(); flag = true; }
                foreach (StructureNodeData structure in planet.PlanetData.StructureNodes)
                {
                    try
                    {
                        Vector3d pos = planet.GetSurfacePosition(structure.Latitude * 0.01745329238474369, structure.Longitude * 0.01745329238474369, structure.ElevationType, structure.Elevation > 1 ? structure.Elevation : 1);
                        if (structure.PrefabPath.Contains("PrimaryLaunchSite") || structure.PrefabPath.Contains("SatelliteDish"))
                        {
                            _fixedNetworkNodes.Add(new NetworkNode(structure.Name + " id: " + _fixedNetworkNodes.Count, pos, planet, this));
                            if (ModSettings.Instance.debugMode) Debug.Log("ADDED GS at height " + structure.Elevation + " prefab " + structure.PrefabPath);
                        }

                        if (structure.SubStructures.Count > 0) InitialiseSubStructures(structure.SubStructures, planet, pos);
                    }
                    catch (Exception e)
                    {
                        if (ModSettings.Instance.debugMode) Debug.LogErrorFormat("Error in fixed nodes initialisation for planet: {0}, structure: {1}, latitude: {2}, longitude: {3}, elevation type: {4}, elevation: {5}, {6}",
                            planet.Name != null ? planet.Name : "null",
                            structure.Name != null ? structure.Name : "null",
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
                    if (ModSettings.Instance.debugMode) Debug.Log("ADDED GS prefab path : " + subStructure.PrefabPath);
                }

                if (subStructure.SubStructures.Count > 0) InitialiseSubStructures((List<SubStructure>)subStructure.SubStructures, planet, pos);
            }
        }

        public void InitialisePlanets()
        {
            planets = new List<PlanetNode>();
            planets.Add((PlanetNode)_flightState.RootNode);
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
                if (ModSettings.Instance.debugMode) Debug.Log("GAME ENDED");
                DCLUIManager.CloseFlightUI();
                _flightActive = false;
            }
        }

        private void OnForegroundStateChanged(bool foreground)
        {
            if (!foreground)
            {
                foreach (NetworkConnection connection in networkConnections)
                {
                    connection.Hide();
                }

                foreach (DCLSphere sphere in _spheres)
                {
                    sphere.DestroySphere();
                }
                _spheres.Clear();
            }
        }

        private void OnPlayerCraftChanged(ICraftNode craftNode) => FlightStateUpdate();

        private void OnCraftNodeChange(CraftNode craftNode)
        {
            if (ModSettings.Instance.debugMode) Debug.Log("OnCraftNodeChange requestFlightStateUpdate");
            requestFlightStateUpdate = true;
        }
        private void OnDocked(string craftNodeNameA, string craftNodeNameB)
        {
            if (ModSettings.Instance.debugMode) Debug.Log("OnDocked requestFlightStateUpdate");
            requestFlightStateUpdate = true;
        }

        private void OnCraftStructureChanged()
        {
            if (ModSettings.Instance.debugMode) Debug.Log("OnCraftStructureChanged requestFlightStateUpdate");
            requestFlightStateUpdate = true;
        }

        private void OnCraftSplited(ICraftScript craftScript)
        {
            if (ModSettings.Instance.debugMode) Debug.Log("OnCraftSplited requestFlightStateUpdate");
            requestFlightStateUpdate = true;
        }

        public void ForceRefresh()
        {
            if (_flightActive) StartCoroutine(CommonMethods.CallNextFrame(Refresh));
        }

        private void Refresh()
        {
            _pathsToGS.Clear();
            _lastPathsFinding.Clear();
            DestroyConnections();
            AntennaMath.ResetFloatAttributes();
            foreach (NetworkNode node in networkNodes) node.ForceRefresh();
            UpdateNetwork();
            UpdateConnections();
        }

        private void Update()
        {
            if (_flightActive)
            {
                try
                {
                    if (_registeredCraftNodeCount != _flightState.CraftNodes.Count())
                    {
                        if (ModSettings.Instance.debugMode) Debug.Log("CraftNode count inconsistency requestFlightStateUpdate");
                        requestFlightStateUpdate = true;
                    }

                    if (requestFlightStateUpdate && _lastFlightStateUpdate + 2f < Time.time) FlightStateUpdate(); //Mega update
                    else if (_lastNetworkUpdate + updatePeriod < Time.time) UpdateNetwork(); //Medium update
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
            foreach (NetworkNode node in networkNodes) node.StartLightNetworkUpdate();
            if (!networkUpdate) foreach (NetworkNode node in networkNodes) node.CheckConnectionsOcclusion();
            foreach (NetworkNode node in networkNodes) { node.GetNodesInRelayRange(null); node.TransfertNodesInRelayRange(); }

            UpdateConnections();
            CheckPlayerRemoteControl();
            GetNodePathToGS(_player);
        }

        private void UpdateNetwork()
        {
            _lastNetworkUpdate = Time.time;

            foreach (NetworkNode node in networkNodes) node.StartNetworkUpdate();
            foreach (NetworkNode node in networkNodes) node.GetNodesInDirectRange();
            LightNetworkUpdate(networkUpdate: true);
        }

        public bool ManageComRequest(string id, bool needsPower = true)
        {
            try
            {
                if (_flightActive)
                {
                    if (Game.Instance.FlightScene.TimeManager.CurrentMode.WarpMode) needsPower = false;
                    if (comRequests.ContainsKey(id) && comRequests[id] + comRequestsPeriod > Time.time) needsPower = false;

                    //if (id != "FlightControlsUpdate" && id != "LockHeading") Debug.Log("[ComRequest] type: " + id + " needs power: " + needsPower);
                    if (playerHasDroonControl) return true;
                    if (playerHasRemoteControl && ModSettings.Instance.needPower)
                    {
                        double comPowerConsumption = 0;
                        if (needsPower)
                        {
                            comRequests[id] = Time.time;
                            NetworkNode firstNode = GetNodePathToGS(_player)[0];
                            NetworkConnection firstConnection = ConnectionOf(_player, firstNode);
                            comPowerConsumption = firstConnection.TxPowerFrom(_player) / NetworkConnection.InAntenna(_player, firstNode).antennaType.txPowerRatio;
                            //Debug.Log("-   power consumption: " + powerConsumption + " Tx power: " + firstConnection.TxPowerFrom(_player) + " power ratio: " + NetworkConnection.InAntenna(_player, firstNode).antennaType.txPowerRatio);
                        }
                        bool enoughPower = _playerBattery.TotalFuel >= comPowerConsumption;
                        if (needsPower && enoughPower) { _playerBattery.RemoveFuel(comPowerConsumption); return true; }

                        return enoughPower;
                    }

                    return !ModSettings.Instance.blockControls;
                }
            }
            catch (Exception e) { Debug.LogError("ManageComRequest Error, id [" + id + "] : " + e); }
            return true;
        }

        // private IEnumerator ManageUpdateFrequency()
        // {
        //     List<int> lastFps = new List<int>();

        //     for (int i = 0; i < 10; i++)
        //     {
        //         lastFps.Add(_fps);
        //         yield return 0;
        //     }

        //     while (_flightActive)
        //     {
        //         if (!Game.Instance.FlightScene.TimeManager.Paused)
        //         {
        //             lastFps.Add(_fps);
        //             lastFps.Remove(lastFps[0]);
        //             averageFps = (int)lastFps.Average();

        //             if (averageFps < Mod.Instance.updateFrequency) updatePeriod += 0.001f * (Mod.Instance.updateFrequency - averageFps);
        //             else updatePeriod -= 0.005f;
        //             if (updatePeriod < Time.deltaTime) updatePeriod = Time.deltaTime;
        //             else if (updatePeriod > 1) updatePeriod = 1;

        //             //if (Game.Instance.FlightScene.TimeManager.CurrentMode.WarpMode) updatePeriod *= 2;
        //         }

        //         yield return 0;
        //     }
        // }

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
            if (!_flightActive) return;
            requestFlightStateUpdate = false;

            int craftsWithAntenna = 0;
            int craftsWithoutAntenna = 0;
            int craftsWithoutCommandPods = 0;
            _registeredCraftNodeCount = _flightState.CraftNodes.Count();
            networkNodes.Clear();
            networkNodeFromId.Clear();
            networkNodeFromCraftNode.Clear();
            _pathsToGS.Clear();
            _lastPathsFinding.Clear();
            _player = null;
            _flightState.ProcessDestroyedCraftNodes();
            AntennaMath.ResetFloatAttributes();
            DestroyConnections();

            if (ModSettings.Instance.debugMode)
            {
                Debug.Log("");
                Debug.LogWarning("------------- START -------------");
            }

            foreach (CraftNode craftNode in _flightState.CraftNodes)
            {
                if (craftNode.Name != null)
                {
                    if (ModSettings.Instance.debugMode) Debug.Log("Craft : " + craftNode.Name + "   Id : " + craftNode.NodeId);
                    XElement craftXml = null;
                    List<Antenna> antennas = new List<Antenna>();

                    if (craftNode.HasCommandPod)
                    {
                        double unloadedCraftBattery = 0;
                        if (craftNode.CraftScript != null)
                        {
                            try
                            {
                                CraftData craftData = craftNode.CraftScript.Data;
                                foreach (PartData part in craftData.Assembly.Parts)
                                {
                                    DCLAntennaScript antennaScript = part.PartScript.GetModifier<DCLAntennaScript>();
                                    if (antennaScript != null)
                                    {
                                        if (ModSettings.Instance.debugMode) Debug.Log("-    Antenna found type: " + antennaScript.antennaData.type.id + ", part id: " + part.Id);
                                        antennas.Add(new Antenna(
                                            antennaScript.antennaData.type.id,
                                            part.GenerateXml(craftNode.GameObject.transform, false),
                                            antennaScript.Data.GenerateStateXml(false),
                                            antennaScript.antennaData.GenerateStateXml(false),
                                            part.PartScript));
                                    }
                                    else if (part.Name.StartsWith(AntennaTypes.dclAntennaPrefix))
                                    {
                                        CustomAntennaResult antennaData = AntennaTypes.ParseCustomAntennaName(part.Name);

                                        if (antennaData.error == "")
                                        {
                                            if (ModSettings.Instance.debugMode) Debug.Log("Custom antenna found, type : " + antennaData.type.id);
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
                                        else if (ModSettings.Instance.debugMode) Debug.LogError("Error when parsing custom antenna data: " + antennaData.error);
                                    }
                                }
                            }
                            catch (Exception e) { if (ModSettings.Instance.debugMode) Debug.LogError("Error on craft " + e); }
                        }
                        else
                        {
                            try
                            {
                                if (ModSettings.Instance.debugMode) Debug.Log("-    Craft script not found searching for craft xml");
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
                                                if (ModSettings.Instance.debugMode) Debug.Log("-    Antenna found, type: " + type + ", part id: " + partXml.Attribute("id").Value);
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

                                            if (antennaData.error == "")
                                            {
                                                if (ModSettings.Instance.debugMode) Debug.Log("-    Custom antenna found, type : " + antennaData.type.id + ", part id: " + partXml.Attribute("id").Value);
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
                                            else if (ModSettings.Instance.debugMode) Debug.LogError("Error when parsing custom antenna data: " + antennaData.error);
                                        }
                                    }
                                    if (ModSettings.Instance.debugMode) Debug.Log("    unloaded craft battery: " + unloadedCraftBattery);
                                }
                            }
                            catch (Exception e) { if (ModSettings.Instance.debugMode) Debug.LogError("Error on craft " + e); }
                        }
                        if (antennas.Count > 0)
                        {
                            craftsWithAntenna++;
                            networkNodes.Add(new NetworkNode(craftNode, antennas, this, unloadedCraftBattery));
                            networkNodeFromCraftNode.Add(craftNode, networkNodes.Last());
                            networkNodeFromId.Add(networkNodes.Last().id, networkNodes.Last());
                            if (craftNode.IsPlayer)
                            {
                                _player = networkNodes.Last();
                                _player.isPlayer = true;
                            }
                        }
                        else craftsWithoutAntenna++;
                    }
                    else craftsWithoutCommandPods++;
                }
                else if (ModSettings.Instance.debugMode) Debug.LogError("-    CraftNode content null error");
            }
            foreach (NetworkNode node in _fixedNetworkNodes)
            {
                networkNodes.Add(node);
                networkNodeFromId.Add(node.id, node);
            }
            if (_playerCraftNode.CraftScript.Data.Assembly.Parts[0].PartType.Name == "Eva") onEva = true;
            else onEva = false;
            CheckPlayerDroonControl(imediate: true);

            if (ModSettings.Instance.debugMode)
            {
                Debug.LogWarning("------------- Result -------------");
                Debug.Log(craftsWithAntenna + " Crafts with antenna");
                Debug.Log(craftsWithoutAntenna + " Crafts without antenna");
                Debug.Log(craftsWithoutCommandPods + " Crafts without command pod");
                if (_player != null) Debug.Log("Player : " + _player.craftNode.Name + " part id : " + _player.craftNode.NodeId);
                else if (onEva) Debug.Log("Player on eva");
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
                if (ModSettings.Instance.debugMode) Debug.Log("Adding property: " + property + " value: " + data.properties[property]);
                typeModifer.Attribute(property)?.SetValue(data.properties[property]);
                antennaModifer.Attribute(property)?.SetValue(data.properties[property]);
            }
        }

        private void UpdateConnections()
        {
            List<NetworkConnection> copy = new List<NetworkConnection>(networkConnections);
            foreach (NetworkConnection connection in copy)
            {
                if (!connection.nodeA.stringNodesInDirectRange.Contains(connection.nodeB.id))
                {
                    connection.Destroy();
                    networkConnections.Remove(connection);
                    NetworkConnectionFromId.Remove(connection.id);
                    NetworkConnectionRemoved?.Invoke(connection.id);
                }
            }

            List<NetworkNode> checkedNodes = new List<NetworkNode>();
            foreach (NetworkNode nodeA in networkNodes)
            {
                checkedNodes.Add(nodeA);
                //foreach (NetworkNode nodeB in nodeA.GetOptimisedNodesInDirectRange(checkedNodes))
                foreach (NetworkNode nodeB in nodeA.objectNodesInDirectRange)
                {
                    if (!ConnectionExists(nodeA, nodeB))
                    {
                        networkConnections.Add(new NetworkConnection(nodeA, nodeB, coordinateConverter));
                        string id = networkConnections.Last().id;
                        NetworkConnectionFromId.Add(id, networkConnections.Last());
                        NetworkConnectionAdded?.Invoke(id);
                    }
                }
            }
        }

        private void DestroyConnections()
        {
            foreach (NetworkConnection connection in networkConnections)
                connection.Destroy();

            networkConnections.Clear();
            NetworkConnectionFromId.Clear();
        }

        public bool ConnectionExists(NetworkNode nodeA, NetworkNode nodeB) => NetworkConnectionFromId.ContainsKey(NetworkConnection.connectionID(nodeA.id, nodeB.id));
        public bool ConnectionExists(string connectionId) => NetworkConnectionFromId.ContainsKey(connectionId);

        public NetworkConnection ConnectionOf(NetworkNode nodeA, NetworkNode nodeB)
        {
            string connectionId = NetworkConnection.connectionID(nodeA.id, nodeB.id);
            if (ConnectionExists(connectionId)) return NetworkConnectionFromId[connectionId];
            Debug.LogError("Connection between [" + nodeA.id + "] and [" + nodeB.id + "] doesn't exist");
            return null;
        }

        public void BenchmarkNetwork()
        {
            float wl;
            List<NetworkNode> doneNodes = new List<NetworkNode>();

            foreach (NetworkNode node in networkNodes)
            {
                foreach (NetworkNode target in node.objectNodesInDirectRange)
                {
                    if (doneNodes.Contains(target)) continue;

                    double distance = Vector3d.Distance(node.position, target.position);
                    foreach (Antenna antennaA in node.antennas)
                    {
                        foreach (Antenna antennaB in target.antennas)
                        {
                            double rank = antennaA.SignalStrengthFrom(antennaB, out wl) * antennaB.SignalStrengthFrom(antennaA, out wl) * distance * Mathd.Pow(10, 30);
                            Debug.Log(antennaA.type + "," + antennaB.type + "," + (rank));
                        }
                    }
                }
                doneNodes.Add(node);
            }
        }

        private void DrawNetwork()
        {
            List<NetworkConnection> notUpdatedConnections = new List<NetworkConnection>(networkConnections);

            switch (_drawMode)
            {
                case DrawModes.All:
                    networkConnections.ForEach(c => c.UpdateConnection());
                    notUpdatedConnections.Clear();
                    break;
                case DrawModes.GsPaths:
                    UpdatePathsToGS(networkNodes);
                    foreach (NetworkNode node in _pathsToGS.Keys)
                    {
                        if (_pathsToGS[node].Count != 0 && ConnectionExists(node, _pathsToGS[node][0]))
                        {
                            NetworkConnection connection = ConnectionOf(node, _pathsToGS[node][0]);
                            connection.UpdateConnection();
                            notUpdatedConnections.Remove(connection);
                        }
                    }
                    break;
                case DrawModes.CraftOnly:
                    if (_player != null)
                    {
                        List<NetworkConnection> copy = new List<NetworkConnection>(networkConnections);
                        foreach (NetworkConnection connection in copy)
                        {
                            if (connection.connects(_player))
                            {
                                connection.UpdateConnection();
                                notUpdatedConnections.Remove(connection);
                            }
                        }

                    }
                    break;
                case DrawModes.GsPath:
                    if (_player != null)
                    {
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
                    }
                    break;
            }

            foreach (NetworkConnection connection in notUpdatedConnections)
            {
                connection.Hide();
            }

            if (ModSettings.Instance.debugMode && _drawMode != DrawModes.None)
            {
                foreach (DCLSphere sphere in _spheres)
                {
                    sphere.DestroySphere();
                }
                _spheres.Clear();
                foreach (NetworkNode node in networkNodes) //networkNodes
                {
                    _spheres.Add(new DCLSphere(node.id, node.position, Mod.Instance.sphereScaleFactor));
                }
            }
        }

        public List<NetworkNode> GetNodePathToGS(NetworkNode node)
        {
            UpdatePathsToGS(new List<NetworkNode> { node });
            if (_pathsToGS.ContainsKey(node)) return _pathsToGS[node];
            else return new List<NetworkNode>();
        }

        private void UpdatePathsToGS(List<NetworkNode> nodes)
        {
            Dictionary<NetworkNode, List<NetworkNode>> paths = new Dictionary<NetworkNode, List<NetworkNode>>();

            foreach (NetworkNode node in nodes)
            {
                if (node == null) { if (ModSettings.Instance.debugMode) Debug.LogError("Update paths to GS error, node null"); continue; }
                if (node.isCraft)
                {
                    List<NetworkNode> list = paths.Values.ToList().Find(l => l.Contains(node));
                    if (list == null)
                    {
                        paths.Add(node, GetPathToGS(node));
                    }
                    else
                    {
                        List<NetworkNode> newList = new List<NetworkNode>(list);
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
            if (_lastPathsFinding.ContainsKey(initialNode) && _lastPathsFinding[initialNode] + updatePeriod > Time.time && _pathsToGS.ContainsKey(initialNode))
                return _pathsToGS[initialNode];
            _lastPathsFinding[initialNode] = Time.time;

            if (!initialNode.gsInRange) return new List<NetworkNode>();

            List<NetworkNode> _nodesInRelayRange = new List<NetworkNode>(initialNode.nodesInRelayRange);
            List<NetworkNode> _nodesInDirectRange = new List<NetworkNode>(initialNode.objectNodesInDirectRange);
            List<NetworkNode> _nodesToCheck = new List<NetworkNode>(_nodesInDirectRange);

            //If there is a ground station in direct range, return the closest one
            if (initialNode.gsInDirectRange) return new List<NetworkNode>() { initialNode.nodesInDirectRange.FindAll(n => !n.node.isCraft).OrderBy(n => n.distance).First().node };
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
            NetworkNode closestGS = Array.Find(sorted, node => !node.isCraft);

            //Debug.Log ("Building path");
            List<NetworkNode> path = new List<NetworkNode>();
            NetworkNode prevNode = closestGS;
            while (prevNode != initialNode)
            {
                path.Add(prevNode);
                //Debug.Log ("PrevNode: " + prevNode.name);
                try { prevNode = prevNodes[Array.IndexOf(nodes, prevNode)]; } catch (Exception e) { if (ModSettings.Instance.debugMode) Debug.Log("PrevNode not found at index " + Array.IndexOf(nodes, prevNode) + ", Nodes Count: " + nodes.Count() + ", PrevNodes Count: " + prevNodes.Count() + ", error: " + e); path.Reverse(); break; }
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
            if (!imediate && _lastDroonControlCheck + updatePeriod > Time.time) return _playerHasDroonControl;
            _lastDroonControlCheck = Time.time;

            if (onEva)
            {
                if (_playerHasDroonControl == false)
                {
                    _playerHasDroonControl = true;
                    DroonControlChanged?.Invoke(true);
                }
                return true;
            }

            foreach (CrewCompartmentData partData in _playerCraftNode.CraftScript.Data.Assembly.GetModifiers<CrewCompartmentData>())
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

            if (_playerHasRemoteControl != _player.gsInRange)
            {
                _playerHasRemoteControl = _player.gsInRange;
                RemoteControlChanged?.Invoke(_player.gsInRange);
            }
            return _playerHasRemoteControl;
        }

        private IUIListItem[,] info;
        public IUIListItem[,] GetInfo()
        {
            if (info == null)
            {
                info = new IUIListItem[4, 4];
                info[0, 0] = new UIListTextValue<int>("Network Nodes", () => networkNodes.Count);
                info[0, 1] = new UIListTextValue<int>("Network Connections", () => networkConnections.Count);
                info[1, 0] = _player != null ? new UIListLabelButton<string>("Player", () => _player.id) as IUIListItem : new UIListTextValue<string>("Player", () => _playerCraftNode.Name);
                info[1, 1] = new UIListTextValue<bool>("Has Antennas", () => _player != null);
                info[1, 2] = new UIListTextValue<bool>("Has Remote Control", () => playerHasRemoteControl);
                info[1, 3] = new UIListTextValue<bool>("Has Droon Control", () => playerHasRemoteControl);
                info[3, 0] = new UIListTextValue<string>("Refresh Rate", () => $"{(1 / updatePeriod):n2} Hz");
                info[3, 1] = new UIListTextValue<int>("Average Fps", () => averageFps);
            }
            return info;
        }
    }
}