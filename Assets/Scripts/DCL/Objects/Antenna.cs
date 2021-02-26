using System.Xml.Linq;
using Assets.Scripts.DroonComLinks.Interfaces;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Objects
{
    public class Antenna {
        public NetworkNode node;
        public IAntennaType antennaType {
            get;
            private set;
        }
        public XElement antennaXml {
            get;
            private set;
        }
        public bool isGS {
            get;
            private set;
        }
        public string type {
            get;
            private set;
        }
        public int partId {
            get;
            private set;
        }
        public float gain {
            get {
                if (isGS) {
                    return 10 * Mathf.Log (_efficiency * (Mathf.PI * _diameter / waveLength) * (Mathf.PI * _diameter / waveLength));
                }
                return _gain;
            }
        }
        public float maxTransmittedPower {
            get;
            private set;
        }

        public double minReceivablePower {
            get;
            private set;
        }
        public float waveLength {
            get;
            private set;
        }
        public bool activated {
            get;
            private set;
        }
        private float _gain;
        private float _diameter;
        private float _efficiency;

        public Antenna ConnectedAntenna {
            get;
            private set;
        }
        public Antenna (string _type, float diameter, float efficiency, float _maxTransmittedPower) {
            type = _type;
            partId = -1;
            activated = true;
            _diameter = diameter;
            _efficiency = efficiency;
            maxTransmittedPower = _maxTransmittedPower;
            _gain = 40;
            isGS = true;
        }

        // public Antenna (string _type, int _partId, bool _activated, float gain, float _maxTransmittedPower, double _minReceivablePower, float _waveLength) {
        //     type = _type;
        //     partId = _partId;
        //     activated = _activated;
        //     _gain = gain;
        //     maxTransmittedPower = _maxTransmittedPower;
        //     minReceivablePower = _minReceivablePower;
        //     waveLength = _waveLength;
        //     isGS = false;
        // }

        public Antenna (XElement partXml, XElement _xml) {
            antennaXml = _xml;
            type = partXml.Attribute ("partType").Value;
            antennaType = AntennaMath.GetAntennaType (type);
            activated = partXml.Attribute ("activated") != null && partXml.Attribute ("activated").Value == "true";
            partId = int.Parse (partXml.Attribute ("id").Value);
            isGS = false;
            RefreshAntenna ();
        }

        public void RefreshAntenna () {
            _gain = AntennaMath.GetGain (antennaType, antennaXml);
            maxTransmittedPower = AntennaMath.GetMaxTransmittedPower (AntennaMath.GetFloatAttribute (antennaType, antennaXml, "Maxpowerconsumption"), antennaType.txPowerRatio);
            minReceivablePower = AntennaMath.GetMinReceivedPower (antennaType.sensitivityConstant, AntennaMath.GetFloatAttribute (antennaType, antennaXml, "Efficency"), gain);
            waveLength = AntennaMath.FrequencyToWaveLength (AntennaMath.GetFloatAttribute (antennaType, antennaXml, "Frequency"));
        }

        public void toggleAntenna (bool active) {
            activated = active;
        }

        /// <summary>Get the strength of the signal recieved from target </summary>
        /// <param name="target">the antenna from wich the signal comes from</param>
        public float SignalStrengthFrom (Antenna target) {
            if (isGS) waveLength = target.waveLength;
            else if (target.isGS) target.waveLength = waveLength;
            else if (waveLength != target.waveLength) return -1;

            double distance = Vector3d.Distance (node.position, target.node.position);
            double receivedPower = AntennaMath.GetReceivedPower (target.maxTransmittedPower, gain, target.gain, waveLength, distance);

            //Debug.Log ("Node: " + node.name + " gain : " + gain + " || Target Node: " + target.node.name + " gain : " + target.gain + " || distance : " + distance + " || received power " + receivedPower + " || Signal Strength: " + (float) ((receivedPower - minReceivablePower) / receivedPower) * Mod.Instance.signalStrengthFactor);

            if (receivedPower >= minReceivablePower) return (float) ((receivedPower - minReceivablePower) / receivedPower) * Mod.Instance.signalStrengthFactor;
            return -1;
        }
    }
}