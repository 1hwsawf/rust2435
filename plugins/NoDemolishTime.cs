using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("No Demolish Time", "Ruize", "2.0.0")]
    [Description("范围激活版：剔除乱码表情，一锤敲击即可激活整栋相连建筑的拆除与旋转功能。")]
    public class NoDemolishTime : RustPlugin
    {
        void OnHammerHit(BasePlayer player, HitInfo info)
        {
            // 确保击中的是建筑模块
            var targetBlock = info?.HitEntity as BuildingBlock;
            if (targetBlock == null) return;

            // 权限判定：只需判定当前敲击的这一块墙是否有权限
            if (targetBlock.OwnerID == player.userID || player.CanBuild() || player.IsAdmin)
            {
                // 获取相连的整栋建筑物
                var building = targetBlock.GetBuilding();
                if (building == null) return;

                int count = 0;

                // 遍历这栋楼的所有建筑模块
                foreach (var decayEnt in building.decayEntities)
                {
                    var block = decayEnt as BuildingBlock;
                    if (block == null) continue;

                    bool changed = false;

                    // 开启“可旋转”
                    if (!block.HasFlag(BaseEntity.Flags.Reserved1))
                    {
                        block.SetFlag(BaseEntity.Flags.Reserved1, true);
                        changed = true;
                    }

                    // 开启“可拆除”
                    if (!block.HasFlag(BaseEntity.Flags.Reserved2))
                    {
                        block.SetFlag(BaseEntity.Flags.Reserved2, true);
                        changed = true;
                    }

                    // 如果该模块状态发生改变，提交网络更新
                    if (changed)
                    {
                        block.SendNetworkUpdate();
                        count++;
                    }
                }

                if (count > 0)
                {
                    player.ChatMessage($"<color=#00FF00>[建筑系统]</color> 已重新激活整栋建筑（共 <color=#FFB455>{count}</color> 个模块）的拆除与旋转菜单！");
                }
            }
            else
            {
                player.ChatMessage("<color=#FF5555>[建筑系统]</color> 提示：你没有该区域的建筑权限或并非房主，操作被拒绝。");
            }
        }
    }
}