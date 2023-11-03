using Assets.Scripts.DroonComLinks.Network;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Ui.Flight
{
    public class DCLSphere : MonoBehaviour
    {
        public const string prefix = "DCLSPHERE_";
        public string id { get; private set; }
        public float Radius { get; private set; }
        public Vector3d Center { get; private set; }
        private MeshRenderer _renderer;
        private Color defaultColor = new(1, 1, 1);
        private Color HighlightedColor = new(0, 0, 1);

        public static DCLSphere CreateNew(string name, Vector3d c, float r)
        {
            DCLSphere sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere).AddComponent<DCLSphere>();
            sphere.Initialise(name, c, r);
            return sphere;
        }

        private void Initialise(string name, Vector3d c, float r)
        {
            id = name;
            Center = c;
            Radius = r;

            gameObject.layer = LayerMask.NameToLayer("MapView");
            gameObject.transform.name = prefix + id;

            _renderer = gameObject.GetComponent<MeshRenderer>();
            _renderer.material = new Material(Shader.Find("Jundroo/MapView/CraftOrbitLine"));

            SetHilighted(false);
            Update();
        }

        public void UpdateSphere(Vector3d c)
        {
            Center = c;
            Update();
        }

        public void UpdateSphere(Vector3d c, float r)
        {
            Center = c;
            Radius = r;
            Update();
        }

        public void SetHilighted(bool hilighted)
        {
            Color color = hilighted ? HighlightedColor : defaultColor;
            _renderer.material.SetColor("_endColor", color);
            _renderer.material.SetColor("_startColor", color);
        }

        public static float ScaleAt(Vector3 pos) => Vector3.Distance(pos, Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.MapCamera.transform.position) / 1000;

        private void Update()
        {
            float scale = Radius / gameObject.GetComponent<SphereCollider>().radius * ScaleAt((Vector3)Center) / 50000000;
            gameObject.transform.position = (Vector3)ComLinksManager.Instance.coordinateConverter.ConvertSolarToMapView(Center);
            gameObject.transform.localScale = scale * Vector3.one;
        }

        public void DestroySphere()
        {
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }
    }
}