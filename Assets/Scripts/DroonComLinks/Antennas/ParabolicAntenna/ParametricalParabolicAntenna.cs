 using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Antennas.ParabolicAntenna
{
    public class ParametricalParabolicAntenna : MonoBehaviour {
        private GameObject _parabola;
        private GameObject _bottom;
        private GameObject _side;
        private GameObject _straightSide;
        private GameObject _bottomOffset;
        private GameObject _cylinderPrefab;
        private GameObject[] _structureRidge = new GameObject[xResolution];
        private GameObject[] _structureBottomRidge = new GameObject[xResolution];
        private GameObject[] _structureSide;
        private GameObject[] _structureSideOffset;
        private GameObject[] _crossStrucure1A;
        private GameObject[] _crossStrucure2A;
        private GameObject[] _crossStrucure1B;
        private GameObject[] _crossStrucure2B;
        public float focalLength => (diameter * diameter) / (16 * depth);
        [Range (0.1f, 5f)]
        public float diameter;
        private float _radius => diameter / 2;
        [Range (0.1f, 0.7f)]
        public float depth;
        [Range (0, 0.7f)]
        public float sideLength;
        [Range (0.005f, 0.05f)]
        public float structureWidth;
        [Range (0, 20)]
        public int sideStructureCount = 6;
        public bool crossStructure;
        [Range (0.9f, 1.1f)]
        public float crossStructureBending;
        private const int xResolution = 32;
        private const int yResolution = 8;
        public float oOffset;
        public bool flatSide;
        public bool ridge;
        public bool squareStructure;
        public bool bottomRidge;
        public float bottomDiameter;
        public float _bottomRadius => bottomDiameter / 2;
        public float bottomDepth;
        [Range (0, 1)]
        public float bottomOffset;
        private float bottom => -bottomDepth - bottomOffset;
        [Range (-2, 2)]
        public float offset;
        private void Start () {
            _parabola = transform.Find ("Parabola").gameObject;
            _bottom = transform.Find ("Bottom").gameObject;
            _side = transform.Find ("Side").gameObject;

            _cylinderPrefab = Resources.Load ("CylinderprefabEditorTests") as GameObject;
            _structureSide = new GameObject[sideStructureCount];
            _structureSideOffset = new GameObject[sideStructureCount];
            _crossStrucure1A = new GameObject[sideStructureCount];
            _crossStrucure2A = new GameObject[sideStructureCount];
            _crossStrucure1B = new GameObject[sideStructureCount];
            _crossStrucure2B = new GameObject[sideStructureCount];
        }

        private void Update () {
            _parabola.transform.localScale = new Vector3 (diameter, diameter, depth);

            _side.transform.localScale = new Vector3 (diameter, diameter, sideLength);
            _side.transform.localPosition = new Vector3 (0, depth - sideLength / 2, 0);

            if (_straightSide != null) _straightSide.GetComponent<CylinderEditorTests> ().Destroy ();
            if (_bottomOffset != null) _bottomOffset.GetComponent<CylinderEditorTests> ().Destroy ();

            if (flatSide) {
                GameObject section = GameObject.Instantiate (_cylinderPrefab);
                section.transform.parent = this.transform;
                CylinderEditorTests sectionScript = section.GetComponent<CylinderEditorTests> ();

                sectionScript.Initialise (false, true, false, false);
                sectionScript.UpdateMesh (new Vector3 (0, -bottomDepth, 0), diameter, new Vector3 (0, depth - sideLength, 0), bottomDiameter);
                _straightSide = section;

                _bottom.transform.localScale = new Vector3 (bottomDiameter, bottomDiameter, 0.001f);
                _bottom.transform.localPosition = new Vector3 (0, bottom, 0);
            } else {
                _bottom.transform.localScale = new Vector3 (diameter, diameter, depth + bottomDepth - sideLength);
                _bottom.transform.localPosition = new Vector3 (0, -bottomDepth, 0);
            }

            if (bottomOffset > 0 && flatSide) {
                GameObject section = GameObject.Instantiate (_cylinderPrefab);
                section.transform.parent = this.transform;
                CylinderEditorTests sectionScript = section.GetComponent<CylinderEditorTests> ();

                sectionScript.Initialise (false, true, false, false);
                sectionScript.UpdateMesh (new Vector3 (0, bottom, 0), bottomDiameter, new Vector3 (0, flatSide ? -bottomDepth : 0, 0), bottomDiameter);
                _bottomOffset = section;
            }

            GenerateStructure ();
        }

        private void GenerateStructure () {
            //Reset Arrays
            for (int i = 0; i < _structureSide.Length; i++) {
                _structureSide[i]?.GetComponent<CylinderEditorTests> ()?.Destroy ();
                _structureSideOffset[i]?.GetComponent<CylinderEditorTests> ()?.Destroy ();
                _crossStrucure1A[i]?.GetComponent<CylinderEditorTests> ()?.Destroy ();
                _crossStrucure2A[i]?.GetComponent<CylinderEditorTests> ()?.Destroy ();
                _crossStrucure1B[i]?.GetComponent<CylinderEditorTests> ()?.Destroy ();
                _crossStrucure2B[i]?.GetComponent<CylinderEditorTests> ()?.Destroy ();
            }
            for (int i = 0; i < _structureRidge.Length; i++) {
                if (_structureRidge[i] != null) _structureRidge[i].GetComponent<CylinderEditorTests> ()?.Destroy ();
                if (_structureBottomRidge[i] != null) _structureBottomRidge[i].GetComponent<CylinderEditorTests> ()?.Destroy ();
            }
            _structureSide = new GameObject[sideStructureCount];
            _structureSideOffset = new GameObject[sideStructureCount];
            _crossStrucure1A = new GameObject[sideStructureCount];
            _crossStrucure2A = new GameObject[sideStructureCount];
            _crossStrucure1B = new GameObject[sideStructureCount];
            _crossStrucure2B = new GameObject[sideStructureCount];

            //Generate Ridge 
            float div = 2 * Mathf.PI / xResolution;
            float topR = _radius + structureWidth;
            float bottomR = _bottomRadius + structureWidth;

            if (ridge) { //Top Ridge
                Vector3 prevPos = new Vector3 (topR, depth, 0);
                Vector3 firstPos = prevPos;

                for (int i = 0; i < xResolution; i++) {
                    float angle = div * (i + 1);
                    Vector3 pos = i + 1 == xResolution? firstPos : new Vector3 (Mathf.Cos (angle) * (topR), depth, Mathf.Sin (angle) * (topR));

                    _structureRidge[i] = generateCylinder (prevPos, false, pos, false, structureWidth * 2, new Vector2 (0, offset));
                    prevPos = pos;
                }
            }

            if (bottomRidge) { //Bottom Ridge
                Vector3 prevPos = new Vector3 (bottomR, bottom, 0);
                Vector3 firstPos = prevPos;

                for (int i = 0; i < xResolution; i++) {
                    float angle = div * (i + 1);
                    Vector3 pos = i + 1 == xResolution? firstPos : new Vector3 (Mathf.Cos (angle) * bottomR, bottom, Mathf.Sin (angle) * bottomR);

                    _structureBottomRidge[i] = generateCylinder (prevPos, false, pos, false, structureWidth * 2, new Vector2 (0, offset));
                    prevPos = pos;
                }
            }

            if (sideStructureCount > 0) { // Side Structure
                float angle = 2 * Mathf.PI / sideStructureCount * _structureSide.Length;
                float minStructureOffset = structureWidth * 20 * depth;
                bool bottomSmaller = bottomDiameter <= diameter;
                Vector3 prevA = new Vector3 (
                    Mathf.Cos (angle) * topR,
                    depth,
                    Mathf.Sin (angle) * topR);
                Vector3 prevB = new Vector3 (
                    Mathf.Cos (angle) * bottomR,
                    bottom,
                    Mathf.Sin (angle) * bottomR);

                for (int i = 0; i < _structureSide.Length; i++) {
                    angle = 2 * Mathf.PI / sideStructureCount * (i + 1);
                    float middleangle = angle - Mathf.PI / sideStructureCount;

                    Vector3 A = new Vector3 (
                        Mathf.Cos (angle) * topR,
                        depth,
                        Mathf.Sin (angle) * topR);
                    Vector3 AOffset = new Vector3 (
                        Mathf.Cos (angle) * topR,
                        depth - (bottomSmaller?sideLength < minStructureOffset && !flatSide? minStructureOffset : sideLength : 0),
                        Mathf.Sin (angle) * topR);
                    Vector3 B = new Vector3 (
                        Mathf.Cos (angle) * bottomR,
                        bottom,
                        Mathf.Sin (angle) * bottomR);
                    Vector3 M = new Vector3 (
                        Mathf.Cos (middleangle) * topR * crossStructureBending,
                        depth / 2,
                        Mathf.Sin (middleangle) * topR * crossStructureBending);

                    _structureSide[i] = generateCylinder (AOffset, !bottomSmaller, B, false, structureWidth * 2, new Vector2 (0, bottomSmaller ? 1 : -1));
                    if (AOffset != A) _structureSideOffset[i] = generateCylinder (AOffset, false, A, true, structureWidth * 2, new Vector2 (0, 1));
                    if (crossStructure) {
                        _crossStrucure1A[i] = generateCylinder (prevA, true, M, true, structureWidth * 2, new Vector2 ());
                        _crossStrucure1B[i] = generateCylinder (M, true, B, true, structureWidth * 2, new Vector2 ());
                        _crossStrucure2A[i] = generateCylinder (A, true, M, true, structureWidth * 2, new Vector2 ());
                        _crossStrucure2B[i] = generateCylinder (M, true, prevB, true, structureWidth * 2, new Vector2 ());
                        prevA = A;
                        prevB = B;
                    }
                }
            }
        }

        private GameObject generateCylinder (Vector3 A, bool a, Vector3 B, bool b, float diameter, Vector2 offset) {
            GameObject section = GameObject.Instantiate (_cylinderPrefab);
            section.transform.parent = this.transform;
            CylinderEditorTests sectionScript = section.GetComponent<CylinderEditorTests> ();

            sectionScript.Initialise (squareStructure, false, a, b);
            sectionScript.UpdateMesh (A, B, diameter, offset);
            return section;
        }
    }
}