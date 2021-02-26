using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Assets.Scripts.Flight.MapView.Interfaces;
using Assets.Scripts.Flight.Sim;
using ModApi.Flight.Sim;
using ModApi.Planet;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Objects {
    public class NetworkNode {
        public bool isCraft;
        public CraftNode craftNode {
            get;
        }
        public bool isControlled {
            get;
        }
        public String name {
            get;
        }
        public bool isUnderWater {
            get {
                if (isCraft && craftNode.Parent.PlanetData.HasWater && craftNode.Parent.PlanetData.SeaLevel > Vector3d.Magnitude (craftNode.Position)) {
                    return true;
                }
                return false;
            }
        }
        private ComLinksManager _ComLinksManager;
        public IMapViewCoordinateConverter _coordinateConverter; // => (IMapViewCoordinateConverter) Game.Instance.FlightScene.ViewManager.MapViewManager.MapView;
        //private AntennaTypes antennaTypes = new AntennaTypes ();
        private List<Antenna> _antennas;
        public List<Antenna> antennas => _antennas;
        private List<DCLLine> lines = new List<DCLLine> ();
        private List<InRangeNode> _nodesInDirectRange = new List<InRangeNode> ();
        private List<NetworkNode> _nodesInRelayRange = new List<NetworkNode> ();
        public List<String> nodesInDirectRange => GetNodesInDirectRange ().Select (inRangeNode => inRangeNode.name).ToList ();
        public List<String> nodeInRelayRange => GetNodesInRelayRange ().Select (inRangeNode => inRangeNode.name).ToList ();
        public List<NetworkNode> objectNodesInDirectRange => GetNodesInDirectRange ().Select (inRangeNode => inRangeNode.node).ToList ();
        public List<NetworkNode> objectNodeInRelayRange => GetNodesInRelayRange (); //.Select (InRangeNode => InRangeNode.Node).ToList ();
        public Vector3d position => isCraft? craftNode.SolarPosition : _gsPosition;
        private Vector3d _gsPosition => _parent.SolarPosition + _parent.SurfaceVectorToPlanetVector (_gslocalPos);
        private Vector3d _gslocalPos;
        private PlanetNode _parent;
        private float _updateFrequency => _ComLinksManager.updateFrequency;
        private double _lastInrangeCheck = 0;
        private double _lastInRelayrangeCheck = 0;
        public NetworkNode (CraftNode craftnode, List<Antenna> antennas, ComLinksManager comLinksManager) {
            isCraft = true;
            craftNode = craftnode;
            _antennas = antennas;
            name = craftNode.Name + " id: " + craftNode.NodeId;
            SetUp (comLinksManager);
        }
        public NetworkNode (String name, Vector3d pos, PlanetNode parent, ComLinksManager comLinksManager) {
            isCraft = false;
            _gslocalPos = pos;
            this.name = name;
            _antennas = new List<Antenna> () { new Antenna ("Ground Station", 70f, 0.85f, 400f) };
            _parent = parent;
            SetUp (comLinksManager);
        }

        public void ForceRefresh () {
            GetNodesInDirectRangeNow (new List<NetworkNode> ());
            foreach (Antenna antenna in _antennas) {
                antenna.RefreshAntenna ();
            }
        }

        private void SetUp (ComLinksManager comLinksManager) {
            _ComLinksManager = comLinksManager;
            _coordinateConverter = _ComLinksManager._coordinateConverter;
            foreach (Antenna antenna in _antennas) {
                antenna.node = this;
            }
        }

        public List<InRangeNode> GetNodesInDirectRange () {
            return GetNodesInDirectRange (new List<NetworkNode> ());
        }

        public List<NetworkNode> GetOptimisedNodesInDirectRange (List<NetworkNode> excludedNodes) {
            return GetNodesInDirectRange (excludedNodes).Select (inRangeNode => inRangeNode.node).ToList ();
        }

        private List<InRangeNode> GetNodesInDirectRange (List<NetworkNode> excludedNodes) {
            if (_lastInrangeCheck + _updateFrequency > Time.time) return _nodesInDirectRange;
            _lastInrangeCheck = Time.time;
            return GetNodesInDirectRangeNow (excludedNodes);

        }
        private List<InRangeNode> GetNodesInDirectRangeNow (List<NetworkNode> excludedNodes) {

            List<NetworkNode> targets = new List<NetworkNode> (_ComLinksManager._networkNodes);
            List<InRangeNode> nodesInRange = new List<InRangeNode> ();

            foreach (NetworkNode target in targets) {
                if (!excludedNodes.Contains (target)) {
                    if ((target != this) && (isCraft || target.isCraft)) {
                        float[] signalInfo = AntennaMath.GetSignalInfo (this, target);
                        if (signalInfo.Length > 1 && signalInfo[0] > 0) {
                            if (!CommonMethods.ConectionObstructed (position, target.position)) {
                                nodesInRange.Add (new InRangeNode (target, target.name, Vector3d.Distance (position, target.position), signalInfo[0], signalInfo[1]));
                            }
                        }
                    }
                }
            }

            IEnumerable<InRangeNode> result = nodesInRange.ToArray ().OrderBy (InRangeNode => InRangeNode.distance);
            if (Mod.Instance.maxConnections > 0) _nodesInDirectRange = result.Take (Mod.Instance.maxConnections).ToList ();
            else _nodesInDirectRange = result.ToList ();
            return _nodesInDirectRange;
        }
        public List<NetworkNode> GetNodesInRelayRange () {
            if (_lastInRelayrangeCheck + _updateFrequency > Time.time) return _nodesInRelayRange;
            _lastInRelayrangeCheck = Time.time;

            List<NetworkNode> nodesInRange = new List<NetworkNode> (objectNodesInDirectRange);
            List<NetworkNode> nodesToCheck = new List<NetworkNode> (nodesInRange);
            List<NetworkNode> CheckedNodes = new List<NetworkNode> () { this };
            List<NetworkNode> nodesInRelayRange = new List<NetworkNode> (nodesInRange);

            while (nodesToCheck.Count > 0) {
                NetworkNode currentNode = nodesToCheck.First ();
                nodesToCheck.Remove (currentNode);
                CheckedNodes.Add (currentNode);

                foreach (NetworkNode node in currentNode.objectNodesInDirectRange) {
                    if (!nodesInRelayRange.Contains (node)) {
                        nodesInRelayRange.Add (node);
                        if (!CheckedNodes.Contains (node) && !nodesToCheck.Contains (node)) nodesToCheck.Add (node);
                    }
                }
                //UnityEngine.Debug.Log (nodesToCheck.Count);
            }

            _nodesInRelayRange = nodesInRelayRange;
            //UnityEngine.Debug.Log ("Got Nodes In Relay Range: " + nodesInRelayRange.Count);
            return nodesInRelayRange;
        }

        public List<String> GetCraftAntennas () {
            List<String> _antennas = new List<String> ();
            return _antennas = antennas.Select (Antenna => Antenna.type + " id: " + Antenna.partId).ToList ();
        }

        public float GetSignalStrengthWith (NetworkNode target) {
            return GetNodesInDirectRange ().Find (inRangeNode => inRangeNode.node == target).signalStrengh;
        }

        public float GetWaveLengthWith (NetworkNode target) {
            return GetNodesInDirectRange ().Find (inRangeNode => inRangeNode.node == target).waveLength;
        }
    }

    public class InRangeNode {
        public NetworkNode node;
        public String name;
        public double distance;
        public float signalStrengh;
        public float waveLength;

        public InRangeNode (NetworkNode _node, String _name, double _distance, float _signalStrengh, float _waveLength) {
            node = _node;
            name = _name;
            distance = _distance;
            signalStrengh = _signalStrengh;
            waveLength = _waveLength;
        }
    }
}