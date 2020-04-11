using BepInEx;
using RoR2;
using RoR2.Achievements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoMod.Cil;
using RoR2.UI;
using Mono.Cecil.Cil;

namespace Dak.AchievementLoader
{
    [BepInPlugin("harbingerofme.forked.dakkhuza.plugins.achievementloader", "AchievementLoader", "3.0.0")]
    public class AchievementLoader : BaseUnityPlugin
    {
		static public readonly List<Assembly> toScan = new List<Assembly>();
		static private Assembly CurrentlyScanning;

		static public void ScanMyAssembly()
		{
			toScan.Add(Assembly.GetCallingAssembly());
		}

		void Awake()
		{
			On.RoR2.UnlockableCatalog.Init += UnlockableCatalog_Init;
			On.RoR2.AchievementManager.CollectAchievementDefs += AchievementManager_CollectAchievementDefs;
			IL.RoR2.AchievementManager.CollectAchievementDefs += AchievementManager_CollectAchievementDefs1;
#if DEBUG
			ScanMyAssembly();
#endif
		}

		private void UnlockableCatalog_Init(On.RoR2.UnlockableCatalog.orig_Init orig)
		{
			MethodInfo RegisterUnlockable = typeof(UnlockableCatalog).GetMethod("RegisterUnlockable", BindingFlags.NonPublic | BindingFlags.Static);
			
			foreach(Assembly assembly in toScan)
			{
				foreach (Type t in assembly.GetTypes())
				{
					var attr = t.GetCustomAttributes(typeof(CustomUnlockable),true).Cast<CustomUnlockable>().ToArray();
					if (attr.Length  > 0)
					{
						UnlockableDef uDef = attr[0].GetUnlockableDef();
						RegisterUnlockable.Invoke(null, new object[] { uDef.name, uDef });
					}
				}
			}
			orig();
		}

		private void AchievementManager_CollectAchievementDefs(On.RoR2.AchievementManager.orig_CollectAchievementDefs orig, Dictionary<string, AchievementDef> map)
		{
			toScan.Add(typeof(BaseAchievement).Assembly);
			map.Clear();
			foreach (Assembly assembly in toScan)
			{
				CurrentlyScanning = assembly;
				orig(map);
			}
			var TheOnlySubscribedDelegate = typeof(AchievementListPanelController).GetMethod("BuildAchievementListOrder", BindingFlags.Static | BindingFlags.NonPublic);
			TheOnlySubscribedDelegate.Invoke(null,null);
		}

		private void AchievementManager_CollectAchievementDefs1(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			FieldInfo curScanfld = typeof(AchievementLoader).GetField("CurrentlyScanning", BindingFlags.NonPublic | BindingFlags.Static);

			//Remove map.Clear();
			c.GotoNext(MoveType.Before,
				x => x.MatchCallOrCallvirt("System.Collections.Generic.Dictionary`2<System.String,RoR2.AchievementDef>","Clear")
			);
			c.Index -= 2;
			c.RemoveRange(3);

			//Replace the BaseAchievementToken
			//followed by a typeof()
			//followeb by get_Assembly()
			c.RemoveRange(3);
			c.Emit(OpCodes.Ldsfld, curScanfld);

			c.GotoNext(MoveType.Before, x => x.MatchLdsfld("RoR2.AchievementManager", "onAchievementsRegistered"));
			c.Emit(OpCodes.Ret);
		}
	}
}
