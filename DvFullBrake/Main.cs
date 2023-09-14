using System;
using System.Collections.Generic;
using System.Reflection;
using DV.CabControls.NonVR;
using DV.KeyboardInput;
using DV.RemoteControls;
using DV.Simulation.Cars;
using DV.Simulation.Controllers;
using DV.UserManagement;
using DV.Utils;
using HarmonyLib;
using IniParser.Model;
using IniParser.Parser;
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
			KeyCode[] fullApplyBrakeKeys;
			bool isFullApplying = keyMap.TryGetValue(KeyBindingsPatches.FullActivateBrake, out fullApplyBrakeKeys) && fullApplyBrakeKeys.IsDown();

			if (!isFullApplying)
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
			KeyCode[] fullApplyBrakeKeys;
			bool isFullApplying = KeyBindings.keyTypeToKeysMap.TryGetValue(KeyBindingsPatches.FullActivateBrake, out fullApplyBrakeKeys) && fullApplyBrakeKeys.IsDown();
			if (!isFullApplying)
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
			KeyCode[] fullApplyBrakeKeys;
			bool isFullApplying = KeyBindings.keyTypeToKeysMap.TryGetValue(KeyBindingsPatches.FullActivateBrake, out fullApplyBrakeKeys) && fullApplyBrakeKeys.IsDown();
			if (!isFullApplying)
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

	class KeyBindingsPatches
	{
		public const KeyBindings.KeyType FullActivateBrake = KeyType.DecreaseHandbrake + 1;


		[KeyBinding(new KeyCode[] { KeyCode.RightControl })]
		public static KeyCode[] fullActivateBrakeKeys;

		[HarmonyPatch(typeof(KeyBindings), nameof(KeyBindings.RefreshKeyTypeToKeysMap))]
		class KeyBindings_RefreshKeyTypeToKeysMap_Patch
		{
			// add the new keybinding to the dictionary
			static void Postfix()
			{
				if (!KeyBindings.keyTypeToKeysMap.ContainsKey(FullActivateBrake))
				{
					KeyBindings.keyTypeToKeysMap.Add(FullActivateBrake, fullActivateBrakeKeys);
				}
			}
		}

		[HarmonyPatch(typeof(KeyBindings), nameof(KeyBindings.AllKeyBindFields))]
		[HarmonyPatch(MethodType.Getter)]
		class KeyBindings_AllKeyBindFields_Patch
		{
			static void Postfix(ref List<FieldInfo> __result)
			{
				var field = typeof(KeyBindingsPatches).GetField("fullActivateBrakeKeys");
				if (!__result.Contains(field))
				{
					__result.Add(field);
				}
			}
		}
	}

	class KeyBindingsConfigurationPatches
	{
		[HarmonyPatch(typeof(KeyBindingsConfiguration), "ReadKeyBindingsFromFile")]
		class KeyBindingsConfiguration_ReadKeyBindingsFromFile_Patch
		{
			static bool Prefix(
				KeyBindingsConfiguration __instance,
				[HarmonyArgument(0)] IniDataParser parser
			)
			{
				IniData iniData = parser.Parse(SingletonBehaviour<UserManager>.Instance.CurrentUser.Preferences.RawData);
				var bindings = KeyBindings.AllChangeableKeyBindFields;
				var field = typeof(KeyBindingsPatches).GetField("fullActivateBrakeKeys");
				if (!bindings.Contains(field))
				{
					bindings.Add(field);
				}
				foreach (FieldInfo fieldInfo in bindings)
				{
					MethodInfo GetConfigKeyForField = __instance.GetType().GetMethod("GetConfigKeyForField", BindingFlags.NonPublic | BindingFlags.Instance);
					MethodInfo ReadKeyBinding = __instance.GetType().GetMethod("ReadKeyBinding", BindingFlags.NonPublic | BindingFlags.Instance);
					KeyCode[] array = (KeyCode[])ReadKeyBinding.Invoke(__instance, new object[] {
						iniData["Non-VR_KeyBindings"][(string)GetConfigKeyForField.Invoke(__instance, new object[] { fieldInfo })]
					});
					if (array.Length == 0)
					{
						Debug.LogError(GetConfigKeyForField.Invoke(__instance, new object[] { fieldInfo }) + " does not have a key binding");
					}
					else
					{
						KeyBindings.SetKeyBinding(fieldInfo, array);
					}
				}
				KeyBindings.RefreshKeyTypeToKeysMap();
				return SKIP_ORIGINAL;
			}
		}
		[HarmonyPatch(typeof(KeyBindingsConfiguration), "WriteKeyBindingsToFile")]
		class KeyBindingsConfiguration_WriteKeyBindingsToFile_Patch
		{
			static bool Prefix(
				KeyBindingsConfiguration __instance,
				[HarmonyArgument(0)] IniDataParser parser,
				[HarmonyArgument(1)] bool onlyMissing
			)
			{
				IniData iniData = onlyMissing ? parser.Parse(SingletonBehaviour<UserManager>.Instance.CurrentUser.Preferences.RawData) : new IniData();
				var bindings = KeyBindings.AllChangeableKeyBindFields;
				var field = typeof(KeyBindingsPatches).GetField("fullActivateBrakeKeys");
				if (!bindings.Contains(field))
				{
					bindings.Add(field);
				}
				foreach (FieldInfo fieldInfo in bindings)
				{
					MethodInfo GetConfigKeyForField = __instance.GetType().GetMethod("GetConfigKeyForField", BindingFlags.NonPublic | BindingFlags.Instance);
					MethodInfo SaveKeyBinding = __instance.GetType().GetMethod("SaveKeyBinding", BindingFlags.NonPublic | BindingFlags.Instance);
					var fieldKey = (string)GetConfigKeyForField.Invoke(__instance, new object[] { fieldInfo });
					if (!onlyMissing || iniData["Non-VR_KeyBindings"][fieldKey] == null)
					{
						SaveKeyBinding.Invoke(__instance, new object[] { iniData, fieldInfo });
					}
				}
				SingletonBehaviour<UserManager>.Instance.CurrentUser.Preferences.RawData = iniData.ToString();
				SingletonBehaviour<UserManager>.Instance.CurrentUser.Preferences.Save();
				Debug.Log("Wrote key bindings configuration: "
					+ SingletonBehaviour<UserManager>.Instance.Storage.GetFilesystemPath(SingletonBehaviour<UserManager>.Instance.CurrentUser.Preferences.Path));
				return SKIP_ORIGINAL;
			}
		}
	}




}

