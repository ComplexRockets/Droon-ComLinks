using System;
using System.Xml.Linq;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.DroonComlinks.Ui;
using Assets.Scripts.DroonComLinks.Network;
using Assets.Scripts.DroonComLinks.Ui.ListItems;
using ModApi.Craft.Parts;
using ModApi.Ui.Inspector;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Antennas
{
    public enum AntennaAttributes { efficiency, frequency, fractionalBandWidth, maxPower, maxTxPower, size, diameter, depth, length }
    public class Antenna : IDisplayable
    {
        public string id { get; private set; }
        public NetworkNode node;
        public IAntennaType AntennaType { get; private set; }
        public XElement AntennaModifier { get; private set; }
        public XElement TypeModifier { get; private set; }
        public IPartScript Part { get; private set; }
        public bool IsGS { get; private set; }
        public string Type { get; private set; }
        public int PartId { get; private set; }
        public bool Activated { get; private set; }
        public bool UnderWater
        {
            get
            {
                if (Part != null) { return Part.WaterPhysics.UnderWaterAmount > 0.25; }
                return node.UnderWater;
            }
        }
        public bool Available => Activated && !UnderWater;
        public Antenna ConnectedAntenna { get; private set; }
        public float MaxTransmittedPower { get; private set; }
        public float[] Frequencies { get; private set; }
        public float[] WaveLengths { get; private set; }
        public float FractionalBandWidth { get; private set; }
        public float CenterFrequencyGain { get; private set; }
        public float efficiency, size;
        public float diameter;
        public float length;

        public Antenna(float _diameter)
        {
            AntennaType = AntennaTypes.groundStation;
            PartId = -1;
            Activated = true;
            diameter = _diameter;
            efficiency = AntennaType.defaultEfficiency;
            MaxTransmittedPower = AntennaMath.GetMaxTransmittedPower(AntennaType.defaultMaxPowerConsumption, AntennaType.txPowerRatio);
            IsGS = true;
            Frequencies = new float[3];
            WaveLengths = new float[3];
            CenterFrequencyGain = 30;
        }

        public Antenna(string _type, XElement partXml, XElement _antennaModifier, XElement _typeModifier, IPartScript _part)
        {
            Part = _part;
            CreateAntenna(_type, partXml, _antennaModifier, _typeModifier);
        }

        public Antenna(string _type, XElement partXml, XElement _antennaModifier, XElement _typeModifier) => CreateAntenna(_type, partXml, _antennaModifier, _typeModifier);

        private void CreateAntenna(string _type, XElement partXml, XElement _antennaModifier, XElement _typeModifier)
        {
            AntennaModifier = _antennaModifier;
            TypeModifier = _typeModifier;
            Type = _type;
            AntennaType = AntennaTypes.GetType(Type);
            Activated = partXml.Attribute("activated") != null && partXml.Attribute("activated").Value == "true";
            PartId = int.Parse(partXml.Attribute("id").Value);
            id = Type + " id: " + PartId;
            IsGS = false;
            Frequencies = new float[3];
            WaveLengths = new float[3];
            RefreshAntenna();
        }

        public void RefreshAntenna()
        {
            if (IsGS) return;

            if (Part != null)
            {
                DCLAntennaScript modifier = Part.GetModifier<DCLAntennaScript>();
                if (modifier != null)
                {
                    AntennaModifier = modifier.Data.GenerateStateXml(false);
                    TypeModifier = modifier.AntennaData.GenerateStateXml(false);
                }
            }
            if (AntennaType == null) Debug.LogError("Antenna type null : " + Type);

            MaxTransmittedPower = AntennaMath.GetMaxTransmittedPower(GetXmlAttribute(AntennaModifier, AntennaAttributes.maxPower), AntennaType.txPowerRatio);

            FractionalBandWidth = GetXmlAttribute(AntennaModifier, AntennaAttributes.fractionalBandWidth);
            Frequencies[1] = GetXmlAttribute(AntennaModifier, AntennaAttributes.frequency);
            float halfBandWidth = AntennaMath.GetHalfBandWidth(Frequencies[1], FractionalBandWidth);

            Frequencies[0] = AntennaTypes.ClampAntennaAtribute(AntennaType, AntennaAttributes.frequency, Frequencies[1] - halfBandWidth);
            Frequencies[2] = AntennaTypes.ClampAntennaAtribute(AntennaType, AntennaAttributes.frequency, Frequencies[1] + halfBandWidth);
            WaveLengths[0] = AntennaMath.FWLConversion(Frequencies[0]);
            WaveLengths[1] = AntennaMath.FWLConversion(Frequencies[1]);
            WaveLengths[2] = AntennaMath.FWLConversion(Frequencies[2]);

            efficiency = GetXmlAttribute(AntennaModifier, AntennaAttributes.efficiency);
            size = GetXmlAttribute(AntennaModifier, AntennaAttributes.size);

            diameter = GetXmlAttribute(TypeModifier, AntennaAttributes.diameter) * size;
            length = GetXmlAttribute(TypeModifier, AntennaAttributes.length) * size;

            CenterFrequencyGain = GetGain(WaveLengths[1]);
        }

        public float GetGain(float waveLength)
        {
            switch (AntennaType.name)
            {
                case AntennaNames.Parabolic: case AntennaNames.GroundStation: return AntennaMath.GetParabolicGain(efficiency, diameter, waveLength);
                case AntennaNames.Whip: return AntennaMath.GetWhipGain(efficiency, length, waveLength);
                case AntennaNames.Patch: return AntennaMath.GetPatchGain(efficiency, size, waveLength);
                case AntennaNames.Dipole: return AntennaMath.GetDipoleGain(efficiency, length, waveLength);
                default: Debug.LogError("Missing gain formula for " + AntennaType.name); return -1;
            }
        }

        public float GetXmlAttribute(XElement xml, AntennaAttributes attributeName)
        {
            XAttribute attribute = xml.Attribute(attributeName.ToString());
            if (attribute != null) return float.Parse(attribute.Value);

            switch (attributeName)
            {
                case AntennaAttributes.efficiency: return AntennaType.defaultEfficiency;
                case AntennaAttributes.frequency: return AntennaType.defaultFrequency;
                case AntennaAttributes.fractionalBandWidth: return AntennaType.defaultFractionalBandWidth;
                case AntennaAttributes.maxPower: return AntennaType.defaultMaxPowerConsumption;
                case AntennaAttributes.size: return AntennaType.defaultSize;
                case AntennaAttributes.diameter: return AntennaTypes.Defaults.parabolicDiameter;
                case AntennaAttributes.length: return AntennaTypes.Defaults.whipLength;
                default: Debug.LogError(attributeName.ToString() + " is not a valid xml attribute"); return -1;
            }
        }

        public void toggle(bool active) => Activated = active;

        public void CreateInfoPanel(InspectorModel inspectorModel) { }

        private IUIListItem[,] info;
        public IUIListItem[,] GetInfo()
        {
            if (info == null)
            {
                info = new IUIListItem[2, 16];
                info[0, 0] = (IUIListItem)new UIListTextValue<string>("Type ", () => AntennaType.id, null);
                info[0, 1] = (IUIListItem)new UIListTextValue<string>("NetworkNode ", () => node.id, null);
                info[0, 2] = (IUIListItem)new UIListTextValue<int>("PartID ", () => PartId, null);
                info[0, 3] = (IUIListItem)new UIListTextValue<float>("Gain ", () => ((int)(CenterFrequencyGain * 100)) / 100, null);
                info[0, 4] = (IUIListItem)new UIListTextValue<string>("Min Frequency ", () => DCLUtilities.FormatFrequency(Frequencies[0]), null);
                info[0, 5] = (IUIListItem)new UIListTextValue<string>("Center Frequency ", () => DCLUtilities.FormatFrequency(Frequencies[1]), null);
                info[0, 6] = (IUIListItem)new UIListTextValue<string>("Max Frequency ", () => DCLUtilities.FormatFrequency(Frequencies[2]), null);
                info[0, 7] = (IUIListItem)new UIListTextValue<string>("fractional Band Width ", () => FractionalBandWidth + " %", null);
                info[0, 8] = (IUIListItem)new UIListTextValue<string>("Center WaveLength", () => $"{WaveLengths[1]:n2} m", null);
                info[0, 9] = (IUIListItem)new UIListTextValue<string>("Max Tx Power", () => DCLUtilities.FormatPower(MaxTransmittedPower), null);
                info[0, 9] = (IUIListItem)new UIListTextValue<string>("Effeciency", () => $"{efficiency:n2}", null);
                info[1, 0] = (IUIListItem)new UIListButton("Antenna Xml", delegate (string T, string s, string t, string d) { OnPrintAntennaXML(); });
            }
            return info;
        }

        public void OnPrintAntennaXML()
        {
            string message = "<b>Antenna Modifer:</b>\n" + AntennaModifier + "\n\n<b>Type Modifer:</b>\n" + TypeModifier;
            Debug.Log(message);
            ModApi.Ui.MessageDialogScript dialog = Game.Instance.UserInterface.CreateMessageDialog(message);
            dialog.ExtraWide = true;
        }
    }
}