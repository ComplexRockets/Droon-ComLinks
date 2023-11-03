using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.DroonComlinks.Ui;
using Assets.Scripts.DroonComLinks.Antennas;
using Assets.Scripts.DroonComLinks.Ui.ListItems;
using Assets.Scripts.Flight.MapView.Interfaces;
using Assets.Scripts.Flight.Sim;
using ModApi.Ui.Inspector;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Network
{
    public class NetworkNode : IDisplayable
    {
        public bool IsCraft { get; } = true;
        public bool isPlayer = false;
        public CraftNode CraftNode { get; }
        public bool GsInRange { get => _gsInDirectRange || _gsInRange; }
        public bool GsInDirectRange { get => _gsInDirectRange; }
        private bool _gsInRange, _gsInDirectRange;
        public string id { get; }
        public bool UnderWater
        {
            get
            {
                if (IsCraft && CraftNode.Parent.PlanetData.HasWater && CraftNode.AltitudeAgl < 0) return true;
                return false;
            }
        }
        public double Battery
        {
            get
            {
                if (CraftNode?.CraftScript != null) return CraftNode.CraftScript.CommandPods.First().BatteryFuelSource.TotalFuel;
                return _unloadedCraftBattery;
            }
        }
        private readonly double _unloadedCraftBattery;
        public int RelayCount => _comLinksManager.GetNodePathToGS(this).Count - 1;
        private ComLinksManager _comLinksManager;
        public IMapViewCoordinateConverter _coordinateConverter;
        public List<Antenna> Antennas { get; private set; } = new();
        public List<Antenna> AvailableAntennas => Antennas.Where(a => a.Available).ToList();
        private readonly Dictionary<string, Antenna> _antennaFromId = new();
        private readonly Dictionary<int, Antenna> _antennaFromPartId = new();
        public List<InRangeNode> nodesInDirectRange = new();
        public Dictionary<string, InRangeNode> InrangeNodeFromId { get; private set; } = new();
        public List<NetworkNode> nodesInRelayRange = new();
        public List<string> stringNodesInDirectRange = new();
        public List<string> stringNodesInRelayRange = new();
        public List<NetworkNode> objectNodesInDirectRange = new();
        public Vector3d Position => IsCraft ? CraftNode.SolarPosition : GsPosition;
        private Vector3d GsPosition => parent.SolarPosition + parent.SurfaceVectorToPlanetVector(_gslocalPos);// * Mod.Instance.heightScale;
        private Vector3d _gslocalPos;
        public PlanetNode parent;
        public bool relayRangeInitialised;
        public string nodesInDirectRangeReport;

        public NetworkNode(CraftNode craftnode, List<Antenna> _antennas, ComLinksManager comlinksManager, double unloadedCraftBattery)
        {
            CraftNode = craftnode;
            id = CraftNode.Name + " id: " + CraftNode.NodeId;
            parent = (PlanetNode)CraftNode.Parent;
            _unloadedCraftBattery = unloadedCraftBattery;

            Antennas = _antennas;
            foreach (Antenna antenna in _antennas)
            {
                _antennaFromId.Add(antenna.id, antenna);
                _antennaFromPartId.Add(antenna.PartId, antenna);
            }

            SetUp(comlinksManager);
        }
        public NetworkNode(string name, Vector3d pos, PlanetNode parent, ComLinksManager comLinksManager)
        {
            IsCraft = false;
            _gslocalPos = pos;
            this.id = name;
            Antennas = new List<Antenna>() { { new Antenna(35f) } }; //, 8f
            this.parent = parent;
            _unloadedCraftBattery = 99999;
            SetUp(comLinksManager);
        }

        public void ForceRefresh() { foreach (Antenna antenna in Antennas) antenna.RefreshAntenna(); }

        private void SetUp(ComLinksManager comLinksManager)
        {
            _comLinksManager = comLinksManager;
            _coordinateConverter = _comLinksManager.coordinateConverter;
            foreach (Antenna antenna in Antennas) antenna.node = this;
        }

        public void StartNetworkUpdate()
        {
            nodesInDirectRange.Clear();
            InrangeNodeFromId.Clear();
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
            nodesInDirectRangeReport = "Nodes in direct range repport ----- [" + id + "] ----- Target count [" + _comLinksManager.NetworkNodes.Count + "]\n";

            if (ModSettings.Instance.NeedPower && Battery < ComLinksManager.minPower) return;

            foreach (NetworkNode target in _comLinksManager.NetworkNodes)
            {
                ConnectionInfo c = CheckConnectionWith(target);
                if (c.connectionResult != ConnectionResults.Success)
                {
                    nodesInDirectRangeReport += "     [" + target.id + "] Failed : " + c.connectionResult + " / " + c.signalInfoResult + "\n";
                    foreach (SignalStrengthResults s in c.signalStrengthResults) nodesInDirectRangeReport += "          " + s + "\n";
                    continue;
                }

                InRangeNode inRangeNode = new(target, c);
                target.OnFoundInRange(this, c);
                nodesInDirectRange.Add(inRangeNode);
                InrangeNodeFromId.Add(target.id, inRangeNode);
                stringNodesInDirectRange.Add(target.id);
                objectNodesInDirectRange.Add(target);
                if (!target.IsCraft) _gsInDirectRange = true;
            }
        }

        public ConnectionInfo CheckConnectionWith(NetworkNode target)
        {
            if (InrangeNodeFromId.ContainsKey(target.id)) return new ConnectionInfo(ConnectionResults.Duplicate);
            if (!IsCraft && !target.IsCraft) return new ConnectionInfo(ConnectionResults.GS2GS);
            if (target.id == this.id) return new ConnectionInfo(ConnectionResults.ToSelf);
            if (ModSettings.Instance.NeedPower && target.Battery < ComLinksManager.minPower) return new ConnectionInfo(ConnectionResults.MissingBattery);
            if (DCLUtilities.ConnectionObstructed(Position, target.Position)) return new ConnectionInfo(ConnectionResults.Obstructed);

            SignalInfoResults signalInforesult = AntennaMath.GetSignalInfo(this, target, out Antenna A, out Antenna B, out float signalStrength, out float waveLength, out double distance, out List<SignalStrengthResults> signalStrengthResults);
            if (signalStrength <= 0) return new ConnectionInfo(ConnectionResults.Signal2Weak, signalInforesult, signalStrengthResults); ;

            return new ConnectionInfo(A, B, signalStrength, waveLength, distance, signalStrengthResults);
        }

        public void CheckConnectionsOcclusion()
        {
            List<InRangeNode> nodesInRelayRange = new(nodesInDirectRange);
            foreach (InRangeNode target in nodesInRelayRange)
            {
                if (DCLUtilities.ConnectionObstructed(Position, target.node.Position))
                {
                    nodesInDirectRange.Remove(target);
                    objectNodesInDirectRange.Remove(target.node);
                    stringNodesInDirectRange.Remove(target.node.id);
                }
            }
        }

        public void OnFoundInRange(NetworkNode target, ConnectionInfo c)
        {
            InRangeNode inRangeNode = new(target, c);
            nodesInDirectRange.Add(inRangeNode);
            InrangeNodeFromId.Add(target.id, inRangeNode);
            stringNodesInDirectRange.Add(target.id);
            objectNodesInDirectRange.Add(target);
            if (!target.IsCraft) _gsInDirectRange = true;
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
                if (!target.IsCraft) _gsInRange = true;
                GetNodesInRelayRange(target);
            }

        }

        public void TransfertNodesInRelayRange()
        {
            List<NetworkNode> baseNodesInRelayRange = new(nodesInRelayRange);
            List<string> baseStringNodesInRelayRange = new(stringNodesInRelayRange);
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

        public Antenna GetAntennaFromPartId(int partId)
        {
            if (!IsCraft)
            {
                Debug.LogError("Node is GroundStation: antenna " + partId + " doesn't exist,    (GetAntennaFromPartId)");
                return null;
            }

            Antenna antenna = _antennaFromPartId[partId];
            if (antenna == null)
            {
                Debug.LogError("Antenna for part " + partId + " not found,    (GetAntennaFromPartId)");
                return null;
            }
            return antenna;
        }

        public Antenna GetAntennaFromId(string antennaId)
        {
            if (!IsCraft)
            {
                Debug.LogError("Node is GroundStation: antenna " + antennaId + " doesn't exist,    (GetAntennaFromId)");
                return null;
            }

            Antenna antenna = _antennaFromId[antennaId];
            if (antenna == null)
            {
                Debug.LogError("Antenna for part " + antennaId + " not found,    (GetAntennaFromId)");
                return null;
            }
            return antenna;
        }

        public void CreateInfoPanel(InspectorModel inspectorModel)
        {
            GroupModel g = inspectorModel.Add(new GroupModel("Node Info"));

            g.Add(new TextModel("Node", () => id));
            g.Add(new TextModel("Location", () => parent.Name));
            g.Add(new TextModel("Type", () => IsCraft ? "Craft" : "Ground Station"));

            g.Add(new SpacerModel());

            if (IsCraft)
            {
                g.Add(new TextModel("Ground Station In Range", () => DCLUtilities.YesNo(GsInRange)));
                g.Add(new TextModel("Relay Count", () => RelayCount.ToString()));
            }
            g.Add(new TextModel("Nodes In Range", () => stringNodesInRelayRange.Count.ToString()));
            g.Add(new TextModel("Nodes In Direct Range", () => nodesInDirectRange.Count.ToString()));
            g.Add(new TextModel("Antennas", () => Antennas.Count.ToString()));
            g.Add(new TextModel("UnderWater", () => DCLUtilities.YesNo(UnderWater)));

            g.Add(new SpacerModel());

            g.Add(new TextButtonModel("Print Report", b => OnPrintReport()));
        }

        private IUIListItem[,] info;
        public IUIListItem[,] GetInfo()
        {
            if (info == null)
            {
                info = new IUIListItem[2, 16];
                if (IsCraft)
                {
                    info[0, 0] = new UIListTextValue<string>("Node Type", () => "Player", null);
                    info[0, 2] = new UIListTextValue<bool>("Ground Station In Range", () => GsInRange, null);
                    info[0, 3] = new UIListTextValue<int>("Relay Count", () => RelayCount, null);
                }
                else
                {
                    info[0, 0] = new UIListTextValue<string>("Node Type", () => "Ground Station", null);
                }
                info[0, 1] = new UIListTextValue<string>("Location", () => parent.Name, null);
                info[0, 4] = new UIListTextValue<int>("Nodes In Range:", () => stringNodesInRelayRange.Count, null);
                info[0, 5] = new UIListDropDown(UIListItems.nodesInRelayRangeDropDownID, () => stringNodesInRelayRange, null);
                info[0, 6] = new UIListTextValue<int>("Nodes In Direct Range:", () => nodesInDirectRange.Count, null);
                info[0, 7] = new UIListDropDown(UIListItems.nodesInDirectRangeDropDownID, () => InrangeNodeFromId.Keys, null);
                info[0, 8] = new UIListTextValue<int>("Antennas:", () => Antennas.Count, null);
                info[0, 9] = new UIListDropDown(UIListItems.antennasDropDownID, () => _antennaFromId.Keys, null);
                info[1, 0] = new UIListTextValue<bool>("UnderWater", () => UnderWater, null);
                info[1, 1] = new UIListButton("Print Report", delegate (string T, string s, string t, string d) { OnPrintReport(); });
            }
            return info;
        }

        public void OnPrintReport()
        {
            DCLUtilities.ShowMapViewMessage("Report printed to dev console");
            Debug.Log(nodesInDirectRangeReport);
        }
    }

    public struct InRangeNode
    {
        public NetworkNode node;
        public double distance;
        public float signalStrength, waveLength, frequency;
        public Antenna antennaA, antennaB;

        public InRangeNode(NetworkNode _node, ConnectionInfo c)
        {
            node = _node;
            distance = c.distance;
            signalStrength = c.signalStrength;
            waveLength = c.waveLength;
            frequency = c.frequency;
            antennaA = c.antennaA;
            antennaB = c.antennaB;
        }
    }

    public enum ConnectionResults { Success, Duplicate, GS2GS, ToSelf, MissingBattery, Obstructed, Signal2Weak };

    public struct ConnectionInfo
    {
        public Antenna antennaA, antennaB;
        public float signalStrength, waveLength, frequency;
        public double distance;
        public ConnectionResults connectionResult;
        public SignalInfoResults signalInfoResult;
        public List<SignalStrengthResults> signalStrengthResults;

        public ConnectionInfo(ConnectionResults _connectionResult, SignalInfoResults _signalInfoResults = SignalInfoResults.NotTested, List<SignalStrengthResults> _signalStrengthResults = null)
        {
            antennaA = antennaB = null;
            distance = signalStrength = waveLength = frequency = -1;
            connectionResult = _connectionResult;
            signalInfoResult = _signalInfoResults;
            signalStrengthResults = _signalStrengthResults == null ? new List<SignalStrengthResults>() : _signalStrengthResults;
        }

        public ConnectionInfo(Antenna A, Antenna B, float _signalStrength, float _waveLength, double _distance, List<SignalStrengthResults> _signalStrengthResults)
        {
            connectionResult = ConnectionResults.Success;
            signalInfoResult = SignalInfoResults.Succes;
            signalStrengthResults = _signalStrengthResults;
            distance = _distance;
            signalStrength = _signalStrength;
            waveLength = _waveLength;
            frequency = AntennaMath.FWLConversion(waveLength);
            antennaA = A;
            antennaB = B;
        }
    }
}