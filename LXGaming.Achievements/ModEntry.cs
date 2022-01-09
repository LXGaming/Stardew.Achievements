using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace LXGaming.Achievements {

    public class ModEntry : Mod {

        public override void Entry(IModHelper helper) {
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

                if (item.Category == Object.FishCategory) {
                    // Ignore already caught fish
                    if (player.fishCaught.ContainsKey(index)) {
                        continue;
                    }

                    player.caughtFish(index, 0);
                    Monitor.Log($"Caught {item.DisplayName}", LogLevel.Info);
                }

                if (item.Category == Object.CookingCategory) {
                    // Ignore learned and cooked recipes
                    if (player.cookingRecipes.ContainsKey(item.Name) && player.recipesCooked.ContainsKey(index)) {
                        return;
                    }

                    // Learn recipe
                    if (!player.cookingRecipes.ContainsKey(item.Name)) {
                        player.cookingRecipes.Add(item.Name, 0);
                    }

                    // Cook recipe
                    if (!player.recipesCooked.ContainsKey(index)) {
                        player.recipesCooked.Add(index, 1);
                    }

                    Game1.stats.checkForCookingAchievements();
                    Monitor.Log($"Cooked {item.DisplayName}", LogLevel.Info);
                }

                if (item.Category is Object.CraftingCategory or Object.BigCraftableCategory) {
                    // Ignore non-existent or already crafted recipes
                    if (!player.craftingRecipes.TryGetValue(item.Name, out var count) || count != 0) {
                        continue;
                    }

                    player.craftingRecipes[item.Name] += 1;
                    Game1.stats.checkForCraftingAchievements();
                    Monitor.Log($"Crafted {item.DisplayName}", LogLevel.Info);
                }
            }
        }
    }
}