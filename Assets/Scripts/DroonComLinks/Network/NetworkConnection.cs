using System;
using System.Collections.Generic;
using Assets.Scripts.DroonComlinks.Ui;
using Assets.Scripts.DroonComLinks.Antennas;
using Assets.Scripts.DroonComLinks.Ui.ListItems;
using Assets.Scripts.Flight.MapView.Interfaces;
using ModApi.Ui.Inspector;
using UnityEngine;
using Vectrosity;

namespace Assets.Scripts.DroonComLinks.Network
{
    public class NetworkConnection : IDisplayable
    {
        public string id { get; }
        public NetworkNode NodeA { get; }
        public NetworkNode NodeB { get; }
        private VectorLine _line;
        private readonly IMapViewCoordinateConverter _coordinateConverter;
        private static Mod Mod => Mod.Instance;
        private readonly float frequency;
        private InRangeNode inRangeNode;
        private Color linecolor = Mod.Instance.defaultLineColor;
        private readonly Material lineMaterial;

        public static string ConnectionID(string A, string B)
        {
            if (string.Compare(A, B) > 0) return A + "\n" + B;
            return B + "\n" + A;
        }

        public static Antenna PlayerAntennaConnectingTo(NetworkNode node)
        {
            NetworkConnection connection = ComLinksManager.Instance.ConnectionOf(ComLinksManager.Instance.Player, node);
            if (connection == null) return null;
            return connection.inRangeNode.antennaA.node.isPlayer ? connection.inRangeNode.antennaA : connection.inRangeNode.antennaB;
        }

        public NetworkConnection(NetworkNode _nodeA, NetworkNode _nodeB, IMapViewCoordinateConverter coordinateConverter)
        {
            lineMaterial = UnityEngine.Object.Instantiate(new Material(Shader.Find("Jundroo/MapView/CraftOrbitLine")));
            _coordinateConverter = coordinateConverter;
            NodeA = _nodeA;
            NodeB = _nodeB;
            inRangeNode = NodeA.InrangeNodeFromId[NodeB.id];
            id = ConnectionID(_nodeA.id, _nodeB.id);

            frequency = inRangeNode.frequency;
            Mod.Instance.LineScaleFactorChanged += ChangeWidth;
        }

        public bool Connects(NetworkNode node) => node == NodeA || node == NodeB;
        public bool Connects(NetworkNode _nodeA, NetworkNode _nodeB) => (_nodeA == NodeA && _nodeB == NodeB) || (_nodeA == NodeB && _nodeB == NodeA);
        public double TxPower => AntennaMath.GetTransmittedPower(
            AntennaMath.GetMinReceivablePower((inRangeNode.antennaA.efficiency + inRangeNode.antennaB.efficiency) / 2),
            inRangeNode.antennaA.GetGain(inRangeNode.waveLength),
            inRangeNode.antennaB.GetGain(inRangeNode.waveLength),
            inRangeNode.waveLength,
            inRangeNode.distance);

        public void UpdateConnection()
        {
            Vector3d A = _coordinateConverter.ConvertSolarToMapView(NodeA.Position);
            Vector3d B = _coordinateConverter.ConvertSolarToMapView(NodeB.Position);

            switch (Mod.Instance.lineColorMode)
            {
                case LineColorModes.SignalStrength:
                    try
                    {
                        float colorFactor = Mathf.Min((inRangeNode.signalStrength - 1) * Mod.Instance.signalStrengthFactor, 1);
                        linecolor = new Color(1 - colorFactor, colorFactor, 0.1f);
                    }
                    catch { linecolor = new Color(0.6f, 0.8f, 1f); }
                    break;

                case LineColorModes.Frequency:
                    linecolor = Mod.GetFrequencyColor(frequency);
                    break;

                default:
                    linecolor = Mod.defaultLineColor;
                    break;

            }

            if (_line == null)
            {
                _line = new VectorLine("DCLLine", new List<Vector3> { (Vector3)A, (Vector3)B }, Mod.Instance.linesScaleFactor * 3, LineType.Continuous)
                {
                    layer = 10,
                    material = lineMaterial
                };
                VectorLine.SetCamera3D(Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera);
                _line.Draw3DAuto();
            }

            _line.points3[0] = (Vector3)A;
            _line.points3[1] = (Vector3)B;
            _line.material.SetColor("_startColor", linecolor);
            _line.material.SetColor("_endColor", linecolor);
            _line.AddNormals();
        }

        public void ChangeWidth(float lineScale) => _line?.SetWidth(lineScale * 3);

        public void Destroy() => VectorLine.Destroy(ref _line);

        public void CreateInfoPanel(InspectorModel inspectorModel) { }
        private IUIListItem[,] info;
        public IUIListItem[,] GetInfo()
        {
            if (info == null)
            {
                info = new IUIListItem[2, 16];
                info[0, 0] = new UIListLabelButton<string>("Node A", () => NodeA.id, null);
                info[1, 0] = new UIListLabelButton<string>("Node B", () => NodeB.id, null);
                info[0, 1] = new UIListLabelButton<string>("Antenna A", () => inRangeNode.antennaA.id, null);
                info[1, 1] = new UIListLabelButton<string>("Antenna B", () => inRangeNode.antennaB.id, null);
                info[0, 2] = new UIListTextValue<string>("Frequency", () => DCLUtilities.FormatFrequency(frequency), null);
                info[0, 3] = new UIListTextValue<string>("Tx Power", () => DCLUtilities.FormatPower(TxPower), null);
                info[0, 4] = new UIListTextValue<float>("Signal Strength", () => inRangeNode.signalStrength, null);
            }
            return info;
        }
    }
}