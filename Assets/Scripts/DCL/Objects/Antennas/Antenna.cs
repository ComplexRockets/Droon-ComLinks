using System;
using System.Xml.Linq;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.DroonComLinks.Interfaces;
using Assets.Scripts.DroonComLinks.Objects.ListItems;
using ModApi.Craft.Parts;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Objects.Antennas
{
    public class Antenna : IDisplayable
    {
        public string id { get; private set; }
        public NetworkNode node;
        public IAntennaType antennaType { get; private set; }
        public XElement antennaModifier { get; private set; }
        public XElement typeModifier { get; private set; }
        public IPartScript part { get; private set; }
        public bool isGS { get; private set; }
        public string type { get; private set; }
        public int partId { get; private set; }
        public bool underWater
        {
            get
            {
                if (part != null) { return part.WaterPhysics.UnderWaterAmount > 0.25; }
                return node.underWater;
            }
        }
        public Antenna ConnectedAntenna { get; private set; }
        public float maxTransmittedPower { get; private set; }
        public double minReceivablePower { get; private set; }
        public float[] frequencies { get; private set; }
        public float[] waveLengths { get; private set; }
        public float fractionalBandWidth { get; private set; }
        public bool activated { get; private set; }
        public float centerFrequencyGain { get; private set; }
        private float _diameter;
        private float _depth;
        private float _efficiency;
        private float _partialParabolicGain;

        public Antenna(float diameter, float depth)
        {
            antennaType = AntennaTypes.groundStation;
            partId = -1;
            activated = true;
            _diameter = diameter;
            _depth = depth;
            _efficiency = antennaType.defaultEfficiency;
            maxTransmittedPower = AntennaMath.GetMaxTransmittedPower(antennaType.defaultMaxPowerConsumption, antennaType.txPowerRatio);
            minReceivablePower = AntennaMath.GetMinReceivablePower(antennaType.sensitivityConstant, _efficiency);
            isGS = true;
            frequencies = new float[3];
            waveLengths = new float[3];
            centerFrequencyGain = 30;
            _partialParabolicGain = AntennaMath.GetPartialParabolicGain(AntennaMath.GetParabolicFocalLength(diameter, depth), diameter);
        }

        public Antenna(string _type, XElement partXml, XElement _antennaModifier, XElement _typeModifier, IPartScript _part)
        {
            part = _part;
            CreateAntenna(_type, partXml, _antennaModifier, _typeModifier);
        }

        public Antenna(string _type, XElement partXml, XElement _antennaModifier, XElement _typeModifier) => CreateAntenna(_type, partXml, _antennaModifier, _typeModifier);

        private void CreateAntenna(string _type, XElement partXml, XElement _antennaModifier, XElement _typeModifier)
        {
            antennaModifier = _antennaModifier;
            typeModifier = _typeModifier;
            type = _type;
            antennaType = AntennaTypes.GetType(type);
            activated = partXml.Attribute("activated") != null && partXml.Attribute("activated").Value == "true";
            partId = int.Parse(partXml.Attribute("id").Value);
            id = type + " id: " + partId;
            isGS = false;
            frequencies = new float[3];
            waveLengths = new float[3];
            RefreshAntenna();
        }

        public void RefreshAntenna()
        {
            if (isGS)
            {
                _efficiency = antennaType.defaultEfficiency;
                maxTransmittedPower = AntennaMath.GetMaxTransmittedPower(antennaType.defaultMaxPowerConsumption, antennaType.txPowerRatio);
                minReceivablePower = AntennaMath.GetMinReceivablePower(antennaType.sensitivityConstant, _efficiency);
            }
            else
            {
                if (part != null)
                {
                    DCLAntennaScript modifier = part.GetModifier<DCLAntennaScript>();
                    if (modifier != null)
                    {
                        antennaModifier = modifier.Data.GenerateStateXml(false);
                        typeModifier = modifier.antennaData.GenerateStateXml(false);
                    }
                }
                if (antennaType == null) Debug.Log("Antenna type null : " + type);

                maxTransmittedPower = AntennaMath.GetMaxTransmittedPower(AntennaMath.GetFloatAttribute(antennaType, antennaModifier, AntennaMath.FloatAttributes.maxPower), antennaType.txPowerRatio);
                minReceivablePower = AntennaMath.GetMinReceivablePower(antennaType.sensitivityConstant, AntennaMath.GetFloatAttribute(antennaType, antennaModifier, AntennaMath.FloatAttributes.efficiency));

                fractionalBandWidth = AntennaMath.GetFloatAttribute(antennaType, antennaModifier, AntennaMath.FloatAttributes.fractionalBandWidth);
                frequencies[1] = AntennaMath.GetFloatAttribute(antennaType, antennaModifier, AntennaMath.FloatAttributes.frequency);
                float halfBandWidth = AntennaMath.GetHalfBandWidth(frequencies[1], fractionalBandWidth);

                frequencies[0] = AntennaTypes.ClampFloatAtribute(antennaType, AntennaTypes.FloatAttributes.Frequency, frequencies[1] - halfBandWidth);
                frequencies[2] = AntennaTypes.ClampFloatAtribute(antennaType, AntennaTypes.FloatAttributes.Frequency, frequencies[1] + halfBandWidth);
                waveLengths[0] = AntennaMath.FWLConversion(frequencies[0]);
                waveLengths[1] = AntennaMath.FWLConversion(frequencies[1]);
                waveLengths[2] = AntennaMath.FWLConversion(frequencies[2]);

                centerFrequencyGain = AntennaMath.GetGain(antennaType, antennaModifier, typeModifier, waveLengths[1]);

                if (antennaType == AntennaTypes.parametricalParabolic)
                {
                    _diameter = AntennaMath.GetFloatAttribute(antennaType, antennaModifier, AntennaMath.FloatAttributes.diameter);
                    _depth = AntennaMath.GetFloatAttribute(antennaType, antennaModifier, AntennaMath.FloatAttributes.depth);
                    _partialParabolicGain = AntennaMath.GetPartialParabolicGain(AntennaMath.GetParabolicFocalLength(_diameter, _depth), _diameter);
                }
            }
        }

        public float GetGain(float waveLength)
        {
            if (isGS || antennaType == AntennaTypes.parametricalParabolic) return AntennaMath.GetOptimisedParabolicGain(_partialParabolicGain, waveLength);
            return AntennaMath.GetGain(antennaType, antennaModifier, typeModifier, waveLength);
        }

        public void toggle(bool active)
        {
            activated = active;
        }

        /// <summary>Get the strength of the signal recieved from target </summary>
        /// <param name="target">the antenna from wich the signal comes from</param>
        public float SignalStrengthFrom(Antenna target, out float wl)
        {
            wl = 0;
            if (isGS) wl = target.waveLengths[1];
            else if (target.isGS)
            {
                wl = waveLengths[1];
            }
            else if (waveLengths[1] == target.waveLengths[1]) wl = waveLengths[1];
            else
            {
                if (Mod.Instance.autoFrequency)
                {
                    if (frequencies[0] > target.frequencies[0] && frequencies[0] < target.frequencies[2])
                        wl = waveLengths[0];
                    else if (target.frequencies[0] > frequencies[0] && target.frequencies[0] < frequencies[2])
                        wl = target.waveLengths[0];
                    else return -1;
                }
                else return -1;
            }

            float gainA = GetGain(wl);
            float gainB = target.GetGain(wl);

            double distance = Vector3d.Distance(node.position, target.node.position);
            double receivedPower = AntennaMath.GetReceivedPower(target.maxTransmittedPower, gainA, gainB, wl, distance);

            //Debug.Log("Node: " + node.id + " gain : " + gain + " || Target Node: " + target.node.id + " gain : " + target.gain + " || distance : " + distance + " || received power " + receivedPower + " || Max tx power: " + target.maxTransmittedPower + " || min receivable power: " + minReceivablePower);

            if (receivedPower > Math.Max(Double.Epsilon * 2, minReceivablePower))
            {
                // Mod.Instance.averageStrength = (Mod.Instance.nbrOfStrength * Mod.Instance.averageStrength + receivedPower) / (Mod.Instance.nbrOfStrength + 1);
                // Mod.Instance.nbrOfStrength++;
                // if (receivedPower < Mod.Instance.minStrength) Mod.Instance.minStrength = receivedPower;
                // if (receivedPower > Mod.Instance.maxStrength) Mod.Instance.maxStrength = receivedPower;
                return (float)receivedPower;
            }
            return -1f;
        }

        private IUIListItem[,] info;
        public IUIListItem[,] GetInfo()
        {
            if (info == null)
            {
                info = new IUIListItem[2, 16];
                info[0, 0] = new UIListTextValue<string>("Type ", () => antennaType.id);
                info[0, 1] = new UIListTextValue<string>("NetworkNode ", () => node.id);
                info[0, 2] = new UIListTextValue<int>("PartID ", () => partId);
                info[0, 3] = new UIListTextValue<float>("Gain ", () => ((int)(centerFrequencyGain * 100)) / 100);
                info[0, 4] = new UIListTextValue<string>("Min Frequency ", () => frequencies[0] + " GHz");
                info[0, 5] = new UIListTextValue<string>("Center Frequency ", () => frequencies[1] + " GHz");
                info[0, 6] = new UIListTextValue<string>("Max Frequency ", () => frequencies[2] + " GHz");
                info[0, 7] = new UIListTextValue<string>("fractional Band Width ", () => fractionalBandWidth + " %");
                info[0, 8] = new UIListTextValue<string>("Center WaveLength", () => $"{waveLengths[1]:n2} m");
                info[0, 9] = new UIListTextValue<string>("Min Rx Power", () => $"{minReceivablePower * Math.Pow(10, 19):n2} E-19 W");
                info[0, 10] = new UIListTextValue<string>("Max Tx Power", () => $"{maxTransmittedPower:n2} W");
                info[1, 0] = new UIListButton("Antenna Xml");
            }
            return info;
        }
    }
}