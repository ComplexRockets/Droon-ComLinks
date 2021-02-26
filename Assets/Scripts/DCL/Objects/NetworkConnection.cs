using Assets.Scripts.Flight.MapView.Interfaces;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Objects
{
    public class NetworkConnection {
        public string id;
        public NetworkNode nodeA {
            get;
        }
        public NetworkNode nodeB {
            get;
        }

        private IMapViewCoordinateConverter _coordinateConverter;
        private DCLLine _line;
        private Mod _mod => Mod.Instance;
        private float frequency;

        public NetworkConnection (NetworkNode _nodeA, NetworkNode _nodeB, IMapViewCoordinateConverter coordinateConverter) {
            _coordinateConverter = coordinateConverter;
            nodeA = _nodeA;
            nodeB = _nodeB;
            id = _nodeA.name + _nodeB.name;
            _line = new DCLLine (id);

            frequency = nodeA.GetWaveLengthWith (nodeB);
        }

        public bool connects (NetworkNode node) {
            return node == nodeA || node == nodeB;
        }

        public bool connects (NetworkNode _nodeA, NetworkNode _nodeB) {
            return (_nodeA == nodeA && _nodeB == nodeB) || (_nodeA == nodeB && _nodeB == nodeA);
        }

        public void UpdateConnection () {
            Vector3d A = _coordinateConverter.ConvertSolarToMapView (nodeA.position);
            Vector3d B = _coordinateConverter.ConvertSolarToMapView (nodeB.position);
            Color linecolor;

            if (_mod.lineColorMode == _mod.lineColorModes[1]) {
                try {
                    float colorFactor = nodeA.GetSignalStrengthWith (nodeB);
                    linecolor = new Color ((1 - colorFactor), colorFactor, 0.1f);
                } catch { linecolor = new Color (0.6f, 0.8f, 1f); }
            } else if (_mod.lineColorMode == _mod.lineColorModes[2]) {
                linecolor = _mod.GetWaveLengthColor (frequency);
            } else {
                linecolor = _mod.defaultLineColor;
            }

            _line.UpdateLine ((Vector3) A, (Vector3) B, linecolor, _mod.lineScale);
        }

        public void Hide () {
            _line.visible (false);
        }

        public void Destroy () {
            _line.DestroyLine ();
        }
    }
}