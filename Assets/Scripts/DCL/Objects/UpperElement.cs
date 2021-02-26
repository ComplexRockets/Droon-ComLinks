namespace Assets.Scripts.DroonComLinks.Objects {
    using System.Collections.Generic;
    using System.Linq;
    using ModApi.Craft.Parts;
    using UnityEngine;

    public class UpperElement {
        public string Name;
        public int Type;
        public GameObject Object;
        private IPartScript _partScript;
        public Vector2 Offset;

        private static Element[] Elements = new Element[3] {
            new Element ("ReceiverUP1", "Basic", new Vector2 (0.12f, 0.2f)),
            new Element ("ReflectorUP1", "Basic", new Vector2 (0.2f, 0.15f)),
            new Element ("ReflectorUP2", "Cassinni", new Vector2 (0, 0.2f))
        };
        public static List<string> Receivers = Elements.Where (e => e.Name.Contains ("Receiver")).Select (e => e.DisplayName).ToList ();
        public static List<string> Reflectors = Elements.Where (e => e.Name.Contains ("Reflector")).Select (e => e.DisplayName).ToList ();

        public UpperElement (string displayName, bool reflector, Transform parent, IPartScript partScript) {
            _partScript = partScript;
            string type = reflector? "Reflector": "Receiver";
            Type = Elements.ToList ().IndexOf (Elements.ToList ().Find (e => e.Name.Contains (type) && e.DisplayName == displayName));
            //Debug.Log ("Upper Type: " + Type);
            Name = Elements[Type].Name;
            Offset = Elements[Type].Offset;
            Object = UnityEngine.Object.Instantiate (Mod.Instance.ResourceLoader.LoadAsset<GameObject> ("Assets/Content/Craft/Parts/Meshes/ParametricalAntenna/Prefabs/" + Name + ".prefab"));
            Object.GetComponentsInChildren<Renderer> ().ToList ().ForEach (r => _partScript.PartMaterialScript.AddRenderer (r, excludeFromDrag : true));
            //Object.transform.parent = parent;
            Object.transform.SetParent(parent, worldPositionStays: false);
        }

        public void Destroy () {
            foreach (GameObject obj in Object.GetComponentsInChildren<GameObject> ()) {
                _partScript.PartMaterialScript.RemoveRenderer (obj.GetComponentInChildren<Renderer> ());
                GameObject.Destroy (obj);
            }
            GameObject.Destroy (Object);
        }

        public static string GetDisplayName (int type) {
            return Elements[type].Name;
        }

        public static class Types {
            public static int ReceiverUP1 = 0;
            public static int ReflectorUP1 = 1;
            public static int ReflectorUP2 = 2;
        }
        private class Element {
            public string Name;
            public string DisplayName;
            public Vector2 Offset;
            public Element (string name, string displayName, Vector2 offset) {
                Name = name;
                Offset = offset;
                DisplayName = displayName;
            }
        }
    }
}