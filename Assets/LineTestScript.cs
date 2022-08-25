using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Vectrosity;

public class LineTestScript : MonoBehaviour
{
    private VectorLine line;
    public Vector3 A;
    public Vector3 B;
    public List<Vector3> points;
    public Material material;
    public Camera thaCamera;
    public float width;

    void Start()
    {
        line = new VectorLine("DCLLine", new List<Vector3> { A, B }, width, LineType.Continuous);
        line.material = material;
        //line.color = new Color(0.2f, 0.9f, 0.2f);
        VectorLine.SetCamera3D(thaCamera);
        line.Draw3DAuto();
    }

    void Update()
    {
        line.points3[0] = A;
        line.points3[1] = B;
        line.SetWidth(width);
        line.AddNormals();
    }
}
