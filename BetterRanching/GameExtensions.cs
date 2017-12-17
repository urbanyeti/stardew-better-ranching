using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterRanching
{
	public static class GameExtensions
	{
		public static bool CanBeRanched(this FarmAnimal animal, string toolName)
		{
			return animal.currentProduce > 0
				&& (animal.age >= animal.ageWhenMature
				&& animal.toolUsedForHarvest.Equals(toolName));
		}

		public static FarmAnimal GetSelectedAnimal(this Farm farm, Rectangle rectangle)
		{
			foreach (FarmAnimal farmAnimal in farm.animals.Values)
			{
				if (farmAnimal.GetBoundingBox().Intersects(rectangle))
				{
					return farmAnimal;
				}
			}
			return null;
		}

		public static FarmAnimal GetSelectedAnimal(this AnimalHouse house, Rectangle rectangle)
		{
			foreach (FarmAnimal farmAnimal in house.animals.Values)
			{
				if (farmAnimal.GetBoundingBox().Intersects(rectangle))
				{
					return farmAnimal;
				}
			}
			return null;
		}

		public static void OverwriteState(this Game1 game, object state, string message = null)
		{
			if (state is MouseState)
			{
				if (message != null && Game1.oldMouseState.LeftButton == ButtonState.Released)
				{
					Game1.showRedMessage(message);
				}
				Game1.oldMouseState = (MouseState)state;
			}
			else if (state is GamePadState)
			{
				if (message != null)
				{
					Game1.showRedMessage(message);
				}
				Game1.oldPadState = (GamePadState)state;
			}
		}

		public static bool HoldingOverridableTool()
		{
			return Game1.player.CurrentTool?.Name == GameConstants.Tools.MilkPail || Game1.player.CurrentTool?.Name == GameConstants.Tools.Shears;
		}

		public static bool IsClickableArea()
		{
			if (Game1.activeClickableMenu != null)
			{
				return false;
			}
			Point newPosition = Game1.getMousePosition();
			foreach (var screen in Game1.onScreenMenus)
			{
				if (screen.isWithinBounds(newPosition.X, newPosition.Y))
				{
					return false;
				}
			}
			return true;
		}
	}
}
