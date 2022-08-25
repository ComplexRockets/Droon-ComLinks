using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Flight;
using Assets.Scripts.State;
using ModApi.Craft;
using UnityEngine;

namespace Assets.Scripts.DroonComLinks
{
    public class DelayManager
    {
        public float delay => Mod.Instance.antennaDelayFactor;
        public double time => Game.Instance.FlightScene.FlightState.Time;
        public FlightState flightState => flightScene.FlightState;
        public FlightSceneScript flightScene => FlightSceneScript.Instance;
        public CraftControls controls => flightScene.CraftNode.Controls;
        private Queue inputBuffer = new Queue();

        private class Input<T>
        {
            public Func<T> input;
            public object[] parameters;
            public double timeStamp;

            public Input(Func<T> input, object[] parameters, double timeStamp)
            {

            }
        }
    }
}