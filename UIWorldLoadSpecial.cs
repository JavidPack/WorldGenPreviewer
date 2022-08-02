using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;
using Terraria.UI.Gamepad;

using Terraria;
using Terraria.ModLoader;
using Terraria.Map;
using System.IO;
using System.Reflection;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.WorldBuilding;
using ReLogic.Content;
using Terraria.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace WorldGenPreviewer
{
	internal class UIWorldLoadSpecial : UIState
	{
		private UIGenProgressBar _progressBar = new UIGenProgressBar();
		private UIHeader _progressMessage = new UIHeader();
		private GenerationProgress _progress;

		public MethodInfo drawMap;
		public MethodInfo drawToMap;
		public MethodInfo drawToMap_Section;
		public FieldInfo genprogress;
		public Asset<Texture2D> menuTexture;
		public Asset<Texture2D> previousTexture;
		public Asset<Texture2D> playTexture;
		public Asset<Texture2D> pauseTexture;
		public Asset<Texture2D> nextTexture;
		public Asset<Texture2D> structureTexture;

		public UIPanel buttonPanel;
		public UIPanel passesPanel;
		public UIList passesList;
		public UIImageButton menuButton;
		public UIImageButton previousButton;
		public UIImageButton playButton;
		public UIImageButton pauseButton;
		public UIImageButton nextButton;
		public UIImageButton structureButton;
		public UIImageButton cancelButton;
		public UIText statusLabel;

		public static UIWorldLoadSpecial instance;

		float spacing = 8f;
		const float panelWidth = 230;

		public UIWorldLoadSpecial(GenerationProgress progress, Mod mod)
		{
			Asset<Texture2D> GetTextureForUI(string assetName) => mod.Assets.Request<Texture2D>(assetName, AssetRequestMode.ImmediateLoad);

			instance = this;
			menuTexture = GetTextureForUI("menu");
			previousTexture = GetTextureForUI("previous");
			playTexture = GetTextureForUI("play");
			pauseTexture = GetTextureForUI("pause");
			nextTexture = GetTextureForUI("next");
			structureTexture = GetTextureForUI("structure");

			menuButton = new UIImageButton(menuTexture);
			previousButton = new UIImageButton(previousTexture);
			playButton = new UIImageButton(playTexture);
			pauseButton = new UIImageButton(pauseTexture);
			nextButton = new UIImageButton(nextTexture);
			structureButton = new UIImageButton(structureTexture);
			cancelButton = new UIImageButton(GetTextureForUI("cancel"));

			passesPanel = new UIPanel();
			passesPanel.SetPadding(3);
			passesPanel.Left.Pixels = listHidden ? panelWidth : 0;
			passesPanel.HAlign = 1f;
			passesPanel.Top.Set(0f, 0f);
			passesPanel.Width.Set(panelWidth, 0f);
			passesPanel.Height.Set(0f, 1f);
			passesPanel.BackgroundColor = new Color(73, 94, 171);

			passesList = new UIList();
			passesList.Width.Set(0f, 1f);
			passesList.Height.Set(0f, 1f);
			passesList.ListPadding = 12f;
			passesPanel.Append(passesList);

			UIScrollbar passesListScrollbar = new UIScrollbar();
			passesListScrollbar.SetView(100f, 1000f);
			passesListScrollbar.Height.Set(0f, 1f);
			passesListScrollbar.HAlign = 1f;
			passesPanel.Append(passesListScrollbar);
			passesList.SetScrollbar(passesListScrollbar);

			int order = 0;
			for (int i = 0; i < WorldGenPreviewerModWorld.generationPasses.Count; i++)
			{
				GenPass pass = WorldGenPreviewerModWorld.generationPasses[i];
				if (pass.Name != "World Gen Paused")
				{
					order++;
					UIPassItem testLabel = new UIPassItem(order, pass, pass.Name, 1f, false);
					//testLabel.Top.Pixels = y;
					//y += 10;
					passesList.Add(testLabel);
					//passesPanel.Append(testLabel);
				}
			}
			Append(passesPanel);

			buttonPanel = new UIPanel();
			buttonPanel.SetPadding(0);
			//buttonPanel.Left.Set(0f, .5f);
			buttonPanel.HAlign = 0.5f;
			buttonPanel.Top.Set(180f, 0f);
			//buttonPanel.Width.Set(170f, 0f);
			buttonPanel.Height.Set(32 + spacing * 2 + 16, 0f);
			buttonPanel.BackgroundColor = new Color(73, 94, 171);

			float calculatedWidth = spacing;

			buttonPanel.Append(menuButton);
			menuButton.OnClick += MenuClick;
			menuButton.Left.Pixels = calculatedWidth;
			menuButton.Top.Pixels = spacing;
			calculatedWidth += spacing + 32;

			buttonPanel.Append(previousButton);
			previousButton.OnClick += PreviousClick;
			previousButton.Left.Pixels = calculatedWidth;
			previousButton.Top.Pixels = spacing;
			calculatedWidth += spacing + 32;

			buttonPanel.Append(playButton);
			playButton.OnClick += PlayClick;
			playButton.Left.Pixels = calculatedWidth;
			playButton.Top.Pixels = spacing;
			calculatedWidth += spacing + 32;

			buttonPanel.Append(pauseButton);
			pauseButton.OnClick += PauseClick;
			pauseButton.Left.Pixels = calculatedWidth;
			pauseButton.Top.Pixels = spacing;
			calculatedWidth += spacing + 32;

			buttonPanel.Append(nextButton);
			nextButton.OnClick += NextClick;
			nextButton.Left.Pixels = calculatedWidth;
			nextButton.Top.Pixels = spacing;
			calculatedWidth += spacing + 32;

			buttonPanel.Append(structureButton);
			structureButton.OnClick += ToggleStructure;
			structureButton.Left.Pixels = calculatedWidth;
			structureButton.Top.Pixels = spacing;
			calculatedWidth += spacing + 32;

			buttonPanel.Append(cancelButton);
			cancelButton.OnClick += CancelClick;
			cancelButton.Left.Pixels = calculatedWidth;
			cancelButton.Top.Pixels = spacing;
			calculatedWidth += spacing + 32;

			statusLabel = new UIText("Status: Normal", 1f, false);
			statusLabel.VAlign = 1f;
			statusLabel.HAlign = 0.5f;
			statusLabel.Top.Set(-5f, 0f);
			buttonPanel.Append(statusLabel);

			buttonPanel.Width.Pixels = calculatedWidth;
			Append(buttonPanel);

			this._progressBar.Top.Pixels = 70f;
			this._progressBar.HAlign = 0.5f;
			this._progressBar.VAlign = 0f;
			this._progressBar.Recalculate();
			this._progressMessage.CopyStyle(this._progressBar);
			UIHeader expr_78_cp_0 = this._progressMessage;
			expr_78_cp_0.Top.Pixels = expr_78_cp_0.Top.Pixels - 70f;
			this._progressMessage.Recalculate();
			this._progress = progress;
			base.Append(this._progressBar);
			base.Append(this._progressMessage);
		}

		private void ToggleStructure(UIMouseEvent evt, UIElement listeningElement) {
			WorldGenPreviewerModWorld.showStructures = !WorldGenPreviewerModWorld.showStructures;
			statusLabel.SetText("Status: Structure visualization " + (WorldGenPreviewerModWorld.showStructures ? "On" : "Off"));
		}

		private void CancelClick(UIMouseEvent evt, UIElement listeningElement)
		{
			// This approach left the world gen continuing in the other thread, corrupting subsequent world gen attempts
			//throw new Exception("WorldGenPreviewer: User canceled World Gen\n");

			// This didn't work because the enumerator is used in the foreach.
			//FieldInfo passesFieldInfo = typeof(WorldGenerator).GetField("_passes", BindingFlags.Instance | BindingFlags.NonPublic);
			//FieldInfo generatorFieldInfo = typeof(WorldGen).GetField("_generator", BindingFlags.Static | BindingFlags.NonPublic);
			//WorldGenerator _generator = (WorldGenerator)generatorFieldInfo.GetValue(null);
			//passesFieldInfo.SetValue(_generator, null);

			// saveLock prevents save, but needs to be restored to false.
			WorldGenPreviewerModWorld.saveLockForced = true;
			Main.skipMenu = true;
	//		WorldGen.saveLock = true;
			FieldInfo methodFieldInfo = typeof(PassLegacy).GetField("_method", BindingFlags.Instance | BindingFlags.NonPublic);
			// This method still can't cancel infinite loops in passes. This can't be avoided. We could try forcing an exception on the world gen thread like `Main.tile = null`, but we'd have to restore the reference somehow.
			foreach (var item in passesList._items)
			{
				UIPassItem passitem = item as UIPassItem;

				PassLegacy passLegacy = passitem.pass as PassLegacy;
				if (passLegacy != null)
				{
					methodFieldInfo.SetValue(passLegacy, (WorldGenLegacyMethod)delegate (GenerationProgress progress, GameConfiguration config) { });
				}
			}
			WorldGenPreviewerModWorld.continueWorldGen = true;
			WorldGenPreviewerModWorld.pauseAfterContinue = false;
			WorldGenPreviewerModWorld.pauseAfterPass = null;
			statusLabel.SetText("Status: Canceling...");
		}

		public int SortMethod2(UIElement item1, UIElement item2)
		{
			return item1.CompareTo(item2);
		}

		private void PauseClick(UIMouseEvent evt, UIElement listeningElement)
		{
			WorldGenPreviewerModWorld.continueWorldGen = false;
			WorldGenPreviewerModWorld.pauseAfterContinue = false;
			WorldGenPreviewerModWorld.pauseAfterPass = null;
			statusLabel.SetText("Status: Pausing...");
			//Main.PlaySound(10);
		}

		private void PlayClick(UIMouseEvent evt, UIElement listeningElement)
		{
			//Main.PlaySound(10);
			WorldGenPreviewerModWorld.continueWorldGen = true;
			WorldGenPreviewerModWorld.pauseAfterContinue = false;
			WorldGenPreviewerModWorld.pauseAfterPass = null;
			statusLabel.SetText("Status: Normal");
		}

		bool listHidden = false;
		private void MenuClick(UIMouseEvent evt, UIElement listeningElement)
		{
			//Main.PlaySound(10, -1, -1, 1);
			//ErrorLogger.Log("MENU");
			//statusLabel.SetText("Status: ??...");
			listHidden = !listHidden;
			passesPanel.Left.Pixels = listHidden ? panelWidth : 0;
			passesPanel.Recalculate();
		}

		private void PreviousClick(UIMouseEvent evt, UIElement listeningElement)
		{
			//Main.PlaySound(10, -1, -1, 1);
			statusLabel.SetText("Status: Waiting to do this step again...");
			WorldGenPreviewerModWorld.repeatPreviousStep = true;
			WorldGenPreviewerModWorld.continueWorldGen = false;
			WorldGenPreviewerModWorld.pauseAfterContinue = false;
			WorldGenPreviewerModWorld.pauseAfterPass = null;
		}

		private void NextClick(UIMouseEvent evt, UIElement listeningElement)
		{
			//Main.PlaySound(10, -1, -1, 1);
			WorldGenPreviewerModWorld.continueWorldGen = true; // so paused will break.
			WorldGenPreviewerModWorld.pauseAfterContinue = true;
			WorldGenPreviewerModWorld.pauseAfterPass = null;
			statusLabel.SetText("Status: Pausing...");
		}

		//public void Resize()
		//      {
		//          float num = this.spacing;
		//          for (int i = 0; i < this.buttonView.children.Count; i++)
		//          {
		//              if (this.buttonView.children[i].Visible)
		//              {
		//                  this.buttonView.children[i].X = num;
		//                  num += this.buttonView.children[i].Width + this.spacing;
		//              }
		//          }
		//          base.Width = num;
		//          this.buttonView.Width = base.Width;
		//      }

		public override void OnActivate()
		{
			if (PlayerInput.UsingGamepadUI)
			{
				UILinkPointNavigator.Points[3000].Unlink();
				UILinkPointNavigator.ChangePoint(3000);
			}

			Main.mapFullscreenScale = .25f;
			drawMap = typeof(Main).Assembly.GetType("Terraria.Main").GetMethod("DrawMap", BindingFlags.Instance | BindingFlags.NonPublic);
			drawToMap = typeof(Main).Assembly.GetType("Terraria.Main").GetMethod("DrawToMap", BindingFlags.Instance | BindingFlags.NonPublic);
			drawToMap_Section = typeof(Main).Assembly.GetType("Terraria.Main").GetMethod("DrawToMap_Section", BindingFlags.Instance | BindingFlags.NonPublic);
			genprogress = typeof(GenerationProgress).GetField("_totalProgress", BindingFlags.Instance | BindingFlags.NonPublic);
		}

		internal static bool BadPass;
		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			if (BadPass)
				return;

			Vector2 MousePosition = new Vector2((float)Main.mouseX, (float)Main.mouseY);
			if (passesPanel.ContainsPoint(MousePosition)) {
				Main.LocalPlayer.mouseInterface = true;
			}

			this._progressBar.SetProgress(this._progress.TotalProgress, this._progress.Value);
			this._progressMessage.Text = this._progress.Message;
			this.UpdateGamepadSquiggle();

			Main.mapFullscreen = true;
			Main.mapMinX = 0;
			Main.mapMinY = 0;
			Main.mapMaxX = Main.maxTilesX;
			Main.mapMaxY = Main.maxTilesY;

			// Zoom functionality.
			if (Main.mapFullscreen)
			{
				float num7 = (float)(PlayerInput.ScrollWheelDelta / 120);
				if (PlayerInput.UsingGamepad)
				{
					num7 += (float)(PlayerInput.Triggers.Current.HotbarPlus.ToInt() - PlayerInput.Triggers.Current.HotbarMinus.ToInt()) * 0.1f;
				}
				if (Main.LocalPlayer.mouseInterface)
					num7 = 0;
				Main.mapFullscreenScale *= 1f + num7 * 0.3f;
			}
			Main.SettingDontScaleMainMenuUp = true;

			// DrawToMap is pretty expensive, try draw only if data
			// DrawToMap_Section might be even better, only updates some sections? Vanilla code limits to 5ms 
			if (Main.loadMap /*|| WorldGenPreviewerModWorld.sections.Count >= 300 || WorldGenPreviewerModWorld.contents*/) {
				WorldGenPreviewerModWorld.sections.Clear();
				WorldGenPreviewerModWorld.contents = false;
				Main.spriteBatch.End();
				// TODO: Look into texture contents lost on resize issue.
				drawToMap.Invoke(Main.instance, null); // Draw to the map texture.
				Main.spriteBatch.Begin();
			}

			Stopwatch stopwatch2 = new Stopwatch();
			stopwatch2.Start();
			while (stopwatch2.ElapsedMilliseconds < 5 && WorldGenPreviewerModWorld.sections.TryDequeue(out Point section)) {
				//			if (WorldGenPreviewerModWorld.sections.TryDequeue(out Point section)) {
				Main.spriteBatch.End();
				// TODO: Look into texture contents lost on resize issue.
				drawToMap_Section.Invoke(Main.instance, new object[] { section.X, section.Y });
				Main.spriteBatch.Begin();
			}

			drawMap.Invoke(Main.instance, new object[] { new GameTime() }); // Draws map texture to screen. Also draws Tooltips.

			//int drawX = (Main.screenWidth / 2) - playTexture.Width + 10;// 100;
			//int drawY = 180;// Main.screenHeight - 40;
			//				//int num139 = 0;
			//int num140 = 130;
			//if (Main.mouseX >= drawX && Main.mouseX <= drawX + 32 && Main.mouseY >= drawY && Main.mouseY <= drawY + 30)
			//{
			//	num140 = 255;
			//	//num139 += 4;
			//	//Main.player[Main.myPlayer].mouseInterface = true;
			//	if (Main.mouseLeft && Main.mouseLeftRelease)
			//	{
			//		Main.PlaySound(10, -1, -1, 1);
			//		Main.mapFullscreen = false;
			//		//throw new Exception("World Gen canceled by user click.");  breaks, not caught.

			//		WorldGenPreviewerModWorld.continueWorldGen2 = !WorldGenPreviewerModWorld.continueWorldGen2;
			//	}
			//}
			//Texture2D texture = WorldGenPreviewerModWorld.continueWorldGen2 ? playTexture : pauseTexture;
			//Main.spriteBatch.Draw(Main.magicPixel, new Vector2((float)drawX, (float)drawY), new Microsoft.Xna.Framework.Rectangle?(new Microsoft.Xna.Framework.Rectangle(0, 0, texture.Width, texture.Height)), Color.Green, 0f, default(Vector2), 1f, SpriteEffects.None, 0f);
			//Main.spriteBatch.Draw(texture, new Vector2((float)drawX, (float)drawY), new Microsoft.Xna.Framework.Rectangle?(new Microsoft.Xna.Framework.Rectangle(0, 0, texture.Width, texture.Height)), new Microsoft.Xna.Framework.Color(num140, num140, num140, num140), 0f, default(Vector2), 1f, SpriteEffects.None, 0f);



			//float progress = (float)genprogress.GetValue(_progress);

			//if(scanprogress != progress)

			//if (progress != oldTotalProgress)
			//{
			//oldTotalProgress = progress;
			//int x = Main.rand.Next(Main.mapMaxX);
			//for (int i = 0; i < Main.maxTilesX; i++)

			// TODO: Do this in a different thread.
			//int ScanLineX = 0;
			//for (int i = ScanLineX; i < ScanLineX + 300; i++) {
			//	for (int j = 0; j < Main.maxTilesY; j++) {
			//		if (WorldGen.InWorld(i, j) && Main.Map.UpdateType(i, j))
			//			Main.Map.Update(i, j, 255);
			//	}
			//}
			////	}
			//ScanLineX += 300;
			//WorldGenPreviewerModWorld.contents = true;
			//if (ScanLineX > Main.maxTilesX) {
			//	ScanLineX = 0;
			//}


			// need exact coords. jerky
			//	Main.spriteBatch.Draw(Main.magicPixel, new Vector2(((float)ScanLineX / Main.maxTilesX) * Main.screenWidth, 0), new Microsoft.Xna.Framework.Rectangle?(new Microsoft.Xna.Framework.Rectangle(0, 0, 10, 500)), Color.Green, 0f, default(Vector2), 1f, SpriteEffects.None, 0f);


			//	spriteBatch.DrawString(WorldGenPreviewer.tiny, "This is tiny text", new Vector2(300,300), Color.White);


			float offscreenXMin = 10f;
			float offscreenYMin = 10f;
			//float num8 = (float)(Main.maxTilesX - 10);
			//float num9 = (float)(Main.maxTilesY - 10);

			float num20 = Main.mapFullscreenPos.X;// does it zoom into cursor or center.
			float num21 = Main.mapFullscreenPos.Y;
			num20 *= Main.mapFullscreenScale;
			num21 *= Main.mapFullscreenScale;
			float panX = -num20 + (float)(Main.screenWidth / 2);
			float num2 = -num21 + (float)(Main.screenHeight / 2);
			panX += offscreenXMin * Main.mapFullscreenScale;
			num2 += offscreenYMin * Main.mapFullscreenScale;

			int tileX = (int)((-panX + (float)Main.mouseX) / Main.mapFullscreenScale + offscreenXMin);
			int tileY = (int)((-num2 + (float)Main.mouseY) / Main.mapFullscreenScale + offscreenYMin);
			if (WorldGen.InWorld(tileX, tileY, 10) && Main.tile[tileX, tileY].HasTile)
			{
				int tileType = Main.tile[tileX, tileY].TileType;
				string tileName = Lang._mapLegendCache.FromTile(Main.Map[tileX, tileY], tileX, tileY);
				if (tileName == "") {
					if (tileType < TileID.Count)
						tileName = $"TileID.{TileID.Search.GetName(tileType)} ({tileType})";
					else
						tileName = TileLoader.GetTile(tileType).Name;
				}
				statusLabel.SetText($"Tile: {tileName}");
				if (Main.mouseRight && Main.mouseRightRelease)
				{
					//	WorldGen.ShroomPatch(tileX, tileY);
					//	WorldGen.MakeDungeon(tileX, tileY);
					//	WorldGen.GrowTree(tileX, tileY);
				}
			}

			int scanX = (int)((WorldGenPreviewerModWorld.ScanLineX - offscreenXMin) * Main.mapFullscreenScale + panX);
			int scanY = (int)((10 - offscreenYMin) * Main.mapFullscreenScale + num2);
			int scanHeight = (int)((Main.maxTilesY - 10) * Main.mapFullscreenScale);
			Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(scanX, scanY, 1, scanHeight), Color.LightPink);

			if (WorldGenPreviewerModWorld.showStructures) {
				if (WorldGenPreviewerModWorld.structures_structures != null) {
					for (int i = 0; i < WorldGenPreviewerModWorld.structures_structures.Count; i++) {
						Rectangle item = WorldGenPreviewerModWorld.structures_structures[i];

						//int x = (int)((-num + Main.mouseX) / Main.mapFullscreenScale + offscreenXMin);
						//int y = (int)((-num2 + Main.mouseY) / Main.mapFullscreenScale + offscreenYMin);
						int x = (int)((item.X - offscreenXMin) * Main.mapFullscreenScale + panX);
						int y = (int)((item.Y - offscreenYMin) * Main.mapFullscreenScale + num2);
						int width = (int)(item.Width * Main.mapFullscreenScale);
						int height = (int)(item.Height * Main.mapFullscreenScale);

						// offscreenMin offsets the draw by 10 pixels

						Rectangle drawRectangle = new Rectangle(x, y, width, height);
						Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, drawRectangle, Color.Green * 0.6f);
					}
				}
				if (WorldGenPreviewerModWorld.structures_protectedStructures != null) {
					for (int i = 0; i < WorldGenPreviewerModWorld.structures_protectedStructures.Count; i++) {
						Rectangle item = WorldGenPreviewerModWorld.structures_protectedStructures[i];

						int x = (int)((item.X - offscreenXMin) * Main.mapFullscreenScale + panX);
						int y = (int)((item.Y - offscreenYMin) * Main.mapFullscreenScale + num2);
						int width = (int)(item.Width * Main.mapFullscreenScale);
						int height = (int)(item.Height * Main.mapFullscreenScale);

						Rectangle drawRectangle = new Rectangle(x, y, width, height);
						Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, drawRectangle, Color.Red * 0.6f);
					}
				}
			}
		}
		//float oldTotalProgress = -1f;
		//int ScanLineX = 0;

		//float scanprogress = -1f;


		private void UpdateGamepadSquiggle()
		{
			Vector2 value = new Vector2((float)Math.Cos((double)(Main.GlobalTimeWrappedHourly * 6.28318548f)), (float)Math.Sin((double)(Main.GlobalTimeWrappedHourly * 6.28318548f * 2f))) * new Vector2(30f, 15f) + Vector2.UnitY * 20f;
			UILinkPointNavigator.Points[3000].Unlink();
			UILinkPointNavigator.SetPosition(3000, new Vector2((float)Main.screenWidth, (float)Main.screenHeight) / 2f + value);
		}

		public string GetStatusText()
		{
			return string.Format("{0:0.0%} - " + this._progress.Message + " - {1:0.0%}", this._progress.TotalProgress, this._progress.Value);
		}
	}
}
