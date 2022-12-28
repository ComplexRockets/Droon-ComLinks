using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Antennas
{
    public static class AntennaTypes
    {
        public readonly static ParabolicAntenna parametricalParabolic = new ParabolicAntenna();
        public readonly static Whip whip = new Whip();
        public readonly static CubeSatSBand cubeSatSBand = new CubeSatSBand();
        public readonly static GroundStation groundStation = new GroundStation();
        public readonly static Dipole dipole = new Dipole();
        public static List<IAntennaType> types => new List<IAntennaType>() { parametricalParabolic, whip, cubeSatSBand, dipole, groundStation };
        public static XElement defaultDCLModiferXml = new XElement("DroonComLinks.DCLAntenna",
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
        public enum FloatAttributes { Efficiency, Frequency, MaxPower, MaxTxPower }

        public static IAntennaType GetType(string typeId) => types.Find(t => t.id == typeId);
        public static bool TypeIsValid(string typeId) => GetType(typeId) != null;
        public static float ClampFloatAtribute(IAntennaType type, FloatAttributes attribute, float value)
        {
            if (attribute == FloatAttributes.Efficiency) return Mathf.Min(1, Mathf.Max(value, 0));
            else if (attribute == FloatAttributes.Frequency) return Mathf.Min(type.maxFrenquency, Mathf.Max(value, type.minFrenquency));
            else if (attribute == FloatAttributes.MaxPower) return Mathf.Min(type.maxPower, Mathf.Max(0, value));
            else if (attribute == FloatAttributes.MaxTxPower) return Mathf.Min(type.maxPower * type.txPowerRatio, Mathf.Max(0, value));
            return value;
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
            CustomAntennaResult result = new CustomAntennaResult();
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
            public string id => "ParabolicAntenna";
            public float sensitivityConstant { get; set; } = 2.5f;
            public float minFrenquency => 2f;
            public float maxFrenquency => 50f;
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
            public XElement defaultModfierXml => new XElement("DroonComLinks.ParametricalParabolicAntenna",
                new XAttribute("Diameter", "3"),
                new XAttribute("Depth", "0.5"));
        }

        public class Whip : IAntennaType
        {
            public bool custom => false;
            public string id => "WhipAntenna";
            public float sensitivityConstant { get; set; } = 1.3f;
            public float txPowerRatio { get; set; } = 0.033f;
            public float minFrenquency => 2f;
            public float maxFrenquency => 40f;
            public float maxFractionalBandWidth => 0.8f;
            public float maxPower => 20f;
            public float azAngle => 360f;
            public float eqAngle => 80f;
            public float defaultFrequency => 10f;
            public float defaultFractionalBandWidth => 0.3f;
            public float defaultSize => 1f;
            public float defaultEfficiency => 0.6f;
            public float defaultMaxPowerConsumption => 10f;
            public bool relayPossible => true;
            public XElement defaultModfierXml => new XElement("DroonComLinks.WhipAntenna");
        }

        public class CubeSatSBand : IAntennaType
        {
            public bool custom => false;
            public string id => "CubeSatSBandAntenna";
            public float sensitivityConstant { get; set; } = 5f;
            public float minFrenquency => 2f;
            public float maxFrenquency => 4f;
            public float maxFractionalBandWidth => 0.2f;
            public float maxPower => 9f;
            public float txPowerRatio { get; set; } = 0.025f;
            public float azAngle => 360f;
            public float eqAngle => 90f;
            public float defaultFrequency => 3f;
            public float defaultFractionalBandWidth => 0.1f;
            public float defaultSize => 1f;
            public float defaultEfficiency => 0.6f;
            public float defaultMaxPowerConsumption => 5f;
            public bool relayPossible => false;
            public XElement defaultModfierXml => new XElement("DroonComLinks.CubeSatAntenna");
        }

        public class Dipole : IAntennaType
        {
            public bool custom => false;
            public string id => "DipoleAntenna";
            public float sensitivityConstant { get; set; } = 0.9f;
            public float txPowerRatio { get; set; } = 0.03f;
            public float minFrenquency => 2f;
            public float maxFrenquency => 35f;
            public float maxFractionalBandWidth => 0.25f;
            public float maxPower => 13f;
            public float azAngle => 360f;
            public float eqAngle => 120f;
            public float defaultFrequency => 10f;
            public float defaultFractionalBandWidth => 0.1f;
            public float defaultSize => 1f;
            public float defaultEfficiency => 0.6f;
            public float defaultMaxPowerConsumption => 7f;
            public bool relayPossible => true;
            public XElement defaultModfierXml => new XElement("DipoleAntenna");
        }

        public class GroundStation : IAntennaType
        {
            public bool custom => false;
            public string id => "Ground Station";
            public float minFrenquency => 0.1f;
            public float maxFrenquency => 100f;
            public float maxFractionalBandWidth => 2f;
            public float maxPower => 300f;
            public float azAngle => 5;
            public float eqAngle => azAngle;
            public float txPowerRatio { get; set; } = 0.09f;
            public float sensitivityConstant { get; set; } = 0.001f;
            public float defaultFrequency => 10f;
            public float defaultFractionalBandWidth => 2f;
            public float defaultSize => 1f;
            public float defaultEfficiency => 0.7f;
            public float defaultMaxPowerConsumption => 300f;
            public bool relayPossible => true;
            public XElement defaultModfierXml => new XElement("GroundStation");
        }

        public class Defaults
        {
            public static float parabolicDiameter => 2f;
            public static float parabolicDepth => 0.25f;
        }

        [Serializable]
        public class CustomType : IAntennaType
        {
            public bool custom => true;
            public string id { get; set; }
            public float minFrenquency { get; set; }
            public float maxFrenquency { get; set; }
            public float maxFractionalBandWidth { get; set; }
            public float maxPower { get; set; }
            public float azAngle { get; set; }
            public float eqAngle { get; set; }
            public float txPowerRatio { get; set; }
            public float sensitivityConstant { get; set; }
            public float defaultFrequency { get; set; }
            public float defaultFractionalBandWidth { get; set; }
            public float defaultSize { get; set; }
            public float defaultEfficiency { get; set; }
            public float defaultMaxPowerConsumption { get; set; }
            public bool relayPossible { get; set; }
            public XElement defaultModfierXml => new XElement("DroonComLinks.CustomType");

            public CustomType() { }

            public CustomType(
                float minFrenquency,
                float maxFrenquency,
                float maxFractionalBandWidth,
                float maxPower,
                float azAngle,
                float eqAngle,
                float txPowerRatio,
                float sensitivityConstant,
                float defaultFrequency,
                float defaultFractionalBandWidth,
                float defaultSize,
                float defaultEfficency,
                float defaultMaxPowerConsumption,
                string typeId,
                bool relay)
            {
                Set(minFrenquency,
                    maxFrenquency,
                    maxFractionalBandWidth,
                    maxPower,
                    azAngle,
                    eqAngle,
                    txPowerRatio,
                    sensitivityConstant,
                    defaultFrequency,
                    defaultFractionalBandWidth,
                    defaultSize,
                    defaultEfficency,
                    defaultMaxPowerConsumption,
                    typeId,
                    relay);
            }

            public void Set(
                float minFrenquency,
                float maxFrenquency,
                float maxFractionalBandWidth,
                float maxPower,
                float azAngle,
                float eqAngle,
                float txPowerRatio,
                float sensitivityConstant,
                float defaultFrequency,
                float defaultFractionalBandWidth,
                float defaultSize,
                float defaultEfficency,
                float defaultMaxPowerConsumption,
                string typeId,
                bool relay)
            {
                this.id = typeId;
                this.minFrenquency = minFrenquency;
                this.maxFrenquency = maxFrenquency;
                this.maxFractionalBandWidth = maxFractionalBandWidth;
                this.maxPower = maxPower;
                this.azAngle = azAngle;
                this.eqAngle = eqAngle;
                this.txPowerRatio = txPowerRatio;
                this.sensitivityConstant = sensitivityConstant;
                this.defaultFrequency = defaultFrequency;
                this.defaultFractionalBandWidth = defaultFractionalBandWidth;
                this.defaultSize = defaultSize;
                this.defaultEfficiency = defaultEfficency;
                this.defaultMaxPowerConsumption = defaultMaxPowerConsumption;
                this.relayPossible = relay;
            }
        }

        public class CustomAntennaResult
        {
            public IAntennaType type;
            public Dictionary<string, string> properties = new Dictionary<string, string>();
            public string error = "";
        }
    }
}