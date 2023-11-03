using Assets.Scripts.DroonComLinks.Network;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Ui.Flight
{
    public class NodeButton : MonoBehaviour
    {
        public const string prefix = "NodeButton_";
        public NetworkNode node;
        public string id { get; private set; }
        public float Radius { get; private set; }
        public bool active;
        public Renderer _coreRenderer, _exteriorRenderer;

        public NodeButton(string id, NetworkNode node, float radius)
        {
            this.id = id;
            this.Radius = radius;
            this.node = node;

            gameObject.transform.name = prefix + this.id;
        }

        public static float ScaleAt(Vector3 pos) => Vector3.Distance(pos, Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.MapCamera.transform.position) / 1000;

        private void Update()
        {
            float scale = Radius / GetComponent<SphereCollider>().radius * ScaleAt((Vector3)node.Position) / 50000000f;
            transform.localScale = scale * Vector3.one;
            transform.position = (Vector3)ComLinksManager.Instance.coordinateConverter.ConvertSolarToMapView(node.Position);
        }

        public void Destroy()
        {
            Destroy(gameObject);
        }
    }
}
