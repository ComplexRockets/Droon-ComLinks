namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System.Collections.Generic;
    using System;
    using Assets.Scripts.Design;
    using Assets.Scripts.DroonComLinks.Interfaces;
    using Assets.Scripts.DroonComLinks.Objects.Antennas.ParabolicAntenna;
    using ModApi.Craft.Parts;
    using UnityEngine;

    public class ParametricalParabolicAntennaScript : PartModifierScript<ParametricalParabolicAntennaData>, IDCLAntennaScript
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
        private string _prefabPath = "Assets/Content/Craft/Parts/ParametricalAntenna/Prefabs/";
        private GameObject _parabolaParent, _parabola, _bottom, _side, _straightSide, _bottomOffset, _cylinderPrefab;
        private UpperElement _upperElement;
        private LowerElement _lowerElement;
        private List<GameObject> _attachPoints = new List<GameObject>(),
                _secondaryAttachPoints = new List<GameObject>(),
                _doubleAttachPoints = new List<GameObject>(),
                _structureRidge = new List<GameObject>(),
                _structureSide = new List<GameObject>(),
                _structureSideOffset = new List<GameObject>(),
                _crossStrucure1A = new List<GameObject>(),
                _crossStrucure2A = new List<GameObject>(),
                _crossStrucure1B = new List<GameObject>(),
                _crossStrucure2B = new List<GameObject>(),
                _structureBottomRidge = new List<GameObject>();
        private List<List<GameObject>> _elements = new List<List<GameObject>>();
        private Vector3 _up => PartScript.GameObject.transform.up;
        private const int xResolution = 32;

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        public void Initialize(DCLAntennaData AntennaData, float size)
        {
            _antennaData = AntennaData;
            if (!antennaData.customAntenna)
            {
                List<GameObject>[] elements = { _attachPoints, _secondaryAttachPoints, _doubleAttachPoints, _structureRidge, _structureBottomRidge, _structureSide, _structureSideOffset, _crossStrucure1A, _crossStrucure2A, _crossStrucure1B, _crossStrucure2B };
                _elements.AddRange(elements);

                _cylinderPrefab = _mod.ResourceLoader.LoadAsset<GameObject>(_prefabPath + "Cylinder.prefab");
                _parabolaParent = transform.Find("Main").gameObject;
                _parabola = _parabolaParent.transform.Find("Parabola").gameObject;
                _bottom = _parabolaParent.transform.Find("Bottom").gameObject;
                _side = _parabolaParent.transform.Find("Side").gameObject;
            }

            Data.OnSizeChanged(size);
        }

        public void CreateAntenna()
        {
            if (!antennaData.customAntenna)
            {//Destroy all gameobject
                try
                {
                    _attachPoints.ForEach(go => go?.GetComponent<Cylinder>()?.Destroy());
                    _secondaryAttachPoints.ForEach(go => go?.GetComponent<Cylinder>()?.Destroy());
                    _doubleAttachPoints.ForEach(go => go?.GetComponent<Cylinder>()?.Destroy());
                    _structureRidge.ForEach(go => go?.GetComponent<Cylinder>()?.Destroy());
                    _structureBottomRidge.ForEach(go => go?.GetComponent<Cylinder>()?.Destroy());
                    _structureSide.ForEach(go => go?.GetComponent<Cylinder>()?.Destroy());
                    _structureSideOffset.ForEach(go => go?.GetComponent<Cylinder>()?.Destroy());
                    _crossStrucure1A.ForEach(go => go?.GetComponent<Cylinder>()?.Destroy());
                    _crossStrucure2A.ForEach(go => go?.GetComponent<Cylinder>()?.Destroy());
                    _crossStrucure1B.ForEach(go => go?.GetComponent<Cylinder>()?.Destroy());
                    _crossStrucure2B.ForEach(go => go?.GetComponent<Cylinder>()?.Destroy());

                    try { _straightSide?.GetComponent<Cylinder>()?.Destroy(); } catch (Exception e) { Debug.Log(e); }
                    try { _bottomOffset?.GetComponent<Cylinder>()?.Destroy(); } catch (Exception e) { Debug.Log(e); }

                    _upperElement?.Destroy();
                    _lowerElement?.Destroy();
                }
                catch (Exception e) { Debug.LogError("error on Destroying all gameobject: " + e); }

                //Reset Lists
                foreach (List<GameObject> list in _elements)
                {
                    list.Clear();
                }

                //Create Upper and Lower Element
                _upperElement = new UpperElement(Data.upperElement, Data.secondaryReflector, _parabolaParent.transform, PartScript);
                if (Data.secondaryReflector) _lowerElement = new LowerElement(Data.lowerElement, _parabolaParent.transform, PartScript);

                //Create Sides / Bottom
                if (Data.straightSide) _straightSide = CreateCylinder(false, true, false, false);
                if (Data.bottomOffset > 0 && Data.straightSide) _bottomOffset = CreateCylinder(false, true, false, false);

                //Create Upper Structure
                for (int i = 0; i < Data.supportArmCount; i++)
                {
                    _attachPoints.Add(CreateCylinder(true, true, false)); //Create objects for basic support
                    if (Data.doubleSupportArmOffset > 0) _doubleAttachPoints.Add(CreateCylinder(true, true, false)); //Create objects for offset support
                    if (_upperElement.offset.y != 0) _secondaryAttachPoints.Add(CreateCylinder(true, false, false)); //Create objects for secondary support
                }

                //Create Lower Structure
                //Create Ridge 
                for (int i = 0; i < xResolution; i++)
                {
                    if (Data.topRidge) _structureRidge.Add(CreateCylinder(false, false));
                    if (Data.bottomRidge) _structureBottomRidge.Add(CreateCylinder(false, false));
                }

                // Create Strucuture
                if (Data.sideStructureCount > 0)
                {
                    for (int i = 0; i < Data.sideStructureCount; i++)
                    {
                        _structureSide.Add(CreateCylinder(Data.bottomDiameter >= Data.diameter, true));
                        //if (AOffset != A) 
                        _structureSideOffset.Add(CreateCylinder(false, true));

                        if (Data.crossStructure)
                        {
                            _crossStrucure1A.Add(CreateCylinder(true, true));
                            _crossStrucure1B.Add(CreateCylinder(true, true));
                            _crossStrucure2A.Add(CreateCylinder(true, true));
                            _crossStrucure2B.Add(CreateCylinder(true, true));
                        }
                    }
                }
                UpdateAntenna();
            }
        }

        private GameObject CreateCylinder(bool a, bool b, bool square)
        {
            return CreateCylinder(square, false, a, b);
        }

        private GameObject CreateCylinder(bool a, bool b)
        {
            return CreateCylinder(Data.square, false, a, b);
        }

        private GameObject CreateCylinder(bool square, bool highres, bool a, bool b)
        {
            GameObject section = UnityEngine.Object.Instantiate(_cylinderPrefab);

            section.transform.SetParent(_parabolaParent.transform, worldPositionStays: false);
            section.GetComponent<Cylinder>().Initialise(square, highres, a, b, _parabolaParent.transform, PartScript);
            return section;
        }

        public void UpdateAntenna()
        {
            if (!antennaData.customAntenna)
            {
                UpdateUpperStructure();
                UpdateLowerStructure();

                //Update scale and position
                _parabolaParent.transform.localScale = new Vector3(Data.size, Data.size, Data.size);
                _parabola.transform.localScale = new Vector3(Data.diameter, Data.diameter, Data.depth);
                _bottom.transform.localScale = new Vector3(Data.diameter, Data.diameter, Data.depth - Data.sideDepth);
                _side.transform.localScale = new Vector3(Data.diameter, Data.diameter, Data.sideDepth);
                _side.transform.localPosition = new Vector3(0, Data.depth - Data.sideDepth / 2, 0);
                _upperElement.gameObject.transform.localPosition = new Vector3(0, Data.focalLength, 0);
                _upperElement.gameObject.transform.localScale = new Vector3(Data.radius, Data.radius, Data.radius);
                if (Data.secondaryReflector) _lowerElement.gameObject.transform.localScale = new Vector3(Data.radius, Data.radius, Data.radius);

                if (Data.straightSide)
                {
                    _straightSide.GetComponent<Cylinder>().UpdateMesh(
                        new Vector3(0, -Data.bottomDepth, 0),
                        Data.diameter,
                        new Vector3(0, Data.depth - Data.sideDepth, 0),
                        Data.bottomDiameter);
                    _bottom.transform.localScale = new Vector3(Data.bottomDiameter, Data.bottomDiameter, 0.001f);
                    _bottom.transform.localPosition = new Vector3(0, Data.bottom, 0);
                }
                else
                {
                    _bottom.transform.localScale = new Vector3(Data.diameter, Data.diameter, Data.depth + Data.bottomDepth - Data.sideDepth);
                    _bottom.transform.localPosition = new Vector3(0, -Data.bottomDepth, 0);
                }

                if (Data.bottomOffset > 0 && Data.straightSide)
                {
                    _bottomOffset.GetComponent<Cylinder>().UpdateMesh(
                        new Vector3(0, Data.bottom, 0),
                        Data.bottomDiameter,
                        new Vector3(0, Data.straightSide ? -Data.bottomDepth : 0, 0),
                        Data.bottomDiameter);
                }
            }
            
            if (Game.InDesignerScene)
            {
                Symmetry.SynchronizePartModifiers(PartScript);
                PartScript.CraftScript.RaiseDesignerCraftStructureChangedEvent();
            }
        }

        private void UpdateUpperStructure()
        {
            Vector3 A, A2, B, C;

            float OffsetY = _upperElement.offset.y * Data.radius;
            float YPos = Data.focalLength + _upperElement.offset.x * Data.radius;
            float angle = 2 * Mathf.PI / Data.supportArmCount;
            float pos = Data.radius * Data.supportArmPos;
            float height = Data.depth - Mathf.Cos(Mathf.Asin(Data.supportArmPos)) * Data.depth;

            for (int i = 0; i < Data.supportArmCount; i++)
            {
                //Generate offset support
                if (OffsetY != 0)
                {
                    B = new Vector3(Mathf.Cos(angle * i) * OffsetY / 2, YPos, Mathf.Sin(angle * i) * OffsetY / 2);
                    C = new Vector3(0, YPos, 0);
                    _secondaryAttachPoints[i].GetComponent<Cylinder>().UpdateMesh(B, C, Data.supportArmDiameter, new Vector2());
                }
                else B = new Vector3(0, YPos, 0);

                //Generate secondary support
                if (Data.doubleSupportArmOffset > 0)
                {
                    float angleOffset = Data.doubleSupportArmOffset * Mathf.PI / Data.supportArmCount;
                    A2 = new Vector3(Mathf.Cos(angle * i - angleOffset) * pos, height, Mathf.Sin(angle * i - angleOffset) * pos);
                    A = new Vector3(Mathf.Cos(angle * i + angleOffset) * pos, height, Mathf.Sin(angle * i + angleOffset) * pos);
                    _doubleAttachPoints[i].GetComponent<Cylinder>().UpdateMesh(A2, B, Data.supportArmDiameter, new Vector2());
                }
                else A = new Vector3(Mathf.Cos(angle * i) * pos, height, Mathf.Sin(angle * i) * pos);
                //Generate basic support
                _attachPoints[i].GetComponent<Cylinder>().UpdateMesh(A, B, Data.supportArmDiameter, new Vector2());
            }
        }

        private void UpdateLowerStructure()
        {
            float topR = Data.radius + Data.supportArmDiameter;
            float bottomR = Data.bottomRadius + Data.supportArmDiameter;

            //Update Ridge 
            if (Data.topRidge || Data.bottomRidge)
            {
                float div = 2 * Mathf.PI / xResolution;
                Vector3 tPrevPos = new Vector3(topR, Data.depth, 0);
                Vector3 tFirstPos = tPrevPos;
                Vector3 bPrevPos = new Vector3(bottomR, Data.bottom, 0);
                Vector3 bFirstPos = bPrevPos;

                for (int i = 0; i < xResolution; i++)
                {
                    float angle = div * (i + 1);

                    if (Data.topRidge)
                    {
                        Vector3 pos = i + 1 == xResolution ? tFirstPos : new Vector3(Mathf.Cos(angle) * (topR), Data.depth, Mathf.Sin(angle) * (topR));
                        _structureRidge[i].GetComponent<Cylinder>().UpdateMesh(tPrevPos, pos, Data.supportArmDiameter, new Vector2(0, 1));
                        tPrevPos = pos;
                    }
                    if (Data.bottomRidge)
                    {
                        Vector3 pos = i + 1 == xResolution ? bFirstPos : new Vector3(Mathf.Cos(angle) * bottomR, Data.bottom, Mathf.Sin(angle) * bottomR);
                        _structureBottomRidge[i].GetComponent<Cylinder>().UpdateMesh(bPrevPos, pos, Data.supportArmDiameter, new Vector2(0, 1));
                        bPrevPos = pos;
                    }
                }
            }

            // Side Structure
            if (Data.sideStructureCount > 0)
            {
                float angle = 2 * Mathf.PI / Data.sideStructureCount;
                float minStructureOffset = Data.supportArmDiameter * 7 * Data.depth;
                bool bottomSmaller = Data.bottomDiameter <= Data.diameter;
                //Debug.Log ("offset: " + minStructureOffset + " sideDepth: " + Data.sideDepth);

                Vector3 prevA = new Vector3(
                    Mathf.Cos(angle) * topR,
                    Data.depth,
                    Mathf.Sin(angle) * topR);
                Vector3 prevB = new Vector3(
                    Mathf.Cos(angle) * bottomR,
                    Data.bottom,
                    Mathf.Sin(angle) * bottomR);

                for (int i = 0; i < Data.sideStructureCount; i++)
                {
                    angle = 2 * Mathf.PI / Data.sideStructureCount * (i + 1);
                    float middleangle = angle - Mathf.PI / Data.sideStructureCount;

                    Vector3 A = new Vector3(
                        Mathf.Cos(angle) * topR,
                        Data.depth,
                        Mathf.Sin(angle) * topR);
                    Vector3 AOffset = new Vector3(
                        Mathf.Cos(angle) * topR,
                        Data.depth - (bottomSmaller ? Data.sideDepth < minStructureOffset && !Data.straightSide ? minStructureOffset : Data.sideDepth * 1.05f : 0),
                        Mathf.Sin(angle) * topR);
                    Vector3 B = new Vector3(
                        Mathf.Cos(angle) * bottomR,
                        Data.bottom - Data.supportArmDiameter / 2,
                        Mathf.Sin(angle) * bottomR);
                    Vector3 M = new Vector3(
                        Mathf.Cos(middleangle) * topR * Data.crossStructureBending,
                        Data.depth / 2,
                        Mathf.Sin(middleangle) * topR * Data.crossStructureBending);

                    _structureSide[i].GetComponent<Cylinder>().UpdateMesh(AOffset, B, Data.supportArmDiameter, new Vector2(0, bottomSmaller ? 1 : -1));
                    if (AOffset != A) _structureSideOffset[i].GetComponent<Cylinder>().UpdateMesh(AOffset, A, Data.supportArmDiameter, new Vector2(0, 1));

                    if (Data.crossStructure)
                    {
                        _crossStrucure1A[i].GetComponent<Cylinder>().UpdateMesh(prevA, M, Data.supportArmDiameter, new Vector2());
                        _crossStrucure1B[i].GetComponent<Cylinder>().UpdateMesh(M, B, Data.supportArmDiameter, new Vector2());
                        _crossStrucure2A[i].GetComponent<Cylinder>().UpdateMesh(A, M, Data.supportArmDiameter, new Vector2());
                        _crossStrucure2B[i].GetComponent<Cylinder>().UpdateMesh(M, prevB, Data.supportArmDiameter, new Vector2());

                        prevA = A;
                        prevB = B;
                    }
                }
            }
        }
    }
}