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
		public Texture2D playTexture;
		public Texture2D pauseTexture;

		public UIWorldLoadSpecial(GenerationProgress progress, Texture2D texture2D1, Texture2D texture2D2)
		{
			playTexture = texture2D1;
			pauseTexture = texture2D2;

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
			drawToMap.Invoke(Main.instance, null);
			Main.spriteBatch.Begin();
			drawMap.Invoke(Main.instance, null);

			int drawX = (Main.screenWidth / 2) -  playTexture.Width + 10;// 100;
			int drawY = 180;// Main.screenHeight - 40;
			//int num139 = 0;
			int num140 = 130;
			if (Main.mouseX >= drawX && Main.mouseX <= drawX + 32 && Main.mouseY >= drawY && Main.mouseY <= drawY + 30)
			{
				num140 = 255;
				//num139 += 4;
				//Main.player[Main.myPlayer].mouseInterface = true;
				if (Main.mouseLeft && Main.mouseLeftRelease)
				{
					Main.PlaySound(10, -1, -1, 1);
					Main.mapFullscreen = false;
					//throw new Exception("World Gen canceled by user click.");  breaks, not caught.

					WorldGenPreviewerModWorld.continueWorldGen2 = !WorldGenPreviewerModWorld.continueWorldGen2;
				}
			}
			Texture2D texture = WorldGenPreviewerModWorld.continueWorldGen2 ? playTexture : pauseTexture;
			Main.spriteBatch.Draw(Main.magicPixel, new Vector2((float)drawX, (float)drawY), new Microsoft.Xna.Framework.Rectangle?(new Microsoft.Xna.Framework.Rectangle(0, 0, texture.Width, texture.Height)), Color.Green, 0f, default(Vector2), 1f, SpriteEffects.None, 0f);
			Main.spriteBatch.Draw(texture, new Vector2((float)drawX, (float)drawY), new Microsoft.Xna.Framework.Rectangle?(new Microsoft.Xna.Framework.Rectangle(0, 0, texture.Width, texture.Height)), new Microsoft.Xna.Framework.Color(num140, num140, num140, num140), 0f, default(Vector2), 1f, SpriteEffects.None, 0f);



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

		}
		//float oldTotalProgress = -1f;
		int ScanLineX = 0;
		private Texture2D texture2D1;
		private Texture2D texture2D2;

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
