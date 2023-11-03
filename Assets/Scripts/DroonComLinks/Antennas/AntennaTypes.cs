using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Antennas
{
    public enum AntennaNames { Parabolic, Whip, Patch, Dipole, GroundStation };
    public static class AntennaTypes
    {
        public static readonly ParabolicAntenna parabolic = new();
        public static readonly Whip whip = new();
        public static readonly Patch patch = new();
        public static readonly GroundStation groundStation = new();
        public static readonly Dipole dipole = new();
        public static List<IAntennaType> types => new() { parabolic, whip, patch, dipole, groundStation };
        public static XElement defaultDCLModiferXml = new("DroonComLinks.DCLAntenna",
            new XAttribute("size", "1"),
            new XAttribute("maxPower", "5"),
            new XAttribute("frequency", "10"),
            new XAttribute("efficiency", "0.6"),
            new XAttribute("fractionalBandWidth", "0.5"),
            new XAttribute("bitPerCycle", "1"),
            new XAttribute("customAntenna", "false"));
        public const string dclAntennaPrefix = "DCLAntenna:";
        public const char propertiesSeperator = ',';
        public const char propertyValueSeperator = ':';

        public static IAntennaType GetType(string typeId) => types.Find(t => t.id == typeId);
        public static bool TypeIsValid(string typeId) => GetType(typeId) != null;
        public static string GetTypeFromName(AntennaNames name) => types.Find(t => t.name == name).id;
        public static string GetTypeFromName(string name) => GetTypeFromName((AntennaNames)Enum.Parse(typeof(AntennaNames), name));
        public static float ClampAntennaAtribute(IAntennaType type, AntennaAttributes attribute, float value)
        {
            switch (attribute)
            {
                case AntennaAttributes.efficiency: return Mathf.Min(1, Mathf.Max(value, 0));
                case AntennaAttributes.frequency: return Mathf.Min(type.maxFrequency, Mathf.Max(value, type.minFrequency));
                case AntennaAttributes.maxPower: return Mathf.Min(type.maxPower, Mathf.Max(0, value));
                case AntennaAttributes.maxTxPower: return Mathf.Min(type.maxPower * type.txPowerRatio, Mathf.Max(0, value));
                default: return value;
            }
        }

        public static string GenerateCustomAntennaName(string type, Dictionary<string, string> attributes)
        {
            string result = dclAntennaPrefix + type;
            IAntennaType antennaType = GetType(type);

            foreach (string attribute in attributes.Keys)
            {
                string value = attributes[attribute];
                if (defaultDCLModiferXml.Attribute(attribute)?.Value != value && antennaType.defaultModfierXml.Attribute(attribute)?.Value != value)
                    result += propertiesSeperator + attribute + propertyValueSeperator + value;
            }
            return result;
        }

        public static CustomAntennaResult ParseCustomAntennaName(string name)
        { //DCLAntenna:TypeName,atribute:value,atribute:value,atribute:value,atribute:value
            CustomAntennaResult result = new();
            string[] antennaData = name.Substring(dclAntennaPrefix.Length, name.Length - dclAntennaPrefix.Length).Split(propertiesSeperator);

            if (antennaData.Length > 0)
            {
                IAntennaType type = AntennaTypes.GetType(antennaData[0]);

                if (type != null)
                {
                    result.type = type;

                    for (int i = 1; i < antennaData.Length; i++)
                    {
                        string[] data = antennaData[i].Split(propertyValueSeperator);
                        if (data.Length == 2)
                        {
                            result.properties[data[0]] = data[1];
                        }
                        else result.error = "Property " + antennaData[i] + " is not valid";
                    }

                }
                else result.error = "Type " + antennaData[0] + " not regonized";
            }
            else result.error = "Part name is not valid";

            if (result.error.Length != 0) result.error += ", full name: " + name;
            return result;
        }

        public class ParabolicAntenna : IAntennaType
        {
            public bool custom => false;
            public AntennaNames name => AntennaNames.Parabolic;
            public string id => "ParabolicAntenna";
            public float minFrequency => 2f;
            public float maxFrequency => 50f;
            public float maxFractionalBandWidth => 1.8f;
            public float maxPower => 100f;
            public float azAngle => 3;
            public float eqAngle => azAngle;
            public float txPowerRatio { get; set; } = 0.075f;
            public float defaultFrequency => 10f;
            public float defaultFractionalBandWidth => 0.5f;
            public float defaultSize => 1f;
            public float defaultEfficiency => 0.6f;
            public float defaultMaxPowerConsumption => 50f;
            public bool relayPossible => false;
            public XElement defaultModfierXml => new("DroonComLinks.ParametricalParabolicAntenna",
                new XAttribute("Diameter", "3"),
                new XAttribute("Depth", "0.5"));
        }

        public class Whip : IAntennaType
        {
            public bool custom => false;
            public AntennaNames name => AntennaNames.Whip;
            public string id => "WhipAntenna";
            public float txPowerRatio { get; set; } = 0.033f;
            public float minFrequency => 2f;
            public float maxFrequency => 40f;
            public float maxFractionalBandWidth => 0.8f;
            public float maxPower => 20f;
            public float azAngle => 360f;
            public float eqAngle => 80f;
            public float defaultFrequency => 10f;
            public float defaultFractionalBandWidth => 0.25f;
            public float defaultSize => 1f;
            public float defaultEfficiency => 0.6f;
            public float defaultMaxPowerConsumption => 10f;
            public bool relayPossible => true;
            public XElement defaultModfierXml => new("DroonComLinks.WhipAntenna");
        }

        public class Patch : IAntennaType
        {
            public bool custom => false;
            public AntennaNames name => AntennaNames.Patch;
            public string id => "PatchAntenna";
            public float minFrequency => 2f;
            public float maxFrequency => 4f;
            public float maxFractionalBandWidth => 0.2f;
            public float maxPower => 9f;
            public float txPowerRatio { get; set; } = 0.01f;
            public float azAngle => 360f;
            public float eqAngle => 90f;
            public float defaultFrequency => 3f;
            public float defaultFractionalBandWidth => 0.1f;
            public float defaultSize => 1f;
            public float defaultEfficiency => 0.6f;
            public float defaultMaxPowerConsumption => 5f;
            public bool relayPossible => false;
            public XElement defaultModfierXml => new("DroonComLinks.PatchAntenna");
        }

        public class Dipole : IAntennaType
        {
            public bool custom => false;
            public AntennaNames name => AntennaNames.Dipole;
            public string id => "DipoleAntenna";
            public float txPowerRatio { get; set; } = 0.025f;
            public float minFrequency => 2f;
            public float maxFrequency => 35f;
            public float maxFractionalBandWidth => 0.25f;
            public float maxPower => 13f;
            public float azAngle => 360f;
            public float eqAngle => 120f;
            public float defaultFrequency => 10f;
            public float defaultFractionalBandWidth => 0.2f;
            public float defaultSize => 1f;
            public float defaultEfficiency => 0.6f;
            public float defaultMaxPowerConsumption => 7f;
            public bool relayPossible => true;
            public XElement defaultModfierXml => new("DipoleAntenna");
        }

        public class GroundStation : IAntennaType
        {
            public bool custom => false;
            public AntennaNames name => AntennaNames.GroundStation;
            public string id => "Ground Station";
            public float minFrequency => 1f;
            public float maxFrequency => 100f;
            public float maxFractionalBandWidth => 2f;
            public float maxPower => 300f;
            public float azAngle => 5;
            public float eqAngle => azAngle;
            public float txPowerRatio { get; set; } = 0.09f;
            public float defaultFrequency => 10f;
            public float defaultFractionalBandWidth => 2f;
            public float defaultSize => 1f;
            public float defaultEfficiency => 0.7f;
            public float defaultMaxPowerConsumption => 300f;
            public bool relayPossible => true;
            public XElement defaultModfierXml => new("GroundStation");
        }

        public class Defaults
        {
            public static float parabolicDiameter => 2f;
            public static float whipLength => 0.7f;
        }

        public class CustomAntennaResult
        {
            public IAntennaType type;
            public Dictionary<string, string> properties = new();
            public string error = "";
        }
    }
}