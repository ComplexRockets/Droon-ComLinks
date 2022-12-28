using System;
using System.Collections.Generic;
using System.Linq;
using ModApi.Craft.Parts;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Antennas.ParabolicAntenna
{
    public class LowerElement
    {
        public string name;
        public int type;
        public GameObject gameObject;
        private IPartScript _partScript;
        public Element elementData => Elements[type];
        private static Element[] Elements = new Element[3] {
                new Element ("ReceiverDown1", "Basic", 1000),
                new Element ("ReceiverDown2", "Saturn", 3000),
                new Element ("ReceiverDown3", "Long", 2500)
            };
        public static List<string> Receivers = Elements.Select(e => e.displayName).ToList();

        public LowerElement(string displayName, Transform parent, IPartScript partScript)
        {
            type = Elements.ToList().IndexOf(Elements.ToList().Find(e => e.displayName == displayName));
            if (type == -1) return;

            _partScript = partScript;
            name = Elements[type].name;

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
            catch (Exception e) { Debug.Log("Error in remove renderers for LowerElement " + e); }
            UnityEngine.Object.Destroy(gameObject);
        }

        public static class Types
        {
            //public static int None = 0;
            public static int ReceiverDown1 = 0;
            public static int ReceiverDown2 = 1;
        }
        public class Element
        {
            public string name;
            public string displayName;
            public int price;
            public Element(string name, string displayName, int price)
            {
                this.name = name;
                this.displayName = displayName;
                this.price = price;
            }
        }
    }
}