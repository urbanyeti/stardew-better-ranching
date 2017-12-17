namespace BetterRanching
{
	internal class Config
	{
		public bool PreventFailedHarvesting { get; internal set; } = true;
		public bool PreventHarvestRepeating { get; internal set; } = true;
		public bool DisplayHearts { get; internal set; } = true;
		public bool DisplayProduce { get; internal set; } = true;
	}
}