using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Flight;
using Assets.Scripts.Flight.MapView.Interfaces;
using Assets.Scripts.Flight.Sim;
using Assets.Scripts.State;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks {
    public static class CommonMethods {
        public static IMapViewCoordinateConverter _coordinateConverter => (IMapViewCoordinateConverter) Game.Instance.FlightScene.ViewManager.MapViewManager.MapView;
        public static List<PlanetNode> planets = new List<PlanetNode> ();
        private static FlightState _flightState => FlightSceneScript.Instance.FlightState;

        public static float map (this float value, float from1, float to1, float from2, float to2) {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public static bool ConectionObstructed (Vector3d A, Vector3d B) {
            foreach (PlanetNode planet in planets) {
                if (LineSphereIntersect (A, B, planet.SolarPosition, planet.PlanetData.Radius)) return true;
            }
            return false;
        }

        private static bool LineSphereIntersect (Vector3d A, Vector3d B, Vector3d C, double r) {
            r *= Mod.Instance.radiusScale;
            double a = (B - A).sqrMagnitude;
            double b = 2 * ((B.x - A.x) * (A.x - C.x) + (B.y - A.y) * (A.y - C.y) + (B.z - A.z) * (A.z - C.z));
            double c = C.sqrMagnitude + A.sqrMagnitude - 2 * Vector3d.Dot (C, A) - r * r;

            double d = b * b - 4 * a * c;
            //if (d > 0 && Math.Cos (Vector3d.Angle (C - A, C - B) * Math.PI / 180) < 0) Debug.Log ("Planet Obstructing: " + planet + " (Center: " + C + " Radius: " + r + ") Line from " + A + " to " + B + " intersect: " + b + "^2 - 4*" + a + "*" + c + " = " + d);

            if (d >= 0) return true;
            return false;
        }

        public static void InitialisePlanets () {
            planets = new List<PlanetNode> ();
            planets.Add ((PlanetNode) _flightState.RootNode);
            GetPlanetsChildren ((PlanetNode) _flightState.RootNode);
        }
        private static void GetPlanetsChildren (PlanetNode parent) {
            foreach (PlanetNode child in parent.ChildPlanets) {
                planets.Add (child);
                if (child.ChildPlanets.Count > 0) GetPlanetsChildren (child);
            }
        }

        public static IEnumerator CallNextFrame (Action action) {
            yield return 0;
            action.Invoke ();
        }

        public static class MathHelper {
            /// <summary>
            /// Clamping a value to be sure it lies between two values
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="aValue"></param>
            /// <param name="aMax"></param>
            /// <param name="aMin"></param>
            /// <returns></returns>
            public static T Clamp<T> (T aValue, T aMin, T aMax) where T : IComparable<T> {
                var _Result = aValue;
                if (aValue.CompareTo (aMax) > 0)
                    _Result = aMax;
                else if (aValue.CompareTo (aMin) < 0)
                    _Result = aMin;
                return _Result;
            }

        }
    }
}