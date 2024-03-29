﻿using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Object = StardewValley.Object;

namespace LXGaming.Achievements {

    public class ModEntry : Mod {

        public override void Entry(IModHelper helper) {
            helper.ConsoleCommands.Add("player_listincompleterecipes", "Lists incomplete recipes\n\nUsage: player_listincompleterecipes", OnListIncompleteRecipes);
            helper.Events.Player.InventoryChanged += OnInventoryChanged;
        }

        private void OnInventoryChanged(object sender, InventoryChangedEventArgs args) {
            var player = args.Player;
            if (!player.IsLocalPlayer) {
                return;
            }

            foreach (var item in args.Added) {
                var index = item.ParentSheetIndex;
                if (index == -1) {
                    continue;
                }

                if (item.Category == Object.FishCategory && !player.fishCaught.ContainsKey(index)) {
                    player.caughtFish(index, 0);
                    Monitor.Log($"Caught {item.DisplayName}", LogLevel.Info);
                }

                var cookingRecipe = GetCookingRecipe(index);
                if (!string.IsNullOrEmpty(cookingRecipe) && !(player.cookingRecipes.ContainsKey(cookingRecipe) && player.recipesCooked.ContainsKey(index))) {
                    // Learn recipe
                    if (!player.cookingRecipes.ContainsKey(cookingRecipe)) {
                        player.cookingRecipes.Add(cookingRecipe, 0);
                    }

                    // Cook recipe
                    if (!player.recipesCooked.ContainsKey(index)) {
                        player.recipesCooked.Add(index, 1);
                    }

                    Game1.stats.checkForCookingAchievements();
                    Monitor.Log($"Cooked {item.DisplayName} ({cookingRecipe})", LogLevel.Info);
                }

                var craftingRecipe = GetCraftingRecipe(index);
                if (!string.IsNullOrEmpty(craftingRecipe) && (!player.craftingRecipes.TryGetValue(craftingRecipe, out var count) || count == 0)) {
                    if (!player.craftingRecipes.ContainsKey(craftingRecipe)) {
                        player.craftingRecipes.Add(craftingRecipe, 1);
                    } else {
                        player.craftingRecipes[craftingRecipe] += 1;
                    }

                    Game1.stats.checkForCraftingAchievements();
                    Monitor.Log($"Crafted {item.DisplayName} ({craftingRecipe})", LogLevel.Info);
                }
            }
        }

        private void OnListIncompleteRecipes(string name, string[] arguments) {
            if (!Context.IsWorldReady) {
                Monitor.Log("You need to load a save to use this command.", LogLevel.Error);
                return;
            }

            var player = Game1.player;

            var cookingRecipes = GetIncompleteCookingRecipes(player);
            if (cookingRecipes.Count != 0) {
                Monitor.Log("Incomplete Cooking Recipes:", LogLevel.Info);
                foreach (var recipe in cookingRecipes) {
                    Monitor.Log($"- {recipe}", LogLevel.Info);
                }
            } else {
                Monitor.Log("Incomplete Cooking Recipes: None", LogLevel.Info);
            }

            var craftingRecipes = GetIncompleteCraftingRecipes(player);
            if (craftingRecipes.Count != 0) {
                Monitor.Log("Incomplete Crafting Recipes:", LogLevel.Info);
                foreach (var recipe in craftingRecipes) {
                    Monitor.Log($"- {recipe}", LogLevel.Info);
                }
            } else {
                Monitor.Log("Incomplete Crafting Recipes: None", LogLevel.Info);
            }
        }

        private int ExtractIndex(string value) {
            return Convert.ToInt32(value.Split('/')[2].Split(' ')[0]);
        }

        private string GetCookingRecipe(int index) {
            return GetCookingRecipes()
                .Where(pair => {
                    var (key, value) = pair;
                    return ExtractIndex(value) == index;
                })
                .Select(pair => pair.Key)
                .SingleOrDefault();
        }

        private ICollection<string> GetIncompleteCookingRecipes(Farmer player) {
            return GetCookingRecipes()
                .Where(pair => {
                    var (key, value) = pair;
                    var index = ExtractIndex(value);
                    return !player.cookingRecipes.ContainsKey(key) || !player.recipesCooked.ContainsKey(index);
                })
                .Select(pair => pair.Key)
                .ToList();
        }

        private Dictionary<string, string> GetCookingRecipes() {
            return Helper.Content.Load<Dictionary<string, string>>("Data/CookingRecipes", ContentSource.GameContent);
        }

        private string GetCraftingRecipe(int index) {
            return GetCraftingRecipes()
                .Where(pair => {
                    var (key, value) = pair;
                    return ExtractIndex(value) == index;
                })
                .Select(pair => pair.Key)
                .SingleOrDefault();
        }

        private ICollection<string> GetIncompleteCraftingRecipes(Farmer player) {
            return GetCraftingRecipes()
                .Where(pair => {
                    var (key, value) = pair;
                    return !player.craftingRecipes.TryGetValue(key, out var count) || count == 0;
                })
                .Select(pair => pair.Key)
                .ToList();
        }

        private Dictionary<string, string> GetCraftingRecipes() {
            return Helper.Content.Load<Dictionary<string, string>>("Data/CraftingRecipes", ContentSource.GameContent);
        }
    }
}