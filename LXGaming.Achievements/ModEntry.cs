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
                    // Ignore already cooked recipes
                    if (player.recipesCooked.ContainsKey(index)) {
                        continue;
                    }

                    player.cookedRecipe(index);
                    Monitor.Log($"Cooked {item.DisplayName}", LogLevel.Info);
                }

                if (item.Category == Object.CraftingCategory || item.Category == Object.BigCraftableCategory) {
                    // Ignore non-existent or already crafted recipes
                    if (!player.craftingRecipes.TryGetValue(item.Name, out var count) || count != 0) {
                        continue;
                    }

                    player.craftingRecipes[item.Name] += 1;
                    Monitor.Log($"Crafted {item.DisplayName}", LogLevel.Info);
                }
            }
        }
    }
}