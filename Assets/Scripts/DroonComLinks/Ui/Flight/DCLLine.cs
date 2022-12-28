using System;
using System.Collections.Generic;
using UnityEngine;
using Vectrosity;

namespace Assets.Scripts.DroonComLinks.Ui.Flight
{
    public class DCLLine : MonoBehaviour
    {
        private string _id;
        private Color _color;
        private float _scale;
        private Vector3 _posA;
        private Vector3 _posB;
        private GameObject _gameObject;
        private LineRenderer _lineRenderer;
        public string id => _id;
        private float timeMultiplier => Math.Max(1, (float)Game.Instance.FlightScene.TimeManager.CurrentMode.TimeMultiplier / 2000000);

        public DCLLine(string name)
        {
            _id = name;

            _gameObject = UnityEngine.Object.Instantiate(Mod.Instance.ResourceLoader.LoadAsset<GameObject>("Assets/Content/Craft/Parts/Prefabs/DCLLine.prefab"));
            _gameObject.layer = 10;
            _lineRenderer = _gameObject.GetComponent<LineRenderer>();

            Material mat = UnityEngine.Object.Instantiate(new Material(Shader.Find("Jundroo/MapView/CraftOrbitLine")));
            _lineRenderer.material = mat; // _ARenderer.material = _BRenderer.material = mat;

            visible(false);
        }
        public void UpdateLine(Vector3 posA, Vector3 posB, Color color, float scale, bool upLink, bool downLink)
        {
            visible(true);
            _posA = posA;
            _posB = posB;
            _color = color;
            _scale = scale;

            try
            {
                _lineRenderer.material.SetColor("_startColor", _color);
                _lineRenderer.material.SetColor("_endColor", _color);
            }
            catch (Exception e)
            {
                Debug.LogError("material error: " + e);
                _lineRenderer.material = UnityEngine.Object.Instantiate(new Material(Shader.Find("Jundroo/MapView/CraftOrbitLine")));
            }

            _lineRenderer.startWidth = ScaleAt(_posA) * _scale / timeMultiplier;
            _lineRenderer.endWidth = ScaleAt(_posB) * _scale / timeMultiplier;
            _lineRenderer.SetPosition(0, _posA);
            _lineRenderer.SetPosition(1, _posB);
        }

        public static float ScaleAt(Vector3 pos) => Vector3.Distance(pos, Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.MapCamera.transform.position) / 1000;

        public void visible(bool visible, bool upLink = true, bool downLink = true)
        {
            try
            {
                _lineRenderer.enabled = visible;
            }
            catch (Exception e) { Debug.LogError("Line visible error: " + e); }
        }

        public void DestroyLine()
        {
            if (_gameObject != null)
            {
                Destroy(_lineRenderer);
                Destroy(_gameObject);
                _gameObject = null;
            }
        }
    }
}