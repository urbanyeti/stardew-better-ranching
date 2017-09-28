using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BetterRanching
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private FarmAnimal AnimalBeingRanched { get; set; }
        /*********
        ** Public methods
        *********/
        /// <summary>Initialise the mod.</summary>
        /// <param name="helper">Provides methods for interacting with the mod directory, such as read/writing a config file or custom JSON files.</param>
        public override void Entry(IModHelper helper)
        {
            GameEvents.UpdateTick += this.Event_UpdateTick;
            GraphicsEvents.OnPreRenderHudEvent += this.Event_OnPreRenderHudEvent;
            ControlEvents.MouseChanged += this.Event_MouseChanged;
            ControlEvents.ControllerButtonPressed += this.Event_ControllerButtonPressed;
        }

        private void Event_UpdateTick(object sender, EventArgs e)
        {
            // Override auto-click on hold for milkpail
            if (GameExtensions.ShouldOverride() && Game1.mouseClickPolling > 200)
            {
                Game1.mouseClickPolling = 100;
            }

            if (!Game1.player.UsingTool && AnimalBeingRanched != null)
            {
                AnimalBeingRanched = null;
            }
        }

        private void Event_MouseChanged(object sender, EventArgsMouseStateChanged e)
        {
            bool ignoreClick = false;
            if (!Game1.hasLoadedGame || !Game1.currentLocation.isFarm)
            {
                return;
            }

            if (e.NewState.LeftButton == ButtonState.Pressed)
            {
                var who = Game1.player;

                if (GameExtensions.ShouldOverride(ignoreClick))
                {
                    who.lastClick = new Vector2(e.NewState.X, e.NewState.Y);
                    OverrideRanching(Game1.currentLocation, (int)who.GetToolLocation(ignoreClick).X, (int)who.GetToolLocation(ignoreClick).Y, who, e.NewState, who.CurrentTool?.Name);
                }
            }
        }

        private void Event_ControllerButtonPressed(object sender, EventArgsControllerButtonPressed e)
        {
            bool ignoreClick = true;
            if (!Game1.hasLoadedGame || !Game1.currentLocation.isFarm)
            {
                return;
            }

            if (e.ButtonPressed == Buttons.X)
            {
                var who = Game1.player;

                if (GameExtensions.ShouldOverride(ignoreClick))
                {
                    OverrideRanching(Game1.currentLocation, (int)who.GetToolLocation(ignoreClick).X, (int)who.GetToolLocation(ignoreClick).Y, who
                        , new GamePadState(Game1.oldPadState.ThumbSticks, Game1.oldPadState.Triggers, new GamePadButtons(Buttons.X), Game1.oldPadState.DPad)
                        , who.CurrentTool?.Name);
                }
            }
        }



        private void OverrideRanching(GameLocation currentLocation, int x, int y, StardewValley.Farmer who, object state, string toolName)
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
            var rectangle = new Microsoft.Xna.Framework.Rectangle(x - Game1.tileSize / 2, y - Game1.tileSize / 2, Game1.tileSize, Game1.tileSize);

            if (currentLocation is AnimalHouse)
            {
                animal = ((AnimalHouse)currentLocation).GetSelectedAnimal(rectangle);
            }
            else if (currentLocation.IsFarm && currentLocation.IsOutdoors)
            {
                animal = ((Farm)currentLocation).GetSelectedAnimal(rectangle);
            }

            if (animal == null)
            {
                Game1.game1.OverwriteState(state, $"{ranchAction} Failed");
                return;
            }

            if (animal.CanBeRanched(toolName))
            {
                if (who.couldInventoryAcceptThisObject(animal.currentProduce, 1, 0))
                {
                    AnimalBeingRanched = animal;
                    return;
                }
                else
                {
                    Game1.game1.OverwriteState(state, "Inventory Full");
                }
            }
            else if (animal != null && animal.isBaby() && animal.toolUsedForHarvest.Equals(toolName))
            {
                Game1.game1.OverwriteState(state);
                DelayedAction.showDialogueAfterDelay($"Baby {animal.name} will produce {ranchProduct} in {animal.ageWhenMature - animal.age} days.", 0);
            }
            else
            {
                Game1.game1.OverwriteState(state, $"{ranchAction} Failed");
            }
        }

        private void Event_OnPreRenderHudEvent(object sender, EventArgs e)
        {
            if (!Game1.hasLoadedGame || !Game1.currentLocation.isFarm)
            {
                return;
            }

            GameLocation currentLocation = Game1.currentLocation;

            List<FarmAnimal> farmAnimalList = new List<FarmAnimal>();
            if (currentLocation is AnimalHouse)
            {
                farmAnimalList = ((AnimalHouse)currentLocation).animals.Values.ToList<FarmAnimal>();
            }
            else if (currentLocation is Farm)
            {
                farmAnimalList = ((Farm)currentLocation).animals.Values.ToList<FarmAnimal>();
            }

            foreach (FarmAnimal farmAnimal in farmAnimalList)
            {
				DrawItemBubble(Game1.spriteBatch, farmAnimal);
            }

            foreach (NPC npc in currentLocation.characters)
            {
                if (npc is Pet)
                {
                    DrawItemBubble(Game1.spriteBatch, (Pet) npc);
                }
            }
        }

        public void DrawItemBubble(SpriteBatch spriteBatch, FarmAnimal animal)
        {
			bool hasProduce = AnimalBeingRanched != animal && (animal.CanBeRanched(GameConstants.Tools.MilkPail) || animal.CanBeRanched(GameConstants.Tools.Shears));
            Rectangle? sourceRectangle = new Rectangle?(new Rectangle(218, 428, 7, 6));

            if (hasProduce || !animal.wasPet)
            {
                float num = (float)(4.0 * Math.Round(Math.Sin(DateTime.Now.TimeOfDay.TotalMilliseconds / 250.0), 2));
                if (animal.isCoopDweller() && !animal.isBaby()) { num -= Game1.tileSize * 1 / 2; }

                // Thought Bubble
                spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2((float)(animal.Position.X + (animal.Sprite.getWidth() / 2)),
                    (float)(animal.Position.Y - Game1.tileSize * 4 / 3) + num)),
                    new Microsoft.Xna.Framework.Rectangle?(new Microsoft.Xna.Framework.Rectangle(141, 465, 20, 24)),
                    Color.White * 0.75f, 0.0f, Vector2.Zero, 4f, SpriteEffects.None,
                    0);

                if (!animal.wasPet)
                {
                    if (hasProduce)
                    {
                        // Small Heart
                        spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2((float)(animal.Position.X + (animal.Sprite.getWidth() / 2) + Game1.tileSize * .65),
                           (float)(animal.Position.Y - Game1.tileSize * 4 / 10) + num)),
                            sourceRectangle,
                            Color.White * 0.75f, 0.0f, new Vector2(8f, 8f), (float)Game1.pixelZoom, SpriteEffects.None,
                            1);
                    }
                    else
                    {
                        // Big Heart
                        spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2((float)(animal.Position.X + (animal.Sprite.getWidth() / 2) + Game1.tileSize * 1.1),
                           (float)(animal.Position.Y - Game1.tileSize * 1 / 10) + num)),
                            sourceRectangle,
                            Color.White * 0.75f, 0.0f, new Vector2(8f, 8f), (float)Game1.pixelZoom * 5 / 3, SpriteEffects.None,
                            1);
                    }
                }

                if (hasProduce)
                {
                    if (!animal.wasPet)
                    {
                        // Small Milk
                        spriteBatch.Draw(Game1.objectSpriteSheet, Game1.GlobalToLocal(Game1.viewport, new Vector2((float)(animal.Position.X + (animal.Sprite.getWidth() / 2) + Game1.tileSize * .85),
                           (float)(animal.Position.Y - Game1.tileSize * 7 / 10) + num)),
                            new Microsoft.Xna.Framework.Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, animal.currentProduce, 16, 16)),
                            Color.White * 0.75f, 0.0f, new Vector2(8f, 8f), (float)(Game1.pixelZoom * .60), SpriteEffects.None,
                            1);
                    }
                    else
                    {
                        // Big Milk
                        spriteBatch.Draw(Game1.objectSpriteSheet, Game1.GlobalToLocal(Game1.viewport, new Vector2((float)(animal.Position.X + (animal.Sprite.getWidth() / 2) + Game1.tileSize * .625),
                           (float)(animal.Position.Y - Game1.tileSize * 7 / 10) + num)),
                            new Microsoft.Xna.Framework.Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, animal.currentProduce, 16, 16)),
                            Color.White * 0.75f, 0.0f, new Vector2(8f, 8f), (float)(Game1.pixelZoom), SpriteEffects.None,
                            1);
                    }
                }
            }
        }

        public void DrawItemBubble(SpriteBatch spriteBatch, Pet pet)
        {
            Rectangle? sourceRectangle = new Rectangle?(new Rectangle(218, 428, 7, 6));
            bool wasPet = (bool)typeof(Pet).GetField("wasPetToday", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pet);
            if (!wasPet)
            {
                float num = (float)(4.0 * Math.Round(Math.Sin(DateTime.Now.TimeOfDay.TotalMilliseconds / 250.0), 2)) - Game1.tileSize * 1/2;

                // Thought Bubble
                spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2((float)(pet.Position.X + (pet.Sprite.getWidth() / 2)),
                    (float)(pet.Position.Y - Game1.tileSize * 4 / 3) + num)),
                    new Microsoft.Xna.Framework.Rectangle?(new Microsoft.Xna.Framework.Rectangle(141, 465, 20, 24)),
                    Color.White * 0.75f, 0.0f, Vector2.Zero, 4f, SpriteEffects.None,
                    0);

                // Big Heart
                spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2((float)(pet.Position.X + (pet.Sprite.getWidth() / 2) + Game1.tileSize * 1.1),
                   (float)(pet.Position.Y - Game1.tileSize * 1 / 10) + num)),
                    sourceRectangle,
                    Color.White * 0.75f, 0.0f, new Vector2(8f, 8f), (float)Game1.pixelZoom * 5 / 3, SpriteEffects.None,
                    1);
            }
        }
    }
}

