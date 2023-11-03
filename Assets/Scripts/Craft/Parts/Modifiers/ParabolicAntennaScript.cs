namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts.Design;
    using Assets.Scripts.DroonComLinks.Antennas;
    using Assets.Scripts.DroonComLinks.Antennas.ParabolicAntenna;
    using ModApi.Craft.Parts;
    using UnityEngine;

    public class ParabolicAntennaScript : PartModifierScript<ParabolicAntennaData>, IDCLAntennaScript
    {
        public IDCLAntennaData data => Data;
        public DCLAntennaData AntennaData
        {
            get
            {
                if (_antennaData == null) _antennaData = base.PartScript.GetModifier<DCLAntennaScript>().Data;
                return _antennaData;
            }
        }
        private DCLAntennaData _antennaData;
        private readonly Mod _mod = Mod.Instance;
        private GameObject _parabolaParent;
        private ParabolicAntennaMesh _parabola;
        public UpperElement upperElement { get; private set; }
        public LowerElement lowerElement { get; private set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        public void Initialize(DCLAntennaData AntennaData, float size)
        {
            _antennaData = AntennaData;
            if (!this.AntennaData.CustomAntenna)
            {
                _parabolaParent = transform.Find("Mesh").gameObject;
                _parabola = transform.GetComponentInChildren<ParabolicAntennaMesh>();
                _parabola.Initialize();
            }

            Data.OnSizeChanged(size);
        }

        public void UpdateAntenna()
        {

            if (!AntennaData.CustomAntenna)
            {
                upperElement?.Destroy();
                upperElement = new UpperElement(Data.UpperElement, Data.SecondaryReflector, _parabolaParent.transform, PartScript);
                upperElement.gameObject.transform.localPosition = new Vector3(0, Data.FocalLength, 0);
                upperElement.gameObject.transform.localScale = new Vector3(Data.Radius, Data.Radius, Data.Radius);

                lowerElement?.Destroy();
                if (Data.SecondaryReflector)
                {
                    lowerElement = new LowerElement(Data.LowerElement, _parabolaParent.transform, PartScript);
                    lowerElement.gameObject.transform.localScale = new Vector3(Data.Radius, Data.Radius, Data.Radius);
                }

                _parabola.CreateMesh(Data, upperElement.offset);
                _parabolaParent.transform.localPosition = new Vector3(0, -_parabola.LowestPoint, 0);

                PartScript.InitializeColliders();
                foreach (Renderer r in _parabola.GetComponentsInChildren<Renderer>())
                {
                    PartScript.PartMaterialScript.RemoveRenderer(r);
                    PartScript.PartMaterialScript.AddRenderer(r, excludeFromCombine: true);
                }

                PartScript.PartMaterialScript.UpdateTextureData();
                if (Game.InDesignerScene) Symmetry.SynchronizePartModifiers(base.PartScript);
            }
        }
    }
}