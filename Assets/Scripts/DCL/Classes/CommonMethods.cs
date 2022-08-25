using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Flight;
using Assets.Scripts.Flight.MapView.Interfaces;
using Assets.Scripts.Flight.Sim;
using Assets.Scripts.State;

namespace Assets.Scripts.DroonComLinks
{
    public static class CommonMethods
    {
        public static float map(this float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
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
    }
}