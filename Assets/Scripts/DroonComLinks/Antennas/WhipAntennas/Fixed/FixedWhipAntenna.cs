using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Craft.Parts.Modifiers;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Antennas.WhipAntennas.Fixed
{
    public abstract class FixedWhipAntenna : IWhipAntenna
    {
        public WhipAntennaTypes type => WhipAntennaTypes.Fixed;
        public bool custom { get; set; }
        public bool animating { get; set; } = false;
        public bool opened { get; set; } = true;
        public Transform parent { get; set; }
        private WhipAntennaStyles _style;
        private Transform antenna;
        private Transform antennaRod;
        private Transform antennaBase;
        public abstract string stylePrefabName { get; set; }
        public abstract float mass { get; set; }

        public static FixedWhipAntenna Create(WhipAntennaStyles style)
        {
            switch (style)
            {
                case WhipAntennaStyles.Simple:
                    return new SimpleFixedWhipAntenna();
                default:
                    Debug.LogError("Unsupported fixed whip antenna style: " + style.ToString());
                    return null;
            }
        }

        public void Initialize(Transform parent, bool customAntenna, WhipAntennaData data)
        {
            this.parent = parent;
            custom = customAntenna;
            _style = data.antennaStyle;

            if (!custom)
            {
                antenna = UnityEngine.Object.Instantiate(Mod.Instance.ResourceLoader.LoadAsset<GameObject>("Assets/Content/Craft/Parts/WhipAntenna/Prefabs/" + stylePrefabName + ".prefab")).transform;
                antennaBase = antenna.Find("AntennaBase");
                antennaRod = antenna.Find("AntennaRod");
                antenna.SetParent(parent, worldPositionStays: false);
            }
        }

        public void SetPosition(bool startOpen) { if (ModSettings.Instance.DebugMode) Debug.LogError("Trying to set position of fixed antenna"); }

        public IEnumerator Toggle(bool open, float animationDuration, WhipAntennaData data) { if (ModSettings.Instance.DebugMode) Debug.LogError("Trying to toggle fixed antenna"); yield return 0; }

        public void UpdateAntenna(WhipAntennaData data)
        {
            float thicknessOffset = data.thickness > 0.02f ? data.thickness - 0.02f : 0;
            antenna.localScale = Vector3.one * data.Size;
            antennaBase.localScale = Vector3.one * (0.014f + thicknessOffset) / 0.014f;
            antennaRod.localScale = new Vector3(data.thickness / 0.01f, data.length, data.thickness / 0.01f);
        }

        public void Destroy()
        {
            UnityEngine.Object.DestroyImmediate(antenna.gameObject);
        }
    }
}