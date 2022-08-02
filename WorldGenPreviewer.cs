using Terraria.ModLoader;
using Terraria;
using Terraria.GameContent.Generation;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Reflection;
using Terraria.WorldBuilding;
using Terraria.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Collections.Concurrent;

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

	internal class WorldGenPreviewerModWorld : ModSystem
	{
		internal static bool saveLockForced = false;
		internal static bool continueWorldGen = true;
		internal static bool pauseAfterContinue = false;
		internal static bool repeatPreviousStep = false;
		internal static bool showStructures = false;
		internal static GenPass pauseAfterPass = null;
		internal static List<GenPass> generationPasses;
		internal static List<Rectangle> structures_structures; // reference to WorldGen.structures._structures
		internal static List<Rectangle> structures_protectedStructures; // reference to WorldGen.structures._protectedStructures

		private static Task updateMapTask;

		public override void PreWorldGen()
		{
			// replace with Monitor.TryEnter(IOLock)?
			if (saveLockForced)
			{
				saveLockForced = false;
				Main.skipMenu = false;
	//			WorldGen.saveLock = false;
			}

			Main.loadMap = true;  // Forces first draw of map

			updateMapTask = Task.Run(UpdateMap);

			FieldInfo structuresField = typeof(StructureMap).GetField("_structures", BindingFlags.Instance | BindingFlags.NonPublic);
			structures_structures = (List<Rectangle>)structuresField.GetValue(WorldGen.structures);

			FieldInfo protectedStructuresField = typeof(StructureMap).GetField("_protectedStructures", BindingFlags.Instance | BindingFlags.NonPublic);
			structures_protectedStructures = (List<Rectangle>)protectedStructuresField.GetValue(WorldGen.structures);

			if (Config.Instance.StartWorldgenPaused) {
				WorldGenPreviewerModWorld.continueWorldGen = false;
				WorldGenPreviewerModWorld.pauseAfterContinue = false;
				WorldGenPreviewerModWorld.pauseAfterPass = null;
			}
		}

		internal static int ScanLineX = 0;
		internal static bool contents = false;
		internal static ConcurrentQueue<Point> sections = new ConcurrentQueue<Point>();
		private void UpdateMap() {
			sections.Clear();
			ScanLineX = 0;
			//Thread.CurrentThread.Priority = ThreadPriority.Lowest;
			int advance = (int)((1200f / Main.maxTilesY) * 300);

			advance = 200; // section width instead of dynamic

			while (WorldGen.generatingWorld) {
				int start = ScanLineX;
				int end = ScanLineX + advance;
				for (ScanLineX = start; ScanLineX < end; ScanLineX++) {
					for (int j = 0; j < Main.maxTilesY; j++) {
						if (WorldGen.InWorld(ScanLineX, j) && Main.Map.UpdateType(ScanLineX, j))
							Main.Map.Update(ScanLineX, j, 255);
						// Draw just this update to a new buffer cleared each pass to see what each pass did?
						// Make separate Map that is updated in tandem.
						// drawToMap and drawToMap_Sections a swapped Main.instance.mapTarget?
						// How will digging holes appear on map, no effect?
					}
				}

				for (int secY = 0; secY < Main.maxTilesY / 150; secY++) {
					sections.Enqueue(new Point(start / 200, secY));
				}

				//ScanLineX += advance;
				if (ScanLineX >= Main.maxTilesX) {
					ScanLineX = 0;
				}

				//for (ScanLineX = 0; ScanLineX < Main.maxTilesX; ScanLineX++) {
				//	for (int j = 0; j < Main.maxTilesY; j++) {
				//		if (WorldGen.InWorld(ScanLineX, j) && Main.Map.UpdateType(ScanLineX, j))
				//			Main.Map.Update(ScanLineX, j, 255);
				//	}
				//}
				contents = true;

				//Thread.Sleep(100); // sleeping just makes it slower for no reason
				if (sections.Count > 100) {
					Thread.Sleep(1000);
					continue;
				}
			}
		}

		public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight)
		{
			generationPasses = tasks;
			// Reset Terrain
			// Reset Special Terrain
			// or after reset
			int ResetStepIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Reset"));
			if (ResetStepIndex != -1)
			{
				tasks.Insert(ResetStepIndex + 1, new PassLegacy("Special World Gen Progress", delegate (GenerationProgress progress, GameConfiguration config)
				{
					Main.FixUIScale();
					progress.Message = "Setting up Special World Gen Progress";
					Main.refreshMap = true;
					var a = new UIWorldLoadSpecial(progress, Mod);
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
					GenPass next = tasks[i];
					tasks.Insert(i, new PassLegacy("World Gen Paused", delegate (GenerationProgress progress, GameConfiguration config)
					{
						UIWorldLoadSpecial.BadPass = next.Name == "Expand World";

						foreach (var item in UIWorldLoadSpecial.instance.passesList._items)
						{
							UIPassItem passitem = item as UIPassItem;
							if (passitem.pass == previous)
							{
								passitem.Complete();
								break;
							}
						}
						if (pauseAfterPass != null) {
							if (pauseAfterPass == previous) {
								continueWorldGen = false;
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
								previous.Apply(progress, WorldGenConfiguration.FromEmbeddedPath("Terraria.GameContent.WorldBuilding.Configuration.json").GetPassConfiguration(previous.Name));
								// Preview: previous.Apply(progress, WorldGen.configuration.GetPassConfiguration(previous.Name));
								//if (continueWorldGen)
								//{
								//	UIWorldLoadSpecial.instance.statusLabel.SetText("Status: Normal");
								//}
								//else
								//{
								UIWorldLoadSpecial.instance.statusLabel.SetText("Status: Paused");
								progress.Message = "World Gen Paused after " + name;
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
				Mod.Logger.Error("WorldGenPreviewer mod unable to do it's thing since someone removed reset step");
			}
		}

		public override void PostWorldGen()
		{
			// reset map to original
			Main.mapFullscreen = false;
			Main.mapStyle = 1;
			structures_structures = null;
			structures_protectedStructures = null;
		}
	}
}
