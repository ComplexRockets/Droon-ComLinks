using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.DroonComLinks.Interfaces;
using ModApi.Craft.Parts;
using UnityEngine;
using static Assets.Scripts.Craft.Parts.Modifiers.WhipAntennaData;

namespace Assets.Scripts.DroonComLinks.Objects.Antennas.WhipAntennas
{
    public class LargeDeployableWhipAntenna : IWhipAntenna
    {
        public WhipAntennaTypes type => WhipAntennaTypes.Deployable;
        public Transform parent { get; set; }
        public bool animating { get; set; } = false;
        public bool opened { get; set; }
        public bool custom { get; set; }
        private GameObject[] _segments;
        private Dictionary<DeployableAntennaStyles, float[]> _segmentsOffsets = new Dictionary<DeployableAntennaStyles, float[]>() {
            { DeployableAntennaStyles.Simple, new float[] { 0, -0.78f, -0.72f, -0.59f, -0.53f, -0.45f, -0.4f } },
            { DeployableAntennaStyles.Thin, new float[] { 0, -1.33f, -1.33f, -1.3f }} };
        private Dictionary<DeployableAntennaStyles, int> _segmentCounts = new Dictionary<DeployableAntennaStyles, int>() {
            { DeployableAntennaStyles.Simple, 7 },
            { DeployableAntennaStyles.Thin, 4 } };
        private Dictionary<DeployableAntennaStyles, string> _stylePrefabNames = new Dictionary<DeployableAntennaStyles, string>() {
            { DeployableAntennaStyles.Simple, "DeployableWhipAntenna1" },
            { DeployableAntennaStyles.Thin, "DeployableWhipAntenna3" } };
        private DeployableAntennaStyles _style;

        public void Initialize(Transform parent, bool customAntenna, WhipAntennaData data)
        {
            this.parent = parent;
            custom = customAntenna;
            opened = data.startOpen;
            _style = (DeployableAntennaStyles)Enum.Parse(typeof(DeployableAntennaStyles), data.antennaStyle);

            if (!custom)
            {
                _segments = new GameObject[_segmentCounts[_style]];
                _segments[0] = UnityEngine.Object.Instantiate(Mod.Instance.ResourceLoader.LoadAsset<GameObject>("Assets/Content/Craft/Parts/WhipAntenna/Prefabs/" + _stylePrefabNames[_style] + ".prefab"));
                _segments[0].transform.SetParent(parent, worldPositionStays: false);

                for (int i = 1; i < _segmentCounts[_style]; i++)
                {
                    _segments[i] = _segments[i - 1].transform.Find("Segment" + i).gameObject;
                }
                SetPosition(data.startOpen);
            }
        }

        public void Update(float size)
        {
            if (!custom) _segments[0].transform.localScale = new Vector3(size, size, size);
        }

        public void SetPosition(bool startOpen)
        {
            if (!custom)
                for (int i = 1; i < _segmentCounts[_style]; i++)
                {
                    _segments[i].transform.localPosition = new Vector3(0, 0, startOpen ? 0 : _segmentsOffsets[_style][i]);
                }
            opened = startOpen;
        }

        public IEnumerator Toggle(bool open, float animationDuration, WhipAntennaData data)
        {
            double currentAnimTime = 0;
            int direction = open ? -1 : 1;
            float[] speed = new float[_segmentCounts[_style]];
            animating = true;

            for (int i = 1; i < _segmentCounts[_style]; i++)
            {
                speed[i] = _segmentsOffsets[_style][i] / animationDuration * direction;
            }

            while (currentAnimTime <= animationDuration)
            {
                if (!custom)
                    for (int i = 1; i < _segmentCounts[_style]; i++)
                    {
                        Vector3 pos = _segments[i].transform.localPosition;
                        pos.z += speed[i] * Time.deltaTime;
                        _segments[i].transform.localPosition = pos;
                    }

                currentAnimTime += Game.Instance.SceneManager.InFlightScene ? Game.Instance.FlightScene.TimeManager.DeltaTime : Time.deltaTime;
                yield return 0;
            }

            opened = data.startOpen = open;
            SetPosition(data.startOpen);
            animating = false;
        }

        public void Destroy()
        {
            if (_segments != null)
            {
                GameObject.Destroy(_segments[0]);
            }
        }
    }
}