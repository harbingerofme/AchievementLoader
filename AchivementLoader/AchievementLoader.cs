using BepInEx;
using RoR2;
using RoR2.Achievements;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using BepInEx.Logging;
using Dak.AchievementLoader;
using System.Reflection;
using MonoMod.Cil;
using Mono.Cecil;
using System.Reflection.Emit;
using Mono.Cecil.Cil;
using System.Xml;

namespace Dak.AchievementLoader
{
    [BepInPlugin("harbingerofme.forked.dakkhuza.plugins.achievementloader", "AchievementLoader", "3.0.0")]
    public class AchievementLoader : BaseUnityPlugin
    {
		static public readonly List<Assembly> toScan = new List<Assembly>();
		static private readonly List<Type> validType = new List<Type>();
		static private Type CurrentlyScanning;

		static public void ScanMyAssembly()
		{
			toScan.Add(Assembly.GetCallingAssembly());
		}

		void Awake()
		{
			On.RoR2.UnlockableCatalog.Init += UnlockableCatalog_Init;
			On.RoR2.AchievementManager.CollectAchievementDefs += AchievementManager_CollectAchievementDefs;
#if DEBUG
			ScanMyAssembly();
#endif
		}


		private void UnlockableCatalog_Init(On.RoR2.UnlockableCatalog.orig_Init orig)
		{
			MethodInfo RegisterUnlockable = typeof(UnlockableCatalog).GetMethod("RegisterUnlockable", BindingFlags.NonPublic | BindingFlags.Static);
			
			foreach(Assembly assembly in toScan)
			{
				bool hasFoundT = false;
				foreach (Type t in assembly.GetTypes())
				{
					var attr = t.GetCustomAttributes(typeof(CustomUnlockable),true).Cast<CustomUnlockable>().ToArray();
					if (attr.Length  > 0)
					{
						UnlockableDef uDef = attr[0].GetUnlockableDef();
						RegisterUnlockable.Invoke(null, new object[] { uDef.name, uDef });
						if (hasFoundT == false)
						{
							hasFoundT = true;
							validType.Add(t);
						}
					}
				}
			}

			orig();
		}

		private void AchievementManager_CollectAchievementDefs(On.RoR2.AchievementManager.orig_CollectAchievementDefs orig, Dictionary<string, AchievementDef> map)
		{
			validType.Add(typeof(BaseAchievement));
			map.Clear();
			foreach (Type type in validType)
			{
				CurrentlyScanning = type;
				IL.RoR2.AchievementManager.CollectAchievementDefs += AchievementManager_CollectAchievementDefs1;
				orig(map);
				IL.RoR2.AchievementManager.CollectAchievementDefs -= AchievementManager_CollectAchievementDefs1;
			}
		}


		private void AchievementManager_CollectAchievementDefs1(MonoMod.Cil.ILContext il)
		{
			ILCursor c = new ILCursor(il);
			
			//Remove map.Clear();
			c.GotoNext(MoveType.Before,
				x => x.MatchCallOrCallvirt("System.Collections.Generic.Dictionary`2<System.String,RoR2.AchievementDef>","Clear")
			);
			c.Index -= 2;
			c.RemoveRange(3);

			//Replace the BaseAchievementToken
			c.Remove();
			c.Emit(Mono.Cecil.Cil.OpCodes.Ldtoken, CurrentlyScanning);
		}
	}
}
