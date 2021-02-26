    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using System;
    using Assets.Scripts.DroonComLinks.Objects;
    using Assets.Scripts.Flight.Sim;
    using Assets.Scripts.Flight;
    using Assets.Scripts.Input;
    using Assets.Scripts.State;
    using ModApi.Common;
    using ModApi.Craft.Parts;
    using ModApi.Craft;
    using ModApi.Flight.UI;
    using ModApi.Input;
    using UnityEngine;

    namespace Assets.Scripts.DroonComLinks {
        public class DelayManager {
            public float Delay => _Delay;
            private float timeStep => Time.unscaledDeltaTime;
            private float _Delay => Mod.Instance.antennaDelay * 10;
            private float _Yaw = 0, _Pitch = 0, _Roll = 0, _OffsetYaw = 0, _OffsetPitch = 0, _OffsetRoll = 0, _TranslateUp = 0, _TranslateRight = 0, _TranslateForward = 0, _Throttle = 0, _Slider1 = 0, _Slider2 = 0, _OffsetSlider1 = 0, _OffsetSlider2 = 0, _OffsetBrake = 0, _Brake = 0;
            private float AnalogYaw => _flightScene.FlightControls.AnalogYaw;
            private float AnalogPitch => _flightScene.FlightControls.AnalogPitch;
            private float AnalogRoll => _flightScene.FlightControls.AnalogRoll;
            private float AnalogThrottle => _flightScene.FlightControls.AnalogThrottle;
            private float AnalogBrake => _flightScene.FlightControls.AnalogBrake;
            private float _throttleIncrement;
            private FlightState _flightState => _flightScene.FlightState;
            private FlightSceneScript _flightScene => FlightSceneScript.Instance;
            private CraftControls Controls => _flightScene.CraftNode.Controls;
            private INavSphere _navSphere => _flightScene.FlightControls.NavSphere;
            private InputSender _InputSender;
            private List<DCLInputs> IncomingInputs = new List<DCLInputs> ();
            private List<IGameInput> Inputs = new List<IGameInput> ();
            private List<OldControls> ControlsBuffer = new List<OldControls> ();
            private CraftControls lastConrtols;
            private bool inputsEnabled = true;
            private bool ignoreNextControlsChange = false;

            public void Initialise (InputSender inputSender) {
                GameInputs g = (GameInputs) Game.Instance.Inputs;
                Inputs = new List<IGameInput> { g.FullThrottle, g.LockHeading, g.LockPrograde, g.LockRetrograde, g.LockTarget, g.Pitch, g.Roll, g.Brake, g.KillThrottle, g.LockHeading, g.LockPrograde, g.LockRetrograde, g.LockTarget, g.Throttle, g.Slider1, g.Slider2, g.Throttle, g.Yaw, g.ActivationGroup1, g.ActivationGroup2, g.ActivationGroup3, g.ActivationGroup4, g.ActivationGroup5, g.ActivationGroup6, g.ActivationGroup7, g.ActivationGroup8, g.ActivationGroup9, g.ActivationGroup10 };
                _InputSender = inputSender;
                _flightScene.CraftNode.CraftScript.ActiveCommandPod.ControlsChanged += OnControlChanged;
                lastConrtols = new CraftControls (null, null);
            }

            public void Update (bool DroonControl, bool RemoteControl) {
                if (Mod.Instance.antennaDelay > 0) {
                    if (!DroonControl && !RemoteControl) {
                        SendControls (new CraftControls (null, null));
                    } else if (Mod.Instance.antennaDelay > 0) {
                        ControlsBuffer.Add (new OldControls (_flightScene.CraftNode.CraftScript.ActiveCommandPod.Controls, Time.realtimeSinceStartup));
                        SendNextControls ();
                    } else lastConrtols = new CraftControls (null, null);
                }
            }

            public void OnControlChanged (ICommandPod source, bool adjustControlsToCom) {
                Debug.Log ("Controls Changed");
                // if (Mod.Instance.antennaDelay > 0) {
                //     if (!ignoreNextControlsChange) {
                //         ControlsBuffer.Add (new OldControls (source.Controls, Time.realtimeSinceStartup));
                //         SendNextControls ();
                //     } else ignoreNextControlsChange = false;
                // } else { ControlsBuffer.Clear (); ignoreNextControlsChange = false; }
            }

            private void SendNextControls () {
                if (ControlsBuffer.Count > 0 && ControlsBuffer.First ().timeStamp + Mod.Instance.antennaDelay > Time.realtimeSinceStartup) {
                    lastConrtols = ControlsBuffer.First ().controls;
                    SendControls (ControlsBuffer.First ().controls);
                    ControlsBuffer.Remove (ControlsBuffer.First ());
                    //Debug.Log ("Delay :" + Mod.Instance.antennaDelay);
                } else SendControls (lastConrtols);
            }

            private void SendControls (CraftControls controls) {
                Controls.OffsetBrake = controls.OffsetBrake;
                Controls.Brake = controls.Brake;
                Controls.OffsetYaw = controls.OffsetYaw;
                Controls.Yaw = controls.Yaw;
                Controls.OffsetPitch = controls.OffsetPitch;
                Controls.Pitch = controls.Pitch;
                Controls.OffsetRoll = controls.OffsetRoll;
                Controls.Roll = controls.Roll;
                Controls.OffsetSlider1 = controls.OffsetSlider1;
                Controls.Slider1 = controls.Slider1;
                Controls.OffsetSlider2 = controls.OffsetSlider2;
                Controls.Slider2 = controls.Slider2;
                Controls.OffsetTranslateForward = controls.OffsetTranslateForward;
                Controls.TranslateForward = controls.TranslateForward;
                Controls.OffsetTranslateRight = controls.OffsetTranslateRight;
                Controls.TranslateRight = controls.TranslateRight;
                Controls.OffsetTranslateUp = controls.OffsetTranslateUp;
                Controls.TranslateUp = controls.TranslateUp;
                Controls.Throttle = controls.Throttle;
                Controls.YawInputReceived = controls.YawInputReceived;
                Controls.PitchInputReceived = controls.PitchInputReceived;
                Controls.RollInputReceived = controls.RollInputReceived;
                //Debug.Log ("Controls Sent at " + Time.realtimeSinceStartup);
                ignoreNextControlsChange = true;
            }

            public void SetInputs (bool active) {
                if (inputsEnabled != active) {
                    foreach (GameInput input in Inputs) {
                        input.Enabled = active;
                    }
                    inputsEnabled = active;
                    Debug.Log ("Inputs: " + active);
                }
            }

            private float CalculateDelay () {
                return _Delay;
            }

            private void UpdateUIInputs () {

            }

            private void ProcessIncomingInput (DCLInputs IncomingInput) {
                Debug.Log ("Input sent at " + _flightState.Time + " after " + _Delay + "s (received at " + IncomingInput.Time + ")");
                for (int i = 0; i < IncomingInput.Types.Count; i++) {
                    _InputSender.UpdateInput (IncomingInput.Types[i], IncomingInput.Values[i]);
                    Debug.Log ("   - input " + IncomingInput.Types[i] + " of value " + IncomingInput.Values[i]);
                }
                Debug.Log ("--- Input End ---");
            }

            private void UpdateKeyboardInputs () {

                foreach (DCLInputs IncomingInput in IncomingInputs.ToList ()) {
                    if ((IncomingInput.Time + _Delay) >= _flightState.Time) {
                        ProcessIncomingInput (IncomingInput);
                        IncomingInputs.Remove (IncomingInput);
                    }
                }

                IncomingInputs.Add (new DCLInputs (_flightState.Time));

                IGameInputs inputs = Game.Instance.Inputs;
                _throttleIncrement = Mathf.Clamp (inputs.Throttle.GetAxis (), -1f, 1f);
                float? controlInput = GetControlInput (inputs.Roll);
                float? controlInput2 = GetControlInput (inputs.Pitch);
                float? controlInput3 = GetControlInput (inputs.Yaw);
                float? controlInput4 = GetControlInput (inputs.Brake);
                Controls.RollInputReceived = false;
                if (!Controls.TranslationModeEnabled) {
                    if (controlInput.HasValue && (!_navSphere.HeadingLocked || controlInput.Value + AnalogRoll != 0f)) {
                        //Controls.Roll = Mathf.Clamp (controlInput.Value + AnalogRoll + Controls.OffsetRoll, -1f, 1f);
                        IncomingInputs.Last ().AddInput ("Roll", Mathf.Clamp (controlInput.Value + AnalogRoll + Controls.OffsetRoll, -1f, 1f));
                        Controls.RollInputReceived = true;
                    }
                    if (controlInput2.HasValue) {
                        if (_navSphere.HeadingLocked) {
                            _navSphere.LockHeading (_navSphere.Pitch + timeStep * (controlInput2.Value + AnalogPitch) * 90f, _navSphere.Heading);
                        } else {
                            //Controls.Pitch = Mathf.Clamp (controlInput2.Value + AnalogPitch + Controls.OffsetPitch, -1f, 1f);
                            IncomingInputs.Last ().AddInput ("Pitch", Mathf.Clamp (controlInput2.Value + AnalogPitch + Controls.OffsetPitch, -1f, 1f));
                        }
                    }
                    if (controlInput3.HasValue) {
                        if (_navSphere.HeadingLocked) {
                            _navSphere.LockHeading (_navSphere.Pitch, _navSphere.Heading + timeStep * (controlInput3.Value + AnalogYaw) * 90f);
                        } else {
                            //Controls.Yaw = Mathf.Clamp (controlInput3.Value + AnalogYaw + Controls.OffsetYaw, -1f, 1f);
                            IncomingInputs.Last ().AddInput ("Yaw", Mathf.Clamp (controlInput3.Value + AnalogYaw + Controls.OffsetYaw, -1f, 1f));
                        }
                    }
                } else {
                    if (!_navSphere.HeadingLocked) {
                        //Controls.Roll = 0f;
                        //Controls.Pitch = 0f;
                        //Controls.Yaw = 0f;
                        IncomingInputs.Last ().AddInput ("Yaw", 0f);
                        IncomingInputs.Last ().AddInput ("Pitch", 0f);
                        IncomingInputs.Last ().AddInput ("Roll", 0f);
                    }
                    if (controlInput.HasValue) {
                        //Controls.TranslateUp = Mathf.Clamp (controlInput.Value + AnalogPitch + Controls.OffsetRoll, -1f, 1f);
                        IncomingInputs.Last ().AddInput ("TranslateUp", Mathf.Clamp (controlInput.Value + AnalogPitch + Controls.OffsetRoll, -1f, 1f));
                    }
                    if (controlInput2.HasValue) {
                        //Controls.TranslateForward = Mathf.Clamp (controlInput2.Value + AnalogThrottle + Controls.OffsetPitch, -1f, 1f);
                        IncomingInputs.Last ().AddInput ("TranslateForward", Mathf.Clamp (controlInput2.Value + AnalogThrottle + Controls.OffsetPitch, -1f, 1f));
                    }
                    if (controlInput3.HasValue) {
                        //Controls.TranslateRight = Mathf.Clamp (controlInput3.Value + AnalogRoll + Controls.OffsetYaw, -1f, 1f);
                        IncomingInputs.Last ().AddInput ("TranslateRight", Mathf.Clamp (controlInput3.Value + AnalogRoll + Controls.OffsetYaw, -1f, 1f));
                    }
                }
                if (controlInput4.HasValue) {
                    //Controls.Brake = Mathf.Clamp (controlInput4.Value + AnalogBrake + Controls.OffsetBrake, -1f, 1f);
                    IncomingInputs.Last ().AddInput ("Brake", Mathf.Clamp (controlInput4.Value + AnalogBrake + Controls.OffsetBrake, -1f, 1f));
                }
                //if (inputs.Throttle.Enabled) {
                //if (flag) {
                //Controls.Throttle = inputs.Throttle.GetAxis ();
                //} else {
                //Controls.Throttle += timeStep * (_throttleIncrement + (Controls.TranslationModeEnabled ? 0f : AnalogThrottle));
                IncomingInputs.Last ().AddInput ("Throttle", Mathf.Clamp01 (Controls.Throttle + timeStep * (_throttleIncrement + (Controls.TranslationModeEnabled ? 0f : AnalogThrottle))));
                //}
                //Controls.Throttle = Mathf.Clamp01 (Controls.Throttle);
                //}
                if (inputs.KillThrottle.GetButtonDown ()) {
                    //Controls.Throttle = 0f;
                    IncomingInputs.Last ().AddInput ("Throttle", 0f);
                } else if (inputs.FullThrottle.GetButtonDown ()) {
                    //Controls.Throttle = 1f;
                    IncomingInputs.Last ().AddInput ("Throttle", 1f);
                }
                //Controls.Slider1 = Mathf.Clamp (inputs.Slider1.GetAxis () + Controls.OffsetSlider1, -1f, 1f);
                //Controls.Slider2 = Mathf.Clamp (inputs.Slider2.GetAxis () + Controls.OffsetSlider2, -1f, 1f);
                IncomingInputs.Last ().AddInput ("Slider1", Mathf.Clamp (inputs.Slider1.GetAxis () + Controls.OffsetSlider1, -1f, 1f));
                IncomingInputs.Last ().AddInput ("Slider2", Mathf.Clamp (inputs.Slider2.GetAxis () + Controls.OffsetSlider2, -1f, 1f));

                if (!_flightScene.TimeManager.Paused && inputs.ActivateStage.GetButtonDown ()) {
                    IncomingInputs.Last ().AddInput ("Stage", 0f);
                }
                if (inputs.ActivationGroup1.GetButtonDown ()) {
                    //Controls.ToggleActivationGroup (1);
                    IncomingInputs.Last ().AddInput ("AG1", 0f);
                }
                if (inputs.ActivationGroup2.GetButtonDown ()) {
                    //Controls.ToggleActivationGroup (2);
                    IncomingInputs.Last ().AddInput ("AG2", 0f);
                }
                if (inputs.ActivationGroup3.GetButtonDown ()) {
                    //Controls.ToggleActivationGroup (3);
                    IncomingInputs.Last ().AddInput ("AG3", 0f);
                }
                if (inputs.ActivationGroup4.GetButtonDown ()) {
                    //Controls.ToggleActivationGroup (4);
                    IncomingInputs.Last ().AddInput ("AG4", 0f);
                }
                if (inputs.ActivationGroup5.GetButtonDown ()) {
                    //Controls.ToggleActivationGroup (5);
                    IncomingInputs.Last ().AddInput ("AG5", 0f);
                }
                if (inputs.ActivationGroup6.GetButtonDown ()) {
                    //Controls.ToggleActivationGroup (6);
                    IncomingInputs.Last ().AddInput ("AG6", 0f);
                }
                if (inputs.ActivationGroup7.GetButtonDown ()) {
                    //Controls.ToggleActivationGroup (7);
                    IncomingInputs.Last ().AddInput ("AG7", 0f);
                }
                if (inputs.ActivationGroup8.GetButtonDown ()) {
                    //Controls.ToggleActivationGroup (8);
                    IncomingInputs.Last ().AddInput ("AG8", 0f);
                }
                if (inputs.ActivationGroup9.GetButtonDown ()) {
                    //Controls.ToggleActivationGroup (9);
                    IncomingInputs.Last ().AddInput ("AG9", 0f);
                }
                if (inputs.ActivationGroup10.GetButtonDown ()) {
                    //Controls.ToggleActivationGroup (10);
                    IncomingInputs.Last ().AddInput ("AG10", 0f);
                }
                if (inputs.LockHeading.GetButtonDown ()) {
                    if (!_navSphere.HeadingLocked) {
                        //_navSphere.LockCurrentHeading ();
                        IncomingInputs.Last ().AddInput ("NavL", 0f);
                    } else {
                        //_navSphere.UnlockHeading ();
                        IncomingInputs.Last ().AddInput ("NavU", 0f);
                    }
                }
                if (inputs.LockPrograde.GetButtonDown ()) {
                    _navSphere.ToggleProgradeLock ();
                    IncomingInputs.Last ().AddInput ("NavP", 0f);
                }
                if (inputs.LockRetrograde.GetButtonDown ()) {
                    _navSphere.ToggleRetrogradeLock ();
                    IncomingInputs.Last ().AddInput ("NavR", 0f);
                }
                if (inputs.LockTarget.GetButtonDown ()) {
                    _navSphere.ToggleTargetLock ();
                    IncomingInputs.Last ().AddInput ("NavT", 0f);
                }
            }
            private float? GetControlInput (IGameInput input) {
                return Mathf.Clamp (input.GetAxis (), -1f, 1f);
            }

            private class OldControls {
                public CraftControls controls;
                public double timeStamp;

                public OldControls (CraftControls _controls, double _timeStamp) {
                    controls = _controls;
                    timeStamp = _timeStamp;
                    Debug.Log ("created Controls at " + Time.realtimeSinceStartup);
                    controls = new CraftControls (Game.Instance.FlightScene.CraftNode.CraftScript.ActiveCommandPod, _controls.GenerateStateXml ());

                    // controls.OffsetBrake = _controls.OffsetBrake;
                    // controls.Brake = _controls.Brake;
                    // controls.OffsetYaw = _controls.OffsetYaw;
                    // controls.Yaw = _controls.Yaw;
                    // controls.OffsetPitch = _controls.OffsetPitch;
                    // controls.Pitch = _controls.Pitch;
                    // controls.OffsetRoll = _controls.OffsetRoll;
                    // controls.Roll = _controls.Roll;
                    // controls.OffsetSlider1 = _controls.OffsetSlider1;
                    // controls.Slider1 = _controls.Slider1;
                    // controls.OffsetSlider2 = _controls.OffsetSlider2;
                    // controls.Slider2 = _controls.Slider2;
                    // controls.OffsetTranslateForward = _controls.OffsetTranslateForward;
                    // controls.TranslateForward = _controls.TranslateForward;
                    // controls.OffsetTranslateRight = _controls.OffsetTranslateRight;
                    // controls.TranslateRight = _controls.TranslateRight;
                    // controls.OffsetTranslateUp = _controls.OffsetTranslateUp;
                    // controls.TranslateUp = _controls.TranslateUp;
                    // controls.Throttle = _controls.Throttle;
                    // controls.YawInputReceived = _controls.YawInputReceived;
                    // controls.PitchInputReceived = _controls.PitchInputReceived;
                    // controls.RollInputReceived = _controls.RollInputReceived;
                }
            }
        }
    }