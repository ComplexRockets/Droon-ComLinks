using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.DroonComLinks.Interfaces;
using Assets.Scripts.DroonComLinks.Objects;
using Assets.Scripts.DroonComLinks.Objects.Antennas;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks
{
    public static class AntennaMath
    {
        private static Dictionary<(XElement, string), float> floatAttributes = new Dictionary<(XElement, string), float>();

        public static double GetMinReceivablePower(float sensitivityConstant, float efficiency) => Mathd.Pow(10, -16) / (efficiency * sensitivityConstant);

        public static float GetMaxTransmittedPower(float maxPowerConsumption, float txPowerRatio) => maxPowerConsumption * txPowerRatio;

        public static double GetReceivedPower(float txPower, float gainA, float gainB, float waveLength, double d) => txPower * gainA * gainB * waveLength * waveLength / (16 * Math.PI * Math.PI * d * d);

        public static double GetTransmittedPower(double rxPower, float gainA, float gainB, float waveLength, double d) => rxPower * 16 * Math.PI * Math.PI * d * d / (gainA * gainB * waveLength * waveLength);

        public static float GetGain(IAntennaType type, XElement antennaModifer, XElement typeModifier, float waveLength)
        {
            try
            {
                //float waveLength = FWLConversion(GetFloatAttribute(type, antennaModifer, FloatAttributes.frequency));

                if (type == AntennaTypes.parametricalParabolic)
                {
                    float diameter = GetFloatAttribute(type, typeModifier, FloatAttributes.diameter);
                    float depth = GetFloatAttribute(type, typeModifier, FloatAttributes.depth);
                    float size = GetFloatAttribute(type, typeModifier, FloatAttributes.size);
                    return GetParabolicGain(GetParabolicFocalLength(diameter, depth), diameter, waveLength);
                }
                else
                {
                    float size = GetFloatAttribute(type, antennaModifer, FloatAttributes.size);
                    float efficiency = GetFloatAttribute(type, antennaModifer, FloatAttributes.efficiency);
                    return GetGain(type, waveLength, size, efficiency);
                }
            }
            catch (Exception e) { Debug.LogError("Error on GetGain " + e); return -1; }
        }

        public static float GetGain(IAntennaType type, float waveLength, float size, float efficiency)
        {
            double a = 9.73 / (waveLength * (type.azAngle * type.eqAngle / 700));
            return (float)(a * a * efficiency * size);
        }

        public static float GetPoleLength(float waveLength, int div) => waveLength / div;
        public static float FWLConversion(float a) => 0.299792458f / a; // Frequency waveLength conversion
        public static float GetBitRate(float bitPerCycle, float frenquecy) => bitPerCycle * frenquecy;
        public static float GetParabolicFocalLength(float diameter, float depth) => diameter * diameter / (16 * depth);
        public static float GetParabolicEficiency(float focalLength, float diameter) => focalLength / diameter;
        public static float GetParabolicGain(float focalLength, float diameter, float waveLength)
        {
            float efficiency = GetParabolicEficiency(focalLength, diameter);
            return efficiency * Mathf.PI * diameter * Mathf.PI * diameter / (waveLength * waveLength);
        }
        public static float GetPartialParabolicGain(float focalLength, float diameter)
        {
            float efficiency = GetParabolicEficiency(focalLength, diameter);
            return efficiency * Mathf.PI * diameter * Mathf.PI * diameter;
        }

        public static float GetOptimisedParabolicGain(float partialGain, float waveLength) => partialGain / (waveLength * waveLength);

        public static float GetHalfBandWidth(float frequency, float fractionalBandWidth) => frequency * fractionalBandWidth / 2;

        public static int GetBasePrice(DCLAntennaData antenna)
        {
            float price = 0;
            price += Mathf.Pow(antenna.size + 1, 4); // Size price
            price += Mathf.Pow(antenna.efficiency + 1, 7); // Efficiency price
            price += Mathf.Pow(antenna.fractionalBandWidth + 1, 5); // Frequency price
            price += Mathf.Pow(antenna.maxTransmittedPower / antenna.Script.antennaData.type.maxPower + 1, 3); // Max tx power price
            return (int)(price);
        }

        public static void GetSignalInfo(NetworkNode A, NetworkNode B, out Antenna[] outA, out Antenna[] outB, out float[] signalStrength, out float[] waveLength)
        {
            signalStrength = new float[2];
            waveLength = new float[2];
            outA = outB = new Antenna[2];

            foreach (Antenna a in A.antennas)
            {
                if (a.activated && !a.underWater)
                {
                    foreach (Antenna b in B.antennas)
                    {
                        if (b.activated && !b.underWater)
                        {
                            float[] wls = new float[2];
                            float abStrength = a.SignalStrengthFrom(b, out wls[0]);
                            float baStrength = b.SignalStrengthFrom(a, out wls[1]);
                            //float wl = a.isGS ? b.waveLength : a.waveLength;

                            if (abStrength > signalStrength[0])
                            {
                                signalStrength[0] = abStrength;
                                waveLength[0] = wls[0];
                                outA[0] = a;
                                outB[0] = b;
                            }
                            if (baStrength > signalStrength[1])
                            {
                                signalStrength[1] = baStrength;
                                waveLength[1] = wls[1];
                                outA[1] = a;
                                outB[1] = b;
                            }
                        }
                    }
                }
            }
        }

        public static void ResetFloatAttributes() => floatAttributes.Clear();

        public static float GetFloatAttribute(IAntennaType type, XElement xml, FloatAttributes attributeName)
        {
            float result = -1;
            if (xml == null) Debug.LogError("XML NULL");
            if (type == null) Debug.LogError("Type NULL");
            try
            {
                if (floatAttributes.ContainsKey((xml, attributeName.ToString()))) return floatAttributes[(xml, attributeName.ToString())];

                XAttribute attribute = xml.Attribute(attributeName.ToString());

                if (attribute != null) result = float.Parse(attribute.Value);
                else if (attributeName == FloatAttributes.efficiency) result = type.defaultEfficiency;
                else if (attributeName == FloatAttributes.frequency) result = type.defaultFrequency;
                else if (attributeName == FloatAttributes.fractionalBandWidth) result = type.defaultFractionalBandWidth;
                else if (attributeName == FloatAttributes.maxPower) result = type.defaultMaxPowerConsumption;
                else if (attributeName == FloatAttributes.size) result = type.defaultSize;
                else if (attributeName == FloatAttributes.diameter) result = AntennaTypes.Defaults.parabolicDiameter;
                else if (attributeName == FloatAttributes.depth) result = AntennaTypes.Defaults.parabolicDepth;
                else { Debug.LogError("atribute : " + attributeName.ToString() + " unknown"); return -1; };

                floatAttributes.Add((xml, attributeName.ToString()), result);
            }
            catch (Exception e) { Debug.LogError("Error on Get Float Attribute" + e); }
            return result;
        }

        public enum FloatAttributes { efficiency, frequency, fractionalBandWidth, maxPower, size, diameter, depth }
    }
}