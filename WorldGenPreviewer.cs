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
		internal static WorldGenPreviewer instance;

		public override void Load()
		{
			instance = this;
		}

		public override void Unload()
		{
			instance = null;
		}
	}

	internal class WorldGenPreviewerModWorld : ModWorld
	{
		internal static bool continueWorldGen = true;
		internal static bool pauseAfterContinue = false;
		internal static bool repeatPreviousStep = false;
		internal static List<GenPass> generationPasses;

		public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight)
		{
			generationPasses = tasks;
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
					var a = new UIWorldLoadSpecial(progress, mod);
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
					GenPass previous = tasks[i - 1];
					tasks.Insert(i, new PassLegacy("World Gen Paused", delegate (GenerationProgress progress)
					{
						foreach (var item in UIWorldLoadSpecial.instance.passesList._items)
						{
							UIPassItem passitem = item as UIPassItem;
							if (passitem.pass == previous)
							{
								passitem.Complete();
								break;
							}
						}
						if (!continueWorldGen)
						{
							progress.Message = "World Gen Paused after " + name;
							UIWorldLoadSpecial.instance.statusLabel.SetText("Status: Paused");
						}
						while (true)
						{
							if (repeatPreviousStep)
							{
								repeatPreviousStep = false;
								//string previousStatus = UIWorldLoadSpecial.instance.statusLabel.SetText
								UIWorldLoadSpecial.instance.statusLabel.SetText("Status: Doing Previous Step Again");
								previous.Apply(progress);
								//if (continueWorldGen)
								//{
								//	UIWorldLoadSpecial.instance.statusLabel.SetText("Status: Normal");
								//}
								//else
								//{
								UIWorldLoadSpecial.instance.statusLabel.SetText("Status: Paused");
								//}
							}
							if (continueWorldGen)
							{
								if (pauseAfterContinue)
								{
									pauseAfterContinue = false;
									continueWorldGen = false;
								}
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
