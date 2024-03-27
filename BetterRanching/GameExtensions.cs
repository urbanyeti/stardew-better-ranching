using System.Linq;
using StardewModdingAPI;
using StardewValley;

namespace BetterRanching;

public static class GameExtensions
{
	public static bool CanBeRanched(this FarmAnimal animal, string toolName)
	{
		return animal.currentProduce.Value != null && animal.isAdult() &&
		       animal.GetAnimalData().HarvestTool == toolName;
	}

	public static void OverwriteState(this IInputHelper input, SButton button, string message = null)
	{
		if (message != null)
			Game1.showRedMessage(message);
		input.Suppress(button);
	}

	public static bool HoldingOverridableTool()
	{
		return Game1.player.CurrentTool?.Name is GameConstants.Tools.MilkPail or GameConstants.Tools.Shears;
	}

	public static bool IsClickableArea()
	{
		if (Game1.activeClickableMenu != null) return false;

		var (x, y) = Game1.getMousePosition();
		return Game1.onScreenMenus.All(screen => !screen.isWithinBounds(x, y));
	}
}