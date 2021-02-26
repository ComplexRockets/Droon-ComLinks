using System;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Objects
{
    public class DCLLine : MonoBehaviour {
        private String _id;
        private Color _color;
        private float _scale;
        private Vector3 _posA;
        private Vector3 _posB;
        private GameObject _gameObject;
        private LineRenderer _lineRenderer;
        public String id => _id;

        public DCLLine (String name) {
            _id = name;
            _gameObject = GameObject.Instantiate (Mod.Instance.ResourceLoader.LoadAsset<GameObject> ("Assets/Content/Craft/Parts/Prefabs/DCLLine.prefab"));
            _gameObject.layer = 10;
            _lineRenderer = _gameObject.GetComponent<LineRenderer> ();
            _lineRenderer.material = UnityEngine.Object.Instantiate (new Material (Shader.Find ("Jundroo/MapView/CraftOrbitLine")));
            visible (false);
        }
        public void UpdateLine (Vector3 posA, Vector3 posB, Color color, float scale) {
            visible (true);
            _posA = posA;
            _posB = posB;
            _color = color;
            _scale = scale;

            try {
                _lineRenderer.material.color = _color;
                _lineRenderer.material.SetColor ("_startColor", _color);
                _lineRenderer.material.SetColor ("_endColor", _color);
            } catch (Exception e) {
                Debug.LogError ("material error: " + e);
                _lineRenderer.material = UnityEngine.Object.Instantiate (new Material (Shader.Find ("Jundroo/MapView/CraftOrbitLine")));
            }

            _lineRenderer.startWidth = Vector3.Distance (_posA, Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.MapCamera.transform.position) / 1000 * _scale;
            _lineRenderer.endWidth = Vector3.Distance (_posB, Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.MapCamera.transform.position) / 1000 * _scale;
            _lineRenderer.SetPosition (0, _posA);
            _lineRenderer.SetPosition (1, _posB);
        }

        public void visible (bool visible) {
            try {
                _lineRenderer.enabled = visible;
            } catch (Exception e) { Debug.LogError ("Line visible error: " + e); }
        }

        public void DestroyLine () {
            if (_gameObject != null) {
                Destroy (_lineRenderer);
                Destroy (_gameObject);
                _gameObject = null;
            }
        }
    }
}