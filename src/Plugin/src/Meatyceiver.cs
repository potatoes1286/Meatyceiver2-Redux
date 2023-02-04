using System.Resources;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Meatyceiver2.Failures;
using Meatyceiver2.Failures.Ammo;
using Meatyceiver2.Failures.Breakage;
using Meatyceiver2.Failures.Firearm;
using UnityEngine;

namespace Meatyceiver2 {
	[BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
	[BepInProcess("h3vr.exe")]
	public class Meatyceiver : BaseUnityPlugin {
		//General Settings

		public static ConfigEntry<bool> enableFirearmFailures;
		public static ConfigEntry<bool> enableAmmunitionFailures;
		public static ConfigEntry<bool> enableBrokenFirearmFailures;
		public static ConfigEntry<bool> enableConsoleDebugging;

		//Multipliers

		public static ConfigEntry<float> generalMult;

		//Secondary Failure - Mag Unreliability

		public static ConfigEntry<bool>  enableMagUnreliability;
		public static ConfigEntry<float> magUnreliabilityGenMultAffect;
		public static ConfigEntry<float> failureIncPerRound;
		public static ConfigEntry<int>   minRoundCount;

		//Secondary Failure - Long Term Breakdown

		public static ConfigEntry<bool>  enableLongTermBreakdown;
		public static ConfigEntry<float> maxFirearmFailureInc;
		public static ConfigEntry<float> maxBrokenFirearmFailureInc;
		public static ConfigEntry<float> longTermBreakdownGenMultAffect;
		public static ConfigEntry<int>   roundsTillMaxBreakdown;


		//Failures - Ammo

		public static ConfigEntry<float> LPSFailureRate;
		public static ConfigEntry<float> handFireRate;

		//Failures - Firearms

		public static ConfigEntry<float> FTFRate;
		public static ConfigEntry<float> FTERate;
		public static ConfigEntry<float> DFRate;
		public static ConfigEntry<float> stovepipeRate;
		public static ConfigEntry<float> stovepipeLerp;

		//Failures - Broken Firearm

		public static ConfigEntry<float> HFRate;
		public static ConfigEntry<float> FTLSlide;
		public static ConfigEntry<float> slamfireRate;


		//Bespoke Failures

		public static ConfigEntry<float> breakActionFTE;
		public static ConfigEntry<float> breakActionFTEMultAffect;

		public static ConfigEntry<float> revolverFTE;

		public static ConfigEntry<float> revolverFTEGenMultAffect;
		//		public static ConfigEntry<float> revolverFTEshakeMult;

		public static System.Random randomVar;

		private void Awake() {
			Logger.LogInfo("Meatyceiver2 started!");
			InitFields();
			InitPatches();
			randomVar = new System.Random();
		}

		public void InitFields() {
			string GeneralSettings = "General Settings";
			enableAmmunitionFailures = Config.Bind(GeneralSettings, "Enable Ammunition Failures", true,
			                                       "Enables failures relating from poor ammunition.");
			enableFirearmFailures = Config.Bind(GeneralSettings, "Enable Firearm Failures", true,
			                                    "Enables failures resulting from poor firearm construction.");
			enableBrokenFirearmFailures = Config.Bind(GeneralSettings, "Enable Broken Firearm Failures", true,
			                                          "Enables breakage related to permanent firearm failure.");
			enableConsoleDebugging = Config.Bind(GeneralSettings, "Enable Console Debugging", false,
			                                     "Spams your console with debug info.");

			string GeneralMultipliers = "Failure Chance Multiplier";
			generalMult = Config.Bind(GeneralMultipliers, "Failure Chance Multiplier", 1f,
			                          "Base failure chance, in %. 1 -> 1%.");

			string MagUnreliability = "Mag Unreliability Settings";
			enableMagUnreliability = Config.Bind(MagUnreliability, "Enable Mag Unreliability", true,
			                                     "Enable failures relating to mag capacities");
			failureIncPerRound = Config.Bind(MagUnreliability, "Mag Unreliability Multiplier", 0.04f,
			                                 "Every round past mag capacity increases chance of failure by this much in %. Not affected by base multiplier");
			minRoundCount = Config.Bind(MagUnreliability, "Minimum Mag Count", 15,
			                            "Maximum mag capacity before mag unreliability starts to increase");
			magUnreliabilityGenMultAffect = Config.Bind(MagUnreliability, "Mag Unreliability General Multiplier Affect", 0.5f,
			                                            "Max mag round counts above this incurs higher unreliability.");

			//enableLongTermBreakdown = Config.Bind(Strings.LongTermBreak_section, Strings.LongTermBreak_key, true, Strings.LongTermBreak_description);

			string ValidInput = "Valid input: Float, 0f - 100f";
			
			string AmmoFailures = "Ammo Failure Settings";
			LPSFailureRate = Config.Bind(AmmoFailures, "Light Primer Strike Failure Rate", 0.25f, ValidInput);
			handFireRate = Config.Bind(AmmoFailures, "Hang Fire Rate", 0.1f, ValidInput);

			string FirearmFailures = "Firearm Failure Settings";
			FTFRate = Config.Bind(FirearmFailures, "Failure to Feed Rate", 0.25f, ValidInput);
			FTERate = Config.Bind(FirearmFailures, "Failure to Eject rate", 0.15f, ValidInput);
			DFRate = Config.Bind(FirearmFailures, "Double Feed Rate", 0.15f, ValidInput);
			//stovepipeRate = Config.Bind(FirearmFailures, "Strings.StovepipeRate_key", 0.1f, ValidInput);
			//stovepipeLerp = Config.Bind(FirearmFailures, "Strings.StovepipeLerp_key", 0.5f, Strings.DEBUG);

			string BrokenFirearmFailure = "Broken Firearm Failures";
			HFRate = Config.Bind(BrokenFirearmFailure, "Hammer Follow Rate", 0.1f, ValidInput);
			FTLSlide = Config.Bind(BrokenFirearmFailure, "Failure to Lock Slide Rate", 5f, ValidInput);
			slamfireRate = Config.Bind(BrokenFirearmFailure, "Slam Fire Rate", 0.1f,
			                           ValidInput);

			string BespokeFailure = "Bespoke Failures";
			breakActionFTE = Config.Bind(BespokeFailure, "Break Action failure to eject rate", 30f,
			                             ValidInput);
			breakActionFTEMultAffect = Config.Bind(BespokeFailure, "Break Action Failure To Eject General Multiplier Affect", 0.5f,
			                                       "General Multiplier is multiplied by this before affecting BA FTE.");
			revolverFTE = Config.Bind(BespokeFailure, "Revolver Failure to eject rate", 30f, ValidInput);
			revolverFTEGenMultAffect = Config.Bind(BespokeFailure, "Revolver Failure To Eject General Multiplier Affect", 0.5f,
			                                       "General Multiplier is multiplied by this before affecting Revolver FTE.");
		}

		public void InitPatches() {
			//ammo
			Harmony.CreateAndPatchAll(typeof(LightPrimerStrike));
			//breakage
			Harmony.CreateAndPatchAll(typeof(FailureToLockSlide));
			Harmony.CreateAndPatchAll(typeof(HammerFollow));
			Harmony.CreateAndPatchAll(typeof(Slamfire));
			//firearm
			Harmony.CreateAndPatchAll(typeof(FailureToExtract));
			Harmony.CreateAndPatchAll(typeof(FailureToFire));
			//other
			Harmony.CreateAndPatchAll(typeof(OtherFailures));
			Harmony.CreateAndPatchAll(typeof(Meatyceiver));
		}

		public static bool CalcFail(float chance) {
			//effectively returns a number between 0 and 100 with 2 decimal points
			float rand = (float)randomVar.Next(0, 10001) / 100;
			if (enableConsoleDebugging.Value) Debug.Log("Chance: " + chance + " Rand: " + rand);
			if (rand <= chance) return true;
			return false;
		}


		//hammer follow. not sure why this was disabled?
		/*		[HarmonyPatch(typeof(Handgun), "CockHammer")]
				[HarmonyPrefix]
				static bool HammerFollowPatch(bool ___isManual)
				{
					var rand = (float)rnd.Next(0, 10001) / 100;
					Debug.Log("Random number generated for HammerFollow: " + rand);
					if (rand <= HammerFollowRate.Value && !___isManual)
					{
						Debug.Log("Hammer follow!");
						return false;
					}
					return true;
				}*/

		//stovepipe fuckery. not working
		/*[HarmonyPatch(typeof(HandgunSlide), "UpdateSlide")]
		[HarmonyPrefix]
		static bool SPHandgunSlide(
			HandgunSlide __instance,
			float ___m_slideZ_forward,
			float ___m_slideZ_rear,
			float ___m_slideZ_current,
			float ___m_curSlideSpeed,
			out float __state
			)
		{
			if (__instance.RotationInterpSpeed == 2)
			{
				___m_slideZ_current = ___m_slideZ_forward - (___m_slideZ_forward - ___m_slideZ_rear) / 2;
				Debug.Log("prefix slidez: " + ___m_slideZ_current);
				___m_curSlideSpeed = 0;
				if (__instance.CurPos == HandgunSlide.SlidePos.LockedToRear)
				{
					__instance.RotationInterpSpeed = 1;
					Debug.Log("Stovepipe cleared!");
				}
			}
			__state = ___m_slideZ_current;
			return true;
		}*/

		//believe this also has to do with stovepipes
		/*[HarmonyPatch(typeof(HandgunSlide), "UpdateSlide")]
		[HarmonyPostfix]
		static void SPHandgunSlideFix(HandgunSlide __instance, float ___m_slideZ_current, float __state)
		{
			//			if (__instance.RotationInterpSpeed == 2) Debug.Log("prefix slidez: " + __state + " postfix slidez: " + ___m_slideZ_current);
			if (__instance.GameObject.transform.localPosition.z >= __state && __instance.RotationInterpSpeed == 2)
			{
				__instance.GameObject.transform.localPosition = new Vector3(__instance.GameObject.transform.localPosition.x, __instance.GameObject.transform.localPosition.y, __state);
				__instance.Handgun.Chamber.UpdateProxyDisplay();
			}
		}*/

		//this is def part of stovepiping
		/*[HarmonyPatch(typeof(Handgun), "UpdateDisplayRoundPositions")]
		[HarmonyPostfix]
		static void SPHandgun(Handgun __instance, FVRFirearmMovingProxyRound ___m_proxy)
		{
			if (__instance.Slide.RotationInterpSpeed == 2)
			{
				Debug.Log("lerping");
				___m_proxy.ProxyRound.transform.localPosition = Vector3.Lerp(__instance.Slide.Point_Slide_Forward.transform.position, __instance.Slide.Point_Slide_Rear.transform.position, stovepipeLerp.Value);
			}
		}*/
	}

	internal static class PluginInfo {
		internal const string NAME    = "Meatyceiver 2";
		internal const string GUID    = "com.potatoes1286.meatyceiver2redux";
		internal const string VERSION = "0.3.2";
	}
}