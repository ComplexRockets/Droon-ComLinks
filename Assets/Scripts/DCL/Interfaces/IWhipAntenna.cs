using System.Collections;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using ModApi.Craft.Parts;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Interfaces
{
    public interface IWhipAntenna
    {
        WhipAntennaTypes type { get; }
        Transform parent { get; set; }
        bool custom { get; set; }
        bool animating { get; set; }
        bool opened { get; set; }
        void Initialize(Transform parent, bool customAntenna, WhipAntennaData data);
        void Update(WhipAntennaData data);
        void SetPosition(bool startOpen);
        IEnumerator Toggle(bool open, float animationDuration, WhipAntennaData data);
        void Destroy();
    }
}