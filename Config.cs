
using Terraria.ModLoader;
using Terraria;
using System;
using Terraria.DataStructures;
using System.ComponentModel;
using Newtonsoft.Json;
using Terraria.ModLoader.Config;
using System.Runtime.Serialization;
using Terraria.ID;
using System.Collections.Generic;

namespace WorldGenPreviewer
{
#pragma warning disable 0649
	[Label("Config")]
	internal class Config : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;

		public static Config Instance;

		[Label("Start Worldgen Paused")]
		[Tooltip("Start Worldgen Paused\nUseful if you need to skip or repeat an early world generation pass.")]
		[DefaultValue(false)]
		public bool StartWorldgenPaused { get; set; }
	}
#pragma warning restore 0649
}