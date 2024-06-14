using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace WorldGenPreviewer
{
	public class WorldGenPreviewer : Mod
	{
		internal static WorldGenPreviewer instance;

		public override void Load() {
			instance = this;

			// Check for old version, reset Main.SettingDontScaleMainMenuUp if found
			var modsThatUpdatedSinceLastLaunchField = typeof(Main).Assembly.GetType("Terraria.ModLoader.Core.ModOrganizer")?.GetField("modsThatUpdatedSinceLastLaunch", BindingFlags.Static | BindingFlags.NonPublic);
			if (modsThatUpdatedSinceLastLaunchField != null) {
				List<(string ModName, Version previousVersion)> modsThatUpdatedSinceLastLaunch = (List<(string ModName, Version previousVersion)>)modsThatUpdatedSinceLastLaunchField.GetValue(null);
				var oldModVersionData = modsThatUpdatedSinceLastLaunch.FirstOrDefault(x => x.ModName == nameof(WorldGenPreviewer));
				if (oldModVersionData != default) {
					Version previousVersion = oldModVersionData.previousVersion;
					if (Main.SettingDontScaleMainMenuUp && previousVersion != null && previousVersion <= new Version(0, 5)) {
						Logger.Info("Detected that WorldGenPreviewer v0.5 or older previously loaded, reverting buggy SettingDontScaleMainMenuUp setting");
						Main.SettingDontScaleMainMenuUp = false;
						Main.SaveSettings();
					}
				}
			}
			if (Main.SettingDontScaleMainMenuUp)
				Logger.Info($"The \"SettingDontScaleMainMenuUp\" setting is currently true, if you are experiencing an issue with the main menu being too small, please manually edit \"{Main.SavePath + Path.DirectorySeparatorChar + "config.json"}\" while tModLoader is closed and change the \"SettingDontScaleMainMenuUp\" setting to false and save the file.");
		}

		public override void Unload() {
			instance = null;
		}
	}
}
