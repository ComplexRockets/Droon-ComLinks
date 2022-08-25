namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts.Design;
    using Assets.Scripts.DroonComLinks.Interfaces;
    using Assets.Scripts.DroonComLinks.Objects.Antennas.ParabolicAntenna;
    using ModApi.Craft.Parts;
    using UnityEngine;

    public class ParabolicAntennaScript : PartModifierScript<ParabolicAntennaData>, IDCLAntennaScript
    {
        public IDCLAntennaData data => Data;
        public DCLAntennaData antennaData
        {
            get
            {
                if (_antennaData == null) _antennaData = base.PartScript.GetModifier<DCLAntennaScript>().Data;
                return _antennaData;
            }
        }
        private DCLAntennaData _antennaData;
        private Mod _mod = Mod.Instance;
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
            if (!antennaData.customAntenna)
            {
                _parabolaParent = transform.Find("Mesh").gameObject;
                _parabola = transform.GetComponentInChildren<ParabolicAntennaMesh>();
                _parabola.Initialize();
            }

            Data.OnSizeChanged(size);
        }

        public void UpdateAntenna()
        {
            if (!antennaData.customAntenna)
            {
                upperElement?.Destroy();
                upperElement = new UpperElement(Data.upperElement, Data.secondaryReflector, _parabolaParent.transform, PartScript);
                upperElement.gameObject.transform.localPosition = new Vector3(0, Data.focalLength, 0);
                upperElement.gameObject.transform.localScale = new Vector3(Data.radius, Data.radius, Data.radius);

                lowerElement?.Destroy();
                if (Data.secondaryReflector)
                {
                    lowerElement = new LowerElement(Data.lowerElement, _parabolaParent.transform, PartScript);
                    lowerElement.gameObject.transform.localScale = new Vector3(Data.radius, Data.radius, Data.radius);
                }

                _parabola.CreateMesh( //TODO: make that cleaner or maybe not
                    Data.radius, Data.depth, Data.parabolaResolution, Data.parabolaDivCount, Data.thickness, Data.ridgeA, Data.ridgeB, Data.ridgeDivCount, Data.ridgeWidth, Data.straightSide, Data.bottomRadius, Data.bottomDepth, Data.bottomOffset,
                    Data.structureResolution, Data.supportArmPos, Data.supportArmRadius, Data.supportArmCount, Data.doubleSupportArmOffset, upperElement.offset
                );
                _parabolaParent.transform.localPosition = new Vector3(0, -_parabola.lowestPoint, 0);

                PartScript.InitializeColliders();
                foreach (Renderer r in _parabola.GetComponentsInChildren<Renderer>())
                {
                    PartScript.PartMaterialScript.RemoveRenderer(r);
                    PartScript.PartMaterialScript.AddRenderer(r, excludeFromCombine: true);
                }

                PartScript.PartMaterialScript.UpdateTextureData();
                if (Game.InDesignerScene)
                {
                    Symmetry.SynchronizePartModifiers(base.PartScript);
                }
            }
        }
    }
}