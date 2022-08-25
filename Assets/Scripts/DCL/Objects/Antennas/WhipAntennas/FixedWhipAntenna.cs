using System.Collections;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.DroonComLinks.Interfaces;
using ModApi.Craft.Parts;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks.Objects.Antennas.WhipAntennas
{
    public class FixedWhipAntenna : IWhipAntenna
    {
        public WhipAntennaTypes type => throw new System.NotImplementedException();

        public bool custom { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool animating { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public bool opened { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public Transform parent { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public void Destroy()
        {
            throw new System.NotImplementedException();
        }

        public void Initialize(bool customAntenna, bool startOpen)
        {
            throw new System.NotImplementedException();
        }

        public void Initialize(Transform parent, bool customAntenna, WhipAntennaData data)
        {
            throw new System.NotImplementedException();
        }

        public void Initialize(Transform parent, PartMaterialScript partMaterialScript, bool customAntenna, bool startOpen)
        {
            throw new System.NotImplementedException();
        }

        public void Initialize(Transform parent, IPartMaterialScript partMaterialScript, bool customAntenna, bool startOpen)
        {
            throw new System.NotImplementedException();
        }

        public void SetPosition(bool startOpen)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerator Toggle(bool open, float animationDuration, WhipAntennaData data)
        {
            throw new System.NotImplementedException();
        }

        public void Update(float size)
        {
            throw new System.NotImplementedException();
        }
    }
}