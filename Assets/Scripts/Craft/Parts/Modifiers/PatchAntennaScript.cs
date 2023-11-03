namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using ModApi.Craft.Parts;
    using UnityEngine;
    using Assets.Scripts.DroonComLinks.Antennas;

    public class PatchAntennaScript : PartModifierScript<PatchAntennaData>, IDCLAntennaScript
    {
        public IDCLAntennaData data => Data;
        private DCLAntennaData _antennaData;
        public DCLAntennaData AntennaData
        {
            get
            {
                _antennaData ??= PartScript.GetModifier<DCLAntennaScript>().Data;
                return _antennaData;
            }
        }
        private GameObject antennaObject;
        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        public void Initialize(DCLAntennaData AntennaData, float size)
        {
            _antennaData = AntennaData;
            if (!AntennaData.CustomAntenna) antennaObject = transform.Find("Antenna").gameObject;
            Data.OnSizeChanged(size);
        }

        public void UpdateAntenna()
        {
            if (!AntennaData.CustomAntenna) antennaObject.transform.localScale = new Vector3(Data.Size, Data.Size, Data.Size);
        }
    }
}