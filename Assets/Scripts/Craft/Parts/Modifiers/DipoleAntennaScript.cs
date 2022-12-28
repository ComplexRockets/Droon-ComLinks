namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts.Design;
    using Assets.Scripts.DroonComLinks.Antennas;
    using ModApi.Craft.Parts;
    using UnityEngine;

    public class DipoleAntennaScript : PartModifierScript<DipoleAntennaData>, IDCLAntennaScript
    {
        public IDCLAntennaData data => Data;
        private DCLAntennaData _antennaData;
        public DCLAntennaData antennaData
        {
            get
            {
                if (_antennaData == null) _antennaData = PartScript.GetModifier<DCLAntennaScript>().Data;
                return _antennaData;
            }
        }
        private GameObject _antennaObject;

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        public void Initialize(DCLAntennaData antennaData, float size)
        {
            _antennaData = antennaData;
            Data.OnSizeChanged(size);
        }

        public void UpdateAntenna()
        {

            if (_antennaObject != null)
            {
                foreach (Renderer r in _antennaObject.GetComponentsInChildren<Renderer>()) PartScript.PartMaterialScript.RemoveRenderer(r);
                GameObject.Destroy(_antennaObject);
            }

            _antennaObject = UnityEngine.Object.Instantiate(Mod.Instance.ResourceLoader.LoadAsset<GameObject>("Assets/Content/Craft/Parts/DipoleAntenna/Prefabs/" + Data.antennaType + "DipoleAntenna.prefab"));
            _antennaObject.transform.SetParent(transform, worldPositionStays: false);
            _antennaObject.transform.localScale = new Vector3(Data.size, Data.size, Data.size);
            float thicknessOffset = Data.thickness > 0.075f ? (Data.thickness - 0.075f) * 2 : 0;
            _antennaObject.transform.Find("Base").localScale = new Vector3(0.2f + thicknessOffset, 0.2f + thicknessOffset, 0.2f + Data.thickness);
            _antennaObject.transform.Find("Segment1").localScale = new Vector3(Data.thickness, Data.length + thicknessOffset, Data.thickness);
            if (Data.antennaType == DipoleAntennaData.DipoleAntennaTypes.Double) _antennaObject.transform.Find("Segment2").localScale = new Vector3(Data.length + thicknessOffset, Data.thickness, Data.thickness);

            PartColliderScript[] componentsInChildren = PartScript.GameObject.GetComponentsInChildren<PartColliderScript>(includeInactive: true);
            foreach (PartColliderScript partColliderScript in componentsInChildren) partColliderScript.gameObject.layer = 31;

            foreach (Renderer r in _antennaObject.GetComponentsInChildren<Renderer>()) PartScript.PartMaterialScript.AddRenderer(r);
            PartScript.OnModifiersCreated();
            if (Game.InDesignerScene) Symmetry.SynchronizePartModifiers(base.PartScript);
        }
    }
}