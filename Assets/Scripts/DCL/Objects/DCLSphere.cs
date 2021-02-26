    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using System;
    using Assets.Scripts.Flight.MapView.Interfaces;
    using Assets.Scripts.Flight.MapView;
    using Assets.Scripts.Flight.Sim;
    using Assets.Scripts.Flight;
    using Assets.Scripts.Scenes;
    using Assets.Scripts.State;
    using Assets.Scripts.Ui.Flight;
    using ModApi.Common;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using ModApi.GameLoop;
    using ModApi.Scenes.Events;
    using UnityEngine;

    namespace Assets.Scripts.DroonComLinks.Objects {
        public class DCLSphere : MonoBehaviour {
            private Material DefaultMaterial;
            private String _name;
            private Vector3 _Center;
            private float _radius;
            private GameObject _gameObject;
            public MapViewScript MapView => Game.Instance.FlightScene.ViewManager.MapViewManager.MapView as MapViewScript;
            private Shader _sphereShader;
            public String Name {
                get => _name;
            }

            public DCLSphere (String name, Vector3 C, float r) {
                _name = name;
                _Center = C;
                _radius = r;

                _sphereShader = Shader.Find ("Standard");
                _gameObject = GameObject.CreatePrimitive (PrimitiveType.Sphere);
                float scale = (_radius / _gameObject.GetComponent<SphereCollider> ().radius);
                _gameObject.transform.localScale = new Vector3 (scale, scale, scale);
                _gameObject.layer = 10;

                Update ();
            }

            public void UpdateSphere (Vector3 C) {
                _Center = C;

                Update ();
            }

            public void UpdateSphere (Vector3 C, float radius) {
                _Center = C;
                _radius = radius;

                Update ();
            }

            private void Update () {
                _gameObject.transform.position = _Center;
                float scale = (_radius / _gameObject.GetComponent<SphereCollider> ().radius);
                _gameObject.transform.localScale = new Vector3 (scale, scale, scale);
            }
            public void DestroySphere () {
                if (_gameObject != null) {
                    Destroy (_gameObject);
                    _gameObject = null;
                }
            }
        }
    }