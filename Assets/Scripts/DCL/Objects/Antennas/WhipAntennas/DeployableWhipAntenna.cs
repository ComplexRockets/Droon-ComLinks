using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.DroonComLinks.Interfaces;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Objects.Antennas.WhipAntennas
{
    public class DeployableWhipAntenna : IWhipAntenna
    {
        public WhipAntennaTypes type => WhipAntennaTypes.Deployable;
        public Transform parent { get; set; }
        public bool animating { get; set; } = false;
        public bool opened { get; set; }
        public bool custom { get; set; }
        private GameObject[] _segments;
        private readonly Dictionary<WhipAntennaStyles, float[]> _segmentsOffsets = new Dictionary<WhipAntennaStyles, float[]>() {
            { WhipAntennaStyles.Simple, new float[] { 0, -0.78f, -0.72f, -0.59f, -0.53f, -0.45f, -0.4f } },
            { WhipAntennaStyles.Thin, new float[] { 0, -1.33f, -1.33f, -1.3f }} };
        private readonly Dictionary<WhipAntennaStyles, int> _segmentCounts = new Dictionary<WhipAntennaStyles, int>() {
            { WhipAntennaStyles.Simple, 7 },
            { WhipAntennaStyles.Thin, 4 } };
        private readonly Dictionary<WhipAntennaStyles, string> _stylePrefabNames = new Dictionary<WhipAntennaStyles, string>() {
            { WhipAntennaStyles.Simple, "DeployableWhipAntenna1" },
            { WhipAntennaStyles.Thin, "DeployableWhipAntenna3" } };
        private WhipAntennaStyles _style;

        public void Initialize(Transform parent, bool customAntenna, WhipAntennaData data)
        {
            this.parent = parent;
            custom = customAntenna;
            opened = data.startOpen;
            _style = data.antennaStyle;

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

        public void Update(WhipAntennaData data)
        {
            if (!custom) _segments[0].transform.localScale = new Vector3(data.size, data.size, data.size);
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
                UnityEngine.Object.DestroyImmediate(_segments[0]);
            }
        }
    }
}