using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.DroonComLinks.Interfaces;
using ModApi.Craft.Parts;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Objects.Antennas.WhipAntennas
{
    public class FixedWhipAntenna : IWhipAntenna
    {
        public WhipAntennaTypes type => WhipAntennaTypes.Fixed;
        public bool custom { get; set; }
        public bool animating { get; set; } = false;
        public bool opened { get; set; } = true;
        public Transform parent { get; set; }
        private Dictionary<WhipAntennaStyles, string> _stylePrefabNames = new Dictionary<WhipAntennaStyles, string>() {
            { WhipAntennaStyles.Simple, "FixedMonopoleAntenna1" }};
        private WhipAntennaStyles _style;
        private Transform antenna;
        private Transform antennaRod;
        private Transform antennaBase;

        public void Initialize(Transform parent, bool customAntenna, WhipAntennaData data)
        {
            this.parent = parent;
            custom = customAntenna;
            _style = data.antennaStyle;

            if (!custom)
            {
                antenna = UnityEngine.Object.Instantiate(Mod.Instance.ResourceLoader.LoadAsset<GameObject>("Assets/Content/Craft/Parts/WhipAntenna/Prefabs/" + _stylePrefabNames[_style] + ".prefab")).transform;
                antennaBase = antenna.Find("AntennaBase");
                antennaRod = antenna.Find("AntennaRod");
                antenna.SetParent(parent, worldPositionStays: false);
            }
        }

        public void SetPosition(bool startOpen) { if (ModSettings.Instance.debugMode) Debug.LogError("Trying to set position of fixed antenna"); }

        public IEnumerator Toggle(bool open, float animationDuration, WhipAntennaData data) { if (ModSettings.Instance.debugMode) Debug.LogError("Trying to toggle fixed antenna"); yield return 0; }

        public void Update(WhipAntennaData data)
        {
            float thicknessOffset = data.thickness > 0.02f ? data.thickness - 0.02f : 0;
            antenna.localScale = Vector3.one * data.size;
            antennaBase.localScale = Vector3.one * (0.014f + thicknessOffset) / 0.014f;
            antennaRod.localScale = new Vector3(data.thickness / 0.01f, data.length, data.thickness / 0.01f);
        }

        public void Destroy()
        {
            UnityEngine.Object.DestroyImmediate(antenna.gameObject);
        }
    }
}