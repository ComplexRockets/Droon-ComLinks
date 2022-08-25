using UnityEditor;
using UnityEngine;

public class EditorParametricalParabolicAntenna : MonoBehaviour {
    [Range (3, 128)]
    public int resolution;

    [Range (0, 32)]
    public int ridgeDivCount;
    public int parabolaDivCount;
    public float diameter;
    public float depth;
    public float verticsScale;
    public int verticsToShow;

    [Range (0.01f, 0.05f)]
    public float ridgeA;

    [Range (-0.2f, 0.2f)]
    public float ridgeB;

    [Range (0f, 0.1f)]
    public float ridgeWidth;
    [Range (0.01f, 0.05f)]
    public float parabolaThickness;
    public int structureRes;
    [Range (0.001f, 0.05f)]
    public float strucureRadius;
    [Range (0f, 1f)]
    public float structurePos;
    public int suportArmCount;
    [Range (0f, 1f)]
    public float doubleSupportArmOffset;
    public bool straightSide;
    public float bottomRadius;
    public float bottomDepth;
    public float bottomOffset;
    public Vector2 offset;
    private EditorParabolicAntennaMesh parabola;
    private float _radius => diameter / 2;
    private float _focalLength => (diameter * diameter) / (16 * depth);

    void Start () {
        parabola = transform.GetComponentInChildren<EditorParabolicAntennaMesh> ();
        parabola.Initialize ();
    }

    void Update () {
        parabola.CreateMesh (
            _radius, depth, resolution, parabolaDivCount, parabolaThickness, ridgeA, ridgeB, ridgeDivCount, ridgeWidth, straightSide, bottomRadius, bottomDepth, bottomOffset,
            structureRes, structurePos, strucureRadius, suportArmCount, doubleSupportArmOffset, offset);
    }
}