using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("战术中继网络", "Ruize", "10.1.0")]
    [Description("全息终端补全版：召回区域网格坐标与设施类型显示，完美融入科幻UI。")]
    public class QuantumRelay : RustPlugin
    {
        private const string UiPanelName = "QuantumRelayMainPanel";
        private const string UiLoadingScreen = "QuantumRelayLoadingScreen";

        private readonly HashSet<ulong> _openUIs = new HashSet<ulong>();
        private readonly Dictionary<ulong, Vector3> _returnPoints = new Dictionary<ulong, Vector3>(); 
        private readonly Dictionary<ulong, Timer> _activeTeleports = new Dictionary<ulong, Timer>();
        private readonly Dictionary<ulong, float> _godMode = new Dictionary<ulong, float>();
        private readonly Dictionary<string, float> _specialCooldowns = new Dictionary<string, float>();
        private readonly Dictionary<ulong, float> _combatTimers = new Dictionary<ulong, float>();
        private readonly Dictionary<string, Vector3> _safeZones = new Dictionary<string, Vector3>();

        private static readonly HashSet<string> SystemBagNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Outpost", "Bandit Camp", "Bandit"
        };

        #region 配置系统
        private Configuration config;

        public class Configuration
        {
            [JsonProperty("【性能优化】传送前预热准备时间 (秒)")] public float WarmupSeconds = 1.5f;
            [JsonProperty("【性能优化】落地后量子重构提示时间 (秒)")] public float LoadingSeconds = 2.0f;
            
            [JsonProperty("食物最高消耗 (卡路里)")] public float MaxCaloriesCost = 60f;
            [JsonProperty("水分最高消耗")] public float MaxHydrationCost = 30f;
            [JsonProperty("特殊节点独立冷却时间 (秒)")] public float ShortCooldownSeconds = 60f;
            [JsonProperty("距离缩放比例 (越大消耗越慢)")] public float DistanceScale = 4000f;
            [JsonProperty("PvP战斗状态锁定时间 (秒)")] public float CombatBlockSeconds = 60f;

            [JsonProperty("传送提示_前哨站 (随机抽取)")]
            public List<string> Tips_Outpost = new List<string> { "💡 <color=#00FFFF>终端提示：</color> 前哨站受重火力保护，严禁交火。" };

            [JsonProperty("传送提示_匪徒营地 (随机抽取)")]
            public List<string> Tips_Bandit = new List<string> { "💡 <color=#00FFFF>终端提示：</color> 匪徒营地，转盘赌狗的天堂与地狱。" };

            [JsonProperty("传送提示_大型渔村 (随机抽取)")]
            public List<string> Tips_Fishing = new List<string> { "💡 <color=#00FFFF>终端提示：</color> 购买潜水服和渔船的绝佳地点。" };

            [JsonProperty("传送提示_私人与返回点 (随机抽取)")]
            public List<string> Tips_Private = new List<string> { "💡 <color=#00FFFF>生存指南：</color> 出门带南瓜，下线锁好箱子。" };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try { config = Config.ReadObject<Configuration>(); if (config == null) LoadDefaultConfig(); }
            catch { LoadDefaultConfig(); }
            SaveConfig();
        }

        protected override void LoadDefaultConfig() { config = new Configuration(); }
        protected override void SaveConfig() { Config.WriteObject(config, true); }
        #endregion

        void OnServerInitialized() { FindSafeZones(); }

        private void FindSafeZones()
        {
            _safeZones.Clear();
            if (TerrainMeta.Path?.Monuments == null) return;

            foreach (var monument in TerrainMeta.Path.Monuments)
            {
                string name = monument.name.ToLower();
                if (name.Contains("outpost") || name.Contains("compound")) _safeZones["Outpost"] = monument.transform.position;
                else if (name.Contains("bandit_town") || name.Contains("banditcamp")) _safeZones["Bandit"] = monument.transform.position;
                else if (name.Contains("fishing_village_c") || name.Contains("large_fishing")) _safeZones["Fishing"] = monument.transform.position;
            }
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CloseUI(player);
                CuiHelper.DestroyUi(player, UiLoadingScreen);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, false);
            }
            foreach (var timer in _activeTeleports.Values) timer?.Destroy();
            _activeTeleports.Clear();
        }

        // 💡 新增：原汁原味的地图网格计算引擎
        private string GetMapGrid(Vector3 pos)
        {
            int worldSize = ConVar.Server.worldsize;
            float halfSize = worldSize / 2f;
            int xCell = Mathf.FloorToInt((pos.x + halfSize) / 146.3f);
            int zCell = Mathf.FloorToInt((halfSize - pos.z) / 146.3f);
            
            string letter = "";
            if (xCell < 0) xCell = 0;
            int firstLetter = xCell / 26;
            int secondLetter = xCell % 26;
            
            if (firstLetter > 0) letter += (char)('A' + firstLetter - 1);
            letter += (char)('A' + secondLetter);
            
            return $"{letter}{zCell}";
        }

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            var victim = entity as BasePlayer;
            if (victim == null) return null;

            if (_godMode.TryGetValue(victim.userID, out float expireTime))
            {
                if (Time.realtimeSinceStartup < expireTime) return true;
                else _godMode.Remove(victim.userID);
            }

            var attacker = info?.Initiator as BasePlayer;
            if (attacker != null && attacker != victim && !attacker.IsNpc && !victim.IsNpc)
            {
                float combatEnd = Time.realtimeSinceStartup + config.CombatBlockSeconds;
                _combatTimers[victim.userID] = combatEnd;
                _combatTimers[attacker.userID] = combatEnd;
            }
            return null;
        }

        private Vector3 CalculateSafeDropPoint(Vector3 centerPosition, float radius = 15f)
        {
            int mask = LayerMask.GetMask("Terrain", "World", "Construction", "Deployed");
            for (int i = 0; i < 5; i++) 
            {
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float r = UnityEngine.Random.Range(5f, radius); 
                Vector3 target = centerPosition + new Vector3(Mathf.Cos(angle) * r, 0, Mathf.Sin(angle) * r);
                target.y = TerrainMeta.HeightMap.GetHeight(target);
                
                Vector3 highPoint = target; highPoint.y += 100f;
                if (Physics.Raycast(highPoint, Vector3.down, out RaycastHit hit, 150f, mask)) target = hit.point;
                target.y += 1.0f; 

                if (!Physics.CheckCapsule(target, target + new Vector3(0, 1f, 0), 0.5f, mask)) return target;
            }
            return centerPosition + new Vector3(0, 2f, 0);
        }

        [ConsoleCommand("relay.toggle")]
        private void CmdConsoleToggle(ConsoleSystem.Arg arg) { var p = arg.Player(); if (p != null) ToggleUI(p); }

        [ChatCommand("tp")]
        private void CmdChatTp(BasePlayer p, string c, string[] a) { ToggleUI(p); }
        
        [ChatCommand("relay")]
        private void CmdChatRelay(BasePlayer p, string c, string[] a) { ToggleUI(p); }

        private void ToggleUI(BasePlayer player)
        {
            if (_activeTeleports.ContainsKey(player.userID))
            {
                player.ChatMessage("<color=#00FFFF>[中继终端]</color> 引擎已点火，请勿重复下达指令。");
                return;
            }

            if (_combatTimers.TryGetValue(player.userID, out float combatExpire) && Time.realtimeSinceStartup < combatExpire)
            {
                player.ChatMessage($"<color=#FF0000>[中继终端]</color> ⚔️ <b>检测到交战区锁定！</b>\n需脱战 <color=#FF5555>{Mathf.CeilToInt(combatExpire - Time.realtimeSinceStartup)} 秒</color>。");
                return;
            }

            if (_openUIs.Contains(player.userID)) CloseUI(player);
            else OpenUI(player);
        }

        private void CloseUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UiPanelName);
            _openUIs.Remove(player.userID);
        }

        [ConsoleCommand("relay.execute")]
        private void CmdRelayExecute(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            int type = arg.GetInt(0); 
            string targetParam = arg.GetString(1);

            CloseUI(player);
            if (type == 0 || _activeTeleports.ContainsKey(player.userID)) return;

            if (_combatTimers.TryGetValue(player.userID, out float combatExpire) && Time.realtimeSinceStartup < combatExpire)
            {
                player.ChatMessage("<color=#FF0000>[中继终端]</color> ⚔️ 遭受火力压制，信标连接切断！"); return;
            }

            Vector3 targetPos = Vector3.zero; string targetName = ""; SleepingBag targetBag = null;

            if (type == 1) 
            {
                if (!ulong.TryParse(targetParam, out ulong bagId)) return;
                targetBag = SleepingBag.FindForPlayer(player.userID, true).FirstOrDefault(b => b.net.ID.Value == bagId);
                if (targetBag == null) { player.ChatMessage("节点数据丢失。"); return; }
                float remaining = targetBag.unlockTime - Time.realtimeSinceStartup;
                if (remaining > 0) { player.ChatMessage($"节点冷却：{Mathf.CeilToInt(remaining)}秒。"); return; }
                targetPos = CalculateSafeDropPoint(targetBag.transform.position, 2f);
                targetName = string.IsNullOrEmpty(targetBag.niceName) ? "私人终端" : targetBag.niceName;
            }
            else if (type == 2) 
            {
                string key = $"{player.userID}_sz_{targetParam}";
                if (_specialCooldowns.TryGetValue(key, out float cd) && cd > Time.realtimeSinceStartup) { player.ChatMessage($"枢纽冷却：{Mathf.CeilToInt(cd - Time.realtimeSinceStartup)}秒。"); return; }
                if (!_safeZones.TryGetValue(targetParam, out Vector3 szPos)) { FindSafeZones(); if (!_safeZones.TryGetValue(targetParam, out szPos)) { player.ChatMessage("无法定位枢纽。"); return; } }
                targetPos = CalculateSafeDropPoint(szPos, 20f); 
                targetName = targetParam == "Outpost" ? "前哨站" : targetParam == "Bandit" ? "匪徒营地" : "大型渔村";
            }
            else if (type == 3) 
            {
                string key = $"{player.userID}_return";
                if (_specialCooldowns.TryGetValue(key, out float cd) && cd > Time.realtimeSinceStartup) { player.ChatMessage($"返回点冷却：{Mathf.CeilToInt(cd - Time.realtimeSinceStartup)}秒。"); return; }
                if (!_returnPoints.TryGetValue(player.userID, out targetPos)) { player.ChatMessage("无战术数据记录。"); return; }
                targetPos = CalculateSafeDropPoint(targetPos, 2f); targetName = "最后坐标";
            }

            float d = Vector3.Distance(player.transform.position, targetPos);
            int foodCost = Mathf.CeilToInt(Mathf.Clamp((d / config.DistanceScale) * config.MaxCaloriesCost, 0f, config.MaxCaloriesCost));
            int waterCost = Mathf.CeilToInt(Mathf.Clamp((d / config.DistanceScale) * config.MaxHydrationCost, 0f, config.MaxHydrationCost));

            if (player.metabolism.calories.value < foodCost || player.metabolism.hydration.value < waterCost)
            {
                player.ChatMessage($"<color=#FF5555>[系统报错]</color> 能量/水分极度匮乏 (需 食:{foodCost} 水:{waterCost})。"); return;
            }

            Vector3 startPos = player.transform.position;
            player.ChatMessage($"<color=#00FFFF>[中继终端]</color> 锚定目标 <b>{targetName}</b>，请保持原坐标 <color=#00FFFF>{config.WarmupSeconds} 秒</color>...");

            _activeTeleports[player.userID] = timer.Once(config.WarmupSeconds, () => 
            {
                _activeTeleports.Remove(player.userID);
                if (player == null || !player.IsConnected) return;
                if (Vector3.Distance(player.transform.position, startPos) > 1.5f) { player.ChatMessage("<color=#FF5555>[系统警告]</color> 物理偏离过大，已紧急终止跃迁。"); return; }

                player.metabolism.calories.value -= foodCost; player.metabolism.hydration.value -= waterCost;
                _returnPoints[player.userID] = player.transform.position;

                if (type == 1 && targetBag != null)
                {
                    foreach (var b in SleepingBag.FindForPlayer(player.userID, true))
                        if (Vector3.Distance(b.transform.position, targetPos) <= 50f) b.unlockTime = Time.realtimeSinceStartup + targetBag.secondsBetweenReuses;
                }
                else if (type == 2) _specialCooldowns[$"{player.userID}_sz_{targetParam}"] = Time.realtimeSinceStartup + config.ShortCooldownSeconds;
                else if (type == 3) _specialCooldowns[$"{player.userID}_return"] = Time.realtimeSinceStartup + config.ShortCooldownSeconds;

                if (player.isMounted) player.EnsureDismounted();
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
                
                if (config.LoadingSeconds > 0) ShowLoadingScreen(player, type, targetParam);

                player.Teleport(targetPos);
                player.UpdateNetworkGroup();
                player.SendNetworkUpdateImmediate();
                
                player.StartSleeping();
                _godMode[player.userID] = Time.realtimeSinceStartup + 8.0f;

                float loadingDelay = config.LoadingSeconds > 0 ? config.LoadingSeconds : 0.2f;
                timer.Once(loadingDelay, () => {
                    if (player == null || !player.IsConnected) return;
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, false);
                    CuiHelper.DestroyUi(player, UiLoadingScreen);
                    player.EndSleeping();
                });
            });
        }

        private string GetRandomTipFromList(List<string> tipsList)
        {
            if (tipsList == null || tipsList.Count == 0) return "💡 <color=#00FFFF>正在同步量子网络...</color>";
            return tipsList[UnityEngine.Random.Range(0, tipsList.Count)];
        }

        private void ShowLoadingScreen(BasePlayer player, int type, string targetParam)
        {
            CuiHelper.DestroyUi(player, UiLoadingScreen);
            var elements = new CuiElementContainer();
            
            elements.Add(new CuiPanel { Image = { Color = "0.02 0.05 0.08 0.98", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }, CursorEnabled = false }, "Overlay", UiLoadingScreen);
            elements.Add(new CuiLabel { Text = { Text = "<b><size=34><color=#00FFFF>T E R M I N A L   S Y N C   I N   P R O G R E S S . . .</color></size></b>\n<size=12><color=#66CCFF>ESTABLISHING SECURE CONNECTION TO SECTOR</color></size>", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = "0.9 0.9 0.9 1" }, RectTransform = { AnchorMin = "0 0.5", AnchorMax = "1 0.75" } }, UiLoadingScreen);

            string randomTip = type == 2 ? (targetParam == "Outpost" ? GetRandomTipFromList(config.Tips_Outpost) : targetParam == "Bandit" ? GetRandomTipFromList(config.Tips_Bandit) : targetParam == "Fishing" ? GetRandomTipFromList(config.Tips_Fishing) : GetRandomTipFromList(config.Tips_Private)) : GetRandomTipFromList(config.Tips_Private);

            string tipPanel = elements.Add(new CuiPanel { Image = { Color = "0 0.1 0.2 0.4" }, RectTransform = { AnchorMin = "0.15 0.25", AnchorMax = "0.85 0.35" } }, UiLoadingScreen);
            elements.Add(new CuiPanel { Image = { Color = "0 1 1 0.6" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "0.003 1" } }, tipPanel); 
            elements.Add(new CuiLabel { Text = { Text = randomTip, FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "0.85 0.95 1 1" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" } }, tipPanel);

            CuiHelper.AddUi(player, elements);
        }

        private bool CanAfford(BasePlayer player, Vector3 pos, out int f, out int w)
        {
            float d = Vector3.Distance(player.transform.position, pos);
            f = Mathf.CeilToInt(Mathf.Clamp((d / config.DistanceScale) * config.MaxCaloriesCost, 0f, config.MaxCaloriesCost));
            w = Mathf.CeilToInt(Mathf.Clamp((d / config.DistanceScale) * config.MaxHydrationCost, 0f, config.MaxHydrationCost));
            return player.metabolism.calories.value >= f && player.metabolism.hydration.value >= w;
        }

        private void OpenUI(BasePlayer player)
        {
            CloseUI(player); _openUIs.Add(player.userID); 
            var elements = new CuiElementContainer();

            elements.Add(new CuiPanel { Image = { Color = "0.03 0.05 0.07 0.92", Material = "assets/content/ui/uibackgroundblur.mat" }, RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-500 -300", OffsetMax = "500 300" }, CursorEnabled = true }, "Overlay", UiPanelName);
            
            elements.Add(new CuiPanel { Image = { Color = "0 1 1 0.8" }, RectTransform = { AnchorMin = "0 0.98", AnchorMax = "1 1" } }, UiPanelName);
            elements.Add(new CuiLabel { Text = { Text = "<b><size=18>Q U A N T U M   R E L A Y</size></b>   <size=12><color=#66CCFF>v10.1 TACTICAL NETWORK</color></size>", FontSize = 18, Align = TextAnchor.MiddleLeft, Color = "0.9 0.95 1 1" }, RectTransform = { AnchorMin = "0.02 0.88", AnchorMax = "0.8 0.96" } }, UiPanelName);
            elements.Add(new CuiButton { Button = { Command = "relay.execute 0 0", Color = "0.1 0.1 0.1 0.9" }, RectTransform = { AnchorMin = "0.94 0.89", AnchorMax = "0.98 0.95" }, Text = { Text = "<color=#FF5555><b>[ DISCONNECT ]</b></color>", FontSize = 12, Align = TextAnchor.MiddleCenter } }, UiPanelName);

            // ================== 左侧区：公共与战术枢纽 ==================
            elements.Add(new CuiPanel { Image = { Color = "0 1 1 0.2" }, RectTransform = { AnchorMin = "0.29 0.05", AnchorMax = "0.292 0.85" } }, UiPanelName);
            elements.Add(new CuiLabel { Text = { Text = "<color=#00FFFF>▶ PUBLIC SECTOR ◀</color>", FontSize = 13, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = "0.02 0.82", AnchorMax = "0.28 0.85" } }, UiPanelName);

            float leftY = 0.72f; float leftHeight = 0.11f; float leftSpacing = 0.02f;

            foreach (var kvp in _safeZones)
            {
                string key = kvp.Key; Vector3 pos = kvp.Value;
                
                string cdKey = $"{player.userID}_sz_{key}";
                float cd = _specialCooldowns.ContainsKey(cdKey) ? _specialCooldowns[cdKey] - Time.realtimeSinceStartup : 0;
                bool isCd = cd > 0;

                bool afford = CanAfford(player, pos, out int f, out int w);
                string title = key == "Outpost" ? "前哨站" : key == "Bandit" ? "匪徒营地" : "大型渔村";
                string gridStr = GetMapGrid(pos); // 💡 计算网格
                
                string btnColor = isCd ? "0.1 0.05 0.05 0.9" : (afford ? "0.05 0.15 0.2 0.9" : "0.15 0.05 0.05 0.9");
                string borderColor = isCd ? "1 0.2 0.2 0.5" : (afford ? "0 1 1 0.5" : "1 0 0 0.5");
                string stat = isCd ? $"<color=#FF5555>SYSTEM COOLDOWN: {Mathf.CeilToInt(cd)}s</color>" : (afford ? $"<color=#FFB455>F:{f}</color> <color=#66CCFF>W:{w}</color>" : "<color=#FF5555>NO SUPPLY</color>");
                string cmd = (!isCd && afford) ? $"relay.execute 2 {key}" : "";

                string panelName = elements.Add(new CuiButton { Button = { Command = cmd, Color = btnColor }, RectTransform = { AnchorMin = $"0.02 {leftY - leftHeight}", AnchorMax = $"0.27 {leftY}" }, Text = { Text = "" } }, UiPanelName);
                elements.Add(new CuiPanel { Image = { Color = borderColor }, RectTransform = { AnchorMin = "0 0", AnchorMax = "0.02 1" } }, panelName);
                
                // 💡 在标题下方加入 SEC-{网格} 显示
                elements.Add(new CuiLabel { Text = { Text = $"<b>{title}</b>\n<size=10><color=#55AAAA>SEC-{gridStr}</color> | 安全区</size>", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = isCd ? "0.6 0.6 0.6 1" : "1 1 1 1" }, RectTransform = { AnchorMin = "0.06 0", AnchorMax = "0.6 1" } }, panelName);
                elements.Add(new CuiLabel { Text = { Text = $"<size=10>{(isCd ? "LOCKED" : "CONSUME")}</size>\n{stat}", FontSize = 12, Align = TextAnchor.MiddleRight, Color = "0.8 0.8 0.8 1" }, RectTransform = { AnchorMin = "0.4 0", AnchorMax = "0.95 1" } }, panelName);

                leftY -= (leftHeight + leftSpacing);
            }

            // 返回点处理
            leftY -= 0.05f;
            if (_returnPoints.TryGetValue(player.userID, out Vector3 retPos))
            {
                string retCdKey = $"{player.userID}_return";
                float retCd = _specialCooldowns.ContainsKey(retCdKey) ? _specialCooldowns[retCdKey] - Time.realtimeSinceStartup : 0;
                bool isRetCd = retCd > 0;
                string retGrid = GetMapGrid(retPos); // 💡 计算返回点网格

                bool affordRet = CanAfford(player, retPos, out int rf, out int rw);
                string retBtnColor = isRetCd ? "0.1 0.05 0.05 0.9" : (affordRet ? "0.2 0.15 0.05 0.9" : "0.15 0.05 0.05 0.9");
                string retBorder = isRetCd ? "1 0.2 0.2 0.5" : (affordRet ? "1 0.6 0 0.5" : "1 0 0 0.5");
                string retStat = isRetCd ? $"<color=#FF5555>COOLDOWN: {Mathf.CeilToInt(retCd)}s</color>" : (affordRet ? $"<color=#FFB455>F:{rf}</color> <color=#66CCFF>W:{rw}</color>" : "<color=#FF5555>NO SUPPLY</color>");
                string retCmd = (!isRetCd && affordRet) ? "relay.execute 3 0" : "";

                string pName = elements.Add(new CuiButton { Button = { Command = retCmd, Color = retBtnColor }, RectTransform = { AnchorMin = $"0.02 {leftY - leftHeight}", AnchorMax = $"0.27 {leftY}" }, Text = { Text = "" } }, UiPanelName);
                elements.Add(new CuiPanel { Image = { Color = retBorder }, RectTransform = { AnchorMin = "0 0", AnchorMax = "0.02 1" } }, pName);
                
                // 💡 加入返回点坐标区域
                elements.Add(new CuiLabel { Text = { Text = $"<b>战术返回点</b>\n<size=10><color=#55AAAA>SEC-{retGrid}</color> | 高危降落</size>", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = isRetCd ? "0.6 0.6 0.6 1" : "1 1 1 1" }, RectTransform = { AnchorMin = "0.06 0", AnchorMax = "0.6 1" } }, pName);
                elements.Add(new CuiLabel { Text = { Text = $"<size=10>{(isRetCd ? "LOCKED" : "CONSUME")}</size>\n{retStat}", FontSize = 12, Align = TextAnchor.MiddleRight, Color = "0.8 0.8 0.8 1" }, RectTransform = { AnchorMin = "0.4 0", AnchorMax = "0.95 1" } }, pName);
            }

            // ================== 右侧区：瀑布流私人矩阵 ==================
            elements.Add(new CuiLabel { Text = { Text = "<color=#00FFFF>▶ PRIVATE SECTOR MATRIX ◀</color>", FontSize = 13, Align = TextAnchor.UpperLeft }, RectTransform = { AnchorMin = "0.32 0.82", AnchorMax = "0.98 0.85" } }, UiPanelName);

            var allBags = SleepingBag.FindForPlayer(player.userID, true)
                .Where(b => b != null && !string.IsNullOrEmpty(b.niceName) && !SystemBagNames.Contains(b.niceName))
                .OrderBy(b => b.niceName).ToList();
                
            int index = 0; float gridTop = 0.72f; float btnH = 0.12f; float spacingY = 0.03f; float btnW = 0.20f; float spacingX = 0.025f;

            foreach (var bag in allBags)
            {
                int row = index / 3; int col = index % 3; 
                float minX = 0.32f + col * (btnW + spacingX);
                float maxY = gridTop - row * (btnH + spacingY); float minY = maxY - btnH;
                if (minY < 0.05f) break; 

                float cd = bag.unlockTime - Time.realtimeSinceStartup;
                bool isCd = cd > 0;
                bool canAff = CanAfford(player, bag.transform.position, out int bf, out int bw);

                // 💡 识别类型 (大床/睡袋/房车)
                string typeStr = bag.ShortPrefabName.Contains("bed") ? "大床" : bag.ShortPrefabName.Contains("camper") ? "房车" : "睡袋";
                // 💡 计算该节点的区域坐标
                string gridStr = GetMapGrid(bag.transform.position);

                string bColor = isCd ? "0.1 0.05 0.05 0.8" : (!canAff ? "0.15 0.05 0.05 0.9" : "0.08 0.12 0.15 0.9");
                string cmd = (isCd || !canAff) ? "" : $"relay.execute 1 {bag.net.ID.Value}";
                
                string bName = elements.Add(new CuiButton { Button = { Command = cmd, Color = bColor }, RectTransform = { AnchorMin = $"{minX} {minY}", AnchorMax = $"{minX + btnW} {maxY}" }, Text = { Text = "" } }, UiPanelName);
                
                string topHighlight = isCd ? "1 0.2 0.2 0.8" : "0 1 1 0.5";
                elements.Add(new CuiPanel { Image = { Color = topHighlight }, RectTransform = { AnchorMin = "0 0.95", AnchorMax = "1 1" } }, bName);
                
                string statusText = isCd ? $"<color=#FF5555>LOCKED: {Mathf.CeilToInt(cd)}s</color>" : (!canAff ? "<color=#FF5555>NO SUPPLY</color>" : $"<color=#FFB455>F:{bf}</color> | <color=#66CCFF>W:{bw}</color>");
                string bNice = string.IsNullOrEmpty(bag.niceName) ? "NODE" : bag.niceName;

                // 💡 把物品类型拼装在名字后面，把坐标放在副标题里，并完美控制换行占比
                elements.Add(new CuiLabel { Text = { Text = $"<b>{bNice}</b> <size=10>[{typeStr}]</size>\n<size=10><color=#55AAAA>SEC-{gridStr}</color></size>", FontSize = 13, Align = TextAnchor.UpperLeft, Color = isCd ? "0.6 0.6 0.6 1" : "0.9 0.9 0.9 1" }, RectTransform = { AnchorMin = "0.05 0.35", AnchorMax = "0.95 0.85" } }, bName);
                elements.Add(new CuiLabel { Text = { Text = statusText, FontSize = 11, Align = TextAnchor.LowerRight, Color = "0.7 0.7 0.7 1" }, RectTransform = { AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.35" } }, bName);

                index++;
            }

            if (index == 0) elements.Add(new CuiLabel { Text = { Text = "<color=#66CCFF>[ NO PRIVATE NODES DETECTED ]</color>", FontSize = 13, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0.32 0", AnchorMax = "0.98 0.7" } }, UiPanelName);

            CuiHelper.AddUi(player, elements);
        }
    }
}