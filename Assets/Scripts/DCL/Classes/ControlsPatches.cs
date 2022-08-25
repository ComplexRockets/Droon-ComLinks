using HarmonyLib;
using Assets.Scripts.Flight;
using Assets.Scripts.Flight.UI;
using System;
using Assets.Scripts.Flight.MapView.Orbits.Chain.ManeuverNodes;
using Assets.Scripts.Craft.Parts;
using ModApi.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using ModApi.Ui.Inspector;
using Assets.Scripts.Flight.MapView.UI.Inspector;
using ModApi.Craft;
using Assets.Scripts.Flight.Sim;
using UnityEngine;
using ModApi.Input;
using ModApi.Flight.UI;

namespace Assets.Scripts.DroonComLinks
{
    //This class uses Harmony https://github.com/pardeike/Harmony Copyright (c) 2017 Andreas Pardeike
    public class ControlsPatches
    {
        public void PatchControls()
        {
            Harmony harmony = new Harmony("com.aram.dcl");
            if (ModSettings.Instance.debugMode) Harmony.DEBUG = true;
            harmony.PatchAll();
        }

        public static bool RegisterExternalCommand(string id, bool needsPower = true) => Mod.Instance.comLinksManager.ManageComRequest(id, needsPower);
    }

    [HarmonyPatch(typeof(InputSliderScript), "UpdateHandlePosition")]
    class InputSliderScriptPatch
    {
        static bool Prefix() => ControlsPatches.RegisterExternalCommand("UpdateHandlePosition", needsPower: false);
    }

    [HarmonyPatch(typeof(NavSphereScript), nameof(NavSphereScript.LockHeading), new Type[] { typeof(float), typeof(float) })]
    class NavSphereScriptLockHeadingfPatch
    {
        static bool Prefix() => ControlsPatches.RegisterExternalCommand("LockHeading", needsPower: false);
    }

    [HarmonyPatch(typeof(NavSphereScript), nameof(NavSphereScript.UnlockHeading))]
    class NavSphereScriptUnlockHeadingfPatch
    {
        static bool Prefix() => ControlsPatches.RegisterExternalCommand("UnlockHeading");
    }

    [HarmonyPatch(typeof(ActivationPanelController), "OnActivationButtonClicked")]
    class ActivationPanelControllerfPatch
    {
        static bool Prefix() => ControlsPatches.RegisterExternalCommand("OnActivationButtonClicked");
    }

    [HarmonyPatch(typeof(StagingPanelController), "OnStagingButtonClicked")]
    class StagingPanelControllerPatch
    {
        static bool Prefix() => ControlsPatches.RegisterExternalCommand("OnStagingButtonClicked");
    }

    [HarmonyPatch(typeof(ManeuverNodeScript), "OnAdjustorChangeBegin")]
    class ManeuverNodeScriptOnAdjustorChangeBeginPatch
    {
        static bool Prefix() => ControlsPatches.RegisterExternalCommand("OnAdjustorChangeBegin", needsPower: false);
    }

    [HarmonyPatch(typeof(ManeuverNodeScript), "OnAdjustorChangeEnd")]
    class ManeuverNodeScriptOnAdjustorChangeEndPatch
    {
        static bool Prefix() => ControlsPatches.RegisterExternalCommand("OnAdjustorChangeEnd");
    }

    [HarmonyPatch(typeof(ManeuverNodeScript), "OnAdjustorChanging")]
    class ManeuverNodeScriptOnAdjustorChangingPatch
    {
        static bool Prefix() => ControlsPatches.RegisterExternalCommand("OnAdjustorChanging", needsPower: false);
    }

    [HarmonyPatch(typeof(ManeuverNodeScript), nameof(ManeuverNodeScript.OnDrag))]
    class ManeuverNodeScriptOnDragPatch
    {
        static bool Prefix() => ControlsPatches.RegisterExternalCommand("ManeuverNodeDrag", needsPower: false);
    }

    [HarmonyPatch(typeof(ManeuverNodeScript), nameof(ManeuverNodeScript.OnEndDrag))]
    class ManeuverNodeScriptOnEndDragPatch
    {
        static bool Prefix() => ControlsPatches.RegisterExternalCommand("ManeuverNodeEndDrag");
    }

    [HarmonyPatch(typeof(PartScript), "ToggleActivationStateFromInspector")]
    class PartScriptToggleActivationStateFromInspectorPatch
    {
        static bool Prefix() => ControlsPatches.RegisterExternalCommand("ToggleActivationStateFromInspector");
    }

    [HarmonyPatch(typeof(PartScript), "OnExplodePartClicked")]
    class PartScriptOnExplodePartClickedPatch
    {
        static bool Prefix() => ControlsPatches.RegisterExternalCommand("OnExplodePartClicked");
    }

    [HarmonyPatch(typeof(SelectedModel), "OnAutoBurnClicked")]
    class SelectedModelOnAutoBurnClickedPatch
    {
        static bool Prefix() => ControlsPatches.RegisterExternalCommand("OnAutoBurnClicked");
    }

    [HarmonyPatch(typeof(CraftControls), "ToggleTranslationMode")]
    class ToggleTranslationModePatch
    {
        static bool Prefix() => ControlsPatches.RegisterExternalCommand("ToggleTranslationMode");
    }

    [HarmonyPatch(typeof(FuelTankScript), "GenerateInspectorModel")]
    class FuelTankScriptInspectorModelPatch
    {
        static void Postfix(IGroupModel group, InspectorModel model, bool flightScene, FuelTankScript __instance)
        {
            // Debug.Log("Instepctor Created for part " + __instance.PartScript.Data.Id);
            ManageGroup(group, __instance);
        }

        private static void ManageGroup(IGroupModel group, FuelTankScript instance)
        {
            // Debug.Log("Group " + group.Name);
            foreach (ItemModel item in group.Items)
            {
                // Debug.Log(" - item, type: " + item.GetType());
                if (item is IGroupModel) ManageGroup((IGroupModel)item, instance);
                if (item is IconButtonRowModel)
                {
                    IconButtonRowModel buttonRow = (IconButtonRowModel)item;

                    foreach (IconButtonModel button in buttonRow.Buttons)
                    {
                        // Debug.Log("     - Button " + button.Sprite);
                        bool flag = false;
                        FuelTransferMode fuelTransferMode = FuelTransferMode.None;
                        if (button.Sprite == "Ui/Sprites/Flight/IconFuelTransferDrain")
                        {
                            flag = true;
                            fuelTransferMode = FuelTransferMode.Drain;
                        }
                        if (button.Sprite == "Ui/Sprites/Flight/IconFuelTransferNone")
                        {
                            flag = true;
                            fuelTransferMode = FuelTransferMode.None;
                        }
                        if (button.Sprite == "Ui/Sprites/Flight/IconFuelTransferFill")
                        {
                            flag = true;
                            fuelTransferMode = FuelTransferMode.Fill;
                        }
                        if (flag)
                        {
                            Traverse.Create(button).Field("_action").SetValue((Action<IconButtonModel>)delegate
                            {
                                if (Mod.Instance.comLinksManager.ManageComRequest("FuelTransferMode" + fuelTransferMode.ToString()))
                                    if ((bool)Traverse.Create(instance).Field("_viewTankSet").GetValue())
                                    {
                                        instance.CraftFuelSource.FuelTransferMode = fuelTransferMode;
                                    }
                                    else instance.FuelTransferMode = fuelTransferMode;
                            });
                            // Debug.Log("         - Patched " + button.Sprite);
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(FlightControls), nameof(FlightControls.Update))]
    class FlightControlsPatch
    {
        static bool Prefix(float timeStep, FlightControls __instance, ref CraftNode ____craftNode, ref INavSphere ____navSphere, FlightSceneScript ____flightScene, ref float ____throttleIncrement)
        {
            if (!ControlsPatches.RegisterExternalCommand("FlightControlsUpdate", needsPower: false)) return false;

            if (____craftNode == null || Game.Instance.UserInterface.AnyDialogsOpen || Game.Instance.UserInterface.IsTextInputFocused)
            {
                return false;
            }
            IGameInputs inputs = Game.Instance.Inputs;
            if (inputs.SwapRollYaw.GetButtonDownIfEnabled())
            {
                Traverse.Create(__instance).Property("SwapRollYaw").SetValue(!__instance.SwapRollYaw);
                if (Game.InFlightScene)
                {
                    Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Roll and Yaw inputs swapped.");
                }
            }
            if (inputs.SwapEvaStrafeTurn.GetButtonDownIfEnabled())
            {
                Traverse.Create(__instance).Property("SwapEvaStrafeTurn").SetValue(!__instance.SwapEvaStrafeTurn);
                if (Game.InFlightScene)
                {
                    Game.Instance.FlightScene.FlightSceneUI.ShowMessage("EVA Strafe and EVA Turn inputs swapped.");
                }
            }
            Traverse.Create(typeof(FlightControls).Assembly.GetType("InputWrapper")).Method("UpdateLastInput", inputs.Throttle).GetValue();
            bool flag = Traverse.Create(typeof(FlightControls).Assembly.GetType("InputWrapper")).Method("LastInputWasNormalAxis", inputs.Throttle).GetValue<bool>();
            if (inputs.Throttle.Enabled && !flag)
            {
                float throttleIncrement = Mathf.Clamp(inputs.Throttle.GetAxis(), -1f, 1f);
                if (____throttleIncrement != throttleIncrement)
                {
                    ____throttleIncrement = throttleIncrement;
                    ControlsPatches.RegisterExternalCommand("Throttle Increment");
                }
            }
            float? controlInput = Traverse.Create(__instance).Method("GetControlInput", inputs.Pitch).GetValue<float?>();
            float? controlInput2 = Traverse.Create(__instance).Method("GetControlInput", __instance.SwapRollYaw ? inputs.Yaw : inputs.Roll).GetValue<float?>();
            float? controlInput3 = Traverse.Create(__instance).Method("GetControlInput", __instance.SwapRollYaw ? inputs.Roll : inputs.Yaw).GetValue<float?>();
            float? controlInput4 = Traverse.Create(__instance).Method("GetControlInput", inputs.Brake).GetValue<float?>();
            float num = Traverse.Create(__instance).Method("GetControlInput", inputs.EvaMoveUpDownNoModifier).GetValue<float?>().GetValueOrDefault();
            float num2 = Traverse.Create(__instance).Method("GetControlInput", inputs.EvaPitchNoModifier).GetValue<float?>().GetValueOrDefault();
            float num3 = Traverse.Create(__instance).Method("GetControlInput", inputs.EvaRollNoModifier).GetValue<float?>().GetValueOrDefault();
            float num4 = Traverse.Create(__instance).Method("GetControlInput", inputs.EvaMoveFwdAft).GetValue<float?>().GetValueOrDefault();
            float num5 = Traverse.Create(__instance).Method("GetControlInput", __instance.SwapEvaStrafeTurn ? inputs.EvaTurn : inputs.EvaStrafe).GetValue<float?>().GetValueOrDefault();
            float num6 = Traverse.Create(__instance).Method("GetControlInput", __instance.SwapEvaStrafeTurn ? inputs.EvaStrafe : inputs.EvaTurn).GetValue<float?>().GetValueOrDefault();
            IGameInput evaEnableJetpackMovement = inputs.EvaEnableJetpackMovement;
            if (!evaEnableJetpackMovement.IsBound || evaEnableJetpackMovement.GetButton())
            {
                if (evaEnableJetpackMovement.IsBound)
                {
                    num4 = 0f;
                    num5 = 0f;
                    num6 = 0f;
                }
                num += Traverse.Create(__instance).Method("GetControlInput", inputs.EvaMoveUpDown).GetValue<float?>().GetValueOrDefault();
                num2 += Traverse.Create(__instance).Method("GetControlInput", inputs.EvaPitch).GetValue<float?>().GetValueOrDefault();
                num3 += Traverse.Create(__instance).Method("GetControlInput", inputs.EvaRoll).GetValue<float?>().GetValueOrDefault();
            }
            float oldTranslateUp = __instance.Controls.TranslateUp;
            float oldTranslateRight = __instance.Controls.TranslateRight;
            float oldTranslateForward = __instance.Controls.TranslateForward;

            __instance.Controls.PitchInputReceived = false;
            __instance.Controls.RollInputReceived = false;
            __instance.Controls.YawInputReceived = false;
            __instance.Controls.TranslateUp = 0f;
            __instance.Controls.TranslateRight = 0f;
            __instance.Controls.TranslateForward = 0f;

            if (!____craftNode.Controls.TranslationModeEnabled)
            {
                if (controlInput2.HasValue && (!____navSphere.HeadingLocked || controlInput2.Value + __instance.AnalogRoll != 0f))
                {
                    __instance.Controls.Roll = Mathf.Clamp(controlInput2.Value + __instance.AnalogRoll + __instance.Controls.OffsetRoll, -1f, 1f);
                    __instance.Controls.RollInputReceived = controlInput2.Value + __instance.AnalogRoll != 0f;
                    if (__instance.Controls.RollInputReceived) ControlsPatches.RegisterExternalCommand("RollInputReceived");
                }
                if (controlInput.HasValue && (!____navSphere.HeadingLocked || controlInput.Value + __instance.AnalogPitch != 0f))
                {
                    __instance.Controls.Pitch = Mathf.Clamp(controlInput.Value + __instance.AnalogPitch + __instance.Controls.OffsetPitch, -1f, 1f);
                    __instance.Controls.PitchInputReceived = controlInput.Value + __instance.AnalogPitch != 0f;
                    if (__instance.Controls.PitchInputReceived) ControlsPatches.RegisterExternalCommand("PitchInputReceived");
                }
                if (controlInput3.HasValue && (!____navSphere.HeadingLocked || controlInput3.Value + __instance.AnalogYaw != 0f))
                {
                    __instance.Controls.Yaw = Mathf.Clamp(controlInput3.Value + __instance.AnalogYaw + __instance.Controls.OffsetYaw, -1f, 1f);
                    __instance.Controls.YawInputReceived = controlInput3.Value + __instance.AnalogYaw != 0f;
                    if (__instance.Controls.YawInputReceived) ControlsPatches.RegisterExternalCommand("YawInputReceived");
                }
                if (____navSphere.HeadingLocked)
                {
                    ____navSphere.LockHeading(____navSphere.Pitch, ____navSphere.Heading);
                }
                __instance.Controls.TranslateUp = Mathf.Clamp(__instance.Controls.OffsetTranslateUp, -1f, 1f);
                __instance.Controls.TranslateForward = Mathf.Clamp(__instance.Controls.OffsetTranslateForward, -1f, 1f);
                __instance.Controls.TranslateRight = Mathf.Clamp(__instance.Controls.OffsetTranslateRight, -1f, 1f);
            }
            else
            {
                if (controlInput2.HasValue)
                {
                    __instance.Controls.TranslateUp = Mathf.Clamp(controlInput.Value + __instance.AnalogPitch + __instance.Controls.OffsetTranslateUp, -1f, 1f);
                }
                if (controlInput.HasValue)
                {
                    __instance.Controls.TranslateForward = Mathf.Clamp(controlInput2.Value + __instance.AnalogThrottle + __instance.Controls.OffsetTranslateForward, -1f, 1f);
                }
                if (controlInput3.HasValue)
                {
                    __instance.Controls.TranslateRight = Mathf.Clamp(controlInput3.Value + __instance.AnalogRoll + __instance.Controls.OffsetTranslateRight, -1f, 1f);
                }
            }
            float translateUp = Mathf.Clamp((__instance.Controls.TranslateUp + Traverse.Create(__instance).Method("GetControlInput", inputs.TranslateUpDown).GetValue<float?>()).GetValueOrDefault(), -1f, 1f);
            if (translateUp != oldTranslateUp)
            {
                __instance.Controls.TranslateUp = translateUp;
                ControlsPatches.RegisterExternalCommand("TranslateUp");
            }
            float translateRight = Mathf.Clamp((__instance.Controls.TranslateRight + Traverse.Create(__instance).Method("GetControlInput", inputs.TranslateLeftRight).GetValue<float?>()).GetValueOrDefault(), -1f, 1f);
            if (translateRight != oldTranslateUp)
            {
                __instance.Controls.TranslateRight = translateRight;
                ControlsPatches.RegisterExternalCommand("TranslateRight");
            }
            float translateForward = Mathf.Clamp((__instance.Controls.TranslateForward + Traverse.Create(__instance).Method("GetControlInput", inputs.TranslateForwardBackward).GetValue<float?>()).GetValueOrDefault(), -1f, 1f);
            if (translateForward != oldTranslateForward)
            {
                __instance.Controls.TranslateForward = translateForward;
                ControlsPatches.RegisterExternalCommand("TranslateForward");
            }
            __instance.Controls.EvaAnalogJump = __instance.EvaJumpUI;
            __instance.Controls.EvaMoveFwdAft = Mathf.Clamp(num4 + __instance.AnalogEvaMoveFwdAft, -1f, 1f);
            __instance.Controls.EvaStrafe = Mathf.Clamp(num5 + __instance.AnalogEvaStrafe, -1f, 1f);
            __instance.Controls.EvaTurn = Mathf.Clamp(num6 + __instance.AnalogYaw, -1f, 1f);
            __instance.Controls.EvaMoveUpDown = Mathf.Clamp(num + __instance.AnalogEvaUpDown, -1f, 1f);
            __instance.Controls.EvaPitch = Mathf.Clamp(num2 + __instance.AnalogPitch, -1f, 1f);
            __instance.Controls.EvaRoll = Mathf.Clamp(num3 + __instance.AnalogRoll, -1f, 1f);
            __instance.Controls.EvaShootTether = inputs.EvaShootTether.GetButtonDownIfEnabled() || __instance.EvaShootTetherUI;
            __instance.Controls.EvaTetherLength = Mathf.Clamp(inputs.EvaTetherLength.GetAxis() + __instance.Controls.EvaTetherLengthOffset, -1f, 1f);
            if (controlInput4.HasValue)
            {
                float brake = Mathf.Clamp(controlInput4.Value + __instance.AnalogBrake + __instance.Controls.OffsetBrake, -1f, 1f);
                if (__instance.Controls.Brake != brake)
                {
                    __instance.Controls.Brake = brake;
                    ControlsPatches.RegisterExternalCommand("Brake");
                }
            }
            if (inputs.Throttle.Enabled)
            {
                float oldThrottle = __instance.Controls.Throttle;
                if (flag)
                {
                    __instance.Controls.Throttle = inputs.Throttle.GetAxis();
                }
                else
                {
                    __instance.Controls.Throttle += timeStep * (____throttleIncrement + (__instance.Controls.TranslationModeEnabled ? 0f : __instance.AnalogThrottle));
                }
                float throttle = Mathf.Clamp01(__instance.Controls.Throttle);
                if (throttle != oldThrottle)
                {
                    __instance.Controls.Throttle = throttle;
                    ControlsPatches.RegisterExternalCommand("Throttle");
                }
            }
            if (inputs.KillThrottle.GetButtonDownIfEnabled())
            {
                __instance.Controls.Throttle = 0f;
                ControlsPatches.RegisterExternalCommand("KillThrottle");
            }
            else if (inputs.FullThrottle.GetButtonDownIfEnabled())
            {
                __instance.Controls.Throttle = 1f;
                ControlsPatches.RegisterExternalCommand("FullThrottle");
            }
            if (inputs.Slider1.Enabled)
            {
                float slider1 = Mathf.Clamp(inputs.Slider1.GetAxis() + __instance.Controls.OffsetSlider1, -1f, 1f);
                if (slider1 != __instance.Controls.Slider1)
                {
                    __instance.Controls.Slider1 = slider1;
                    ControlsPatches.RegisterExternalCommand("Slider1");
                }
            }
            if (inputs.Slider2.Enabled)
            {
                float slider2 = Mathf.Clamp(inputs.Slider2.GetAxis() + __instance.Controls.OffsetSlider2, -1f, 1f);
                if (slider2 != __instance.Controls.Slider2)
                {
                    __instance.Controls.Slider2 = slider2;
                    ControlsPatches.RegisterExternalCommand("Slider1");
                }
            }
            if (inputs.Slider3.Enabled)
            {
                float slider3 = Mathf.Clamp(inputs.Slider3.GetAxis() + __instance.Controls.OffsetSlider3, -1f, 1f);
                if (slider3 != __instance.Controls.Slider3)
                {
                    __instance.Controls.Slider3 = slider3;
                    ControlsPatches.RegisterExternalCommand("Slider1");
                }
            }
            if (inputs.Slider4.Enabled)
            {
                float slider4 = Mathf.Clamp(inputs.Slider4.GetAxis() + __instance.Controls.OffsetSlider4, -1f, 1f);
                if (slider4 != __instance.Controls.Slider4)
                {
                    __instance.Controls.Slider4 = slider4;
                    ControlsPatches.RegisterExternalCommand("Slider1");
                }
            }
            if (!____flightScene.TimeManager.Paused)
            {
                if (inputs.ActivateStage.GetButtonDownIfEnabled())
                {
                    Traverse.Create(__instance).Method("ActivateStage").GetValue();
                    ControlsPatches.RegisterExternalCommand("ActivateStage");
                }
                if (inputs.EvaToggleWalk.GetButtonUpIfEnabled())
                {
                    __instance.Controls.EvaWalk = !__instance.Controls.EvaWalk;
                    ____flightScene.FlightSceneUI.ShowMessage((__instance.Controls.EvaWalk ? "Walking" : "Running") ?? "");
                }
            }
            if (inputs.ActivationGroup1.GetButtonDownIfEnabled())
            {
                __instance.Controls.ToggleActivationGroup(1);
                ControlsPatches.RegisterExternalCommand("ToggleActivationGroup1");
            }
            if (inputs.ActivationGroup2.GetButtonDownIfEnabled())
            {
                __instance.Controls.ToggleActivationGroup(2);
                ControlsPatches.RegisterExternalCommand("ToggleActivationGroup2");
            }
            if (inputs.ActivationGroup3.GetButtonDownIfEnabled())
            {
                __instance.Controls.ToggleActivationGroup(3);
                ControlsPatches.RegisterExternalCommand("ToggleActivationGroup3");
            }
            if (inputs.ActivationGroup4.GetButtonDownIfEnabled())
            {
                __instance.Controls.ToggleActivationGroup(4);
                ControlsPatches.RegisterExternalCommand("ToggleActivationGroup4");
            }
            if (inputs.ActivationGroup5.GetButtonDownIfEnabled())
            {
                __instance.Controls.ToggleActivationGroup(5);
                ControlsPatches.RegisterExternalCommand("ToggleActivationGroup5");
            }
            if (inputs.ActivationGroup6.GetButtonDownIfEnabled())
            {
                __instance.Controls.ToggleActivationGroup(6);
                ControlsPatches.RegisterExternalCommand("ToggleActivationGroup6");
            }
            if (inputs.ActivationGroup7.GetButtonDownIfEnabled())
            {
                __instance.Controls.ToggleActivationGroup(7);
                ControlsPatches.RegisterExternalCommand("ToggleActivationGroup7");
            }
            if (inputs.ActivationGroup8.GetButtonDownIfEnabled())
            {
                __instance.Controls.ToggleActivationGroup(8);
                ControlsPatches.RegisterExternalCommand("ToggleActivationGroup8");
            }
            if (inputs.ActivationGroup9.GetButtonDownIfEnabled())
            {
                __instance.Controls.ToggleActivationGroup(9);
                ControlsPatches.RegisterExternalCommand("ToggleActivationGroup9");
            }
            if (inputs.ActivationGroup10.GetButtonDownIfEnabled())
            {
                __instance.Controls.ToggleActivationGroup(10);
                ControlsPatches.RegisterExternalCommand("ToggleActivationGroup10");
            }
            if (inputs.LockHeading.GetButtonDownIfEnabled())
            {
                if (!____navSphere.HeadingLocked)
                {
                    ____navSphere.LockCurrentHeading();
                    ControlsPatches.RegisterExternalCommand("LockCurrentHeading");
                }
                else
                {
                    ____navSphere.UnlockHeading();
                    ControlsPatches.RegisterExternalCommand("UnlockHeading");
                }
            }
            if (inputs.LockPrograde.GetButtonDownIfEnabled())
            {
                ____navSphere.ToggleProgradeLock();
                ControlsPatches.RegisterExternalCommand("LockPrograde");
            }
            if (inputs.LockRetrograde.GetButtonDownIfEnabled())
            {
                ____navSphere.ToggleRetrogradeLock();
                ControlsPatches.RegisterExternalCommand("LockRetrograde");
            }
            if (inputs.LockTarget.GetButtonDownIfEnabled())
            {
                ____navSphere.ToggleTargetLock();
                ControlsPatches.RegisterExternalCommand("LockTarget");
            }
            if (inputs.ToggleTranslationMode.GetButtonDownIfEnabled())
            {
                __instance.Controls.ToggleTranslationMode();
                ControlsPatches.RegisterExternalCommand("ToggleTranslationMode");
            }
            if (inputs.ActivateCameraLook.GetButtonDown())
            {
                inputs.ActivateCameraLook.Enabled = !inputs.ActivateCameraLook.Enabled;
            }
            return false;
        }
    }
}