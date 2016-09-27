using Terraria.ModLoader;
using Terraria;
using Terraria.GameContent.Generation;
using System.Collections.Generic;
using Terraria.World.Generation;
using Microsoft.Xna.Framework;

namespace WorldGenPreviewer
{
	public class WorldGenPreviewer : Mod
	{
		public WorldGenPreviewer()
		{
			Properties = new ModProperties()
			{
				Autoload = true, // We need Autoload to be true so our ModWorld class below will be loaded.
			};
		}
	}

	internal class WorldGenPreviewerModWorld : ModWorld
	{
		internal static bool continueWorldGen2 = true;

		public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight)
		{
			// Reset Terrain
			// Reset Special Terrain
			// or after reset
			int ResetStepIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Reset"));
			if (ResetStepIndex != -1)
			{
				tasks.Insert(ResetStepIndex + 1, new PassLegacy("Special World Gen Progress", delegate (GenerationProgress progress)
				{
					progress.Message = "Setting up Special World Gen Progress";
					Main.refreshMap = true;
					var a = new UIWorldLoadSpecial(progress, mod.GetTexture("pause"), mod.GetTexture("play"));
					Main.updateMap = false;
					Main.mapFullscreen = true;
					Main.mapStyle = 0;
					Main.mapReady = true;

					Main.MenuUI.SetState(a);

					Main.mapFullscreenScale = Main.screenWidth / (float)Main.maxTilesX * 0.8f;
					Main.mapFullscreen = true;
					Main.mapMinX = 0;
					Main.mapMinY = 0;
					Main.mapMaxX = Main.maxTilesX;
					Main.mapMaxY = Main.maxTilesY;
					Main.mapFullscreenPos = new Vector2(Main.maxTilesX / 2, Main.maxTilesY / 2);
				}));

				// Reset Special Paused Terrain Paused ...
				for (int i = tasks.Count - 1; i >= ResetStepIndex + 2; i--)
				{
					string name = tasks[i - 1].Name;
					tasks.Insert(i, new PassLegacy("World Gen Paused", delegate (GenerationProgress progress)
					{
						if (!continueWorldGen2)
						{
							progress.Message = "World Gen Paused after " + name;
						}
						while (true)
						{
							if (continueWorldGen2)
							{
								break;
							}
						}
					}));
				}
			}
			else
			{
				ErrorLogger.Log("WorldGenPreviewer mod unable to do it's thing since someone removed reset step");
			}
		}

		public override void PostWorldGen()
		{
			// reset map to original
			Main.mapFullscreen = false;
			Main.mapStyle = 1;
		}
	}
}
