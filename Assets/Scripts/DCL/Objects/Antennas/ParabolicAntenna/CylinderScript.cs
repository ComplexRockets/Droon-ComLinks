using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Objects.Antennas.ParabolicAntenna {
    public class CylinderScript : MonoBehaviour {
        private GameObject _Cylinder;
        private GameObject _SphereA;
        private GameObject _SphereB;
        private Mesh _Mesh;
        private bool A = false;
        private bool B = false;

        public void Initialise (Vector3 vA, Vector3 vB, float width, bool square, bool a, bool b) {
            A = a;
            B = b;
            if (square) _Cylinder = transform.Find ("SquareCylinder").gameObject;
            else _Cylinder = transform.Find ("Cylinder").gameObject;
            _Cylinder.GetComponent<MeshRenderer> ().enabled = true;
            _Cylinder.GetComponent<MeshCollider> ().enabled = true;

            if (A) {
                if (square) _SphereA = transform.Find ("SquareCapA").gameObject;
                else _SphereA = transform.Find ("SphereA").gameObject;
                _SphereA.GetComponent<MeshRenderer> ().enabled = true;
                _SphereA.transform.parent = this.transform.parent;
            }
            if (B) {
                if (square) _SphereB = transform.Find ("SquareCapB").gameObject;
                else _SphereB = transform.Find ("SphereB").gameObject;
                _SphereB.GetComponent<MeshRenderer> ().enabled = true;
                _SphereB.transform.parent = this.transform.parent;
            }

            UpdateMesh (vA, vB, width);
        }

        public void UpdateMesh (Vector3 vA, Vector3 vB, float width) {
            float length = Vector3.Distance (vA, vB);
            transform.localPosition = vA;
            transform.localRotation = Quaternion.LookRotation (vB - vA, transform.parent.up);
            _Cylinder.transform.localScale = new Vector3 (width, width, length);
            _Cylinder.transform.localPosition = new Vector3 (0, 0, length / 2);

            if (A) {
                _SphereA.transform.localPosition = vA;
                _SphereA.transform.localScale = new Vector3 (width, width, width);
                _SphereA.transform.localRotation = Quaternion.LookRotation (Vector3.Normalize (vB - vA), transform.parent.up);
            }
            if (B) {
                _SphereB.transform.localPosition = vB;
                _SphereB.transform.localScale = new Vector3 (width, width, width);
                _SphereB.transform.localRotation = Quaternion.LookRotation (-Vector3.Normalize (vB - vA), transform.parent.up);
            }
        }

        public void Destroy () {
            if (A) GameObject.Destroy(_SphereA);
            if (B) GameObject.Destroy(_SphereB);
            GameObject.Destroy(this.gameObject);
        }
    }
}