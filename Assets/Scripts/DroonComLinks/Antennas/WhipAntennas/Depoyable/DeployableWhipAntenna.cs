using System.Collections;
using Assets.Scripts.Craft.Parts.Modifiers;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Antennas.WhipAntennas.Deployable
{
    public abstract class DeployableWhipAntenna : MonoBehaviour, IWhipAntenna
    {
        public WhipAntennaTypes type => WhipAntennaTypes.Deployable;
        public Transform parent { get; set; }
        public bool animating { get; set; } = false;
        public bool opened { get; set; }
        public bool custom { get; set; }
        private GameObject[] _segments;
        public abstract float[] segmentsOffset { get; set; }
        public abstract int segmentCount { get; set; }
        public abstract string stylePrefabName { get; set; }
        public abstract float mass { get; set; }
        private WhipAntennaStyles _style;

        public static DeployableWhipAntenna Create(WhipAntennaStyles style)
        {
            switch (style)
            {
                case WhipAntennaStyles.Simple:
                    return new SimpleDeployableWhipAntenna();
                case WhipAntennaStyles.Thin:
                    return new ThinDeployableWhipAntenna();
                default:
                    Debug.LogError("Unsupported deployable whip antenna style: " + style.ToString());
                    return null;
            }
        }

        public void Initialize(Transform parent, bool customAntenna, WhipAntennaData data)
        {
            this.parent = parent;
            custom = customAntenna;
            opened = data.startOpen;
            _style = data.antennaStyle;

            if (!custom)
            {
                _segments = new GameObject[segmentCount];
                _segments[0] = UnityEngine.Object.Instantiate(Mod.Instance.ResourceLoader.LoadAsset<GameObject>("Assets/Content/Craft/Parts/WhipAntenna/Prefabs/" + stylePrefabName + ".prefab"));
                _segments[0].transform.SetParent(parent, worldPositionStays: false);

                for (int i = 1; i < segmentCount; i++)
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
                for (int i = 1; i < segmentCount; i++)
                {
                    _segments[i].transform.localPosition = new Vector3(0, 0, startOpen ? 0 : segmentsOffset[i]);
                }
            opened = startOpen;
        }

        public IEnumerator Toggle(bool open, float animationDuration, WhipAntennaData data)
        {
            animating = true;
            double currentAnimTime = 0;
            int direction = open ? -1 : 1;
            float[] speed = new float[segmentCount];

            for (int i = 1; i < segmentCount; i++)
            {
                speed[i] = segmentsOffset[i] / animationDuration * direction;
            }

            while (currentAnimTime <= animationDuration)
            {
                if (!custom)
                    for (int i = 1; i < segmentCount; i++)
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