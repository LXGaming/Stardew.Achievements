using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValleyObject = StardewValley.Object;

namespace LXGaming.Achievements;

public class ModEntry : Mod {

    public override void Entry(IModHelper helper) {
        helper.ConsoleCommands.Add("player_listincompleterecipes", "Lists incomplete recipes\n\nUsage: player_listincompleterecipes", OnListIncompleteRecipes);
        helper.Events.Player.InventoryChanged += OnInventoryChanged;
    }

    private void OnInventoryChanged(object? sender, InventoryChangedEventArgs args) {
        var player = args.Player;
        if (!player.IsLocalPlayer) {
            return;
        }

        foreach (var item in args.Added) {
            if (IsCatchable(item) && !player.fishCaught.ContainsKey(item.QualifiedItemId)) {
                player.caughtFish(item.QualifiedItemId, 0);
                Monitor.Log($"Caught {item.DisplayName}", LogLevel.Info);
            }

            foreach (var cookingRecipe in GetCookingRecipes(item.QualifiedItemId)) {
                if (string.IsNullOrEmpty(cookingRecipe)) {
                    continue;
                }

                // Learn recipe
                bool learnt;
                if (!player.cookingRecipes.ContainsKey(cookingRecipe)) {
                    player.cookingRecipes.Add(cookingRecipe, 0);
                    learnt = true;
                } else {
                    learnt = false;
                }

                // Cook recipe
                bool cooked;
                if (!player.recipesCooked.ContainsKey(cookingRecipe)) {
                    player.recipesCooked.Add(cookingRecipe, 1);
                    cooked = true;
                } else {
                    cooked = false;
                }

                if (!learnt && !cooked) {
                    continue;
                }

                Game1.stats.checkForCookingAchievements();
                Monitor.Log($"Cooked {item.DisplayName} ({cookingRecipe})", LogLevel.Info);
            }

            foreach (var craftingRecipe in GetCraftingRecipes(item.QualifiedItemId)) {
                if (string.IsNullOrEmpty(craftingRecipe)) {
                    continue;
                }

                bool crafted;
                if (player.craftingRecipes.TryGetValue(craftingRecipe, out var count)) {
                    if (count == 0) {
                        player.craftingRecipes[craftingRecipe] += 1;
                        crafted = true;
                    } else {
                        crafted = false;
                    }
                } else {
                    player.craftingRecipes.Add(craftingRecipe, 1);
                    crafted = true;
                }

                if (!crafted) {
                    continue;
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

        var cookingRecipes = GetIncompleteCookingRecipes(player).ToList();
        if (cookingRecipes.Count != 0) {
            Monitor.Log("Incomplete Cooking Recipes:", LogLevel.Info);
            foreach (var recipe in cookingRecipes) {
                Monitor.Log($"- {recipe}", LogLevel.Info);
            }
        } else {
            Monitor.Log("Incomplete Cooking Recipes: None", LogLevel.Info);
        }

        var craftingRecipes = GetIncompleteCraftingRecipes(player).ToList();
        if (craftingRecipes.Count != 0) {
            Monitor.Log("Incomplete Crafting Recipes:", LogLevel.Info);
            foreach (var recipe in craftingRecipes) {
                Monitor.Log($"- {recipe}", LogLevel.Info);
            }
        } else {
            Monitor.Log("Incomplete Crafting Recipes: None", LogLevel.Info);
        }
    }

    private bool IsCatchable(Item item) {
        return item.Category == StardewValleyObject.FishCategory
               || item.QualifiedItemId == "(O)CaveJelly"
               || item.QualifiedItemId == "(O)RiverJelly"
               || item.QualifiedItemId == "(O)SeaJelly";
    }

    private IEnumerable<string> ParseItemIds(string value, bool qualified = false) {
        var data = value.Split('/');
        if (data.Length < 3) {
            yield break;
        }

        var itemData = data[2].Split(' ');
        for (var index = 0; index < itemData.Length; index += 2) {
            var itemId = itemData[index];
            if (qualified) {
                var metadata = ItemRegistry.GetMetadata(itemId);
                if (metadata != null) {
                    yield return metadata.QualifiedItemId;
                }
            } else {
                yield return itemId;
            }
        }
    }

    private IEnumerable<string> GetCookingRecipes(string itemId) {
        return GetCookingRecipes()
            .Where(pair => ParseItemIds(pair.Value, true).Contains(itemId))
            .Select(pair => pair.Key);
    }

    private IEnumerable<string> GetIncompleteCookingRecipes(Farmer player) {
        return GetCookingRecipes()
            .Where(pair => !player.cookingRecipes.ContainsKey(pair.Key) || !ParseItemIds(pair.Value).Any(itemId => player.recipesCooked.ContainsKey(itemId)))
            .Select(pair => pair.Key);
    }

    private Dictionary<string, string> GetCookingRecipes() {
        return Helper.GameContent.Load<Dictionary<string, string>>("Data/CookingRecipes");
    }

    private IEnumerable<string> GetCraftingRecipes(string itemId) {
        return GetCraftingRecipes()
            .Where(pair => ParseItemIds(pair.Value, true).Contains(itemId))
            .Select(pair => pair.Key);
    }

    private IEnumerable<string> GetIncompleteCraftingRecipes(Farmer player) {
        return GetCraftingRecipes()
            .Select(pair => pair.Key)
            .Where(key => !player.craftingRecipes.TryGetValue(key, out var count) || count == 0);
    }

    private Dictionary<string, string> GetCraftingRecipes() {
        return Helper.GameContent.Load<Dictionary<string, string>>("Data/CraftingRecipes");
    }
}