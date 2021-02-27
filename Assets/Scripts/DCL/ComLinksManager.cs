using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.DroonComLinks.Objects;
using Assets.Scripts.Flight;
using Assets.Scripts.Flight.MapView;
using Assets.Scripts.Flight.MapView.Interfaces;
using Assets.Scripts.Flight.Sim;
using Assets.Scripts.State;
using Assets.Scripts.Ui.Flight;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Planet;
using ModApi.Scenes.Events;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks {
    public class ComLinksManager : MonoBehaviour {
        public List<String> connectedNodes {
            get {
                if (_player != null) return _player.nodesInDirectRange;
                return new List<String> (new String[] { "Player's craft doesn't have any Antenna" });
            }
        }
        public List<String> playerAntennas {
            get {
                if (_player != null) return _player.GetCraftAntennas ();
                return new List<String> (new String[] { "Player's craft doesn't have any Antenna" });
            }
        }
        public bool playerHasRemoteControl {
            get {
                //UpdatePathsToGS ();
                //if (_pathsToGS[_player].Count () > 0) return true;
                //return false;
                return _player.gsInRange;
            }
        }
        public bool onEva {
            get;
            private set;
        }
        public bool playerHasDroonControl => CheckPlayerDroonControl ();
        private bool _playerHasDroonControl;
        public FlightState _flightState;
        public IMapViewCoordinateConverter _coordinateConverter;
        private DroonComLinksControlPanelUIScript _dialogScript;
        private DelayManager _delayManager = new DelayManager ();
        private InputSender _inputSender = new InputSender ();
        private NetworkNode _player;
        private CraftNode _playerCraftNode => (CraftNode) FlightSceneScript.Instance.CraftNode;
        public List<NetworkNode> _networkNodes = new List<NetworkNode> ();
        public List<NetworkNode> _fixedNetworkNodes = new List<NetworkNode> ();
        private Dictionary<NetworkNode, List<NetworkNode>> _pathsToGS = new Dictionary<NetworkNode, List<NetworkNode>> ();
        private List<NetworkConnection> _networkConnections = new List<NetworkConnection> ();
        private List<String> _lines = new List<String> ();
        private List<DCLSphere> _spheres = new List<DCLSphere> ();
        public MapViewScript mapView => Game.Instance.FlightScene.ViewManager.MapViewManager.MapView as MapViewScript;
        public float updateFrequency => Mod.Instance.updateFrequency;
        private string _drawMode => Mod.Instance.drawMode;
        private float _lastPathsFinding = 0, _lastDroonControlCheck = 0, _lastConnectionUpdate = 0;
        private float lastFlightStateUpdate = 0;

        protected virtual void Awake () {
            Debug.Log ("ComLinks Awake");
            Mod.Instance.ComLinksManager = this;

            Game.Instance.SceneManager.SceneLoaded += OnSceneloaded;
            Game.Instance.SceneManager.SceneUnloaded += OnSceneUnloaded;
        }
        private void OnSceneloaded (object sender, SceneEventArgs e) {
            if (e.Scene == "Flight") {
                Debug.Log ("GAME STARTED");
                StartCoroutine (CommonMethods.CallNextFrame (Initialise));
            }
        }

        private void Initialise () {
            _flightState = FlightSceneScript.Instance.FlightState;
            _coordinateConverter = (IMapViewCoordinateConverter) Game.Instance.FlightScene.ViewManager.MapViewManager.MapView;

            try { droonComLinksUI.InitializeDCLUI (this); } catch (Exception E) { Debug.LogError ("InitializeDCLUI error " + E); }
            try { _delayManager.Initialise (_inputSender); } catch (Exception E) { Debug.LogError ("_DelayManager error " + E); }
            try { CommonMethods.InitialisePlanets (); } catch (Exception E) { Debug.LogError ("GetPlanets error " + E); }
            Debug.Log (CommonMethods.planets.Count ());
            try { InitialiseFixedNodes (); } catch (Exception E) { Debug.LogError ("InitialiseFixedNodes error " + E); }
            try { FlightStateUpdate (); } catch (Exception E) { Debug.LogError ("ConnectionUpdate error " + E); }

            Game.Instance.FlightScene.ViewManager.MapViewManager.ForegroundStateChanged += OnForegroundStateChanged;
            Game.Instance.FlightScene.CraftChanged += OnPlayerCraftChanged;
        }

        private void OnSceneUnloaded (object sender, SceneEventArgs e) {
            if (e.Scene == "Flight") {
                Debug.Log ("GAME ENDED");
                droonComLinksUI.Close ();
            }
        }

        private void OnForegroundStateChanged (bool foreground) {
            if (!foreground) {
                foreach (NetworkConnection connection in _networkConnections) {
                    connection.Hide ();
                }

                foreach (DCLSphere sphere in _spheres) {
                    sphere.DestroySphere ();
                }
                _spheres.Clear ();
            }
        }

        private void OnPlayerCraftChanged (ICraftNode craftNode) {
            FlightStateUpdate ();
        }

        private void OnCraftNodeChange (CraftNode craftNode) {
            FlightStateUpdate ();
        }

        public void ForceRefresh () {
            StartCoroutine (CommonMethods.CallNextFrame (refresh));
        }

        private void refresh () {
            _pathsToGS.Clear ();
            foreach (NetworkNode node in _networkNodes) {
                node.ForceRefresh ();
            }
            DestroyConnections ();
            UpdateConnectionsNow ();
        }

        protected virtual void Update () {
            if (Game.Instance.SceneManager.InFlightScene) {
                try {
                    if (!Game.Instance.FlightScene.TimeManager.Paused) {
                        UpdateConnections ();
                        droonComLinksUI.Update ();
                        // if (!playerHasDroonControl && !playerHasRemoteControl) {
                        //     _delayManager.SetInputs (false);
                        // } else _delayManager.SetInputs (true);
                        _delayManager.Update (playerHasDroonControl, playerHasRemoteControl);
                    }
                    if (Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.Visible) {
                        if (_player != null) DrawNetwork ();
                    }
                } catch (Exception e) { Debug.LogError ("Error On Update : " + e); ManageError (); }
            }
        }

        private void OnDrawGizmos () {
            Gizmos.DrawCube (new Vector3 (), new Vector3 ());
        }

        private void ManageError () {
            if (lastFlightStateUpdate + 5 < Time.time) {
                DestroyConnections ();
                try { FlightStateUpdate (); } catch (Exception E) { Debug.LogError ("ConnectionUpdate error " + E); }
                lastFlightStateUpdate = Time.time;
            }
        }

        private void DestroyConnections () {
            List<NetworkConnection> copy = new List<NetworkConnection> (_networkConnections);
            foreach (NetworkConnection connection in copy) {
                connection.Destroy ();
                _networkConnections.Remove (connection);
            }
        }

        public void FlightStateUpdate () {
            int craftsWithAntenna = 0;
            int craftsWithoutAntenna = 0;
            int craftsWithoutCommandPods = 0;
            _networkNodes = new List<NetworkNode> ();
            _player = null;
            _flightState.ProcessDestroyedCraftNodes ();
            AntennaMath.ResetFloatAttributes ();
            DestroyConnections ();

            Debug.Log ("");
            Debug.LogWarning ("--------------START--------------");

            foreach (CraftNode craftNode in _flightState.CraftNodes) {
                if (craftNode.Name != null) {
                    Debug.Log ("Craft : " + craftNode.Name + "   Id : " + craftNode.NodeId);
                    XElement craftXml = null;
                    List<Antenna> Antennas = new List<Antenna> ();

                    if (craftNode.HasCommandPod) {
                        if (craftNode.CraftScript != null) {
                            try {
                                CraftData craftData = craftNode.CraftScript.Data;
                                foreach (PartData part in craftData.Assembly.Parts) {
                                    DCLAntennaScript antennaScript = part.PartScript.GetModifier<DCLAntennaScript> ();
                                    if (antennaScript != null) {
                                        Debug.Log ("-    " + craftNode.Name.ToString () + " has antenna of type " + part.PartType.Id + " part id : " + part.Id);
                                        Antennas.Add (new Antenna (part.GenerateXml (craftNode.GameObject.transform, false), antennaScript.Data.GenerateStateXml (false)));
                                    }
                                }
                            } catch (Exception e) { Debug.LogError ("Error on craft " + e); }
                        } else {
                            try {
                                Debug.Log ("-    Craft script not found searching for craft xml");
                                craftXml = _flightState.LoadCraftXml (craftNode.NodeId);
                                if (craftXml != null) {
                                    foreach (XElement partXml in craftXml.Element ("Assembly").Element ("Parts").Elements ()) {
                                        foreach (XElement modiferXml in partXml.Elements ()) {
                                            if (modiferXml.Name == "DroonComLinks.DCLAntenna") {
                                                XAttribute type = partXml.Attribute ("partType");
                                                Debug.Log ("-    " + craftNode.Name + " has Antenna of Type " + type.Value + " part id : " + partXml.Attribute ("id").Value);
                                                Antennas.Add (new Antenna (partXml, modiferXml));
                                            }
                                        }
                                    }
                                }
                            } catch (Exception e) { Debug.LogError ("Error on craft " + e); }
                        }
                        if (Antennas.Count > 0) {
                            craftsWithAntenna++;
                            _networkNodes.Add (new NetworkNode (craftNode, Antennas, this));
                            if (craftNode.IsPlayer) _player = _networkNodes.Last ();
                        } else craftsWithoutAntenna++;
                    } else craftsWithoutCommandPods++;
                } else Debug.LogError ("-    CraftNode content null error");
            }
            _networkNodes.AddRange (_fixedNetworkNodes);
            if (_playerCraftNode.CraftScript.Data.Assembly.Parts[0].PartType.Name == "Eva") onEva = true;

            Debug.LogWarning ("---------- end result ----------");
            Debug.Log (craftsWithAntenna + " Crafts With Antenna");
            Debug.Log (craftsWithoutAntenna + " Crafts without Antenna");
            Debug.Log (craftsWithoutCommandPods + " Crafts without Command Pod");
            if (_player != null) Debug.Log ("Player : " + _player.craftNode.Name);
            else if (onEva) Debug.Log ("Player On Eva");
            else Debug.Log ("Player Not Found");
            Debug.LogWarning ("-------------- END --------------");
        }

        private void UpdateConnections () {
            if (_lastConnectionUpdate + updateFrequency > Time.time) return;
            _lastConnectionUpdate = Time.time;
            UpdateConnectionsNow ();
        }

        private void UpdateConnectionsNow () {
            List<NetworkConnection> copy = new List<NetworkConnection> (_networkConnections);
            foreach (NetworkConnection connection in copy) {
                if (!connection.nodeA.nodesInDirectRange.Contains (connection.nodeB.name)) {

                    connection.Destroy ();
                    _networkConnections.Remove (connection);
                }
            }

            List<NetworkNode> checkedNodes = new List<NetworkNode> ();
            foreach (NetworkNode nodeA in _networkNodes) {
                checkedNodes.Add (nodeA);
                foreach (NetworkNode nodeB in nodeA.GetOptimisedNodesInDirectRange (checkedNodes)) {
                    if (!ConnectionExists (nodeA, nodeB)) {
                        _networkConnections.Add (new NetworkConnection (nodeA, nodeB, _coordinateConverter));
                    }
                }
            }
        }

        private bool ConnectionExists (NetworkNode nodeA, NetworkNode nodeB) {
            foreach (NetworkConnection connection in _networkConnections) {
                if (connection.connects (nodeA, nodeB)) return true;
            }
            return false;
        }

        private NetworkConnection ConnectionOf (NetworkNode nodeA, NetworkNode nodeB) {
            foreach (NetworkConnection connection in _networkConnections) {
                if (connection.connects (nodeA, nodeB)) return connection;
            }
            return null;
        }

        private void DrawNetwork () {
            List<NetworkConnection> notUpdatedConnections = new List<NetworkConnection> (_networkConnections);

            if (_drawMode != "None") {
                if (_drawMode == "All") {
                    _networkConnections.ForEach (c => c.UpdateConnection ());
                    notUpdatedConnections.Clear ();

                } else if (_drawMode == "Craft Only") {
                    List<NetworkConnection> copy = new List<NetworkConnection> (_networkConnections);
                    foreach (NetworkConnection connection in copy) {
                        if (connection.connects (_player)) {
                            connection.UpdateConnection ();
                            notUpdatedConnections.Remove (connection);
                        }
                    }

                } else if (_drawMode == "GS Path") {
                    UpdatePathsToGS ();
                    if (_pathsToGS.ContainsKey (_player)) {
                        List<NetworkNode> path = _pathsToGS[_player];
                        NetworkNode prevNode = _player;
                        foreach (NetworkNode node in path) {
                            NetworkConnection connection = ConnectionOf (prevNode, node);
                            if (connection != null) {
                                connection.UpdateConnection ();
                                notUpdatedConnections.Remove (connection);
                            }
                            prevNode = node;
                        }
                    }

                } else if (_drawMode == "GS Paths") {
                    UpdatePathsToGS ();
                    foreach (NetworkNode node in _pathsToGS.Keys) {
                        if (_pathsToGS[node].Count != 0) {
                            NetworkConnection connection = ConnectionOf (node, _pathsToGS[node][0]);
                            if (connection != null) {
                                connection.UpdateConnection ();
                                notUpdatedConnections.Remove (connection);
                            }
                        }
                    }
                }

                foreach (NetworkConnection connection in notUpdatedConnections) {
                    connection.Hide ();
                }
            }
        }

        private void UpdatePathsToGS () {
            if (_drawMode == "GS Paths") UpdatePathsToGS (_networkNodes);
            else UpdatePathsToGS (new List<NetworkNode> { _player });
        }

        private void UpdatePathsToGS (List<NetworkNode> nodes) {
            if (_lastPathsFinding + updateFrequency > Time.time) return;
            _lastPathsFinding = Time.time;

            Dictionary<NetworkNode, List<NetworkNode>> paths = new Dictionary<NetworkNode, List<NetworkNode>> ();

            foreach (NetworkNode node in nodes) {
                if (node.isCraft) {
                    List<NetworkNode> list = paths.Values.ToList ().Find (l => l.Contains (node));
                    if (list == null) {
                        paths.Add (node, GetPathToGS (node));
                    } else {
                        List<NetworkNode> newList = new List<NetworkNode> (list);
                        newList.RemoveRange (0, list.IndexOf (node) + 1);
                        paths.Add (node, newList);
                    }
                }
            }
            foreach (NetworkNode node in paths.Keys) {
                if (_pathsToGS.ContainsKey (node)) _pathsToGS[node] = paths[node];
                else _pathsToGS.Add (node, paths[node]);
            }
        }

        private List<NetworkNode> GetPathToGS (NetworkNode initialNode) {
            List<NetworkNode> _nodesInRelayRange = new List<NetworkNode> (initialNode.objectNodeInRelayRange);
            if (!initialNode.gsInRange) return new List<NetworkNode> ();

            List<NetworkNode> _nodesInDirectRange = new List<NetworkNode> (initialNode.objectNodesInDirectRange);
            List<NetworkNode> _nodesToCheck = new List<NetworkNode> (_nodesInDirectRange);

            foreach (NetworkNode node in _nodesInDirectRange) {
                if (!node.isCraft) return new List<NetworkNode> () { node };
            }
            //Debug.Log ("GS is not in direct range");

            List<NetworkNode> path = new List<NetworkNode> ();
            NetworkNode[] nodes = _nodesInRelayRange.ToArray ();
            //Debug.Log ("_nodesInRelayRange: " + _nodesInRelayRange.Count);
            float[] distances = Enumerable.Repeat (float.PositiveInfinity, nodes.Count ()).ToArray ();;
            NetworkNode[] prevNodes = new NetworkNode[nodes.Count ()];

            foreach (InRangeNode node in initialNode.GetNodesInDirectRange ()) {
                int nodeIndex = Array.IndexOf (nodes, node.node);
                distances[nodeIndex] = (float) node.distance;
                prevNodes[nodeIndex] = initialNode;

            }

            // Debug.Log ("Done with directrange");

            while (_nodesToCheck.Count > 0) {
                NetworkNode currentNode = _nodesToCheck[0];
                int currentNodeIndex = Array.IndexOf (nodes, currentNode);

                foreach (InRangeNode node in currentNode.GetNodesInDirectRange ()) {
                    NetworkNode Nnode = node.node;

                    if (_nodesInRelayRange.Contains (Nnode)) {
                        if (!_nodesToCheck.Contains (Nnode)) _nodesToCheck.Add (Nnode);
                        int nodeIndex = Array.IndexOf (nodes, Nnode);
                        float newDistance = (float) (distances[currentNodeIndex] + node.distance);
                        if (distances[nodeIndex] > newDistance) {
                            distances[nodeIndex] = newDistance;
                            prevNodes[nodeIndex] = currentNode;
                        }
                    }
                }
                _nodesInRelayRange.Remove (currentNode);
                _nodesToCheck.Remove (currentNode);
            }
            //Debug.Log ("EndLoop");

            NetworkNode[] sorted = new NetworkNode[nodes.Length];
            Array.Copy (nodes, sorted, nodes.Length);

            Array.Sort (distances, sorted);
            NetworkNode closestGS = Array.FindAll (sorted, node => !node.isCraft) [0];

            //Debug.Log ("Building List");

            NetworkNode prevNode = closestGS;
            while (prevNode != initialNode) {
                path.Add (prevNode);
                //Debug.Log ("PrevNode: " + prevNode.name);
                try { prevNode = prevNodes[Array.IndexOf (nodes, prevNode)]; } catch (Exception e) { Debug.Log ("PrevNode not found at index " + Array.IndexOf (nodes, prevNode) + "Nodes Count: " + nodes.Count () + "PrevNodes Count: " + prevNodes.Count () + " error: " + e); path.Reverse (); break; }
                if (prevNode == null) {
                    //Debug.LogError ("PrevNodeNull");
                    break;
                }
            }
            //Debug.Log ("Path count : " + path.Count);

            path.Reverse ();
            // Debug.LogWarning ("---- Path Found ----");
            // Debug.Log ("Initial Node : " + initialNode.name);
            // foreach (NetworkNode node in path) {
            //     Debug.Log ("-  Node : " + node.name);
            // }
            return path;
        }

        private bool CheckPlayerDroonControl () {
            if (_lastDroonControlCheck + updateFrequency > Time.time) return _playerHasDroonControl;
            _lastDroonControlCheck = Time.time;

            if (onEva) {
                _playerHasDroonControl = true;
                return true;
            }
            foreach (CrewCompartmentData partData in _playerCraftNode.CraftScript.Data.Assembly.GetModifiers<CrewCompartmentData> ()) {
                if (partData.Script.Crew.Count > 0) {
                    _playerHasDroonControl = true;
                    return true;
                }
            }
            _playerHasDroonControl = false;
            return false;
        }

        private void InitialiseFixedNodes () {
            _fixedNetworkNodes.Clear ();
            foreach (PlanetNode planet in CommonMethods.planets) {
                bool flag = false;
                if (!planet.TerrainDataLoaded) { planet.LoadTerrainData (); flag = true; }
                foreach (StructureNodeData structure in planet.PlanetData.StructureNodes) {
                    try {
                        Vector3d pos = planet.GetSurfacePosition (structure.Latitude, structure.Longitude, structure.ElevationType, structure.Elevation > 1 ? structure.Elevation : 1);
                        if (structure.PrefabPath.Contains ("PrimaryLaunchSite") || structure.PrefabPath.Contains ("SatelliteDish")) {
                            _fixedNetworkNodes.Add (new NetworkNode (structure.Name, pos, planet, this));
                            Debug.Log ("ADDED GS at height " + structure.Elevation + " prefab " + structure.PrefabPath);
                        }

                        if (structure.SubStructures.Count > 0) InitialiseSubStructures (structure.SubStructures, planet, pos);
                    } catch (Exception e) {
                        Debug.LogErrorFormat ("Error in fixed nodes initialisation for planet: {0}, structure: {1}, latitude: {2}, longitude: {3}, elevation type: {4}, elevation: {5}, {6}",
                            planet.Name != null? planet.Name: "null",
                            structure.Name != null? structure.Name: "null",
                            structure.Latitude,
                            structure.Longitude,
                            structure.ElevationType,
                            structure.Elevation,
                            e);
                    }
                }
                if (flag) planet.UnloadTerrainData ();
            }
        }

        private void InitialiseSubStructures (List<SubStructure> subStructures, PlanetNode planet, Vector3d parentPos) {
            foreach (SubStructure subStructure in subStructures) {
                Vector3d pos = parentPos + subStructure.LocalPosition;
                if (subStructure.PrefabPath.Contains ("PrimaryLaunchSite") || subStructure.PrefabPath.Contains ("SatelliteDish")) {
                    _fixedNetworkNodes.Add (new NetworkNode (subStructure.Name, pos, planet, this));
                    Debug.Log ("ADDED GS prefab path : " + subStructure.PrefabPath);
                }

                if (subStructure.SubStructures.Count > 0) InitialiseSubStructures ((List<SubStructure>) subStructure.SubStructures, planet, pos);
            }
        }

        private List<String> GetNames (List<NetworkNode> nodes) {
            List<String> result = new List<String> ();
            foreach (NetworkNode node in nodes) {
                result.Add (node.craftNode.Name);
            }
            return result;
        }

        public NetworkNode GetNode (ICraftNode craft) {
            foreach (NetworkNode node in _networkNodes) {
                if (node.isCraft && node.craftNode == craft) return node;
            }
            return null;
        }

        public NetworkNode GetNode (String nodeName) {
            foreach (NetworkNode node in _networkNodes) {
                if (node.name == nodeName) return node;
            }
            return null;
        }
    }
}