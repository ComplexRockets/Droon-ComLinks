using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.DroonComlinks.Ui;
using Assets.Scripts.DroonComLinks.Antennas;
using Assets.Scripts.DroonComLinks.Ui.ListItems;
using Assets.Scripts.Flight.MapView.Interfaces;
using Assets.Scripts.Flight.Sim;
using ModApi.Craft.Parts;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Network
{
    public class NetworkNode : IDisplayable
    {
        public bool isCraft { get; } = true;
        public bool isPlayer = false;
        public CraftNode craftNode { get; }
        public bool gsInRange
        {
            get
            {
                return _gsInDirectRange || _gsInRange;
            }
        }
        public bool gsInDirectRange
        {
            get
            {
                return _gsInDirectRange;
            }
        }
        private bool _gsInRange, _gsInDirectRange;
        public string id { get; }
        public bool underWater
        {
            get
            {
                if (isCraft && craftNode.Parent.PlanetData.HasWater && craftNode.AltitudeAgl < 0) return true;
                return false;
            }
        }
        public double battery
        {
            get
            {
                if (craftNode?.CraftScript != null) return craftNode.CraftScript.CommandPods.First().BatteryFuelSource.TotalFuel;
                return _unloadedCraftBattery;
            }
        }
        private double _unloadedCraftBattery;
        public int relayCount => _comLinksManager.GetNodePathToGS(this).Count;
        private ComLinksManager _comLinksManager;
        public IMapViewCoordinateConverter _coordinateConverter;
        public List<Antenna> antennas { get; private set; } = new List<Antenna>();
        public Dictionary<string, Antenna> antennaFromId { get; private set; } = new Dictionary<string, Antenna>();
        public Dictionary<int, Antenna> antennaFromPartId { get; private set; } = new Dictionary<int, Antenna>();
        public List<InRangeNode> nodesInDirectRange = new List<InRangeNode>();
        public Dictionary<string, InRangeNode> inrangeNodeFromId { get; private set; } = new Dictionary<string, InRangeNode>();
        public List<NetworkNode> nodesInRelayRange = new List<NetworkNode>();
        public List<string> stringNodesInDirectRange = new List<string>();
        public List<string> stringNodesInRelayRange = new List<string>();
        public List<NetworkNode> objectNodesInDirectRange = new List<NetworkNode>();
        public Vector3d position => isCraft ? craftNode.SolarPosition : _gsPosition;
        private Vector3d _gsPosition => _parent.SolarPosition + _parent.SurfaceVectorToPlanetVector(_gslocalPos) * Mod.Instance.heightScale;
        private Vector3d _gslocalPos;
        private PlanetNode _parent;
        private float _updateFrequency => _comLinksManager.updatePeriod;
        private double _lastInrangeCheck = 0;
        private double _lastInRelayrangeCheck = 0;
        public bool relayRangeInitialised;

        public NetworkNode(CraftNode craftnode, List<Antenna> _antennas, ComLinksManager comLinksManager, double unloadedCraftBattery)
        {
            craftNode = craftnode;
            id = craftNode.Name + " id: " + craftNode.NodeId;
            _parent = (PlanetNode)craftNode.Parent;
            _unloadedCraftBattery = unloadedCraftBattery;

            antennas = _antennas;
            foreach (Antenna antenna in _antennas)
            {
                antennaFromId.Add(antenna.id, antenna);
                antennaFromPartId.Add(antenna.partId, antenna);
            }

            SetUp(comLinksManager);
        }
        public NetworkNode(string name, Vector3d pos, PlanetNode parent, ComLinksManager comLinksManager)
        {
            isCraft = false;
            _gslocalPos = pos;
            this.id = name;
            antennas = new List<Antenna>() { { new Antenna(35f, 8f) } };
            _parent = parent;
            _unloadedCraftBattery = 99999;
            SetUp(comLinksManager);
        }

        public void ForceRefresh()
        {
            foreach (Antenna antenna in antennas) antenna.RefreshAntenna();
        }

        private void SetUp(ComLinksManager comLinksManager)
        {
            _comLinksManager = comLinksManager;
            _coordinateConverter = _comLinksManager.coordinateConverter;
            foreach (Antenna antenna in antennas)
            {
                antenna.node = this;
            }
        }

        public void StartNetworkUpdate()
        {
            nodesInDirectRange.Clear();
            inrangeNodeFromId.Clear();
            stringNodesInDirectRange.Clear();
            objectNodesInDirectRange.Clear();
            _gsInDirectRange = _gsInRange = false;
        }

        public void StartLightNetworkUpdate()
        {
            nodesInRelayRange.Clear();
            stringNodesInRelayRange.Clear();
            relayRangeInitialised = false;
        }

        public void GetNodesInDirectRange()
        {
            List<NetworkNode> targets = new List<NetworkNode>(_comLinksManager.networkNodes);

            if (ModSettings.Instance.needPower && battery < ComLinksManager.minPower) return;

            foreach (NetworkNode target in targets)
            {
                if (inrangeNodeFromId.ContainsKey(target.id)) continue;
                if (target == this) continue;
                if (!isCraft && !target.isCraft) continue;
                if (ModSettings.Instance.needPower && target.battery < ComLinksManager.minPower) continue;
                if (ConnectionObstructed(position, target.position)) continue;

                float[] signalStrength, waveLength;
                Antenna[] A, B;
                AntennaMath.GetSignalInfo(this, target, out A, out B, out signalStrength, out waveLength);
                if (signalStrength[0] <= 0 || signalStrength[1] <= 0) continue; // || to differentiate Up and down link

                InRangeNode inRangeNode = new InRangeNode(target, A, B, Vector3d.Distance(position, target.position), signalStrength, waveLength);
                target.OnFoundInRange(this, inRangeNode);
                nodesInDirectRange.Add(inRangeNode);
                inrangeNodeFromId.Add(target.id, inRangeNode);
                stringNodesInDirectRange.Add(target.id);
                objectNodesInDirectRange.Add(target);
                if (!target.isCraft) _gsInDirectRange = true;
            }
        }

        public void CheckConnectionsOcclusion()
        {
            List<InRangeNode> nodesInRelayRange = new List<InRangeNode>(nodesInDirectRange);
            foreach (InRangeNode target in nodesInRelayRange)
            {
                if (ConnectionObstructed(position, target.node.position))
                {
                    nodesInDirectRange.Remove(target);
                    objectNodesInDirectRange.Remove(target.node);
                    stringNodesInDirectRange.Remove(target.node.id);
                }
            }
        }

        public void OnFoundInRange(NetworkNode target, InRangeNode targetInRangeNode)
        {
            float[] signalStrength = new float[2] { targetInRangeNode.signalStrength[1], targetInRangeNode.signalStrength[0] };
            float[] waveLength = new float[2] { targetInRangeNode.waveLength[1], targetInRangeNode.waveLength[0] };
            InRangeNode inRangeNode = new InRangeNode(target, targetInRangeNode.antennaB, targetInRangeNode.antennaA, targetInRangeNode.distance, signalStrength, waveLength);
            nodesInDirectRange.Add(inRangeNode);
            inrangeNodeFromId.Add(target.id, inRangeNode);
            stringNodesInDirectRange.Add(target.id);
            objectNodesInDirectRange.Add(target);
            if (!target.isCraft) _gsInDirectRange = true;
        }

        public void GetNodesInRelayRange(NetworkNode node)
        {
            if (node == null && relayRangeInitialised) return; //If this is the initial recursive call and this node is already initialised, skip
            List<NetworkNode> inRangeNodes = node == null ? objectNodesInDirectRange : node.objectNodesInDirectRange;

            foreach (NetworkNode target in inRangeNodes)
            {
                if (nodesInRelayRange.Contains(target)) continue;
                if (target == this) continue;

                nodesInRelayRange.Add(target);
                stringNodesInRelayRange.Add(target.id);
                if (!target.isCraft) _gsInRange = true;
                GetNodesInRelayRange(target);
            }

        }

        public void TransfertNodesInRelayRange()
        {
            List<NetworkNode> baseNodesInRelayRange = new List<NetworkNode>(nodesInRelayRange);
            List<string> baseStringNodesInRelayRange = new List<string>(stringNodesInRelayRange);
            baseNodesInRelayRange.Add(this);
            stringNodesInRelayRange.Add(this.id);

            foreach (NetworkNode target in nodesInRelayRange)
            {
                if (!target.relayRangeInitialised) target.OnFoundInRelayRange(baseNodesInRelayRange, baseStringNodesInRelayRange, _gsInRange);
            }

            relayRangeInitialised = true;
        }

        public void OnFoundInRelayRange(List<NetworkNode> baseNodesInRelayRange, List<string> baseStringNodesInRelayRange, bool gsInRange)
        {
            nodesInRelayRange = new List<NetworkNode>(baseNodesInRelayRange);
            stringNodesInRelayRange = new List<string>(baseStringNodesInRelayRange);
            nodesInRelayRange.Remove(this);
            stringNodesInRelayRange.Remove(this.id);
            _gsInRange = gsInRange;
            relayRangeInitialised = true;
        }

        public bool ConnectionObstructed(Vector3d A, Vector3d B)
        {
            Vector3d center = (A + B) / 2;
            foreach (PlanetNode planet in _comLinksManager.planets)
            {
                double planetMagnitude = planet.SolarPosition.magnitude;
                if (!((A.magnitude > planetMagnitude * 1.1 && B.magnitude > planetMagnitude * 1.1) || (A.magnitude < planetMagnitude * 0.9 && B.magnitude < planetMagnitude * 0.9)))
                {
                    if (LineSphereIntersect(A, B, planet.SolarPosition, planet.PlanetData.Radius)) return true;
                    if ((center - planet.SolarPosition).magnitude < planet.PlanetData.Radius) return true;
                }
            }
            return false;
        }

        private bool LineSphereIntersect(Vector3d A, Vector3d B, Vector3d C, double r)
        {
            r *= Mod.Instance.radiusScale;

            double u = ((C.x - A.x) * (B.x - A.x) + (C.y - A.y) * (B.y - A.y) + (C.z - A.z) * (B.z - A.z)) / ((B.x - A.x) * (B.x - A.x) + (B.y - A.y) * (B.y - A.y) + (B.z - A.z) * (B.z - A.z));
            double d = (C - A + (A - B) * u).magnitude;
            if (u > 0 && u < 1 && d < r) return true;
            return false;
        }

        private IUIListItem[,] info;
        public IUIListItem[,] GetInfo()
        {
            if (info == null)
            {
                info = new IUIListItem[2, 16];
                if (isCraft)
                {
                    info[0, 0] = (IUIListItem)new UIListTextValue<string>("Node Type", () => "Player");
                    info[0, 2] = (IUIListItem)new UIListTextValue<bool>("Ground Station In Range", () => gsInRange);
                    info[0, 3] = (IUIListItem)new UIListTextValue<int>("Relay Count", () => relayCount);
                }
                else
                {
                    info[0, 0] = (IUIListItem)new UIListTextValue<string>("Node Type", () => "Ground Station");
                }
                info[0, 1] = (IUIListItem)new UIListTextValue<string>("location", () => _parent.Name);
                info[0, 4] = (IUIListItem)new UIListTextValue<int>("Nodes In Range:", () => stringNodesInRelayRange.Count);
                info[0, 5] = (IUIListItem)new UIListDropDown(UIListItems.nodesInRelayRangeDropDownID, () => stringNodesInRelayRange);
                info[0, 6] = (IUIListItem)new UIListTextValue<int>("Nodes In Direct Range:", () => nodesInDirectRange.Count);
                info[0, 7] = (IUIListItem)new UIListDropDown(UIListItems.nodesInDirectRangeDropDownID, () => inrangeNodeFromId.Keys);
                info[0, 8] = (IUIListItem)new UIListTextValue<int>("Antennas:", () => antennas.Count);
                info[0, 9] = (IUIListItem)new UIListDropDown(UIListItems.antennasDropDownID, () => antennaFromId.Keys);
                info[1, 0] = (IUIListItem)new UIListTextValue<bool>("UnderWater", () => underWater);
            }
            return info;
        }
    }

    public class InRangeNode
    {
        public NetworkNode node;
        public double distance;
        public float[] signalStrength, waveLength, frequency = new float[2];
        public Antenna[] antennaA, antennaB;

        public InRangeNode(NetworkNode _node, Antenna[] A, Antenna[] B, double _distance, float[] _signalStrength, float[] _waveLength)
        {
            node = _node;
            distance = _distance;
            signalStrength = _signalStrength;
            waveLength = _waveLength;
            frequency[0] = AntennaMath.FWLConversion(waveLength[0]);
            frequency[1] = AntennaMath.FWLConversion(waveLength[1]);
            antennaA = A;
            antennaB = B;
        }
    }
}