// using System;
// using System.Linq;
// using Assets.Scripts.Flight;
// using ModApi.Craft;
// using ModApi.Craft.Parts;
// using ModApi.Flight.UI;
// using UnityEngine;

// namespace Assets.Scripts.DroonComLinks
// {
//     public class InputSender {
//         private CraftControls Controls => CommandPod.Controls;
//         private FlightSceneScript _flightScene => FlightSceneScript.Instance;
//         private INavSphere _navSphere => _flightScene.FlightControls.NavSphere;
//         private ICommandPod CommandPod => _flightScene.CraftNode.CraftScript?.ActiveCommandPod;
//         public float _Yaw = 0,
//             _Pitch = 0,
//             _Roll = 0,
//             _OffsetYaw = 0,
//             _OffsetPitch = 0,
//             _OffsetRoll = 0,
//             _TranslateUp = 0,
//             _TranslateRight = 0,
//             _TranslateForward = 0,
//             _Throttle = 0,
//             _Slider1 = 0,
//             _Slider2 = 0,
//             _OffsetSlider1 = 0,
//             _OffsetSlider2 = 0,
//             _OffsetBrake = 0,
//             _Brake = 0;

//         public void UpdateInput (string Type, float Value) {
//             if (Type == "Yaw") _Yaw = Value;
//             else if (Type == "Pitch") _Pitch = Value;
//             else if (Type == "Roll") _Roll = Value;
//             else if (Type == "OffsetYaw") _OffsetYaw = Value;
//             else if (Type == "OffsetPitch") _OffsetPitch = Value;
//             else if (Type == "OffsetRoll") _OffsetRoll = Value;
//             else if (Type == "TranslateUp") _TranslateUp = Value;
//             else if (Type == "TranslateRight") _TranslateRight = Value;
//             else if (Type == "TranslateForward") _TranslateForward = Value;
//             else if (Type == "Throttle") _Throttle = Value;
//             else if (Type == "Slider1") _Slider1 = Value;
//             else if (Type == "Slider2") _Slider2 = Value;
//             else if (Type == "OffsetSlider1") _OffsetSlider1 = Value;
//             else if (Type == "OffsetSlider2") _OffsetSlider2 = Value;
//             else if (Type == "OffsetBrake") _OffsetBrake = Value;
//             else if (Type == "Brake") _Brake = Value;
//             else if (Type == "Stage") ActivateStage ();
//             else if (Type.Contains ("AG")) {
//                 if (Type.Last ().ToString () != "0") Controls.ToggleActivationGroup (Type.Last ());
//                 else Controls.ToggleActivationGroup (10);
//             } else if (Type.Contains ("Nav")) {
//                 if (Type.Last ().ToString () != "L") _navSphere.LockCurrentHeading ();
//                 else if (Type.Last ().ToString () != "U") _navSphere.UnlockHeading ();
//                 else if (Type.Last ().ToString () != "P") _navSphere.ToggleProgradeLock ();
//                 else if (Type.Last ().ToString () != "R") _navSphere.ToggleRetrogradeLock ();
//                 else if (Type.Last ().ToString () != "T") _navSphere.ToggleTargetLock ();
//             } else Debug.LogError ("Input of type " + Type + " Not Recognized, Could Not Send This Input");

//         }
//         public void UpdateInputs () {
//             Controls.Yaw = _Yaw;
//             Controls.Pitch = _Pitch;
//             Controls.Roll = _Roll;
//             Controls.OffsetYaw = _OffsetYaw;
//             Controls.OffsetPitch = _OffsetPitch;
//             Controls.OffsetRoll = _OffsetRoll;
//             Controls.TranslateUp = _TranslateUp;
//             Controls.TranslateRight = _TranslateRight;
//             Controls.TranslateForward = _TranslateForward;
//             Controls.Throttle = _Throttle;
//             Controls.Slider1 = _Slider1;
//             Controls.Slider2 = _Slider2;
//             Controls.OffsetSlider1 = _OffsetSlider1;
//             Controls.OffsetSlider2 = _OffsetSlider2;
//             Controls.OffsetBrake = _OffsetBrake;
//             Controls.Brake = _Brake;
//         }

//         private void ActivateStage () {
//             if (CommandPod != null) {
//                 if (!_flightScene.TimeManager.CurrentMode.WarpMode) {
//                     CommandPod.ActivateStage ();
//                 } else {
//                     _flightScene.FlightSceneUI.ShowMessage ("Cannot activate stage during time warp");
//                 }
//             }
//         }
//     }
// }