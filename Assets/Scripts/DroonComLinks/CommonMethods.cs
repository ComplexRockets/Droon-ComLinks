using System;
using System.Collections;
using ModApi.Design.PartProperties;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks
{
    public static class DCLUtilities
    {
        public static float map(this float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public static float RoundN2(float val) => Mathf.Round(val * 100f) / 100f;
        public static float Round(float val, int precision)
        {
            float p = Mathf.Pow(10f, precision);
            return Mathf.Round(val * p) / p;
        }

        public static string YesNo(bool value)
        {
            if (value) return "Yes";
            return "No";
        }

        public static IEnumerator CallNextFrame(Action action)
        {
            yield return 0;
            action.Invoke();
        }

        public static void SetDesignerSliderMinMax(ISliderProperty property, ref float value, float min, float max, int step = 1000)
        {
            property.UpdateSliderSettings(min, max, step);
            if (value < min) value = min;
            else if (value > max) value = max;
        }

        public static float Pythagore(float a, float b) => Mathf.Sqrt(a * a + b * b);
        public static float SphereVolume(float r) => (4 * Mathf.PI * r * r * r) / 3;
        public static float CircleSurface(float r) => Mathf.PI * r * r;
    }
}