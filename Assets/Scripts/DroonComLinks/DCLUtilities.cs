using System;
using System.Collections;
using Assets.Scripts.DroonComLinks.Network;
using Assets.Scripts.Flight.Sim;
using ModApi.Design.PartProperties;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks
{
    public static class DCLUtilities
    {
        public const string errorColor = "<color=#b33e46>";

        public static void ShowMapViewMessage(string message, bool error = false, float size = 1f, bool bold = false)
        {
            if (Game.Instance.FlightScene == null)
            {
                Debug.LogError("Tried to show message when flight view not active: " + message);
                return;
            }

            Game.Instance.FlightScene.FlightSceneUI.ShowMessage((error ? errorColor : "") + (bold ? "<b>" : "") + "<size=" + size * 100 + "%>" + message);
        }

        public static float Map(this float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public static float RoundN2(float val) => Mathf.Round(val * 100f) / 100f;
        public static float Round(float val, int precision)
        {
            float p = Mathf.Pow(10f, precision);
            return Mathf.Round(val * p) / p;
        }

        public static string YesNo(bool value) => value ? "yes" : "No";

        public static string FormatFrequency(float f) => f.ToString("n2") + "GHz";
        public static string FormatPower(double P)
        {
            if (P < 0.000000000000000001) return (P * 1000000000000000000).ToString("n2") + "10E-18W";
            if (P < 0.000000000000001) return (P * 1000000000000000).ToString("n2") + "10E-15W";
            if (P < 0.000000000001) return (P * 1000000000000).ToString("n2") + "10E-12W";
            if (P < 0.000000001) return (P * 1000000000).ToString("n2") + "10E-9W";
            if (P < 0.000001) return (P * 1000000).ToString("n2") + "10E-6W";
            if (P < 0.001) return (P * 1000).ToString("n2") + "mW";
            if (P > 1000000000) return (P / 1000000000).ToString("n2") + "GW";
            if (P > 1000000) return (P / 1000000).ToString("n2") + "MW";
            if (P > 1000) return (P / 1000).ToString("n2") + "kW";
            return P.ToString("n2") + "W";
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

        public static bool ConnectionObstructed(Vector3d A, Vector3d B)
        {
            Vector3d center = (A + B) / 2;
            foreach (PlanetNode planet in ComLinksManager.Instance.planets)
            {
                double planetMagnitude = planet.SolarPosition.magnitude;
                if (!((A.magnitude > planetMagnitude * 1.1 && B.magnitude > planetMagnitude * 1.1) || (A.magnitude < planetMagnitude * 0.9 && B.magnitude < planetMagnitude * 0.9)))
                {
                    if (LineSphereIntersect(A, B, planet.SolarPosition, planet.PlanetData.Radius)) return true;
                    if ((center - planet.SolarPosition).magnitude < planet.PlanetData.Radius) return true;
                }
            }
            return false;
        }

        private static bool LineSphereIntersect(Vector3d A, Vector3d B, Vector3d C, double r)
        {
            r *= Mod.Instance.radiusScale;

            double u = ((C.x - A.x) * (B.x - A.x) + (C.y - A.y) * (B.y - A.y) + (C.z - A.z) * (B.z - A.z)) / ((B.x - A.x) * (B.x - A.x) + (B.y - A.y) * (B.y - A.y) + (B.z - A.z) * (B.z - A.z));
            double d = (C - A + (A - B) * u).magnitude;
            if (u > 0 && u < 1 && d < r) return true;
            return false;
        }

        public static float Pythagore(float a, float b) => Mathf.Sqrt(a * a + b * b);
        public static float SphereVolume(float r) => (4 * Mathf.PI * r * r * r) / 3;
        public static float CircleSurface(float r) => Mathf.PI * r * r;
    }
}