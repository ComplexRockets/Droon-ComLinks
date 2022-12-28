using System.Xml.Linq;

namespace Assets.Scripts.DroonComLinks.Antennas
{
    public interface IAntennaType
    {
        bool custom { get; }
        string id { get; }
        float sensitivityConstant { get; set; }
        float txPowerRatio { get; set;}
        float minFrenquency { get; } //GHz
        float maxFrenquency { get; } //GHz
        float maxFractionalBandWidth { get; }
        float maxPower { get; } //W
        float azAngle { get; } //degrees
        float eqAngle { get; } //degress
        float defaultFrequency { get; } //GHz
        float defaultFractionalBandWidth { get; }
        float defaultSize { get; }
        float defaultEfficiency { get; }
        float defaultMaxPowerConsumption { get; } //W
        bool relayPossible { get; }
        XElement defaultModfierXml { get; }
    }
}