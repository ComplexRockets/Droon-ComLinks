namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Assets.Scripts.Design;
    using Assets.Scripts.DroonComLinks.Interfaces;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
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
            _antennaObject.transform.localScale = new Vector3(Data.size * 0.3f, Data.size * 0.3f, Data.size * 0.3f);
            _antennaObject.transform.Find("Segment1").localScale = new Vector3(1, Data.length, 1);
            if (Data.antennaType == DipoleAntennaData.DipoleAntennaTypes.Double) _antennaObject.transform.Find("Segment2").localScale = new Vector3(Data.length, 1, 1);

            PartColliderScript[] componentsInChildren = PartScript.GameObject.GetComponentsInChildren<PartColliderScript>(includeInactive: true);
            foreach (PartColliderScript partColliderScript in componentsInChildren) partColliderScript.gameObject.layer = 31;

            foreach (Renderer r in _antennaObject.GetComponentsInChildren<Renderer>()) PartScript.PartMaterialScript.AddRenderer(r);
            PartScript.OnModifiersCreated();
            if (Game.InDesignerScene) Symmetry.SynchronizePartModifiers(base.PartScript);
        }
    }
}