using System;
using Assets.Scripts.Flight.MapView;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Ui.Flight
{
    public class DCLSphere : MonoBehaviour
    {
        public string id { get; private set; }
        public float radius { get; private set; }
        public Vector3d center { get; private set; }
        private GameObject _gameObject;

        public DCLSphere(string name, Vector3d c, float r)
        {
            id = name;
            center = c;
            radius = r;

            _gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _gameObject.layer = 10;

            MeshRenderer renderer = _gameObject.GetComponent<MeshRenderer>();
            renderer.material = UnityEngine.Object.Instantiate(new Material(Shader.Find("Jundroo/MapView/CraftOrbitLine")));
            renderer.material.SetColor("_startColor", new Color(1, 1, 1));
            renderer.material.SetColor("_endColor", new Color(1, 1, 1));

            Update();
        }

        public void UpdateSphere(Vector3d c)
        {
            center = c;
            Update();
        }

        public void UpdateSphere(Vector3d c, float r)
        {
            center = c;
            radius = r;
            Update();
        }

        private void Update()
        {
            float scale = (radius / _gameObject.GetComponent<SphereCollider>().radius) * DCLLine.ScaleAt((Vector3)center) / 50000000;
            _gameObject.transform.position = (Vector3)Mod.Instance.comLinksManager.coordinateConverter.ConvertSolarToMapView(center);
            _gameObject.transform.localScale = new Vector3(scale, scale, scale);
        }
        public void DestroySphere()
        {
            if (_gameObject != null)
            {
                Destroy(_gameObject);
                _gameObject = null;
            }
        }
    }
}