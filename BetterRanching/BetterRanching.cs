using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;

namespace BetterRanching;

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
		helper.Events.GameLoop.GameLaunched += OnGameLaunched;
		helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
		helper.Events.Display.RenderedWorld += OnRenderedWorld;
		helper.Events.Input.ButtonPressed += OnButtonPressed;
	}

	public override object GetApi()
	{
		return new BetterRanchingApi(Config);
	}

	/// <summary>
	///     Raised after the game is launched, right before the first update tick. This happens once per game session
	///     (unrelated to loading saves). All mods are loaded and initialised at this point, so this is a good time to set up
	///     mod integrations.
	/// </summary>
	private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
	{
		var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
		if (configMenu is null) return;

		SetupConfigMenu(configMenu);
	}

	/*********
	 ** Private methods
	 *********/
	/// <summary>Raised after the game state is updated (≈60 times per second).</summary>
	/// <param name="sender">The event sender.</param>
	/// <param name="e">The event data.</param>
	private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
	{
		//Override auto-click on hold for milk pail
		if (Config.PreventFailedHarvesting && GameExtensions.HoldingOverridableTool() &&
		    GameExtensions.IsClickableArea() && Game1.mouseClickPolling > 50)
			Game1.mouseClickPolling = 50;

		if (!Game1.player.UsingTool && AnimalBeingRanched != null) AnimalBeingRanched = null;
	}

	/// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
	/// <param name="sender">The event sender.</param>
	/// <param name="e">The event data.</param>
	private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
	{
		// ignore if player hasn't loaded a save yet
		if (!Context.IsWorldReady) return;

		if (!e.Button.IsUseToolButton() || !Config.PreventFailedHarvesting ||
		    !GameExtensions.HoldingOverridableTool() || !GameExtensions.IsClickableArea()) return;
		var who = Game1.player;

		var position = !Game1.wasMouseVisibleThisFrame
			? who.GetToolLocation()
			: new Vector2(Game1.getOldMouseX() + Game1.viewport.X, Game1.getOldMouseY() + Game1.viewport.Y);
		var toolLocation = who.GetToolLocation(position);
		who.FacingDirection = who.getGeneralDirectionTowards(new Vector2((int)toolLocation.X, (int)toolLocation.Y));
		who.lastClick = new Vector2((int)position.X, (int)position.Y);

		var toolRect = new Rectangle((int)toolLocation.X - 32, (int)toolLocation.Y - 32, 64, 64);

		AnimalBeingRanched = Utility.GetBestHarvestableFarmAnimal(
			Game1.currentLocation.animals.Values, Game1.player.CurrentTool,
			toolRect);

		OverrideRanching(Game1.currentLocation, (int)who.GetToolLocation().X, (int)who.GetToolLocation().Y, who,
			e.Button, who.CurrentTool?.Name);
	}

	private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
	{
		if (!Context.IsWorldReady || Game1.eventUp) return;

		var farmAnimalList = Game1.currentLocation.animals.Values;

		foreach (var farmAnimal in farmAnimalList)
		{
			if (ShouldRenderBubble(farmAnimal)) continue;
			Api.DrawItemBubble(Game1.spriteBatch, farmAnimal, AnimalBeingRanched == farmAnimal);
		}

		if (!Config.DisplayPetHearts || Game1.eventUp) return;

		foreach (var npc in Game1.currentLocation.characters)
			if (npc is Pet pet)
			{
				if (ShouldRenderBubble(pet)) continue;
				Api.DrawHeartBubble(Game1.spriteBatch, pet,
					() => !pet.lastPetDay.TryGetValue(Game1.player.UniqueMultiplayerID, out var lastValue) ||
					      lastValue != Game1.Date.TotalDays);
			}
	}

	private bool ShouldRenderBubble(Character animal)
	{
		// credit goes to Ophaneom and Mini-Bars for this method of range detection
		var npcX = animal.Position.X;
		var npcY = animal.Position.Y;
		var playerX = Game1.player.Position.X;
		var playerY = Game1.player.Position.Y;
		var range = Config.BubbleDisplayRange * 20 * Game1.pixelZoom; // gotta love weird math...

		return npcX >= playerX + range || npcX <= playerX - range || npcY >= playerY + range || npcY <= playerY - range;
	}

	private void OverrideRanching(GameLocation currentLocation, int x, int y, Farmer who, SButton button,
		string toolName)
	{
		AnimalBeingRanched = null;
		FarmAnimal animal = null;
		var ranchAction = string.Empty;
		var ranchActionPresent = string.Empty;
		var ranchProduct = string.Empty;

		if (toolName == null) return;

		switch (toolName)
		{
			case GameConstants.Tools.MilkPail:
				ranchAction = Helper.Translation.Get("action.unable.milk");
				ranchActionPresent = Helper.Translation.Get("action.out_of_range.milk");
				ranchProduct = Helper.Translation.Get("product.milk");
				break;
			case GameConstants.Tools.Shears:
				ranchAction = Helper.Translation.Get("action.unable.shear");
				ranchActionPresent = Helper.Translation.Get("action.out_of_range.shear");
				ranchProduct = Helper.Translation.Get("product.wool");
				break;
		}

		animal = Utility.GetBestHarvestableFarmAnimal(toolRect: new Rectangle(x - 32, y - 32, 64, 64),
			animals: currentLocation.animals.Values, tool: who.CurrentTool);

		if (animal == null)
		{
			Helper.Input.OverwriteState(button,
				Helper.Translation.Get("notification.out_of_range", new { ranchActionPresent }));
			return;
		}

		var animalData = animal.GetAnimalData();

		if (animal.CanBeRanched(toolName))
		{
			if (who.couldInventoryAcceptThisItem(animal.currentProduce.Value,
				    !animal.hasEatenAnimalCracker.Value ? 1 : 2, animal.produceQuality.Value))
				AnimalBeingRanched = animal;
			else
				Helper.Input.OverwriteState(button, Helper.Translation.Get("notification.inventory_full"));
		}
		else if (animal.isBaby() && animalData.HarvestTool == toolName)
		{
			Helper.Input.OverwriteState(button);
			DelayedAction.showDialogueAfterDelay(
				Helper.Translation.Get("notification.util_action",
					new { animal.Name, Product = ranchProduct, Days = animalData.DaysToMature }),
				0);
		}
		else
		{
			Helper.Input.OverwriteState(button,
				Helper.Translation.Get("notification.unable", new { Action = ranchAction }));
		}
	}

	private void SetupConfigMenu(IGenericModConfigMenuApi configMenu)
	{
		// register mod
		configMenu.Register(
			ModManifest,
			() => Config = new ModConfig(),
			() => Helper.WriteConfig(Config)
		);

		// add some config options
		configMenu.AddBoolOption(
			ModManifest,
			name: () => Helper.Translation.Get("config.option.failed_harvest.name"),
			tooltip: () => Helper.Translation.Get("config.option.failed_harvest.description"),
			getValue: () => Config.PreventFailedHarvesting,
			setValue: value => Config.PreventFailedHarvesting = value
		);

		configMenu.AddBoolOption(
			ModManifest,
			name: () => Helper.Translation.Get("config.option.animal_produce.name"),
			tooltip: () => Helper.Translation.Get("config.option.animal_produce.description"),
			getValue: () => Config.DisplayProduce,
			setValue: value => Config.DisplayProduce = value
		);

		configMenu.AddBoolOption(
			ModManifest,
			name: () => Helper.Translation.Get("config.option.hearts_animal.name"),
			tooltip: () => Helper.Translation.Get("config.option.hearts_animal.description"),
			getValue: () => Config.DisplayFarmAnimalHearts,
			setValue: value => Config.DisplayFarmAnimalHearts = value
		);

		configMenu.AddBoolOption(
			ModManifest,
			name: () => Helper.Translation.Get("config.option.hearts_pet.name"),
			tooltip: () => Helper.Translation.Get("config.option.hearts_pet.description"),
			getValue: () => Config.DisplayPetHearts,
			setValue: value => Config.DisplayPetHearts = value
		);

		configMenu.AddBoolOption(
			ModManifest,
			name: () => Helper.Translation.Get("config.option.hearts_enabled.name"),
			tooltip: () => Helper.Translation.Get("config.option.hearts_enabled.description"),
			getValue: () => Config.DisplayHearts,
			setValue: value => Config.DisplayHearts = value
		);

		configMenu.AddBoolOption(
			ModManifest,
			name: () => Helper.Translation.Get("config.option.hearts_max_friendship.name"),
			tooltip: () => Helper.Translation.Get("config.option.hearts_max_friendship.description"),
			getValue: () => Config.HideHeartsWhenFriendshipIsMax,
			setValue: value => Config.HideHeartsWhenFriendshipIsMax = value
		);

		configMenu.AddNumberOption(
			ModManifest,
			name: () => Helper.Translation.Get("config.option.bubble_display_range.name"),
			tooltip: () => Helper.Translation.Get("config.option.bubble_display_range.description"),
			interval: 1,
			min: 1,
			max: 50, // should be enough to cover entire visible area on normal zoom
			getValue: () => Config.BubbleDisplayRange,
			setValue: value => Config.BubbleDisplayRange = value
		);
	}
}