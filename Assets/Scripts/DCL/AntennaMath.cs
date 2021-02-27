using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Assets.Scripts.DroonComLinks.Interfaces;
using Assets.Scripts.DroonComLinks.Objects;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks {
    public static class AntennaMath {
        private static Dictionary < (XElement, string), float > floatAttributes = new Dictionary < (XElement, string), float > ();
        public static double GetMinReceivedPower (float sensitivityConstant, float efficiency, float gain) {
            if (Mod.Instance.logaritmicValues) {
                return 15 * Mathd.Log10 (Mathd.Pow (10, -16) / (efficiency * sensitivityConstant));
            } else {
                return Mathd.Pow (10, -16) / (efficiency * sensitivityConstant);
            }
        }

        public static float GetMaxTransmittedPower (float maxPowerConsumption, float txPowerRatio) => maxPowerConsumption / txPowerRatio;

        public static double GetReceivedPower (float txPower, float gainA, float gainB, float waveLength, double d) {
            if (Mod.Instance.logaritmicValues) {
                return 10 * Mathd.Log10 (txPower) + 10 * Math.Log10 (gainA) + 10 * Math.Log10 (gainB) + 20 * Mathd.Log10 (waveLength / (4 * Math.PI * d));
            } else {
                return txPower * gainA * gainB * waveLength / (4 * Math.PI * d) * waveLength / (4 * Math.PI * d);
            }
        }
        public static float GetGain (IAntennaType type, XElement xml) {
            try {
                float waveLength = FrequencyToWaveLength (GetFloatAttribute (type, xml, "Frequency"));

                if (type == AntennaTypes.parametricalParabolic) {
                    float diameter = GetFloatAttribute (type, xml, "Diameter");
                    float depth = GetFloatAttribute (type, xml, "Depth");
                    return GetParabolicGain (GetParabolicFocalLength (diameter, depth), diameter, waveLength);
                } else {
                    float size = GetFloatAttribute (type, xml, "Size");
                    float efficiency = GetFloatAttribute (type, xml, "Efficency");
                    return GetGain (type, waveLength, size, efficiency);
                }
            } catch { Debug.LogError ("Error on GetGain"); return -1; }
        }

        public static float GetGain (IAntennaType type, float waveLength, float size, float efficiency) {
            double a = 9.73 / (waveLength * Mathd.Pow (10, type.azAngle * type.eqAngle / Mod.Instance.gainConstant));
            return (float) (a * a * efficiency + 10 * size);
        }
        public static float FrequencyToWaveLength (float frequency) => 0.3f / frequency;
        public static float GetBitRate (float bitPerCycle, float frenquecy) => bitPerCycle * frenquecy;
        public static float GetParabolicGain (float focalLength, float diameter, float waveLength) {
            float efficiency = GetParabolicEficiency (focalLength, diameter);
            return efficiency * (Mathf.PI * diameter / waveLength) * (Mathf.PI * diameter / waveLength);
        }
        public static float GetParabolicFocalLength (float diameter, float depth) => (diameter * diameter) / (16 * depth);
        public static float GetParabolicEficiency (float focalLength, float diameter) => focalLength / diameter;

        public static void GetSignalInfo (NetworkNode A, NetworkNode B, out float signalStrengh, out float waveLength) {
            signalStrengh = waveLength = 0;
            foreach (Antenna a in A.antennas) {
                if (a.activated) {
                    foreach (Antenna b in B.antennas) {
                        if (b.activated) {
                            float abStrength = a.SignalStrengthFrom (b);
                            if (abStrength > signalStrengh) {
                                float baStrength = b.SignalStrengthFrom (a);
                                if (baStrength > signalStrengh) {
                                    signalStrengh = abStrength > baStrength? baStrength : abStrength;
                                    waveLength = a.isGS? b.waveLength : a.waveLength;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static IAntennaType GetAntennaType (string typeID) {
            if (typeID == "ParametricalParabolicAntenna") return AntennaTypes.parametricalParabolic;
            if (typeID == "WhipAntenna") return AntennaTypes.whip;
            if (typeID == "CubeSatSBandAntenna") return AntennaTypes.cubeSatSBand;
            return null;
        }

        public static void ResetFloatAttributes () {
            floatAttributes.Clear ();
        }

        public static float GetFloatAttribute (IAntennaType type, XElement xml, string attributeName) {
            float result = -1;
            try {
                if (floatAttributes.ContainsKey ((xml, attributeName))) return floatAttributes[(xml, attributeName)];

                XAttribute attribute = xml.Attribute (attributeName);

                if (attribute != null) result = float.Parse (attribute.Value);
                else if (attributeName == "Efficency") result = type.defaultEfficency;
                else if (attributeName == "Frequency") result = type.defaultFrequency;
                else if (attributeName == "Maxpowerconsumption") result = type.defaultMaxPowerConsumption;
                else if (attributeName == "Size") result = type.defaultSize;
                else if (attributeName == "Diameter") result = AntennaTypes.Defaults.parabolicDiameter;
                else if (attributeName == "Depth") result = AntennaTypes.Defaults.parabolicDepth;

                floatAttributes.Add ((xml, attributeName), result);
            } catch (Exception e) { Debug.LogError ("Error on Get Float Attribute " + e); }
            return result;
        }
    }
}