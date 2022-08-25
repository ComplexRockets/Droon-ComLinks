using UnityEngine;

public class CylinderEditorTests : MonoBehaviour {
    private GameObject _cylinder;
    private GameObject _sphereA;
    private GameObject _sphereB;
    // private GameObject _sphereAParent;
    // private GameObject _sphereBParent;
    private Mesh _mesh;
    private bool A = false;
    private bool B = false;

    public void Initialise (bool square, bool highRes, bool a, bool b) {
        A = a;
        B = b;
        if (square) _cylinder = GameObject.Instantiate (Resources.Load ("SquareCylinderEditorTests") as GameObject);
        else if (highRes) _cylinder = GameObject.Instantiate (Resources.Load ("HighResCylinderEditorTests") as GameObject);
        else _cylinder = GameObject.Instantiate (Resources.Load ("RoundCylinderEditorTests") as GameObject);
        _cylinder.transform.parent = transform;

        GameObject Cap;
        if (square) Cap = Resources.Load ("SquareCapEditorTests") as GameObject;
        else Cap = Resources.Load ("RoundCapEditorTests") as GameObject;

        if (A) {
            // _sphereAParent = new GameObject ();
            // _sphereAParent.transform.parent = transform;

            _sphereA = GameObject.Instantiate (Cap);
            _sphereA.transform.parent = transform;
        }
        if (B) {
            // _sphereBParent = new GameObject ();
            // _sphereBParent.transform.parent = transform;

            _sphereB = GameObject.Instantiate (Cap);
            _sphereB.transform.parent = transform;
        }
    }

    public void UpdateMesh (Vector3 vA, Vector3 vB, float width, Vector2 offset) {
        float length = Vector3.Distance (vA, vB);
        bool vertical = Vector3.Normalize (vB - vA) == transform.parent.up || Vector3.Normalize (vB - vA) == -transform.parent.up;
        transform.localPosition = vA;
        transform.localRotation = Quaternion.LookRotation (vB - vA, -Vector3.Lerp (vB, vA, 0.5f));
        _cylinder.transform.localScale = new Vector3 (width, width, length);
        _cylinder.transform.localPosition = new Vector3 (offset.x * width / 2, offset.y * width / 2, length / 2);

        if (A) {
            // _sphereAParent.transform.localPosition = vA;
            // _sphereAParent.transform.localRotation = Quaternion.LookRotation (vB - vA, vertical? transform.parent.localPosition - vA : transform.parent.up);

            _sphereA.transform.localPosition = new Vector3 (offset.x * width / 2, offset.y * width / 2, 0);
            _sphereA.transform.localScale = new Vector3 (width, width, width);
        }
        if (B) {
            // _sphereBParent.transform.localPosition = vB;
            // _sphereBParent.transform.localRotation = Quaternion.LookRotation (vA - vB, vertical? vA - transform.parent.localPosition : transform.parent.up);

            //_sphereB.transform.localRotation = Quaternion.Euler (-_sphereB.transform.localRotation.eulerAngles);
            _sphereB.transform.Rotate(transform.forward, 180);
            _sphereB.transform.localPosition = new Vector3 (offset.x * width / 2, offset.y * width / 2, length);
            _sphereB.transform.localScale = new Vector3 (width, width, width);
        }
    }

    public void UpdateMesh (Vector3 vA, float dA, Vector3 vB, float dB) {
        UpdateMesh (vA, vB, 1, new Vector2 ());
        Mesh mesh = _cylinder.GetComponent<MeshFilter> ().mesh;
        Vector3[] vertices = mesh.vertices;

        float zRef = vertices[0].z;
        for (int i = 0; i < vertices.Length; i++) {
            if (vertices[i].z == zRef) {
                vertices[i].x *= dB;
                vertices[i].y *= dB;
            } else {
                vertices[i].x *= dA;
                vertices[i].y *= dA;
            }
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals ();
        mesh.RecalculateTangents ();
        mesh.RecalculateBounds ();
    }

    public void Destroy () {
        if (A) {
            GameObject.Destroy (_sphereA);
            // GameObject.Destroy (_sphereAParent);
        }
        if (B) {
            GameObject.Destroy (_sphereB);
            // GameObject.Destroy (_sphereBParent);
        }
        GameObject.Destroy (this.gameObject);
    }
}