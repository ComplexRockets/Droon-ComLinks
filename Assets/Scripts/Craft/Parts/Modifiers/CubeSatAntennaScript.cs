namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts.DroonComLinks.Interfaces;
    using ModApi.Craft.Parts;
    using UnityEngine;

    public class CubeSatAntennaScript : PartModifierScript<CubeSatAntennaData>, IDCLAntennaScript
    {
        public IDCLAntennaData data => Data;
        private DCLAntennaData _antennaData;
        public DCLAntennaData antennaData
        {
            get
            {
                if (_antennaData == null) _antennaData = base.PartScript.GetModifier<DCLAntennaScript>().Data;
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
            if (!AntennaData.customAntenna) antennaObject = transform.Find("Antenna").gameObject;
            Data.OnSizeChanged(size);
        }

        public void UpdateAntenna()
        {
            if (!antennaData.customAntenna) antennaObject.transform.localScale = new Vector3(Data.size, Data.size, Data.size);
        }
    }
}