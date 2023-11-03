using System.Xml.Linq;

namespace Assets.Scripts.DroonComLinks.Antennas
{
    public interface IDCLAntennaData
    {
        IAntennaType type { get; }
        int price { get; } //$
        float mass { get; } //Kg
        void OnSizeChanged(float _size);
        XElement GenerateStateXml(bool optimizeXml = true);
    }
}