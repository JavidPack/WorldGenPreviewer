using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.WorldBuilding;

namespace WorldGenPreviewer
{
	class UIPassItem : UIElement
	{
		public GenPass pass;

		int order = 0;
		bool complete = false;
		public bool skip = false;
		bool skipped = false;
		int repeatCount = 1;

		UIText uitext;
		UIImageButton close;

		public UIPassItem(int order, GenPass pass, string text, float textScale = 1, bool large = false) {
			this.pass = pass;
			this.order = order;

			Width = StyleDimension.Fill;
			Height.Pixels = 15;

			uitext = new UIText(text, textScale, large);
			uitext.Left.Set(20, 0);
			uitext.OnLeftClick += StopAfterThisPass;
			Append(uitext);

			close = new UIImageButton(WorldGenPreviewer.instance.Assets.Request<Texture2D>("closeButton", AssetRequestMode.ImmediateLoad));
			close.OnLeftClick += RemoveThisPass;
			close.Left.Set(0, 0);
			Append(close);
		}

		private void StopAfterThisPass(UIMouseEvent evt, UIElement listeningElement) {
			if (!complete && !skipped) {
				skip = false;
				uitext.TextColor = Color.White;
				WorldGenPreviewerModSystem.continueWorldGen = true;
				WorldGenPreviewerModSystem.pauseAfterContinue = false;
				WorldGenPreviewerModSystem.pauseAfterPass = pass;
				UIWorldLoadSpecial.instance.statusLabel.SetText($"Status: Pausing after {pass.Name}");
			}
		}

		private void RemoveThisPass(UIMouseEvent evt, UIElement listeningElement) {
			skip = !skip;
			uitext.TextColor = skip ? Color.Gray : Color.White;
		}

		public void Skipped() {
			skipped = true;
			uitext.TextColor = Color.Yellow;
			close.Remove();
		}

		public void Complete() {
			complete = true;
			uitext.TextColor = Color.Red;
			close.Remove();
		}

		public void Repeated() {
			repeatCount++;
			uitext.SetText($"{pass.Name} (x{repeatCount})");
		}

		public override int CompareTo(object obj) {
			UIPassItem other = obj as UIPassItem;
			return order.CompareTo(other.order);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			base.DrawSelf(spriteBatch);
			if (IsMouseHovering) {
				string text;
				if (skipped) {
					text = "Skipped";
				}
				else if (complete) {
					text = "Complete";
				}
				else {
					text = "Click to advance to " + pass.Name;
				}
				UIWorldLoadSpecial.instance.statusLabel.SetText(text);
				UICommon.TooltipMouseText(text);
			}
		}
	}
}
