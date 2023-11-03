using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.DroonComLinks.Network;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Antennas
{
    public enum SignalInfoResults { Succes, NoAvailableAntennas, Signal2Weak, NotTested }
    public enum SignalStrengthResults { Succes, NoCommonFreq, Signal2Weak }

    public static class AntennaMath
    {
        public static double GetMinReceivablePower(float efficiency) => Mathd.Pow(10, -15) / efficiency;

        public static float GetMaxTransmittedPower(float maxPowerConsumption, float txPowerRatio) => maxPowerConsumption * txPowerRatio;

        public static double GetReceivedPower(float txPower, float gainA, float gainB, float waveLength, double d) => txPower * gainA * gainB * waveLength * waveLength / (16 * Math.PI * Math.PI * d * d);

        public static double GetTransmittedPower(double rxPower, float gainA, float gainB, float waveLength, double d) => rxPower * 16 * Math.PI * Math.PI * d * d / (gainA * gainB * waveLength * waveLength);

        public static float FWLConversion(float a) => 0.299792458f / a; // Frequency waveLength conversion
        public static float GetBitRate(float bitPerCycle, float frenquecy) => bitPerCycle * frenquecy;
        public static float GetParabolicFocalLength(float diameter, float depth) => diameter * diameter / (16 * depth);
        public static float GetParabolicGain(float efficiency, float diameter, float waveLength)
        {
            float a = Mathf.PI * diameter / waveLength;
            return efficiency * a * a;
        }
        public static float GetWhipGain(float efficiency, float length, float waveLength) => efficiency * 1.76f * length / waveLength;
        public static float GetDipoleGain(float efficiency, float length, float waveLength) => efficiency * 1.5f * length / waveLength;
        public static float GetPatchGain(float efficiency, float size, float waveLength) => efficiency * 4 * Mathf.PI * 0.0025f * size / (waveLength * waveLength); //0.0025m2 = area of patch antenna of scale 1

        public static float GetHalfBandWidth(float frequency, float fractionalBandWidth) => frequency * fractionalBandWidth / 2;

        public static int GetBasePrice(DCLAntennaData antenna)
        {
            float price = 0;
            price += Mathf.Pow(antenna.Size + 1, 4); // Size price
            price += Mathf.Pow(antenna.Efficiency + 1, 13); // Efficiency price
            price += Mathf.Pow(antenna.FractionalBandWidth + 1, 5); // Frequency price
            price += Mathf.Pow(antenna.MaxTransmittedPower / antenna.Script.AntennaData.type.maxPower + 1, 3); // Max tx power price
            return (int)(price);
        }

        public static SignalInfoResults GetSignalInfo(NetworkNode A, NetworkNode B, out Antenna outA, out Antenna outB, out float signalStrength, out float waveLength, out double distance, out List<SignalStrengthResults> signalStrengthResults)
        {
            signalStrengthResults = new List<SignalStrengthResults>();
            distance = signalStrength = waveLength = 0;
            outA = outB = null;

            if (A.AvailableAntennas.Count == 0 || B.AvailableAntennas.Count == 0) return SignalInfoResults.NoAvailableAntennas;

            foreach (Antenna a in A.AvailableAntennas)
            {
                foreach (Antenna b in B.AvailableAntennas)
                {
                    signalStrengthResults.Add(SignalStrengthBetween(a, b, out double strength, out float wl, out distance));

                    if (strength < signalStrength) continue;
                    signalStrength = (float)strength;
                    waveLength = wl;
                    outA = a;
                    outB = b;
                }

            }

            if (signalStrength == 0) return SignalInfoResults.Signal2Weak;
            if (signalStrength > Mod.Instance.maxSignalStrength) Mod.Instance.maxSignalStrength = signalStrength;
            if (signalStrength < Mod.Instance.minSignalStrength) Mod.Instance.minSignalStrength = signalStrength;
            return SignalInfoResults.Succes;
        }

        public static SignalStrengthResults SignalStrengthBetween(Antenna A, Antenna B, out double signalStrength, out float wl, out double distance)
        {
            distance = signalStrength = wl = 0;

            if (A.IsGS) wl = B.WaveLengths[1];
            else if (B.IsGS) wl = A.WaveLengths[1];
            else if (A.Frequencies[0] >= B.Frequencies[0] && A.Frequencies[0] <= B.Frequencies[2])
                wl = A.WaveLengths[0];
            else if (B.Frequencies[0] >= A.Frequencies[0] && B.Frequencies[0] <= A.Frequencies[2])
                wl = B.WaveLengths[0];
            else return SignalStrengthResults.NoCommonFreq;

            float gainA = A.GetGain(wl);
            float gainB = B.GetGain(wl);

            distance = Vector3d.Distance(A.node.Position, B.node.Position);
            signalStrength = GetReceivedPower(Mathf.Min(A.MaxTransmittedPower, B.MaxTransmittedPower), gainA, gainB, wl, distance) / GetMinReceivablePower(A.efficiency * B.efficiency);

            if (signalStrength > 1) return SignalStrengthResults.Succes;
            return SignalStrengthResults.Signal2Weak;
        }
    }
}