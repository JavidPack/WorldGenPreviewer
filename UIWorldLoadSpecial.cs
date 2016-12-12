using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;
using Terraria.UI.Gamepad;
using Terraria.World.Generation;

using Terraria;
using Terraria.ModLoader;
using Terraria.Map;
using System.IO;
using System.Reflection;

namespace WorldGenPreviewer
{
	internal class UIWorldLoadSpecial : UIState
	{
		private UIGenProgressBar _progressBar = new UIGenProgressBar();
		private UIHeader _progressMessage = new UIHeader();
		private GenerationProgress _progress;

		public MethodInfo drawMap;
		public MethodInfo drawToMap;
		public FieldInfo genprogress;
		public Texture2D menuTexture;
		public Texture2D previousTexture;
		public Texture2D playTexture;
		public Texture2D pauseTexture;
		public Texture2D nextTexture;

		public UIPanel buttonPanel;
		public UIPanel passesPanel;
		public UIList passesList;
		public UIImageButton menuButton;
		public UIImageButton previousButton;
		public UIImageButton playButton;
		public UIImageButton pauseButton;
		public UIImageButton nextButton;
		public UIText statusLabel;

		public static UIWorldLoadSpecial instance;

		float spacing = 8f;

		public UIWorldLoadSpecial(GenerationProgress progress, Mod mod)
		{
			instance = this;
			menuTexture = mod.GetTexture("menu");
			previousTexture = mod.GetTexture("previous");
			playTexture = mod.GetTexture("play");
			pauseTexture = mod.GetTexture("pause");
			nextTexture = mod.GetTexture("next");

			menuButton = new UIImageButton(menuTexture);
			previousButton = new UIImageButton(previousTexture);
			playButton = new UIImageButton(playTexture);
			pauseButton = new UIImageButton(pauseTexture);
			nextButton = new UIImageButton(nextTexture);

			passesPanel = new UIPanel();
			passesPanel.SetPadding(3);
			passesPanel.Left.Pixels = listHidden ? 170 : 0;
			passesPanel.HAlign = 1f;
			passesPanel.Top.Set(0f, 0f);
			passesPanel.Width.Set(170f, 0f);
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

		public int SortMethod2(UIElement item1, UIElement item2)
		{
			return item1.CompareTo(item2);
		}

		private void PauseClick(UIMouseEvent evt, UIElement listeningElement)
		{
			WorldGenPreviewerModWorld.continueWorldGen = false;
			WorldGenPreviewerModWorld.pauseAfterContinue = false;
			statusLabel.SetText("Status: Pausing...");
			//Main.PlaySound(10);
		}

		private void PlayClick(UIMouseEvent evt, UIElement listeningElement)
		{
			//Main.PlaySound(10);
			WorldGenPreviewerModWorld.continueWorldGen = true;
			WorldGenPreviewerModWorld.pauseAfterContinue = false;
			statusLabel.SetText("Status: Normal");
		}

		bool listHidden = true;
		private void MenuClick(UIMouseEvent evt, UIElement listeningElement)
		{
			//Main.PlaySound(10, -1, -1, 1);
			//ErrorLogger.Log("MENU");
			//statusLabel.SetText("Status: ??...");
			listHidden = !listHidden;
			passesPanel.Left.Pixels = listHidden ? 170 : 0;
			passesPanel.Recalculate();
		}

		private void PreviousClick(UIMouseEvent evt, UIElement listeningElement)
		{
			//Main.PlaySound(10, -1, -1, 1);
			statusLabel.SetText("Status: Waiting to do this step again...");
			WorldGenPreviewerModWorld.repeatPreviousStep = true;
			WorldGenPreviewerModWorld.continueWorldGen = false;
			WorldGenPreviewerModWorld.pauseAfterContinue = false;
		}

		private void NextClick(UIMouseEvent evt, UIElement listeningElement)
		{
			//Main.PlaySound(10, -1, -1, 1);
			WorldGenPreviewerModWorld.continueWorldGen = true; // so paused will break.
			WorldGenPreviewerModWorld.pauseAfterContinue = true;
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
			genprogress = typeof(GenerationProgress).GetField("_totalProgress", BindingFlags.Instance | BindingFlags.NonPublic);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
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
				Main.mapFullscreenScale *= 1f + num7 * 0.3f;
			}

			Main.spriteBatch.End();
			drawToMap.Invoke(Main.instance, null); // Draw to the map texture.
			Main.spriteBatch.Begin();
			drawMap.Invoke(Main.instance, null); // Draws map texture to screen. Also draws Tooltips.

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
			for (int i = ScanLineX; i < ScanLineX + 300; i++)
			{
				for (int j = 0; j < Main.maxTilesY; j++)
				{
					if (WorldGen.InWorld(i, j) && Main.Map.UpdateType(i, j))
						Main.Map.Update(i, j, 255);
				}
			}
			//	}
			ScanLineX += 300;
			if (ScanLineX > Main.maxTilesX)
			{
				ScanLineX = 0;
			}

			// need exact coords. jerky
			//	Main.spriteBatch.Draw(Main.magicPixel, new Vector2(((float)ScanLineX / Main.maxTilesX) * Main.screenWidth, 0), new Microsoft.Xna.Framework.Rectangle?(new Microsoft.Xna.Framework.Rectangle(0, 0, 10, 500)), Color.Green, 0f, default(Vector2), 1f, SpriteEffects.None, 0f);


		//	spriteBatch.DrawString(WorldGenPreviewer.tiny, "This is tiny text", new Vector2(300,300), Color.White);


			float offscreenXMin = 10f;
			float offscreenYMin = 10f;
			//float num8 = (float)(Main.maxTilesX - 10);
			//float num9 = (float)(Main.maxTilesY - 10);

			float num20 = Main.mapFullscreenPos.X;
			float num21 = Main.mapFullscreenPos.Y;
			num20 *= Main.mapFullscreenScale;
			num21 *= Main.mapFullscreenScale;
			float num = -num20 + (float)(Main.screenWidth / 2);
			float num2 = -num21 + (float)(Main.screenHeight / 2);
			num += offscreenXMin * Main.mapFullscreenScale;
			num2 += offscreenYMin * Main.mapFullscreenScale;

			int tileX = (int)((-num + (float)Main.mouseX) / Main.mapFullscreenScale + offscreenXMin);
			int tileY = (int)((-num2 + (float)Main.mouseY) / Main.mapFullscreenScale + offscreenYMin);
			if(WorldGen.InWorld(tileX, tileY, 10))
			{
				statusLabel.SetText("TileID: " + Main.tile[tileX, tileY].type);
				if (Main.mouseRight && Main.mouseRightRelease)
				{
				//	WorldGen.ShroomPatch(tileX, tileY);
				//	WorldGen.MakeDungeon(tileX, tileY);
				//	WorldGen.GrowTree(tileX, tileY);
				}
			}
		}
		//float oldTotalProgress = -1f;
		int ScanLineX = 0;

		//float scanprogress = -1f;


		private void UpdateGamepadSquiggle()
		{
			Vector2 value = new Vector2((float)Math.Cos((double)(Main.GlobalTime * 6.28318548f)), (float)Math.Sin((double)(Main.GlobalTime * 6.28318548f * 2f))) * new Vector2(30f, 15f) + Vector2.UnitY * 20f;
			UILinkPointNavigator.Points[3000].Unlink();
			UILinkPointNavigator.SetPosition(3000, new Vector2((float)Main.screenWidth, (float)Main.screenHeight) / 2f + value);
		}

		public string GetStatusText()
		{
			return string.Format("{0:0.0%} - " + this._progress.Message + " - {1:0.0%}", this._progress.TotalProgress, this._progress.Value);
		}
	}
}
