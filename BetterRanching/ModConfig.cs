namespace BetterRanching
{
	internal class ModConfig
	{
		public bool PreventFailedHarvesting { get; set; } = true;
		public bool DisplayHearts { get; set; } = true;
		public bool DisplayProduce { get; set; } = true;
		public bool DisplayFarmAnimalHearts { get; set; } = true;
		public bool DisplayPetHearts { get; set; } = true;
		public bool HideHeartsWhenFriendshipIsMax { get; set; } = false;
	}
}