﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData.Pets;
using StardewValley.ItemTypeDefinitions;

namespace BetterRanching
{
	public class BetterRanchingApi
	{
		private readonly ModConfig _config;

		internal BetterRanchingApi(ModConfig config)
		{
			_config = config;
		}

		public void DrawHeartBubble(SpriteBatch spriteBatch, Character character, Func<bool> displayHeart)
		{
			var friendship = character is Pet pet ? pet.friendshipTowardFarmer.TargetValue : Game1.player.tryGetFriendshipLevelForNPC(character.Name);
			DrawHeartBubble(spriteBatch, character.Position.X + 13, character.Position.Y, character.GetSpriteWidthForPositioning(),
				displayHeart, character is FarmAnimal, character is Pet, friendship.GetValueOrDefault());
		}

		public void DrawHeartBubble(SpriteBatch spriteBatch, float xPosition, float yPosition, int spriteWidth,
			Func<bool> displayHeart, bool isFarmAnimal, bool isPet, int friendship)
		{
			if (!displayHeart() || !_config.DisplayHearts ||
				(isFarmAnimal && !_config.DisplayFarmAnimalHearts) || (isPet && !_config.DisplayPetHearts) ||
				_config.HideHeartsWhenFriendshipIsMax && friendship >= 1000) return;

			Rectangle? sourceRectangle = new Rectangle(218, 428, 7, 6);
			var num = (float)(4.0 * Math.Round(Math.Sin(DateTime.Now.TimeOfDay.TotalMilliseconds / 250.0), 2)) -
					  32;

			// Thought bubble
			spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(
					xPosition + spriteWidth / 2f,
					yPosition - 85 + num)),
				new Rectangle(141, 465, 20, 24),
				Color.White * 0.75f, 0.0f, Vector2.Zero, 4f, SpriteEffects.None,
				0);

			// Big heart icon
			spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(
					(float)(xPosition + spriteWidth / 2f + Game1.tileSize * 1.1),
					yPosition - 6 + num)),
				sourceRectangle,
				Color.White * 0.75f, 0.0f, new Vector2(8f, 8f), (float)4f * 5 / 3, SpriteEffects.None,
				1);
		}

		public void DrawItemBubble(SpriteBatch spriteBatch, FarmAnimal animal, bool ranchingInProgress)
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(animal.currentProduce.Value);
			int produceId = dataOrErrorItem.SpriteIndex;

			DrawItemBubble(
				spriteBatch,
				animal.Position.X + 13,
				animal.Position.Y,
				animal.GetSpriteWidthForPositioning() * (animal.buildingTypeILiveIn.Contains("Coop") && animal.isAdult() ? -1 : 1),
				animal.buildingTypeILiveIn.Contains("Coop") && animal.isAdult(),
				produceId,
				() => !ranchingInProgress && (animal.CanBeRanched(GameConstants.Tools.MilkPail) ||
												animal.CanBeRanched(GameConstants.Tools.Shears)),
				() => !animal.wasPet.Value,
				true,
				false,
				animal.friendshipTowardFarmer.Value,
				dataOrErrorItem
			);
		}

		public void DrawItemBubble(SpriteBatch spriteBatch, float xPosition, float yPosition, int spriteWidth,
			bool isShortTarget, int produceIcon, Func<bool> displayItem, Func<bool> displayHeart,
			bool isFarmAnimal, bool isPet, int friendship, ParsedItemData? itemData = null)
		{
			var showItem = displayItem() && _config.DisplayProduce;
			var showHeart = displayHeart() &&
							_config.DisplayHearts &&
							(isFarmAnimal && _config.DisplayFarmAnimalHearts || isPet && _config.DisplayPetHearts) &&
							(!_config.HideHeartsWhenFriendshipIsMax || friendship < 1000);

			Rectangle? sourceRectangle = new Rectangle(218, 428, 7, 6);

			if (!showItem && !showHeart) return;

			var num = (float)(4.0 * Math.Round(Math.Sin(DateTime.Now.TimeOfDay.TotalMilliseconds / 250.0), 2));
			if (isShortTarget) num -= 32;

			// Thought bubble
			spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport,
					new Vector2(xPosition + spriteWidth / 2f,
						yPosition - 85 + num)),
				new Rectangle(141, 465, 20, 24),
				Color.White * 0.75f, 0.0f, Vector2.Zero, (float)Game1.pixelZoom, SpriteEffects.None,
				0);

			if (showHeart)
			{
				if (showItem)
				{
					spriteBatch.Draw(Game1.mouseCursors,
						Game1.GlobalToLocal(Game1.viewport,
							new Vector2(xPosition + spriteWidth / 2f + 40,
								yPosition - 25 + num)), sourceRectangle, Color.White * 0.75f, 0.0f, new Vector2(8f, 8f),
						Game1.pixelZoom, SpriteEffects.None, 1);
				}
				else
				{
					spriteBatch.Draw(Game1.mouseCursors,
						Game1.GlobalToLocal(Game1.viewport,
							new Vector2((float)(xPosition + spriteWidth / 2f + Game1.tileSize * 1.1),
								yPosition - 7 + num)), sourceRectangle, Color.White * 0.75f, 0.0f, new Vector2(8f, 8f),
						(float)Game1.pixelZoom * 5 / 3, SpriteEffects.None, 1);
				}
			}

			if (!showItem) return;

			if (showHeart)
			{
				// Small item icon
				spriteBatch.Draw(itemData != null ? itemData.GetTexture() : Game1.objectSpriteSheet, Game1.GlobalToLocal(Game1.viewport,
						new Vector2(xPosition + spriteWidth / 2f + 56,
							yPosition - 45 + num)),
					Game1.getSourceRectForStandardTileSheet(itemData != null ? itemData.GetTexture() : Game1.objectSpriteSheet, produceIcon, 16, 16),
					Color.White * 0.75f, 0.0f, new Vector2(8f, 8f), (float)(Game1.pixelZoom * .60),
					SpriteEffects.None,
					1);
			}
			else
			{
				// Big item icon
				spriteBatch.Draw(itemData != null ? itemData.GetTexture() : Game1.objectSpriteSheet, Game1.GlobalToLocal(Game1.viewport,
						new Vector2((float)(xPosition + spriteWidth / 2f + Game1.tileSize * .625),
				yPosition - 45 + num)),
					Game1.getSourceRectForStandardTileSheet(itemData != null ? itemData.GetTexture() : Game1.objectSpriteSheet, produceIcon, 16, 16),
					Color.White * 0.75f, 0.0f, new Vector2(8f, 8f), Game1.pixelZoom, SpriteEffects.None,
					1);
			}

		}


		[Obsolete("Backwards compatibility for < v1.9.0")]
		public void DrawHeartBubble(SpriteBatch spriteBatch, float xPosition, float yPosition, int spriteWidth,
			Func<bool> displayHeart)
		{
			DrawHeartBubble(spriteBatch, xPosition, yPosition, spriteWidth,
				displayHeart, false, false, -1);
		}

		[Obsolete("Backwards compatibility for < v1.9.0")]
		public void DrawItemBubble(SpriteBatch spriteBatch, float xPosition, float yPosition, int spriteWidth,
			bool isShortTarget, int produceIcon, Func<bool> displayItem, Func<bool> displayHeart)
		{
			DrawItemBubble(spriteBatch, xPosition, yPosition, spriteWidth, isShortTarget, produceIcon, displayItem,
				displayHeart,
				false, false, -1);
		}
	}
}