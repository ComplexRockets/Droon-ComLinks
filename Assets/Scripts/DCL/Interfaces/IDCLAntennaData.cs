using System.Xml.Linq;

namespace Assets.Scripts.DroonComLinks.Interfaces
{
    public interface IDCLAntennaData
    {
        IAntennaType type { get; }
        float gain { get; }
        int price { get; } //$
        float mass { get; } //Kg
        void OnSizeChanged(float _size);
        XElement GenerateStateXml(bool optimizeXml = true);
    }
}