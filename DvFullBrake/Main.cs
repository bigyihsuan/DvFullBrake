using System;
using System.Collections.Generic;
using System.Reflection;
using DV.CabControls.NonVR;
using DV.KeyboardInput;
using DV.RemoteControls;
using DV.Simulation.Cars;
using DV.Simulation.Controllers;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using VRTK.Examples;
using static KeyBindings;

namespace DvFullBrake;

public static class Main
{
	public static UnityModManager.ModEntry? mod;
	public const string PREFIX = "[DvFullBrake] ";

	private const bool SKIP_ORIGINAL = false;
	private const bool KEEP_ORIGINAL = true;

	// Unity Mod Manage Wiki: https://wiki.nexusmods.com/index.php/Category:Unity_Mod_Manager
	private static bool Load(UnityModManager.ModEntry modEntry)
	{
		Harmony? harmony = null;
		mod = modEntry;

		try
		{
			harmony = new Harmony(modEntry.Info.Id);
			harmony.PatchAll(Assembly.GetExecutingAssembly());

			// Other plugin startup logic
			modEntry.Logger.Log("Loaded!");
		}
		catch (Exception ex)
		{
			modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
			harmony?.UnpatchAll(modEntry.Info.Id);
			return false;
		}

		return true;
	}

	[HarmonyPatch(typeof(MouseScrollKeyboardInput), nameof(MouseScrollKeyboardInput.Tick))]
	class MouseScrollKeyboardInput_Tick_Patch
	{
		static bool Prefix(
			MouseScrollKeyboardInput __instance,
			bool ____isScrollingInProgress,
			[HarmonyArgument(0)] Dictionary<KeyBindings.KeyType, KeyCode[]> keyMap,
			[HarmonyArgument(1)] float deltaTime
		)
		{
			KeyCode[] applyBrakeKeys;
			bool isApplyingBrakes = keyMap.TryGetValue(__instance.scrollUpKey, out applyBrakeKeys) && applyBrakeKeys.IsDown();
			KeyCode[] releaseBrakeKeys;
			bool isReleasingBrakes = keyMap.TryGetValue(__instance.scrollDownKey, out releaseBrakeKeys) && releaseBrakeKeys.IsDown();

			if (!KeyCode.RightControl.IsPressed())
			{
				return KEEP_ORIGINAL;
			}
			// train brakes
			if (__instance.scrollUpKey == KeyType.IncreaseBrake)
			{
				if (isApplyingBrakes)
				{
					Debug.Log(PREFIX + "Trying to full apply train brakes!");
					var lever = (LeverNonVR)__instance.gameObject.GetComponent<IMouseWheelHoverScrollable>();
					if (lever != null)
					{
						((IMouseWheelHoverScrollable)lever).OnHoverScrolledUp(MouseWheelScrollSource.Mouse);
						lever.SetValue(1f);
						return SKIP_ORIGINAL;
					}
				}
				else if (isReleasingBrakes)
				{
					Debug.Log(PREFIX + "Trying to full release train brakes!");
					var lever = (LeverNonVR)__instance.gameObject.GetComponent<IMouseWheelHoverScrollable>();
					if (lever != null)
					{
						((IMouseWheelHoverScrollable)lever).OnHoverScrolledDown(MouseWheelScrollSource.Mouse);
						lever.SetValue(0f);
						return SKIP_ORIGINAL;
					}
				}
			}
			// independent brakes
			else if (__instance.scrollUpKey == KeyType.IncreaseIndependentBrake)
			{
				if (isApplyingBrakes)
				{
					Debug.Log(PREFIX + "Trying to full apply indy brakes!");
					var lever = (LeverNonVR)__instance.gameObject.GetComponent<IMouseWheelHoverScrollable>();
					if (lever != null)
					{
						((IMouseWheelHoverScrollable)lever).OnHoverScrolledUp(MouseWheelScrollSource.Mouse);
						lever.SetValue(1f);
						return SKIP_ORIGINAL;
					}
				}
				else if (isReleasingBrakes)
				{
					Debug.Log(PREFIX + "Trying to full release indy brakes!");
					var lever = (LeverNonVR)__instance.gameObject.GetComponent<IMouseWheelHoverScrollable>();
					if (lever != null)
					{
						((IMouseWheelHoverScrollable)lever).OnHoverScrolledDown(MouseWheelScrollSource.Mouse);
						lever.SetValue(0f);
						return SKIP_ORIGINAL;
					}
				}
			}
			return KEEP_ORIGINAL;
		}
	}

	[HarmonyPatch(typeof(RemoteControllerModule), nameof(RemoteControllerModule.UpdateBrake))]
	class RemoteControllerModule_UpdateBrake_Patch
	{
		static bool Prefix(
			RemoteControllerModule __instance,
			TrainCar ___car,
			BaseControlsOverrider ___controlsOverrider,
			[HarmonyArgument(0)] float factor
		)
		{
			if (!KeyCode.RightControl.IsPressed())
			{
				return KEEP_ORIGINAL;
			}

			if (___car.brakeSystem.selfLappingController)
			{
				BrakeControl brake = ___controlsOverrider.Brake;
				if (brake == null)
				{
					return KEEP_ORIGINAL;
				}
				brake.Set(factor > 0 ? 1f : 0f);
			}
			else
			{
				BrakeControl brake = ___controlsOverrider.Brake;
				if (brake == null)
				{
					return KEEP_ORIGINAL;
				}
				float num = (factor == 0f) ? 0f : (Mathf.Sign(factor) * 0.15f);
				brake.Set(0.5f + num);
			}
			return SKIP_ORIGINAL;
		}
	}
	[HarmonyPatch(typeof(RemoteControllerModule), nameof(RemoteControllerModule.UpdateIndependentBrake))]
	class RemoteControllerModule_UpdateIndependentBrake_Patch
	{
		static bool Prefix(
			RemoteControllerModule __instance,
			TrainCar ___car,
			BaseControlsOverrider ___controlsOverrider,
			[HarmonyArgument(0)] float factor
		)
		{
			if (!KeyCode.RightControl.IsPressed())
			{
				return KEEP_ORIGINAL;
			}
			IndependentBrakeControl indyBrake = ___controlsOverrider.IndependentBrake;
			if (indyBrake == null)
			{
				return KEEP_ORIGINAL;
			}
			indyBrake.Set(factor > 0 ? 1f : 0f);
			return SKIP_ORIGINAL;
		}
	}

	// TODO: make the key configurable
}

