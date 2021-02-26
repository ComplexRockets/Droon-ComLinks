namespace Assets.Scripts.DroonComLinks.Interfaces {
    public interface IDCLAntennaData {
        string antennaType {
            get;
        }
        IAntennaType type {
            get;
        }
        float gain {
            get;
        }

        void OnSizeChanged (float _size);
    }
}