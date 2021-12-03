using System;
using System.Collections.Generic;
using System.Linq;
using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;

namespace BetterRanching
{
	/// <summary>The mod entry class loaded by SMAPI.</summary>
	public class BetterRanching : Mod
	{
		private FarmAnimal AnimalBeingRanched { get; set; }
		private ModConfig Config { get; set; }
		private BetterRanchingApi Api { get; set; }

		/// <summary>The mod entry point, called after the mod is first loaded.</summary>
		/// <param name="helper">Provides simplified APIs for writing mods.</param>
		public override void Entry(IModHelper helper)
		{
			Config = helper.ReadConfig<ModConfig>();
			Api = new BetterRanchingApi(Config);
			helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
			helper.Events.Display.RenderedWorld += OnRenderedWorld;
			helper.Events.Input.ButtonPressed += OnButtonPressed;
			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
		}

		public override object GetApi()
		{
			return new BetterRanchingApi(Config);
		}

		/// <summary>
		/// Raised after the game is launched, right before the first update tick. This happens once per game session (unrelated to loading saves). All mods are loaded and initialised at this point, so this is a good time to set up mod integrations. 
		/// </summary>
		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			// get Generic Mod Config Menu's API (if it's installed)
			var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
			if (configMenu is null)
			{
				return;
			}

			// register mod
			configMenu.Register(
				mod: ModManifest,
				reset: () => Config = new ModConfig(),
				save: () => Helper.WriteConfig(Config)
			);

			// add some config options
			configMenu.AddBoolOption(
				mod: ModManifest,
				name: () => "Prevent Failed Harvesting",
				tooltip: () => "Prevents the failed milking/shearing animation and sound effect if no valid animal is selected. Note: Disable this if using the 'Tap-to-move & Auto-Attack' control scheme on Android.",
				getValue: () => Config.PreventFailedHarvesting,
				setValue: value => Config.PreventFailedHarvesting = value
			);
			configMenu.AddBoolOption(
				mod: ModManifest,
				name: () => "Display Hearts",
				tooltip: () => "Display hearts above animals that have not yet been petted.",
				getValue: () => Config.DisplayHearts,
				setValue: value => Config.DisplayHearts = value
			);
			configMenu.AddBoolOption(
				mod: ModManifest,
				name: () => "Display Produce",
				tooltip: () => "Displays produce above animal if it is ready to be harvested.",
				getValue: () => Config.DisplayProduce,
				setValue: value => Config.DisplayProduce = value
			);
		}

		/// <summary>Raised after the game state is updated (≈60 times per second).</summary>
		/// <param name="sender">The event sender.</param>
		/// <param name="e">The event data.</param>
		private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
		{
			//Override auto-click on hold for milk pail
			if (Config.PreventFailedHarvesting && GameExtensions.HoldingOverridableTool() && GameExtensions.IsClickableArea() && Game1.mouseClickPolling > 50)
			{
				Game1.mouseClickPolling = 50;
			}

			if (!Game1.player.UsingTool && AnimalBeingRanched != null)
			{
				AnimalBeingRanched = null;
			}
		}

		/// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
		/// <param name="sender">The event sender.</param>
		/// <param name="e">The event data.</param>
		private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			if (!Context.IsWorldReady || Game1.currentLocation == null || !Game1.currentLocation.IsFarm)
			{
				return;
			}

			if (e.Button.IsUseToolButton() && Config.PreventFailedHarvesting && GameExtensions.HoldingOverridableTool() && GameExtensions.IsClickableArea())
			{
				Farmer who = Game1.player;

				if (e.Button == SButton.MouseLeft)
				{
					Vector2 pos = Game1.mouseCursorTransparency == 0.0 ? Game1.player.GetToolLocation() : new Vector2(Game1.getOldMouseX() + @Game1.viewport.X, Game1.getOldMouseY() + Game1.viewport.Y);

					if (Utility.withinRadiusOfPlayer((int)pos.X, (int)pos.Y, 1, Game1.player)
						&& (Math.Abs(pos.X - Game1.player.getStandingX()) >= (double)(Game1.tileSize / 2) || Math.Abs(pos.Y - Game1.player.getStandingY()) >= (double)(Game1.tileSize / 2)))
					{
						if (who.CanMove && Game1.mouseCursorTransparency != 0.0 && !Game1.isAnyGamePadButtonBeingHeld())
						{
							who.Halt();
							who.faceGeneralDirection(new Vector2((int)pos.X, (int)pos.Y), 0);
						}
					}
					OverrideRanching(Game1.currentLocation, (int)who.GetToolLocation().X, (int)who.GetToolLocation().Y, who, e.Button, who.CurrentTool?.Name);
				}
				else
				{
					OverrideRanching(Game1.currentLocation, (int)who.GetToolLocation().X, (int)who.GetToolLocation().Y, who, e.Button, who.CurrentTool?.Name);
				}
			}
		}

		private void OverrideRanching(GameLocation currentLocation, int x, int y, Farmer who, SButton button, string toolName)
		{
			AnimalBeingRanched = null;
			FarmAnimal animal = null;
			string ranchAction = string.Empty;
			string ranchProduct = string.Empty;

			if (toolName == null)
			{
				return;
			}

			switch (toolName)
			{
				case GameConstants.Tools.MilkPail:
					ranchAction = "Milking";
					ranchProduct = "milk";
					break;
				case GameConstants.Tools.Shears:
					ranchAction = "Shearing";
					ranchProduct = "wool";
					break;
			}
			var rectangle = new Rectangle(x - (Game1.tileSize / 2), y - (Game1.tileSize / 2), Game1.tileSize, Game1.tileSize);

			if (currentLocation is AnimalHouse animalHouse)
			{
				animal = animalHouse.GetSelectedAnimal(rectangle);
			}
			else if (currentLocation.IsFarm && currentLocation.IsOutdoors)
			{
				animal = ((Farm)currentLocation).GetSelectedAnimal(rectangle);
			}

			if (animal == null)
			{
				Helper.Input.OverwriteState(button, $"{ranchAction} Failed");
				return;
			}

			if (animal.CanBeRanched(toolName))
			{
				if (who.couldInventoryAcceptThisObject(animal.currentProduce.Value, 1, 0))
				{
					AnimalBeingRanched = animal;
					return;
				}
				else
				{
					Helper.Input.OverwriteState(button, "Inventory Full");
				}
			}
			else if (animal?.isBaby() == true && animal.toolUsedForHarvest.Equals(toolName))
			{
				Helper.Input.OverwriteState(button);
				DelayedAction.showDialogueAfterDelay($"Baby {animal.Name} will produce {ranchProduct} in {animal.ageWhenMature.Value - animal.age.Value} days.", 0);
			}
			else
			{
				Helper.Input.OverwriteState(button, $"{ranchAction} Failed");
			}
		}

		private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
		{
			if (!Context.IsWorldReady || !Game1.currentLocation.IsFarm || Game1.eventUp)
			{
				return;
			}

			GameLocation currentLocation = Game1.currentLocation;

			List<FarmAnimal> farmAnimalList = new List<FarmAnimal>();
			if (currentLocation is AnimalHouse animalHouse)
			{
				farmAnimalList = animalHouse.animals.Values.ToList();
			}
			else if (currentLocation is Farm farm)
			{
				farmAnimalList = farm.animals.Values.ToList();
			}

			foreach (FarmAnimal farmAnimal in farmAnimalList)
			{
				Api.DrawItemBubble(Game1.spriteBatch, farmAnimal, AnimalBeingRanched == farmAnimal);
			}


			if (Config.DisplayHearts && !Game1.eventUp)
			{
				foreach (NPC npc in currentLocation.characters)
				{
					if (npc is Pet pet)
					{
						Api.DrawHeartBubble(Game1.spriteBatch, pet, () => !(pet.lastPetDay.Values.Any(day => day == Game1.Date.TotalDays)));
					}
				}
			}
		}
	}
}
