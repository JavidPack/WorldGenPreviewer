using Microsoft.Xna.Framework;
using MonoMod.Cil;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace WorldGenPreviewer
{
	internal class WorldGenPreviewerModSystem : ModSystem
	{
		internal static bool continueWorldGen = true;
		internal static bool pauseAfterContinue = false;
		internal static bool repeatPreviousStep = false;
		internal static bool cancelRequested = false;
		internal static bool showStructures = false;
		internal static GenPass pauseAfterPass = null;
		internal static List<GenPass> generationPasses;
		internal static List<Rectangle> structures_structures; // reference to WorldGen.structures._structures
		internal static List<Rectangle> structures_protectedStructures; // reference to WorldGen.structures._protectedStructures

		private static Task updateMapTask;

		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_structures")]
		extern static ref List<Rectangle> StructureMap_structures(StructureMap c);

		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_protectedStructures")]
		extern static ref List<Rectangle> StructureMap_protectedStructures(StructureMap c);

		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_passes")]
		extern static ref List<GenPass> WorldGenerator_passes(WorldGenerator c);

		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_seed")]
		extern static ref int WorldGenerator_seed(WorldGenerator c);

		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_configuration")]
		extern static ref WorldGenConfiguration WorldGenerator_configuration(WorldGenerator c);

		public override void Load() {
			On_WorldGenerator.GenerateWorld += On_WorldGenerator_GenerateWorld;
			IL_WorldGen.do_worldGenCallBack += IL_WorldGen_do_worldGenCallBack;
		}

		/// <summary>
		/// Prevents the actual saving if cancelRequested is true.
		/// </summary>
		private void IL_WorldGen_do_worldGenCallBack(MonoMod.Cil.ILContext il) {
			try {
				/* Changes:
					GenerateWorld(Main.ActiveWorldFileData.Seed, threadContext as GenerationProgress);
				+	if(WorldGenPreviewerModSystem.cancelRequested)
						goto SkipSaveWorld;
					WorldFile.SaveWorld(Main.ActiveWorldFileData.IsCloudSave, resetTime: true);
				+	SkipSaveWorld:
				+	WorldGenPreviewerModSystem.cancelRequested = false;
					BackupIO.archiveLock = false;
				*/
				ILCursor c = new(il);
				c.GotoNext(MoveType.After, i => i.MatchCall<WorldGen>(nameof(WorldGen.GenerateWorld)));
				var afterGenerateWorld = c.DefineLabel();
				c.EmitLdsfld(typeof(WorldGenPreviewerModSystem).GetField(nameof(WorldGenPreviewerModSystem.cancelRequested), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
				c.EmitBrtrue(afterGenerateWorld);
				c.GotoNext(MoveType.After, i => i.MatchCall<WorldFile>(nameof(WorldFile.SaveWorld)));
				c.MarkLabel(afterGenerateWorld);
				c.EmitDelegate(() => { cancelRequested = false; }); // TODO: Can just reset this in PreWorldGen.
				// c.EmitDelegate(() => cancelRequested = false); // ATTN: This is a Func<bool> actually.

				MonoModHooks.DumpIL(Mod, il);
			}
			catch {
				Mod.Logger.Error("Failed to apply WorldGen.do_worldGenCallBack IL edit. Cancel will not work correctly");
			}
		}

		private void On_WorldGenerator_GenerateWorld(On_WorldGenerator.orig_GenerateWorld orig, WorldGenerator self, GenerationProgress progress) {
			// Completely detour original method.
			if (false) {
				orig(self, progress);
				return;
			}
			var _passes = WorldGenerator_passes(self);
			var _seed = WorldGenerator_seed(self);
			var _configuration = WorldGenerator_configuration(self);
			GenPass previous = null;

			Stopwatch stopwatch = new Stopwatch();
			double num = 0.0;
			foreach (GenPass pass in _passes) {
				num += pass.Weight;
			}

			if (progress == null)
				progress = new GenerationProgress();

			OpenUIWorldGenSpecial(progress, _passes);

			WorldGenerator.CurrentGenerationProgress = progress;
			progress.TotalWeight = num;

			int i = 0;
			foreach (GenPass pass2 in _passes) {
				WorldGen._genRand = new UnifiedRandom(_seed);
				Main.rand = new UnifiedRandom(_seed);
				stopwatch.Start();
				progress.Start(pass2.Weight);

				try {
					UIPassItem passItem = UIWorldLoadSpecial.instance.passesList._items[i] as UIPassItem;
					HandleUserInteractions(progress, previous, _configuration);
					if (cancelRequested) {
						break;
					}
					if (!passItem.skip) {
						pass2.Apply(progress, _configuration.GetPassConfiguration(pass2.Name));
						previous = pass2;
					}
					else {
						passItem.Skipped();
					}
				}
				catch (Exception e) {
					string message = string.Join(
						"\n",
						Language.GetTextValue("tModLoader.WorldGenError"),
						pass2.Name,
						e
					);
					Utils.ShowFancyErrorMessage(message, 0);
					throw;
				}
				i++;

				progress.End();
				stopwatch.Reset();
			}

			WorldGenerator.CurrentGenerationProgress = null;
		}

		private void OpenUIWorldGenSpecial(GenerationProgress progress, List<GenPass> tasks) {
			generationPasses = tasks;

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
		}

		private void HandleUserInteractions(GenerationProgress progress, GenPass previous, WorldGenConfiguration configuration) {
			if (previous == null)
				return;

			UIPassItem passitem = null;
			foreach (var item in UIWorldLoadSpecial.instance.passesList._items) {
				passitem = item as UIPassItem;
				if (passitem.pass == previous) {
					passitem.Complete();
					break;
				}
			}
			if (pauseAfterPass != null) {
				if (pauseAfterPass == previous) {
					continueWorldGen = false;
				}
			}
			if (!continueWorldGen) {
				progress.Message = "World Gen Paused after " + previous.Name;
				UIWorldLoadSpecial.instance.statusLabel.SetText("Status: Paused");
			}
			while (true) {
				if (repeatPreviousStep) {
					repeatPreviousStep = false;
					UIWorldLoadSpecial.instance.statusLabel.SetText("Status: Doing Previous Step Again");
					previous.Apply(progress, configuration.GetPassConfiguration(previous.Name));
					passitem.Repeated();
					UIWorldLoadSpecial.instance.statusLabel.SetText("Status: Paused");
					progress.Message = "World Gen Paused after " + previous.Name;
				}
				if (continueWorldGen) {
					if (pauseAfterContinue) {
						pauseAfterContinue = false;
						continueWorldGen = false;
					}
					break;
				}
			}
		}

		public override void PreWorldGen() {
			Main.loadMap = true;  // Forces first draw of map

			updateMapTask = Task.Run(UpdateMap);

			/*
			FieldInfo structuresField = typeof(StructureMap).GetField("_structures", BindingFlags.Instance | BindingFlags.NonPublic);
			structures_structures = (List<Rectangle>)structuresField.GetValue(GenVars.structures);

			FieldInfo protectedStructuresField = typeof(StructureMap).GetField("_protectedStructures", BindingFlags.Instance | BindingFlags.NonPublic);
			structures_protectedStructures = (List<Rectangle>)protectedStructuresField.GetValue(GenVars.structures);
			*/
			structures_structures = StructureMap_structures(GenVars.structures);
			structures_protectedStructures = StructureMap_protectedStructures(GenVars.structures);

			WorldGenPreviewerModSystem.continueWorldGen = true;
			WorldGenPreviewerModSystem.pauseAfterContinue = false;
			WorldGenPreviewerModSystem.pauseAfterPass = null;
			if (Config.Instance.StartWorldgenPaused) {
				WorldGenPreviewerModSystem.continueWorldGen = false;
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

		public override void PostWorldGen() {
			// reset map to original
			Main.mapFullscreen = false;
			Main.mapStyle = 1;
			structures_structures = null;
			structures_protectedStructures = null;
		}
	}
}
