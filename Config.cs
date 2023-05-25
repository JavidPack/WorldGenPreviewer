using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WorldGenPreviewer
{
#pragma warning disable 0649
	internal class Config : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;

		public static Config Instance;

		[DefaultValue(false)]
		public bool StartWorldgenPaused { get; set; }
	}
#pragma warning restore 0649
}