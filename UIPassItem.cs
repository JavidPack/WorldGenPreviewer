using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;
using Terraria.UI.Gamepad;

using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;
using Terraria.GameContent.Generation;
using System.Reflection;
using Terraria.WorldBuilding;
using ReLogic.Content;
using Microsoft.Xna.Framework.Graphics;
using Terraria.IO;

namespace WorldGenPreviewer
{
	class UIPassItem : UIElement
	{
		int order = 0;
		bool complete = false;
		public GenPass pass;
		UIText uitext;
		public UIPassItem(int order, GenPass pass, string text, float textScale = 1, bool large = false)
		{
			this.pass = pass;
			this.order = order;
			//TextColor = Color.Blue;

			Width = StyleDimension.Fill;
			Height.Pixels = 15;

			uitext = new UIText(text, textScale, large);
			uitext.Left.Set(20, 0);
			uitext.OnLeftClick += StopAfterThisPass;
			Append(uitext);

			UIImageButton close = new UIImageButton(WorldGenPreviewer.instance.Assets.Request<Texture2D>("closeButton", AssetRequestMode.ImmediateLoad));
			close.OnLeftClick += RemoveThisPass;
			//close.Left.Set(-45, 1);
			close.Left.Set(0, 0);
			Append(close);
		}

		private void StopAfterThisPass(UIMouseEvent evt, UIElement listeningElement) {
			if (!complete) {
				WorldGenPreviewerModWorld.continueWorldGen = true;
				WorldGenPreviewerModWorld.pauseAfterContinue = false;
				WorldGenPreviewerModWorld.pauseAfterPass = pass;
				UIWorldLoadSpecial.instance.statusLabel.SetText($"Status: Pausing after {pass.Name}");
			}
		}

		private void RemoveThisPass(UIMouseEvent evt, UIElement listeningElement)
		{
			PassLegacy passLegacy = pass as PassLegacy;
			if (passLegacy != null)
			{
				//private WorldGenLegacyMethod _method;
				FieldInfo methodFieldInfo = typeof(PassLegacy).GetField("_method", BindingFlags.Instance | BindingFlags.NonPublic);
				methodFieldInfo.SetValue(passLegacy, (WorldGenLegacyMethod) delegate (GenerationProgress progress, GameConfiguration config) { });
			}
			UIWorldLoadSpecial.instance.passesList.Remove(this);
		}

		public void Complete()
		{
			complete = true;
			uitext.TextColor = Color.Red;
			//Recalculate();
		}

		public override int CompareTo(object obj)
		{
			UIPassItem other = obj as UIPassItem;
			return order.CompareTo(other.order);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			base.DrawSelf(spriteBatch);
			if (IsMouseHovering) {
				UIWorldLoadSpecial.instance.statusLabel.SetText("Click to advance to " + pass.Name);
			}
		}
	}
}
