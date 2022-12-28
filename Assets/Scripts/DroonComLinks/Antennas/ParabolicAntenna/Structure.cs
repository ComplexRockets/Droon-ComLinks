using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Antennas.ParabolicAntenna {
    public class Structure : MonoBehaviour {
        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;
        private Mesh _mesh;
        private Mesh _colliderMesh;
        private Vector3[] _vertices;
        private Vector4[] _uvs;
        private Vector3 noPoint = new Vector3 (9182367, 2973, 0209736);
        private int[] _triangles;
        private int verticsIndex, triangleIndex, _verticsToShow, _parabolaDivCount, pointsToConnect, _totalVertices, _resolution, subPartId;
        private float parabolaDivWidth, ridgeDivWidth, _focalLength, _depth, _verticsScale, _radius, _strucureRadius, structureDivAngle;

        public void Initialize () {
            _mesh = new Mesh ();
            _colliderMesh = new Mesh ();
            _meshFilter = this.gameObject.GetComponent<MeshFilter> ();
            _meshCollider = this.gameObject.GetComponent<MeshCollider> ();
        }

        public void CreateMesh (float radius, float depth, int resolution, float structurePos, float strucureRadius, int suportArmCount, float doubleSupportArmOffset, Vector2 offset = new Vector2 (), bool collider = false) {
            _radius = radius;
            _depth = depth;
            _strucureRadius = strucureRadius;
            _resolution = resolution;
            _totalVertices = suportArmCount * resolution * 2;
            _vertices = new Vector3[0];
            _triangles = new int[0];
            _focalLength = (_radius * _radius) / (4 * depth);
            structureDivAngle = 2 * Mathf.PI / resolution;
            verticsIndex = 0;
            triangleIndex = 0;
            pointsToConnect = 0;
            subPartId = 1;

            float angle = 0;
            float angleStep = 2 * Mathf.PI / suportArmCount;

            for (int i = 0; i < suportArmCount; i++, angle += angleStep) {
                float pos = structurePos * _radius;
                Vector3 A = new Vector3 (offset.y, _focalLength, 0);
                Vector3 B = new Vector3 (pos, HeightOnParabola (pos));

                Point (_focalLength + offset.x * radius, 0, angle, Vector3.right);
                AnglePoint (_focalLength + offset.x * radius, offset.y * radius, angle, Vector3.right, B - A);
                PointOnParabola (pos, angle + angleStep * doubleSupportArmOffset / 2, B - A);
                ConnectPoints (3);
                if (doubleSupportArmOffset > 0) {
                    AnglePoint (_focalLength + offset.x * radius, offset.y * radius, angle, noPoint, B - A);
                    PointOnParabola (pos, angle + angleStep * -doubleSupportArmOffset / 2, B - A);
                    ConnectPoints (3);
                }
            }

            if (!collider) {
                _meshFilter.sharedMesh = null;
                _mesh.Clear ();
                _mesh.SetVertices (_vertices);
                _mesh.SetTriangles (_triangles, 0);
                _mesh.SetUVs (0, _uvs);
                _mesh.RecalculateNormals ();
                _mesh.RecalculateTangents ();
                _meshFilter.sharedMesh = _mesh;

                CreateMesh (radius, depth, 3, structurePos, strucureRadius, suportArmCount, doubleSupportArmOffset, offset, collider = true);
            } else {
                _meshCollider.sharedMesh = null;
                _colliderMesh.Clear ();
                _colliderMesh.SetVertices (_vertices);
                _colliderMesh.SetTriangles (_triangles, 0);
                _meshCollider.sharedMesh = _colliderMesh;
            }
        }

        private float HeightOnParabola (float r) => r * r / (4 * _focalLength);

        private void Point (float height, float centerOffset, float angle, Vector3 rotation, float hemiOffset = 0) {
            Point ();
            Vector3 center = new Vector3 (Mathf.Cos (angle) * centerOffset, height, Mathf.Sin (angle) * centerOffset);
            rotation = Quaternion.Euler (0, -angle * Mathf.Rad2Deg, 0) * rotation;

            Plane p = new Plane (Vector3.forward, Vector3.zero);
            Vector3 xAxis = Vector3.up;
            Vector3 yAxis = Vector3.right;
            if (p.GetSide (rotation)) {
                yAxis = Vector3.left;
            }
            Vector3.OrthoNormalize (ref rotation, ref xAxis, ref yAxis);

            float angle2 = 0;
            for (int i = 0; i < _resolution; i++, verticsIndex++, angle2 -= structureDivAngle) {
                float angleSin = Mathf.Sin (angle2);
                _vertices[verticsIndex] =
                    xAxis * Mathf.Cos (angle2) * _strucureRadius +
                    yAxis * (angleSin * _strucureRadius + hemiOffset * Mathf.Sign (angleSin)) +
                    center;
            }
        }

        private void PointOnParabola (float centerOffset, float angle, Vector3 rotation) {
            Point ();
            float u = rotation.x / rotation.y;
            float f = 1 / (4 * _focalLength);

            rotation = Quaternion.Euler (0, -angle * Mathf.Rad2Deg, 0) * rotation;
            Plane p = new Plane (Vector3.forward, Vector3.zero);
            Vector3 xAxis = Vector3.up;
            Vector3 yAxis = Vector3.right;

            if (p.GetSide (rotation)) yAxis = Vector3.left;
            Vector3.OrthoNormalize (ref rotation, ref xAxis, ref yAxis);

            float angle2 = 0;
            for (int i = 0; i < _resolution; i++, verticsIndex++, angle2 -= structureDivAngle) {
                float x = Mathf.Cos (angle2) * _strucureRadius + centerOffset;
                float y = HeightOnParabola (x);
                x = (1 - Mathf.Sqrt (1 - 4 * u * f * (-u * y + x))) / (2 * u * f);

                _vertices[verticsIndex] = yAxis * Mathf.Sin (angle2) * _strucureRadius +
                    new Vector3 (
                        Mathf.Cos (angle) * x,
                        HeightOnParabola (x),
                        Mathf.Sin (angle) * x);
            }
        }

        private void AnglePoint (float height, float offset, float angle, Vector3 rotationA, Vector3 rotationB) {
            if (rotationA != noPoint) {
                Point (height, offset, angle, rotationA);
                if (rotationB != noPoint) Point (height, offset, angle, (rotationA + rotationB) / 2);
            }
            if (rotationB != noPoint) Point (height, offset, angle, rotationB);
        }

        private void Point () {
            Array.Resize (ref _vertices, _vertices.Length + _resolution);
            Array.Resize (ref _triangles, _triangles.Length + _resolution * 2 * 3);
            Array.Resize (ref _uvs, _vertices.Length);
            pointsToConnect++;
        }

        private void ConnectPoints (int matId) {
            int vertex = verticsIndex - 1;

            while (pointsToConnect > 1) {
                pointsToConnect--;

                for (int quad = 0; quad < _resolution; quad++, triangleIndex += 6, vertex--) {
                    _triangles[triangleIndex] = vertex;
                    _triangles[triangleIndex + 2] = _triangles[triangleIndex + 5] = vertex - 1;
                    _triangles[triangleIndex + 1] = _triangles[triangleIndex + 3] = vertex - _resolution;
                    _triangles[triangleIndex + 4] = vertex - _resolution - 1;

                    _uvs[vertex] = (new Vector4 (
                        _vertices[vertex].x,
                        _vertices[vertex].z,
                        0,
                        subPartId * 10f + matId + 0.1f
                    ));
                }
                _triangles[triangleIndex - 6 + 4] += _resolution;
                _triangles[triangleIndex - 6 + 2] = _triangles[triangleIndex - 6 + 5] += _resolution;
            }

            for (; vertex >= 0; vertex--)
                _uvs[vertex] = (new Vector4 (
                    _vertices[vertex].x,
                    _vertices[vertex].z,
                    0,
                    subPartId * 10f + matId + 0.1f
                ));

            pointsToConnect = 0;
        }
    }
}