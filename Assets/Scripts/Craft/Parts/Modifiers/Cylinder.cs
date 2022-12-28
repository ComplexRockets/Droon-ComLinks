using Assets.Scripts;
using ModApi.Craft.Parts;
using UnityEngine;
using Assets.Scripts.DroonComLinks.Antennas.ParabolicAntenna;

public class Cylinder : MonoBehaviour {
    private Mod _mod => Mod.Instance;
    private IPartScript _partScript;
    //private GameObject _parent;
    private GameObject _cylinder;
    private GameObject _sphereA;
    private GameObject _sphereB;
    //private GameObject _sphereAParent;
    //private GameObject_sphereBParent;
    private Mesh _mesh;
    private bool A = false;
    private bool B = false;

    public void Initialise (bool square, bool highRes, bool a, bool b, Transform parent, IPartScript partScript) {
        A = a;
        B = b;
        _partScript = partScript;
        //_parent = new GameObject ();
        //_parent.transform.SetParent (this.transform);

        if (square) _cylinder = UnityEngine.Object.Instantiate (_mod.ResourceLoader.LoadAsset<GameObject> (ParabolicAntennaMesh.prefabPath + "SquareCylinder.prefab"));
        else if (highRes) _cylinder = UnityEngine.Object.Instantiate (_mod.ResourceLoader.LoadAsset<GameObject> (ParabolicAntennaMesh.prefabPath + "HighResCylinder.prefab"));
        else _cylinder = UnityEngine.Object.Instantiate (_mod.ResourceLoader.LoadAsset<GameObject> (ParabolicAntennaMesh.prefabPath + "RoundCylinder.prefab"));

        //Debug.Log ("GameObject2 : " + gameObject.name);
        transform.SetParent (parent, worldPositionStays : false);
        _cylinder.transform.SetParent (transform, worldPositionStays : false);
        _partScript.PartMaterialScript.AddRenderer (_cylinder.GetComponentInChildren<Renderer> (), excludeFromDrag : false);

        GameObject Cap;
        if (square) Cap = _mod.ResourceLoader.LoadAsset<GameObject> (ParabolicAntennaMesh.prefabPath + "SquareCap.prefab");
        else Cap = _mod.ResourceLoader.LoadAsset<GameObject> (ParabolicAntennaMesh.prefabPath + "RoundCap.prefab");

        if (A) {
            // _sphereAParent = new GameObject ();
            //_sphereAParent.transform.SetParent (transform, worldPositionStays : false);

            _sphereA = UnityEngine.Object.Instantiate (Cap);
            _sphereA.transform.SetParent (transform, worldPositionStays : false);
            _partScript.PartMaterialScript.AddRenderer (_sphereA.GetComponentInChildren<Renderer> (), excludeFromDrag : false);
        }
        if (B) {
            //_sphereBParent = new GameObject ();
            //_sphereBParent.transform.SetParent (transform, worldPositionStays : false);

            _sphereB = UnityEngine.Object.Instantiate (Cap);
            _sphereB.transform.SetParent (transform, worldPositionStays : false);
            _partScript.PartMaterialScript.AddRenderer (_sphereB.GetComponentInChildren<Renderer> (), excludeFromDrag : false);
        }
    }

    public void UpdateMesh (Vector3 vA, Vector3 vB, float width, Vector2 offset) {
        float length = Vector3.Distance (vA, vB);
        //bool vertical = Vector3.Normalize (vB - vA) == _partScript.GameObject.transform.up || Vector3.Normalize (vB - vA) == -_partScript.GameObject.transform.up;
        transform.localPosition = vA;
        transform.localRotation = Quaternion.LookRotation (vB - vA, -Vector3.Lerp (vB, vA, 0.5f));
        _cylinder.transform.localScale = new Vector3 (width, width, length);
        _cylinder.transform.localPosition = new Vector3 (offset.x * width / 2, offset.y * width / 2, length / 2);

        if (A) {
            _sphereA.transform.localPosition = new Vector3 (offset.x * width / 2, offset.y * width / 2, 0);
            _sphereA.transform.localScale = new Vector3 (width, width, width);
        }
        if (B) {
            _sphereB.transform.Rotate (transform.forward, 180);

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
        mesh.Optimize ();
        mesh.RecalculateNormals ();
    }

    public void Destroy () {
        if (A) {
            _partScript.PartMaterialScript.RemoveRenderer (_sphereA.GetComponentInChildren<Renderer> ());
            GameObject.Destroy (_sphereA);
        }
        if (B) {
            _partScript.PartMaterialScript.RemoveRenderer (_sphereB.GetComponentInChildren<Renderer> ());
            GameObject.Destroy (_sphereB);
        }
        _partScript.PartMaterialScript.RemoveRenderer (_cylinder.GetComponentInChildren<Renderer> ());
        GameObject.Destroy (this.gameObject);
    }
}