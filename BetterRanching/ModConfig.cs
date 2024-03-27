using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BetterRanching;

internal class ModConfig : INotifyPropertyChanged
{
	private int _bubbleDisplayRange = 3; // nice for only up-close bubbles
	public bool PreventFailedHarvesting { get; set; } = true;
	public bool DisplayHearts { get; set; } = true;
	public bool DisplayProduce { get; set; } = true;
	public bool DisplayFarmAnimalHearts { get; set; } = true;
	public bool DisplayPetHearts { get; set; } = true;
	public bool HideHeartsWhenFriendshipIsMax { get; set; } = false;

	public int BubbleDisplayRange
	{
		get => _bubbleDisplayRange;
		set
		{
			_bubbleDisplayRange = value;
			OnPropertyChanged();
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}