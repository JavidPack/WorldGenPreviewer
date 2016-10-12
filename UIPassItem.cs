using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;
using Terraria.UI.Gamepad;
using Terraria.World.Generation;

using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace WorldGenPreviewer
{
	class UIPassItem : UIText
	{
		int order = 0;
		bool complete = false;
		public GenPass pass;
		public UIPassItem(int order, GenPass pass, string text, float textScale = 1, bool large = false) : base(text, textScale, large)
		{
			this.pass = pass;
			this.order = order;
			//TextColor = Color.Blue;
		}

		public void Complete()
		{
			complete = true;
			TextColor = Color.Red;
			//Recalculate();
		}

		public override int CompareTo(object obj)
		{
			UIPassItem other = obj as UIPassItem;
			return order.CompareTo(other.order);
		}
	}
}
