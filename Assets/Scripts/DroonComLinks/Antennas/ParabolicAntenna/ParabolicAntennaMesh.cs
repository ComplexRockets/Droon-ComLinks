using System;
using UnityEngine;
namespace Assets.Scripts.DroonComLinks.Antennas.ParabolicAntenna
{
    public class ParabolicAntennaMesh : MonoBehaviour
    {
        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;
        private Mesh _mesh;
        private Mesh _colliderMesh;
        private Vector3[] _vertices;
        private Vector4[] _uvs;
        private int[] _triangles;
        private Vector2 _upperElementOffset;
        private Vector3 noPoint = new Vector3(9182367, 2973, 0209736);
        private float _divAngle, _parabolaDivWidth, _ridgeDivWidth, _focalLength, _depth, _verticsScale, _ridgeDivCount, _ridgeA, _ridgeB, _radius, _ridgeEndX, _ridgeEndY, _strucureRadius, structureDivAngle, _thickness, _bottomRadius, _bottomdepth, _bottomOffset, _structurePos, _doubleSupportArmOffset, _ridgeWidth;
        private int verticsIndex, preVertices, _totalVertices, _totalDivCount, _parabolaDivCount, _parabolaRes, triangleIndex, pointsToConnect, subPartId, _structureRes, _suportArmCount;
        private bool _straightSide;
        public float lowestPoint { get; private set; }
        public const string prefabPath = "Assets/Content/Craft/Parts/ParametricalAntenna/Prefabs/";


        public void Initialize()
        {
            _colliderMesh = new Mesh();
            _mesh = new Mesh();
            _meshFilter = this.gameObject.GetComponent<MeshFilter>();
            _meshCollider = this.gameObject.GetComponent<MeshCollider>();
        }

        public void CreateMesh(float r, float depth, int parabolaRes, int parabolaDivCount, float thickness, float ridgeA, float ridgeB, int ridgeDivCount, float ridgeWidth, bool straightSide, float bottomRadius, float bottomDepth, float bottomOffset,
            int structureRes, float structurePos, float strucureRadius, int suportArmCount, float doubleSupportArmOffset, Vector2 upperElementOffset, bool collider = false)
        {
            _radius = r;
            _depth = depth;
            _parabolaRes = parabolaRes;
            _parabolaDivCount = parabolaDivCount;
            _thickness = thickness;
            _ridgeA = ridgeA;
            _ridgeB = ridgeB;
            _ridgeDivCount = ridgeDivCount;
            _divAngle = 2 * Mathf.PI / parabolaRes;
            _ridgeWidth = ridgeWidth;
            _ridgeDivWidth = ridgeWidth / ridgeDivCount;
            _straightSide = straightSide;
            _bottomRadius = !straightSide && bottomOffset == 0 ? 0 : bottomRadius; // Ignore bottom radius if not straightside and no bottom offset
            _bottomdepth = !straightSide ? 0 : bottomDepth; // Ignore bottom depth if not straightSide
            _bottomOffset = bottomOffset;
            _structureRes = structureRes;
            _structurePos = structurePos;
            _strucureRadius = strucureRadius;
            _suportArmCount = suportArmCount;
            _doubleSupportArmOffset = doubleSupportArmOffset;
            _upperElementOffset = upperElementOffset;
            verticsIndex = triangleIndex = 0;

            _vertices = new Vector3[2];
            _uvs = new Vector4[2];
            _triangles = new int[0];
            _focalLength = (_radius * _radius) / (4 * depth);

            CreateParabola();
            CreateStructure();
            ApplyMesh(collider);
        }

        private void CreateParabola()
        {
            float height = 0;
            float divHeight = _depth / _parabolaDivCount;
            float curentRadius = _parabolaDivWidth = _radius / _parabolaDivCount; ;
            _totalDivCount = subPartId = 0; //Top parabola sub part id
            _ridgeEndX = _ridgeEndY = 0;
            lowestPoint = -_bottomdepth - _bottomOffset - (_straightSide ? 0 : _thickness);

            //FirstVertex
            verticsIndex = 1;
            _uvs[0] = new Vector4(0, 0, 0, subPartId * 10f + 0 + 0.1f);

            //Vertices
            GenerateParabola(curentRadius, 0, 1, out curentRadius, out height, 0); //Top Parabola

            subPartId = 1; //Ridge sub part id
            AddDivision(height, curentRadius, 2); //Sharp Edge

            if (_ridgeWidth > 0)
            {
                GenerateRidge(curentRadius, 0, 1, out curentRadius, out height, 2); //Top Ridge
                AddDivision(height - _thickness, curentRadius, 2); //Antenna Thickness
                GenerateRidge(curentRadius, -_thickness, -1, out curentRadius, out height, 2); //Bottom Ridge
            }

            subPartId = 2; //Bottom parabola sub part id
            AddDivision(height, curentRadius, 1); //Sharp Edge
            if (_straightSide) AddDivision(-_bottomdepth, _bottomRadius, 1); //Straight Side
            else GenerateParabola(curentRadius, -_thickness, -1, out curentRadius, out height, 1, _bottomRadius); //Round Side
            if (_bottomOffset != 0)
            {
                if (_straightSide) AddDivision(-_bottomdepth, _bottomRadius, 2); //Sharp Edge
                else AddDivision(height, curentRadius, 2);
                AddDivision(lowestPoint, _bottomRadius, 2); //Bottom Offset
            }

            //LastVertex
            verticsIndex++;
            _vertices[_vertices.Length - 1] = new Vector3(0, lowestPoint, 0);
            _uvs[_vertices.Length - 1] = new Vector4(0, 0, 0, _bottomOffset != 0 ? subPartId * 10f + 2 + 0.1f : subPartId * 10f + 1 + 0.1f);

            //Triangles
            _totalVertices = _parabolaRes * _totalDivCount + 2;
            _triangles = new int[3 * _parabolaRes + 6 * (_totalVertices - 1)];
            //First division to center point
            int vertex = 0;
            for (int tri = 0; tri < _parabolaRes + 1; tri++, vertex += 3)
            {
                _triangles[vertex] = tri + 1;
                _triangles[vertex + 1] = tri;
                _triangles[vertex + 2] = 0;
            }
            _triangles[vertex - 3] = 1;

            //Rest of divisions
            int prevVertices = _parabolaRes;
            for (int div = 1; div < _totalDivCount; div++)
            {
                int startVertex = vertex;
                for (int quad = 0; quad < _parabolaRes; quad++, vertex += 6, prevVertices++)
                {
                    _triangles[vertex + 2] = _triangles[vertex + 4] = prevVertices + 1;
                    _triangles[vertex + 5] = prevVertices;
                    _triangles[vertex + 1] = prevVertices - _parabolaRes + 1;
                    _triangles[vertex + 3] = _triangles[vertex] = prevVertices - _parabolaRes;
                }
                _triangles[startVertex + 5] = prevVertices;
                _triangles[startVertex + 3] = _triangles[startVertex] = prevVertices - _parabolaRes;
            }

            //Last division to center point
            prevVertices -= _parabolaRes - 1;
            for (int tri = 0; tri < _parabolaRes; tri++, vertex += 3, prevVertices++)
            {
                _triangles[vertex] = prevVertices + 1;
                _triangles[vertex + 1] = _vertices.Length - 1;
                _triangles[vertex + 2] = prevVertices;
            }
            _triangles[vertex - 3] -= _parabolaRes;
        }

        private void GenerateParabola(float radius, float heightOffset, int direction, out float endRadius, out float endHeight, int matId, float stopRadius = -1)
        {
            endHeight = 0;
            for (int div = 1; div < _parabolaDivCount; div++)
            {
                radius += _parabolaDivWidth * direction;
                endHeight = HeightOnParabola(radius) + heightOffset;
                if (stopRadius > 0 && (direction == 1 && radius > stopRadius) || (direction == -1 && radius < stopRadius))
                {
                    radius = stopRadius;
                    endHeight = HeightOnParabola(radius) + heightOffset;
                    AddDivision(endHeight, radius, matId);
                    break;
                }
                AddDivision(endHeight, radius, matId);
                if (radius == stopRadius) break;
            }
            endRadius = radius;
        }

        private void GenerateRidge(float radius, float heightOffset, int direction, out float endRadius, out float endHeight, int matId)
        {
            endHeight = 0;
            for (int div = _parabolaDivCount; div < _parabolaDivCount + _ridgeDivCount; div++)
            {
                radius += _ridgeDivWidth * direction;
                endHeight = _ridgeB + _depth - _ridgeB / (_ridgeA * _ridgeA) * (radius - _radius - _ridgeA) * (radius - _radius - _ridgeA) + heightOffset;
                AddDivision(endHeight, radius, matId);

                if (radius > _ridgeEndX) _ridgeEndX = radius;
            }
            endRadius = radius;
            if (direction == 1) _ridgeEndY = endRadius;
        }

        private void AddDivision(float height, float radius, int matId)
        {
            Array.Resize(ref _vertices, _vertices.Length + _parabolaRes);
            Array.Resize(ref _uvs, _vertices.Length);
            _totalDivCount++;

            float angle = 0;
            for (int i = 0; i < _parabolaRes; i++, verticsIndex++)
            {
                angle += _divAngle;
                _vertices[verticsIndex] = new Vector3(
                    Mathf.Cos(angle) * radius,
                    height,
                    Mathf.Sin(angle) * radius
                );

                _uvs[verticsIndex] = (new Vector4(
                    _vertices[verticsIndex].x,
                    _vertices[verticsIndex].z,
                    0,
                    subPartId * 10f + matId + 0.1f
                ));
            }
        }

        private void CreateStructure()
        {
            float angle = 0;
            float angleStep = 2 * Mathf.PI / _suportArmCount;
            structureDivAngle = 2 * Mathf.PI / _structureRes;
            pointsToConnect = 0;
            preVertices = verticsIndex;
            triangleIndex = _triangles.Length;
            subPartId = 3; //Structure sub part id

            for (int i = 0; i < _suportArmCount; i++, angle += angleStep)
            {
                float pos = _structurePos * _radius;
                Vector3 A = new Vector3(_upperElementOffset.y, _focalLength, 0);
                Vector3 B = new Vector3(pos, HeightOnParabola(pos));

                Point(_focalLength + _upperElementOffset.x * _radius, 0, angle, Vector3.right);
                AnglePoint(_focalLength + _upperElementOffset.x * _radius, _upperElementOffset.y * _radius, angle, Vector3.right, B - A);
                PointOnParabola(pos, angle + angleStep * _doubleSupportArmOffset / 2, B - A);
                ConnectPoints(3);
                if (_doubleSupportArmOffset > 0)
                {
                    AnglePoint(_focalLength + _upperElementOffset.x * _radius, _upperElementOffset.y * _radius, angle, noPoint, B - A);
                    PointOnParabola(pos, angle + angleStep * -_doubleSupportArmOffset / 2, B - A);
                    ConnectPoints(3);
                }
            }
        }

        private void Point(float height, float centerOffset, float angle, Vector3 rotation, float hemiOffset = 0)
        {
            Point();
            Vector3 center = new Vector3(Mathf.Cos(angle) * centerOffset, height, Mathf.Sin(angle) * centerOffset);
            rotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0) * rotation;

            Plane p = new Plane(Vector3.forward, Vector3.zero);
            Vector3 xAxis = Vector3.up;
            Vector3 yAxis = Vector3.right;
            if (p.GetSide(rotation))
            {
                yAxis = Vector3.left;
            }
            Vector3.OrthoNormalize(ref rotation, ref xAxis, ref yAxis);

            float angle2 = 0;
            for (int i = 0; i < _structureRes; i++, verticsIndex++, angle2 -= structureDivAngle)
            {
                float angleSin = Mathf.Sin(angle2);
                _vertices[verticsIndex] =
                    xAxis * Mathf.Cos(angle2) * _strucureRadius +
                    yAxis * (angleSin * _strucureRadius + hemiOffset * Mathf.Sign(angleSin)) +
                    center;
            }
        }

        private void PointOnParabola(float centerOffset, float angle, Vector3 rotation)
        {
            Point();
            float u = rotation.x / rotation.y;
            float f = 1 / (4 * _focalLength);

            rotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0) * rotation;
            Plane p = new Plane(Vector3.forward, Vector3.zero);
            Vector3 xAxis = Vector3.up;
            Vector3 yAxis = Vector3.right;

            if (p.GetSide(rotation)) yAxis = Vector3.left;
            Vector3.OrthoNormalize(ref rotation, ref xAxis, ref yAxis);

            float angle2 = 0;
            for (int i = 0; i < _structureRes; i++, verticsIndex++, angle2 -= structureDivAngle)
            {
                float x = Mathf.Cos(angle2) * _strucureRadius + centerOffset;
                float y = HeightOnParabola(x);
                x = (1 - Mathf.Sqrt(1 - 4 * u * f * (-u * y + x))) / (2 * u * f);

                _vertices[verticsIndex] = yAxis * Mathf.Sin(angle2) * _strucureRadius +
                    new Vector3(
                        Mathf.Cos(angle) * x,
                        HeightOnParabola(x),
                        Mathf.Sin(angle) * x);
            }
        }

        private void AnglePoint(float height, float offset, float angle, Vector3 rotationA, Vector3 rotationB)
        {
            if (rotationA != noPoint)
            {
                Point(height, offset, angle, rotationA);
                if (rotationB != noPoint) Point(height, offset, angle, (rotationA + rotationB) / 2);
            }
            if (rotationB != noPoint) Point(height, offset, angle, rotationB);
        }

        private void Point()
        {
            Array.Resize(ref _vertices, _vertices.Length + _structureRes);
            Array.Resize(ref _triangles, _triangles.Length + _structureRes * 2 * 3);
            Array.Resize(ref _uvs, _vertices.Length);
            pointsToConnect++;
        }

        private void ConnectPoints(int matId)
        {
            int vertex = verticsIndex - 1;

            while (pointsToConnect > 1)
            {
                pointsToConnect--;

                for (int quad = 0; quad < _structureRes; quad++, triangleIndex += 6, vertex--)
                {
                    _triangles[triangleIndex] = vertex;
                    _triangles[triangleIndex + 2] = _triangles[triangleIndex + 5] = vertex - 1;
                    _triangles[triangleIndex + 1] = _triangles[triangleIndex + 3] = vertex - _structureRes;
                    _triangles[triangleIndex + 4] = vertex - _structureRes - 1;

                    _uvs[vertex] = (new Vector4(
                        _vertices[vertex].x,
                        _vertices[vertex].z,
                        0,
                        subPartId * 10f + matId + 0.1f
                    ));
                }
                _triangles[triangleIndex - 6 + 4] += _structureRes;
                _triangles[triangleIndex - 6 + 2] = _triangles[triangleIndex - 6 + 5] += _structureRes;
            }

            for (; vertex >= preVertices; vertex--)
                _uvs[vertex] = (new Vector4(
                    _vertices[vertex].x,
                    _vertices[vertex].z,
                    0,
                    subPartId * 10f + matId + 0.1f
                ));

            pointsToConnect = 0;
        }

        // private void GenerateSideStructure () {
        //     Vector2 RidgeAnchor = new Vector2 (_ridgeEndX, _ridgeEndY);
        //     Vector2 BottomAnchor = new Vector2 ();
        // }

        private void ApplyMesh(bool collider)
        {
            if (collider)
            {
                _meshCollider.sharedMesh = null;
                _colliderMesh.Clear();
                _colliderMesh.SetVertices(_vertices);
                _colliderMesh.SetTriangles(_triangles, 0);
                _meshCollider.sharedMesh = _colliderMesh;
            }
            else
            {
                _meshFilter.sharedMesh = null;
                _mesh.Clear();
                _mesh.SetVertices(_vertices);
                _mesh.SetTriangles(_triangles, 0);
                _mesh.SetUVs(0, _uvs);
                _mesh.RecalculateNormals();
                _mesh.RecalculateTangents();
                _meshFilter.sharedMesh = _mesh;

                CreateMesh(_radius, _depth, 16, 2, _thickness, 0, 0, 0, 0, _straightSide, _bottomRadius, _bottomdepth, _bottomOffset, 3, _structurePos, _strucureRadius, _suportArmCount, _doubleSupportArmOffset, _upperElementOffset, true);
            }
        }

        private float HeightOnParabola(float r) => r * r / (4 * _focalLength);
    }
}