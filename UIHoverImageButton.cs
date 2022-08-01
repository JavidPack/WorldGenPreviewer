using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace WorldGenPreviewer
{
	internal class UIHoverImageButton : UIImageButton
	{
		internal Func<string> hoverText;

		public UIHoverImageButton(Asset<Texture2D> texture, Func<string> hoverText) : base(texture)
		{
			this.hoverText = hoverText;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			base.DrawSelf(spriteBatch);
			if (IsMouseHovering)
			{
				Main.hoverItemName = hoverText();
			}
		}
	}
}
