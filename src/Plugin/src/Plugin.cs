using BepInEx;

namespace H3VRMod
{
	[BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
	[BepInProcess("h3vr.exe")]
	public class Plugin : BaseUnityPlugin
	{
		
	}
	internal static class PluginInfo
	{
		internal const string NAME    = "Meatyceiver 2";
		internal const string GUID    = "com.potatoes1286.meatyceiver2redux";
		internal const string VERSION = "1.0.0";
	}
}