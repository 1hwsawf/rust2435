using System;
using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Simple Sorter", "Ruize", "2.4.0")]
    [Description("Backpack and loot container sorting buttons for Rust.")]
    public class SimpleSorter : RustPlugin
    {
        private const string UiPanelName = "SimpleSorterPanel";

        private readonly List<ItemContainer> _lootContainers = new List<ItemContainer>(4);
        private readonly List<Item> _itemBuffer = new List<Item>(128);
        private readonly HashSet<int> _itemIdBuffer = new HashSet<int>();

        private static readonly Comparison<Item> ItemComparison = (a, b) =>
        {
            int result = a.info.category.CompareTo(b.info.category);
            if (result != 0) return result;

            result = a.info.itemid.CompareTo(b.info.itemid);
            if (result != 0) return result;

            return b.amount.CompareTo(a.amount);
        };

        private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (player == null || entity == null) return;
            ScheduleDrawUI(player);
        }

        private void OnLootPlayer(BasePlayer player, BasePlayer target)
        {
            if (player == null || target == null) return;
            ScheduleDrawUI(player);
        }

        private void OnPlayerLootEnd(PlayerLoot lootContainer)
        {
            var player = lootContainer.GetComponent<BasePlayer>();
            if (player != null)
                DestroyUI(player);
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
                DestroyUI(player);
        }

        [ConsoleCommand("simplesorter.cmd")]
        private void OnSortCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            string action = arg.GetString(0);

            GetLootContainers(player, _lootContainers);
            if (_lootContainers.Count == 0) return;

            try
            {
                var playerMain = player.inventory.containerMain;

                if (action == "take_match")
                {
                    FillItemIds(playerMain, _itemIdBuffer);
                    MoveMatchingItems(_lootContainers, playerMain, _itemIdBuffer);
                }
                else if (action == "deposit_match")
                {
                    FillItemIds(_lootContainers, _itemIdBuffer);
                    MoveMatchingItems(playerMain, _lootContainers, _itemIdBuffer);
                }
                else if (action == "take_all")
                {
                    MoveAllItems(_lootContainers, playerMain);
                }
                else if (action == "deposit_all")
                {
                    MoveAllItems(playerMain, _lootContainers);
                }
                else if (action == "sort_player")
                {
                    SortContainer(playerMain);
                }
                else if (action == "sort_box")
                {
                    foreach (var container in _lootContainers)
                        SortContainer(container);
                }
            }
            finally
            {
                _lootContainers.Clear();
            }
        }

        private void ScheduleDrawUI(BasePlayer player)
        {
            timer.Once(0.1f, () =>
            {
                if (player != null && player.IsConnected && HasLootContainers(player))
                    DrawUI(player);
            });
        }

        private bool HasLootContainers(BasePlayer player)
        {
            GetLootContainers(player, _lootContainers);
            bool hasContainers = _lootContainers.Count > 0;
            _lootContainers.Clear();
            return hasContainers;
        }

        private void GetLootContainers(BasePlayer player, List<ItemContainer> containers)
        {
            containers.Clear();

            var loot = player.inventory.loot;
            if (loot == null || !loot.IsLooting() || loot.containers == null) return;

            foreach (var container in loot.containers)
            {
                if (container == null || container.capacity <= 0 || IsPlayerContainer(player, container)) continue;
                containers.Add(container);
            }
        }

        private bool IsPlayerContainer(BasePlayer player, ItemContainer container)
        {
            return container == player.inventory.containerMain ||
                   container == player.inventory.containerBelt ||
                   container == player.inventory.containerWear;
        }

        private void FillItemIds(ItemContainer container, HashSet<int> itemIds)
        {
            itemIds.Clear();

            foreach (var item in container.itemList)
                itemIds.Add(item.info.itemid);
        }

        private void FillItemIds(List<ItemContainer> containers, HashSet<int> itemIds)
        {
            itemIds.Clear();

            foreach (var container in containers)
            {
                foreach (var item in container.itemList)
                    itemIds.Add(item.info.itemid);
            }
        }

        private void MoveMatchingItems(List<ItemContainer> sources, ItemContainer target, HashSet<int> matchingIds)
        {
            bool movedToTarget = false;

            foreach (var source in sources)
            {
                CopyItems(source, _itemBuffer);

                bool movedFromSource = false;
                foreach (var item in _itemBuffer)
                {
                    if (matchingIds.Contains(item.info.itemid) && item.MoveToContainer(target, -1, true))
                    {
                        movedFromSource = true;
                        movedToTarget = true;
                    }
                }

                if (movedFromSource)
                    RefreshContainer(source);
            }

            if (movedToTarget)
                target.MarkDirty();

            _itemBuffer.Clear();
        }

        private void MoveMatchingItems(ItemContainer source, List<ItemContainer> targets, HashSet<int> matchingIds)
        {
            CopyItems(source, _itemBuffer);

            bool moved = false;
            foreach (var item in _itemBuffer)
            {
                if (!matchingIds.Contains(item.info.itemid)) continue;

                foreach (var target in targets)
                {
                    if (item.MoveToContainer(target, -1, true))
                    {
                        moved = true;
                        break;
                    }
                }
            }

            if (moved)
            {
                source.MarkDirty();
                RefreshContainers(targets);
            }

            _itemBuffer.Clear();
        }

        private void MoveAllItems(List<ItemContainer> sources, ItemContainer target)
        {
            bool movedToTarget = false;

            foreach (var source in sources)
            {
                CopyItems(source, _itemBuffer);

                bool movedFromSource = false;
                foreach (var item in _itemBuffer)
                {
                    if (item.MoveToContainer(target, -1, true))
                    {
                        movedFromSource = true;
                        movedToTarget = true;
                    }
                }

                if (movedFromSource)
                    RefreshContainer(source);
            }

            if (movedToTarget)
                target.MarkDirty();

            _itemBuffer.Clear();
        }

        private void MoveAllItems(ItemContainer source, List<ItemContainer> targets)
        {
            CopyItems(source, _itemBuffer);

            bool moved = false;
            foreach (var item in _itemBuffer)
            {
                foreach (var target in targets)
                {
                    if (item.MoveToContainer(target, -1, true))
                    {
                        moved = true;
                        break;
                    }
                }
            }

            if (moved)
            {
                source.MarkDirty();
                RefreshContainers(targets);
            }

            _itemBuffer.Clear();
        }

        private void SortContainer(ItemContainer container)
        {
            CopyItems(container, _itemBuffer);
            if (_itemBuffer.Count <= 1)
            {
                _itemBuffer.Clear();
                return;
            }

            _itemBuffer.Sort(ItemComparison);

            for (int i = 0; i < _itemBuffer.Count; i++)
                _itemBuffer[i].RemoveFromContainer();

            for (int i = 0; i < _itemBuffer.Count; i++)
                _itemBuffer[i].MoveToContainer(container, i, false);

            RefreshContainer(container);
            _itemBuffer.Clear();
        }

        private void CopyItems(ItemContainer container, List<Item> items)
        {
            items.Clear();

            foreach (var item in container.itemList)
                items.Add(item);
        }

        private void RefreshContainers(List<ItemContainer> containers)
        {
            foreach (var container in containers)
                RefreshContainer(container);
        }

        private void RefreshContainer(ItemContainer container)
        {
            container.MarkDirty();

            var owner = container.entityOwner;
            if (owner != null)
                owner.InvalidateNetworkCache();
        }

        private void DrawUI(BasePlayer player)
        {
            DestroyUI(player);
            var elements = new CuiElementContainer();

            elements.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.0", AnchorMax = "0.5 0.0", OffsetMin = "250 15", OffsetMax = "425 105" },
                CursorEnabled = false
            }, "Overlay", UiPanelName);

            string btnColor = "0.41 0.51 0.22 0.65";
            string textColor = "1 1 1 0.65";

            elements.Add(new CuiButton
            {
                Button = { Command = "simplesorter.cmd take_match", Color = btnColor },
                RectTransform = { AnchorMin = "0 0.70", AnchorMax = "0.48 1.0" },
                Text = { Text = "<b><  MATCH</b>", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = textColor }
            }, UiPanelName);

            elements.Add(new CuiButton
            {
                Button = { Command = "simplesorter.cmd deposit_match", Color = btnColor },
                RectTransform = { AnchorMin = "0.52 0.70", AnchorMax = "1 1.0" },
                Text = { Text = "<b>MATCH  ></b>", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = textColor }
            }, UiPanelName);

            elements.Add(new CuiButton
            {
                Button = { Command = "simplesorter.cmd take_all", Color = btnColor },
                RectTransform = { AnchorMin = "0 0.35", AnchorMax = "0.48 0.65" },
                Text = { Text = "<b><  ALL</b>", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = textColor }
            }, UiPanelName);

            elements.Add(new CuiButton
            {
                Button = { Command = "simplesorter.cmd deposit_all", Color = btnColor },
                RectTransform = { AnchorMin = "0.52 0.35", AnchorMax = "1 0.65" },
                Text = { Text = "<b>ALL  ></b>", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = textColor }
            }, UiPanelName);

            elements.Add(new CuiButton
            {
                Button = { Command = "simplesorter.cmd sort_player", Color = btnColor },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.48 0.30" },
                Text = { Text = "<b><  SORT</b>", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = textColor }
            }, UiPanelName);

            elements.Add(new CuiButton
            {
                Button = { Command = "simplesorter.cmd sort_box", Color = btnColor },
                RectTransform = { AnchorMin = "0.52 0", AnchorMax = "1 0.30" },
                Text = { Text = "<b>SORT  ></b>", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = textColor }
            }, UiPanelName);

            CuiHelper.AddUi(player, elements);
        }

        private void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UiPanelName);
        }
    }
}
