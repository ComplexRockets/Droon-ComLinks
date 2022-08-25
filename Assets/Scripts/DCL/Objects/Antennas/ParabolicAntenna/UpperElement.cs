using System;
using System.Collections.Generic;
using System.Linq;
using ModApi.Craft.Parts;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Objects.Antennas.ParabolicAntenna
{
    public class UpperElement
    {
        public string name;
        public int type;
        public GameObject gameObject;
        private IPartScript _partScript;
        public Vector2 offset;
        public Element elementData => Elements[type];
        private static Element[] Elements = new Element[3] {
            new Element ("ReceiverUP1", "Basic", new Vector2 (0.065f, 0.1f), 1000),
            new Element ("ReflectorUP1", "Basic", new Vector2 (0.1f, 0.075f), 1000),
            new Element ("ReflectorUP2_1", "Saturn", new Vector2 (0.05f, 0.15f), 3000)//0.06f, 0.15f //0.1f, 0.075
        };
        public static List<string> Receivers = Elements.Where(e => e.name.Contains("Receiver")).Select(e => e.displayName).ToList();
        public static List<string> Reflectors = Elements.Where(e => e.name.Contains("Reflector")).Select(e => e.displayName).ToList();

        public UpperElement(string displayName, bool reflector, Transform parent, IPartScript partScript)
        {
            _partScript = partScript;
            string typeName = reflector ? "Reflector" : "Receiver";
            type = Elements.ToList().IndexOf(Elements.ToList().Find(e => e.name.Contains(typeName) && e.displayName == displayName));
            name = Elements[type].name;
            offset = Elements[type].offset;
            gameObject = UnityEngine.Object.Instantiate(Mod.Instance.ResourceLoader.LoadAsset<GameObject>(ParabolicAntennaMesh.prefabPath + name + ".prefab"));
            gameObject.GetComponentsInChildren<Renderer>().ToList().ForEach(r => _partScript.PartMaterialScript.AddRenderer(r, excludeFromDrag: false));
            gameObject.transform.SetParent(parent, worldPositionStays: false);
        }

        public void Destroy()
        {
            try
            {
                MeshRenderer[] componentsInChildren = gameObject.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer renderer in componentsInChildren)
                {
                    _partScript.PartMaterialScript.RemoveRenderer(renderer);
                }
            }
            catch (Exception e) { Debug.Log("Error in remove renderers for UpperElement " + e); }
            UnityEngine.Object.Destroy(gameObject);
        }

        public static string GetDisplayName(int type)
        {
            return Elements[type].name;
        }

        public static class Types
        {
            public static int ReceiverUP1 = 0;
            public static int ReflectorUP1 = 1;
            public static int ReflectorUP2 = 2;
        }
        public class Element
        {
            public string name;
            public string displayName;
            public Vector2 offset; // x:height y:radius
            public int price;
            public Element(string name, string displayName, Vector2 offset, int price)
            {
                this.name = name;
                this.offset = offset;
                this.displayName = displayName;
                this.price = price;
            }
        }
    }
}