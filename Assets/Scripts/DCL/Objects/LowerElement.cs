namespace Assets.Scripts.DroonComLinks.Objects {
    using System.Collections.Generic;
    using System.Linq;
    using ModApi.Craft.Parts;
    using UnityEngine;

    public class LowerElement {
        public string Name;
        public int Type;
        public GameObject Object;
        private IPartScript _partScript;

        private static Element[] Elements = new Element[2] {
            //new Element ("None", "None"),
            new Element ("ReceiverDown1", "Basic"),
            new Element ("ReceiverDown2", "Cassinni")
        };
        public static List<string> Receivers = Elements.Select (e => e.DisplayName).ToList ();

        public LowerElement (string displayName, Transform parent, IPartScript partScript) {
            _partScript = partScript;
            Type = Elements.ToList ().IndexOf (Elements.ToList ().Find (e => e.DisplayName == displayName));
            if (Type == -1) return;
            Name = Elements[Type].Name;
            //if (Type != 0) {
            Object = UnityEngine.Object.Instantiate (Mod.Instance.ResourceLoader.LoadAsset<GameObject> ("Assets/Content/Craft/Parts/Meshes/ParametricalAntenna/Prefabs/" + Name + ".prefab"));
            Object.GetComponentsInChildren<Renderer> ().ToList ().ForEach (r => _partScript.PartMaterialScript.AddRenderer (r, excludeFromDrag : true));
            //Object.transform.parent = parent;
            //Object.transform.localPosition = new Vector3 (0, 0, 0);
            Object.transform.SetParent(parent, worldPositionStays: false);
            //}
        }

        public void Destroy () {
            foreach (GameObject obj in Object.GetComponentsInChildren<GameObject> ()) {
                _partScript.PartMaterialScript.RemoveRenderer (obj.GetComponentInChildren<Renderer> ());
                GameObject.Destroy (obj);
            }
            GameObject.Destroy (Object);
        }

        public static class Types {
            //public static int None = 0;
            public static int ReceiverDown1 = 0;
            public static int ReceiverDown2 = 1;
        }
        private class Element {
            public string Name;
            public string DisplayName;
            public Element (string name, string displayName) {
                Name = name;
                DisplayName = displayName;
            }
        }
    }
}