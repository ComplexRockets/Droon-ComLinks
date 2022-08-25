using System;
using System.Collections.Generic;
using Assets.Scripts.DroonComLinks.Interfaces;
using Assets.Scripts.DroonComLinks.Objects.Antennas;
using Assets.Scripts.DroonComLinks.Objects.ListItems;
using Assets.Scripts.Flight.MapView.Interfaces;
using UnityEngine;
using Vectrosity;

namespace Assets.Scripts.DroonComLinks.Objects
{
    public class NetworkConnection : IDisplayable
    {
        public string id { get; }
        public NetworkNode nodeA { get; }
        public NetworkNode nodeB { get; }
        public bool upLink { get; } = true;
        public bool downLink { get; } = true;
        private VectorLine _line;
        private IMapViewCoordinateConverter _coordinateConverter;
        //private DCLLine _line;
        private Mod _mod => Mod.Instance;
        private float[] frequency;
        private InRangeNode inRangeNode;
        float lineWidth = Mod.Instance.linesScaleFactor;
        Color linecolor = Mod.Instance.defaultLineColor;
        Material lineMaterial;

        public static string connectionID(string A, string B)
        {
            if (string.Compare(A, B) > 0)
                return A + "\n" + B;
            return B + "\n" + A;
        }

        public static Antenna InAntenna(NetworkNode node, NetworkNode sender)
        {
            NetworkConnection connection = Mod.Instance.comLinksManager.ConnectionOf(node, sender);
            if (connection != null) return connection.inRangeNode.antennaB[connection.ConnectionDirectionTo(node)];
            return null;
        }

        public static Antenna OutAntenna(NetworkNode node, NetworkNode receiver)
        {
            NetworkConnection connection = Mod.Instance.comLinksManager.ConnectionOf(node, receiver);
            if (connection != null) return connection.inRangeNode.antennaA[connection.ConnectionDirectionTo(node)];
            return null;
        }

        public int ConnectionDirectionTo(NetworkNode node) => node == nodeA ? 1 : 0;

        public NetworkConnection(NetworkNode _nodeA, NetworkNode _nodeB, IMapViewCoordinateConverter coordinateConverter)
        {
            lineMaterial = UnityEngine.Object.Instantiate(new Material(Shader.Find("Jundroo/MapView/CraftOrbitLine")));
            _coordinateConverter = coordinateConverter;
            nodeA = _nodeA;
            nodeB = _nodeB;
            inRangeNode = nodeA.inrangeNodeFromId[nodeB.id];
            id = connectionID(_nodeA.id, _nodeB.id);
            //_line = new DCLLine(id);

            // if (ModSettings.Instance.diffirenciateUpDownLink)
            // {
            //     upLink = inRangeNode.signalStrength[0] > 0;
            //     downLink = inRangeNode.signalStrength[1] > 0;
            // }
            // else
            // {
            //     upLink = downLink = true;
            // }
            upLink = downLink;
            frequency = inRangeNode.frequency;
            Mod.Instance.LineScaleFactorChanged += ChangeWidth;
        }

        public bool connects(NetworkNode node) => node == nodeA || node == nodeB;
        public bool connects(NetworkNode _nodeA, NetworkNode _nodeB) => (_nodeA == nodeA && _nodeB == nodeB) || (_nodeA == nodeB && _nodeB == nodeA);
        public double upLinkTxPower => AntennaMath.GetTransmittedPower(
            inRangeNode.antennaB[0].minReceivablePower,
            inRangeNode.antennaA[0].GetGain(inRangeNode.waveLength[0]),
            inRangeNode.antennaB[0].GetGain(inRangeNode.waveLength[0]),
            inRangeNode.waveLength[0],
            inRangeNode.distance);
        public double downLinkTxPower => AntennaMath.GetTransmittedPower(
            inRangeNode.antennaA[1].minReceivablePower,
            inRangeNode.antennaB[1].GetGain(inRangeNode.waveLength[1]),
            inRangeNode.antennaA[1].GetGain(inRangeNode.waveLength[1]),
            inRangeNode.waveLength[1],
            inRangeNode.distance);

        public double TxPowerFrom(NetworkNode node)
        {
            if (node == nodeA) return upLinkTxPower;
            return downLinkTxPower;
        }

        public void UpdateConnection()
        {
            Vector3d A = _coordinateConverter.ConvertSolarToMapView(nodeA.position);
            Vector3d B = _coordinateConverter.ConvertSolarToMapView(nodeB.position);

            switch (Mod.Instance.lineColorMode)
            {
                case (LineColorModes.SignalStrength):
                    try
                    {
                        float colorFactor = (inRangeNode.signalStrength[0] + inRangeNode.signalStrength[1]) / 2 * Mod.Instance.signalStrengthFactor;
                        linecolor = new Color((1 - colorFactor), colorFactor, 0.1f);
                    }
                    catch { linecolor = new Color(0.6f, 0.8f, 1f); }
                    break;

                case (LineColorModes.Frequency):
                    linecolor = _mod.GetFrequencyColor(upLink ? frequency[0] : frequency[1]);
                    break;

                case (LineColorModes.LinkDirection):
                    if (upLink && downLink) linecolor = _mod.defaultLineColor;
                    else if (upLink) linecolor = _mod.defaultLineColors[0];
                    else linecolor = _mod.defaultLineColors[1];
                    break;

                default:
                    linecolor = _mod.defaultLineColor;
                    break;

            }

            if (_line == null)
            {
                _line = new VectorLine("DCLLine", new List<Vector3> { (Vector3)A, (Vector3)B }, Mod.Instance.linesScaleFactor, LineType.Continuous);
                _line.layer = 10;
                _line.material = lineMaterial;
                VectorLine.SetCamera3D(Game.Instance.FlightScene.ViewManager.MapViewManager.MapViewCamera);
                _line.Draw3DAuto();
                ChangeWidth(Mod.Instance.linesScaleFactor);
            }

            _line.points3[0] = (Vector3)A;
            _line.points3[1] = (Vector3)B;
            _line.material.SetColor("_startColor", linecolor);
            _line.material.SetColor("_endColor", linecolor);
            _line.AddNormals();


            //_line.UpdateLine((Vector3)A, (Vector3)B, linecolor, _mod.linesScaleFactor, upLink, downLink);
        }

        public void ChangeWidth(float lineScale)
        {
            lineWidth = lineScale;
            _line?.SetWidth(lineScale);
        }

        public void Hide()
        {
            //_line.visible(false);
            VectorLine.Destroy(ref _line);
        }

        public void Destroy()
        {
            //_line.DestroyLine();
            VectorLine.Destroy(ref _line);
        }

        private IUIListItem[,] info;
        public IUIListItem[,] GetInfo()
        {
            if (info == null)
            {
                info = new IUIListItem[2, 16];
                info[0, 0] = new UIListLabelButton<string>("Node A", () => nodeA.id);
                info[1, 0] = new UIListLabelButton<string>("Node B", () => nodeB.id);
                info[0, 1] = new UIListLabelButton<string>("up A", () => inRangeNode.antennaA[0].id);
                info[1, 1] = new UIListLabelButton<string>("down A", () => inRangeNode.antennaA[1].id);
                info[0, 2] = new UIListLabelButton<string>("up B", () => inRangeNode.antennaB[0].id);
                info[1, 2] = new UIListLabelButton<string>("down B", () => inRangeNode.antennaB[1].id);
                info[0, 3] = new UIListTextValue<string>("upLink / downLink", () => CommonMethods.YesNo(upLink) + " / " + CommonMethods.YesNo(downLink));
                info[0, 4] = new UIListTextValue<string>("upLink Frequency", () => frequency[0] + " GHz");
                info[1, 4] = new UIListTextValue<string>("downLink Frequency", () => frequency[1] + " GHz");
                info[0, 5] = new UIListTextValue<string>("upLink Tx Power", () => $"{upLinkTxPower:n2}W");
                info[1, 5] = new UIListTextValue<string>("downLink Tx Power", () => $"{downLinkTxPower:n2}W");
                info[0, 6] = new UIListTextValue<float>("upLink Strength", () => inRangeNode.signalStrength[0]);
                info[1, 6] = new UIListTextValue<float>("downLink Strength", () => inRangeNode.signalStrength[1]);
            }
            return info;
        }
    }
}