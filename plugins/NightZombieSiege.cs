// NightZombieSiege.cs - Oxide/uMod plugin for Rust
// v5.5.0 True Undead Edition Fixed: Corrected Mummy Suit Shortname & Native Mummy Prefab.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Oxide.Plugins
{
    [Info("NightZombieSiege", "AI-Generated", "5.5.0")]
    [Description("夜间尸潮围攻系统 - 真木乃伊紧急修复版 (修复裸体BUG，优先原生木乃伊预制体)。")]
    public class NightZombieSiege : RustPlugin
    {
        // 核心修复：加入原生木乃伊预制体作为最高优先级
        private static readonly string[] PrefabFallbacks = new string[]
        {
            "assets/prefabs/npc/mummy/mummy.prefab",
            "assets/prefabs/npc/murderer/murderer.prefab",
            "assets/prefabs/npc/scarecrow/scarecrow.prefab"
        };

        private static readonly string[] NormalWeapons = { "bone.club", "sickle", "pitchfork", "machete", "knife.bone" };
        private static readonly string[] RangedWeapons = { "bow.hunting", "crossbow", "pistol.revolver", "shotgun.waterpipe", "pistol.semiauto", "smg.2" };
        private static readonly string[] AmmoTypes = { "ammo.pistol", "ammo.rifle", "ammo.shotgun", "arrow.wooden" };
        private static readonly string[] MedTypes = { "bandage", "syringe.medical" };

        private static readonly int GroundMask = LayerMask.GetMask("Terrain", "World", "Construction");
        private static readonly int ObstacleMask = LayerMask.GetMask("Construction", "Deployed");

        private PluginConfig _cfg;
        private bool _isNight;

        private Timer _nightCheckTimer;
        private Timer _baseRefreshTimer;
        private Timer _spawnTimer;
        private Timer _aiTickTimer;
        private Timer _burnTimer;
        private Coroutine _aiCoroutine;
        private Coroutine _destroyCoroutine;

        private readonly Dictionary<ulong, ZombieAI> _zombieRegistry = new Dictionary<ulong, ZombieAI>();
        private readonly Dictionary<ulong, BaseRecord> _baseRecords = new Dictionary<ulong, BaseRecord>();

        public enum DifficultyTier { Easy, Medium, Hard, Epic }
        public enum ZombieState { MarchToTC, AttackObstacle, ChasePlayer, Idle }

        private sealed class TierLootConfig
        {
            [JsonProperty("废料掉落量 (格式: 最小-最大)")] public string ScrapRange;
            [JsonProperty("弹药掉落量 (格式: 最小-最大)")] public string AmmoRange;
            [JsonProperty("医疗品掉落量 (格式: 最小-最大)")] public string MedsRange;
            [JsonProperty("远程武器掉落概率 (0-1)")] public float WeaponChance;

            public TierLootConfig(string scrap, string ammo, string meds, float weaponChance)
            {
                ScrapRange = scrap; AmmoRange = ammo; MedsRange = meds; WeaponChance = weaponChance;
            }
            public TierLootConfig() { }
        }

        private sealed class PluginConfig
        {
            [JsonProperty("夜晚开始时间 (0-24)")] public float NightStartHour = 20f;
            [JsonProperty("夜晚结束时间 (0-24)")] public float NightEndHour = 6f;
            [JsonProperty("每个领地柜最大僵尸数")] public int MaxZombiesPerBase = 12;
            [JsonProperty("全服僵尸总数量上限")] public int GlobalZombieCap = 120;
            [JsonProperty("玩家活跃判定半径 (米)")] public float PlayerActiveRadius = 150f;
            
            [JsonProperty("普通僵尸移动速度倍率 (缓步)")] public float ZombieSpeedMultiplier = 0.55f; 
            [JsonProperty("精英僵尸移速倍率 (极慢)")] public float EliteSpeedMultiplier = 0.35f; 
            [JsonProperty("精英僵尸生成概率 (0-1)")] public float EliteSpawnChance = 0.15f;

            [JsonProperty("难度阈值 - 中型基地 (金属量)")] public float ThresholdMedium = 5000f;
            [JsonProperty("难度阈值 - 大型基地 (金属量)")] public float ThresholdHard = 15000f;
            [JsonProperty("难度阈值 - 史诗级基地 (金属量)")] public float ThresholdEpic = 45000f;

            [JsonProperty("简单难度战利品")] 
            public TierLootConfig EasyLoot = new TierLootConfig("5-15", "3-6", "1-2", 0.05f);
            [JsonProperty("中等难度战利品")] 
            public TierLootConfig MediumLoot = new TierLootConfig("15-30", "6-12", "1-3", 0.15f);
            [JsonProperty("困难难度战利品")] 
            public TierLootConfig HardLoot = new TierLootConfig("30-60", "12-20", "2-4", 0.25f);
            [JsonProperty("史诗难度战利品")] 
            public TierLootConfig EpicLoot = new TierLootConfig("60-120", "20-40", "3-5", 0.40f);
        }

        private sealed class BaseRecord
        {
            public Vector3 Position;
            public DifficultyTier Tier;
            public int ActiveZombieCount;
            public BaseEntity TCEntity;
            public BuildingPrivlidge Privlidge;
        }

        private sealed class ZombieAI : MonoBehaviour
        {
            public BaseEntity TargetTC;
            public BasePlayer TargetPlayer;
            public NavMeshAgent Agent;
            
            public ZombieState State = ZombieState.MarchToTC;
            public DifficultyTier Tier = DifficultyTier.Easy;
            public ulong OwnerBaseId;
            public bool IsElite = false;
            public bool IsBurningToDeath = false;

            public float GlobalSpeedMultiplier = 1f;
            public float EliteSpeedMultiplier = 1f;
            public float NextAttackAt;
            public float NextThinkAt;
            public float PathCacheExpiry;
            public Vector3 LastKnownDestination = Vector3.zero;

            private void LateUpdate()
            {
                if (Agent == null || !Agent.isOnNavMesh) return;
                
                float baseSpeed = (State == ZombieState.ChasePlayer) ? 3.5f : 2.5f;
                float finalSpeed = baseSpeed * GlobalSpeedMultiplier * (IsElite ? EliteSpeedMultiplier : 1f);
                Agent.speed = finalSpeed;
            }
        }

        protected override void LoadDefaultConfig()
        {
            _cfg = new PluginConfig();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try { _cfg = Config.ReadObject<PluginConfig>() ?? new PluginConfig(); }
            catch { _cfg = new PluginConfig(); }
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_cfg, true);

        private void OnServerInitialized()
        {
            AddCovalenceCommand("sc", nameof(CmdSiegeAdmin));
            _nightCheckTimer = timer.Every(10f, CheckDayNightCycle);
            _baseRefreshTimer = timer.Every(90f, RefreshAllBaseCaches);

            timer.Once(3f, () =>
            {
                RefreshAllBaseCaches();
                CheckDayNightCycle();
            });
            LogInfo("[夜间尸潮] v5.5.0 真·木乃伊版加载成功！修复了裸奔BUG，全员木乃伊皮肤已实装。");
        }

        private void Unload()
        {
            _nightCheckTimer?.Destroy();
            _baseRefreshTimer?.Destroy();
            _spawnTimer?.Destroy();
            _aiTickTimer?.Destroy();
            _burnTimer?.Destroy();

            if (_aiCoroutine != null) ServerMgr.Instance.StopCoroutine(_aiCoroutine);
            if (_destroyCoroutine != null) ServerMgr.Instance.StopCoroutine(_destroyCoroutine);

            ClearAllZombiesImmediate();
        }

        private void CheckDayNightCycle()
        {
            var sky = TOD_Sky.Instance;
            if (sky == null) return;

            bool nowNight = IsNightHour(sky.Cycle.Hour);
            if (nowNight == _isNight) return;

            _isNight = nowNight;
            if (_isNight) OnNightBegin();
            else OnDayBegin();
        }

        private bool IsNightHour(float hour)
        {
            float start = _cfg.NightStartHour;
            float end = _cfg.NightEndHour;
            return start > end ? hour >= start || hour < end : hour >= start && hour < end;
        }

        private void OnNightBegin()
        {
            Server.Broadcast("<color=#ff6666>夜幕降临，满身绷带的丧尸群正拖着沉重的步伐逼近...</color>");
            _spawnTimer?.Destroy();
            _aiTickTimer?.Destroy();
            _burnTimer?.Destroy();
            
            _spawnTimer = timer.Every(60f, SpawnWaveForAllBases);
            _aiTickTimer = timer.Every(0.75f, TickAllZombieAI);
            SpawnWaveForAllBases();
        }

        private void OnDayBegin()
        {
            Server.Broadcast("<color=#ffd166>黎明已至，丧尸在烈日下化为灰烬，物资也被悉数烧毁。</color>");
            _spawnTimer?.Destroy();
            _aiTickTimer?.Destroy();

            foreach (var ai in _zombieRegistry.Values.ToList())
            {
                if (ai != null)
                {
                    ai.IsBurningToDeath = true;
                    var npc = ai.GetComponent<NPCPlayer>();
                    if (npc != null) npc.SetFlag(BaseEntity.Flags.OnFire, true); 
                }
            }

            _burnTimer?.Destroy();
            _burnTimer = timer.Every(1.5f, () =>
            {
                if (_isNight) { _burnTimer?.Destroy(); return; }
                
                bool hasAlive = false;
                foreach (var ai in _zombieRegistry.Values.ToList())
                {
                    if (ai == null) continue;
                    var npc = ai.GetComponent<NPCPlayer>();
                    if (npc != null && !npc.IsDead())
                    {
                        hasAlive = true;
                        npc.Hurt(15f, Rust.DamageType.Heat, null, true);
                    }
                }
                if (!hasAlive) _burnTimer?.Destroy();
            });
        }

        private void RefreshAllBaseCaches()
        {
            var allTCs = BaseNetworkable.serverEntities.OfType<BuildingPrivlidge>()
                .Where(tc => tc != null && !tc.IsDestroyed).ToList();

            var liveIds = new HashSet<ulong>();
            foreach (var tc in allTCs) liveIds.Add(GetEntityId(tc));

            foreach (ulong staleId in _baseRecords.Keys.Where(id => !liveIds.Contains(id)).ToList())
                _baseRecords.Remove(staleId);

            foreach (var tc in allTCs)
            {
                ulong tcId = GetEntityId(tc);
                
                float score = 0f;
                if (tc.inventory?.itemList != null)
                {
                    foreach (var item in tc.inventory.itemList)
                    {
                        if (item.info.shortname == "metal.fragments") score += item.amount * 0.5f;
                        if (item.info.shortname == "metal.refined") score += item.amount * 2.0f;
                    }
                }
                
                if (!_baseRecords.TryGetValue(tcId, out var rec))
                {
                    rec = new BaseRecord { Privlidge = tc };
                    _baseRecords[tcId] = rec;
                }

                rec.Position = tc.transform.position;
                rec.Tier = ScoreToTier(score);
                rec.TCEntity = tc;
            }
        }

        private DifficultyTier ScoreToTier(float score)
        {
            if (score >= _cfg.ThresholdEpic) return DifficultyTier.Epic;
            if (score >= _cfg.ThresholdHard) return DifficultyTier.Hard;
            if (score >= _cfg.ThresholdMedium) return DifficultyTier.Medium;
            return DifficultyTier.Easy;
        }

        private void SpawnWaveForAllBases()
        {
            if (!_isNight) return;
            int globalCount = _zombieRegistry.Count;
            if (globalCount >= _cfg.GlobalZombieCap) return;

            foreach (var kv in _baseRecords.ToList())
            {
                if (globalCount >= _cfg.GlobalZombieCap) break;
                if (!IsOwnerOrAuthOnlineAndNearby(kv.Value)) continue;
                SpawnWaveForBase(kv.Key, kv.Value, ref globalCount);
            }
        }

        private bool IsOwnerOrAuthOnlineAndNearby(BaseRecord rec)
        {
            if (rec.Privlidge == null || rec.Privlidge.authorizedPlayers == null) return false;
            float activeRadiusSqr = _cfg.PlayerActiveRadius * _cfg.PlayerActiveRadius;

            foreach (ulong userId in rec.Privlidge.authorizedPlayers)
            {
                BasePlayer player = BasePlayer.FindByID(userId);
                if (player != null && player.IsConnected && !player.IsDead())
                {
                    if ((player.transform.position - rec.Position).sqrMagnitude < activeRadiusSqr)
                        return true;
                }
            }
            return false;
        }

        private void SpawnWaveForBase(ulong tcId, BaseRecord rec, ref int globalCount)
        {
            if (rec == null || rec.TCEntity == null || rec.TCEntity.IsDestroyed) return;
            if (rec.ActiveZombieCount >= _cfg.MaxZombiesPerBase) return;

            int toSpawn = Math.Min(2, _cfg.MaxZombiesPerBase - rec.ActiveZombieCount);
            for (int i = 0; i < toSpawn; i++)
            {
                Vector3 spawnPos = GetSafeSpawnPosition(rec.Position, 24f, 48f);
                if (spawnPos != Vector3.zero && SpawnSingleZombie(spawnPos, rec, tcId) != null)
                {
                    rec.ActiveZombieCount++;
                    globalCount++;
                }
            }
        }

        private Vector3 GetSafeSpawnPosition(Vector3 center, float minRadius, float maxRadius)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist = UnityEngine.Random.Range(minRadius, maxRadius);
                Vector3 candidate = center + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

                if (Physics.Raycast(candidate + Vector3.up * 80f, Vector3.down, out RaycastHit hit, 160f, GroundMask))
                    return hit.point;
            }
            return Vector3.zero;
        }

        private NPCPlayer SpawnSingleZombie(Vector3 pos, BaseRecord rec, ulong tcId, bool forceElite = false)
        {
            BaseEntity entity = null;
            foreach (string prefab in PrefabFallbacks)
            {
                entity = GameManager.server.CreateEntity(prefab, pos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
                if (entity != null) break;
            }

            if (entity == null) return null;

            var npc = entity as NPCPlayer;
            if (npc == null)
            {
                entity.Kill();
                return null;
            }

            bool isElite = forceElite || UnityEngine.Random.value <= _cfg.EliteSpawnChance;
            float health = isElite ? 450f : 120f;
            
            npc.startHealth = health;
            npc.Spawn();
            npc._maxHealth = health;
            npc.health = health;

            CustomizeZombieLoadout(npc, isElite);

            var ai = npc.gameObject.AddComponent<ZombieAI>();
            ai.Tier = rec.Tier;
            ai.OwnerBaseId = tcId;
            ai.TargetTC = rec.TCEntity;
            ai.IsElite = isElite;
            
            ai.GlobalSpeedMultiplier = _cfg.ZombieSpeedMultiplier;
            ai.EliteSpeedMultiplier = _cfg.EliteSpeedMultiplier;
            ai.Agent = npc.GetComponent<NavMeshAgent>();

            _zombieRegistry[GetEntityId(npc)] = ai;
            return npc;
        }

        // =====================================
        // 核心修复：精确的木乃伊皮肤覆盖
        // =====================================
        private void CustomizeZombieLoadout(NPCPlayer npc, bool isElite)
        {
            if (npc.inventory == null) return;

            // 1. 拔光原生服装，强制更衣
            if (npc.inventory.containerWear != null)
            {
                var oldClothes = npc.inventory.containerWear.itemList.ToList();
                foreach (var old in oldClothes) { old.RemoveFromContainer(); old.Remove(); }

                // 致命BUG修复点：必须使用 "halloween.mummysuit" 而不是 "mummysuit"
                GiveItemToWear(npc, "halloween.mummysuit");

                if (isElite)
                {
                    // 精英怪叠加冷酷无情的铁面具
                    GiveItemToWear(npc, "metal.facemask");
                }
            }

            // 2. 收缴枪械，发放生锈冷兵器
            if (npc.inventory.containerBelt != null)
            {
                var oldWeapons = npc.inventory.containerBelt.itemList.ToList();
                foreach (var old in oldWeapons) { old.RemoveFromContainer(); old.Remove(); }

                string weaponToGive = isElite ? "longsword" : NormalWeapons[UnityEngine.Random.Range(0, NormalWeapons.Length)];
                var weaponDef = ItemManager.FindItemDefinition(weaponToGive);
                if (weaponDef != null)
                {
                    var weapon = ItemManager.Create(weaponDef, 1, 0);
                    if (weapon != null && !weapon.MoveToContainer(npc.inventory.containerBelt, 0)) weapon.Remove();
                }
            }
        }

        private void GiveItemToWear(NPCPlayer npc, string shortname)
        {
            var def = ItemManager.FindItemDefinition(shortname);
            if (def != null)
            {
                var item = ItemManager.Create(def, 1, 0);
                if (item != null && !item.MoveToContainer(npc.inventory.containerWear))
                    item.Remove();
            }
        }

        private void TickAllZombieAI()
        {
            if (_aiCoroutine != null) return;
            _aiCoroutine = ServerMgr.Instance.StartCoroutine(TickBatchCoroutine(_zombieRegistry.Keys.ToList()));
        }

        private IEnumerator TickBatchCoroutine(List<ulong> keys)
        {
            int processedThisFrame = 0;
            foreach (ulong npcId in keys)
            {
                if (_zombieRegistry.TryGetValue(npcId, out var ai))
                    TickSingleZombie(npcId, ai);

                if (++processedThisFrame >= 16)
                {
                    processedThisFrame = 0;
                    yield return null;
                }
            }
            _aiCoroutine = null;
        }

        private void TickSingleZombie(ulong npcId, ZombieAI ai)
        {
            if (ai == null) return;
            var npc = ai.GetComponent<NPCPlayer>();
            if (npc == null || npc.IsDestroyed || npc.IsDead())
            {
                RemoveZombieRecord(npcId, ai.OwnerBaseId);
                return;
            }

            float now = Time.realtimeSinceStartup;
            if (now < ai.NextThinkAt) return;
            ai.NextThinkAt = now + 0.75f;

            switch (ai.State)
            {
                case ZombieState.MarchToTC: HandleMarchToTC(npc, ai, now); break;
                case ZombieState.AttackObstacle: HandleAttackObstacle(npc, ai, now); break;
                case ZombieState.ChasePlayer: HandleChasePlayer(npc, ai, now); break;
            }
        }

        private void HandleMarchToTC(NPCPlayer npc, ZombieAI ai, float now)
        {
            if (ai.TargetTC == null || ai.TargetTC.IsDestroyed) return;
            Vector3 dest = ai.TargetTC.transform.position;
            
            if ((npc.transform.position - dest).sqrMagnitude < 6.25f) return;

            Vector3 direction = (dest - npc.transform.position).normalized;
            if (Physics.Raycast(npc.transform.position + Vector3.up * 0.7f, direction, out RaycastHit hit, 4f, ObstacleMask))
            {
                ai.State = ZombieState.AttackObstacle;
                return;
            }
            SetDestination(ai, dest, now);
        }

        private void HandleAttackObstacle(NPCPlayer npc, ZombieAI ai, float now)
        {
            float cooldown = 2.0f * (ai.IsElite ? 1.8f : 1f);
            if (now < ai.NextAttackAt) return;
            ai.NextAttackAt = now + cooldown;

            if (!Physics.Raycast(npc.transform.position + Vector3.up * 0.8f, npc.transform.forward, out RaycastHit hit, 2.7f, ObstacleMask))
            {
                ai.State = ai.TargetPlayer == null ? ZombieState.MarchToTC : ZombieState.ChasePlayer;
                return;
            }

            var target = hit.GetEntity() as BaseCombatEntity;
            if (target != null && !target.IsDestroyed)
            {
                float dmg = 14f * 0.45f * (ai.IsElite ? 0.6f : 1f);
                target.Hurt(dmg, Rust.DamageType.Slash, npc, true);
            }
        }

        private void HandleChasePlayer(NPCPlayer npc, ZombieAI ai, float now)
        {
            if (ai.TargetPlayer == null || ai.TargetPlayer.IsDead())
            {
                ai.State = ZombieState.MarchToTC;
                return;
            }

            float sqrDist = (npc.transform.position - ai.TargetPlayer.transform.position).sqrMagnitude;
            if (sqrDist > 400f)
            {
                ai.TargetPlayer = null;
                ai.State = ZombieState.MarchToTC;
                return;
            }

            SetDestination(ai, ai.TargetPlayer.transform.position, now);

            float cooldown = 2.0f * (ai.IsElite ? 1.8f : 1f);
            if (sqrDist < 4f && now >= ai.NextAttackAt)
            {
                ai.NextAttackAt = now + cooldown;
                float dmg = 12f * (ai.IsElite ? 0.6f : 1f);
                ai.TargetPlayer.Hurt(dmg, Rust.DamageType.Slash, npc, false);
            }
        }

        private void SetDestination(ZombieAI ai, Vector3 destination, float now)
        {
            if (ai.Agent == null || !ai.Agent.isOnNavMesh) return;
            if (now > ai.PathCacheExpiry || (destination - ai.LastKnownDestination).sqrMagnitude > 9f)
            {
                ai.Agent.SetDestination(destination);
                ai.LastKnownDestination = destination;
                ai.PathCacheExpiry = now + 2f;
            }
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            var npc = entity as NPCPlayer;
            if (npc == null || info == null) return;

            ulong npcId = GetEntityId(npc);
            if (!_zombieRegistry.TryGetValue(npcId, out var ai)) return;

            if (ai.IsElite && info.damageTypes != null) info.damageTypes.ScaleAll(0.75f);

            if (info.InitiatorPlayer != null)
            {
                ai.State = ZombieState.ChasePlayer;
                ai.TargetPlayer = info.InitiatorPlayer;
            }
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            var npc = entity as NPCPlayer;
            if (npc == null) return;

            ulong npcId = GetEntityId(npc);
            if (!_zombieRegistry.TryGetValue(npcId, out var ai)) return;
            
            ulong ownerBaseId = ai.OwnerBaseId;
            timer.Once(1.5f, () => RemoveZombieRecord(npcId, ownerBaseId));
        }

        private void OnCorpsePopulate(BasePlayer player, BaseCorpse baseCorpse)
        {
            var npc = player as NPCPlayer;
            var corpse = baseCorpse as PlayerCorpse;
            if (npc == null || corpse == null) return;

            ulong npcId = GetEntityId(npc);
            if (!_zombieRegistry.TryGetValue(npcId, out var ai)) return;

            if (corpse.containers != null)
            {
                foreach (var container in corpse.containers)
                {
                    if (container != null && container.itemList != null)
                    {
                        var itemsToDestroy = container.itemList.ToList();
                        foreach (var item in itemsToDestroy) { item.RemoveFromContainer(); item.Remove(); }
                    }
                }
            }

            var dispenser = corpse.GetComponent<ResourceDispenser>();
            if (dispenser != null) UnityEngine.Object.Destroy(dispenser);

            if (ai.IsBurningToDeath) return; 

            InjectLootIntoCorpse(corpse, ai.Tier, ai.IsElite);
        }

        private void InjectLootIntoCorpse(PlayerCorpse corpse, DifficultyTier tier, bool isElite)
        {
            if (corpse.containers == null || corpse.containers.Length == 0) return;
            var mainContainer = corpse.containers[0];

            TierLootConfig lootCfg;
            switch (tier)
            {
                case DifficultyTier.Epic: lootCfg = _cfg.EpicLoot; break;
                case DifficultyTier.Hard: lootCfg = _cfg.HardLoot; break;
                case DifficultyTier.Medium: lootCfg = _cfg.MediumLoot; break;
                default: lootCfg = _cfg.EasyLoot; break;
            }

            int mult = isElite ? 2 : 1;

            int scrap = ParseRangeAndRoll(lootCfg.ScrapRange) * mult;
            if (scrap > 0) GiveItemSafe(mainContainer, "scrap", scrap);

            int ammo = ParseRangeAndRoll(lootCfg.AmmoRange) * mult;
            if (ammo > 0) GiveItemSafe(mainContainer, AmmoTypes[UnityEngine.Random.Range(0, AmmoTypes.Length)], ammo);

            int meds = ParseRangeAndRoll(lootCfg.MedsRange) * mult;
            if (meds > 0) GiveItemSafe(mainContainer, MedTypes[UnityEngine.Random.Range(0, MedTypes.Length)], meds);

            if (UnityEngine.Random.value <= lootCfg.WeaponChance)
            {
                string gun = RangedWeapons[UnityEngine.Random.Range(0, RangedWeapons.Length)];
                GiveItemSafe(mainContainer, gun, 1);
            }
        }

        private int ParseRangeAndRoll(string rangeStr)
        {
            if (string.IsNullOrEmpty(rangeStr)) return 0;
            var parts = rangeStr.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out int min) && int.TryParse(parts[1], out int max))
                return UnityEngine.Random.Range(min, max + 1);
            if (parts.Length == 1 && int.TryParse(parts[0], out int exact))
                return exact;
            return 0;
        }

        private void GiveItemSafe(ItemContainer container, string shortname, int amount)
        {
            var def = ItemManager.FindItemDefinition(shortname);
            if (def != null)
            {
                var item = ItemManager.Create(def, amount);
                if (item != null && !item.MoveToContainer(container)) item.Remove();
            }
        }

        private void RemoveZombieRecord(ulong npcId, ulong ownerBaseId)
        {
            _zombieRegistry.Remove(npcId);
            if (_baseRecords.TryGetValue(ownerBaseId, out var rec))
                rec.ActiveZombieCount = Math.Max(0, rec.ActiveZombieCount - 1);
        }

        private void SafeDestroyZombie(ulong npcId)
        {
            var entity = BaseNetworkable.serverEntities.Find(new NetworkableId(npcId)) as BaseEntity;
            if (entity != null && !entity.IsDestroyed) entity.Kill();
        }

        private void ClearAllZombiesImmediate()
        {
            var snapshot = _zombieRegistry.Keys.ToList();
            _zombieRegistry.Clear();
            foreach (var rec in _baseRecords.Values) rec.ActiveZombieCount = 0;
            foreach (ulong id in snapshot) SafeDestroyZombie(id);
        }

        private void ClearAllZombies()
        {
            var snapshot = _zombieRegistry.Keys.ToList();
            _zombieRegistry.Clear();
            foreach (var rec in _baseRecords.Values) rec.ActiveZombieCount = 0;
            if (_destroyCoroutine != null) ServerMgr.Instance.StopCoroutine(_destroyCoroutine);
            _destroyCoroutine = ServerMgr.Instance.StartCoroutine(DestroyBatchCoroutine(snapshot));
        }

        private IEnumerator DestroyBatchCoroutine(List<ulong> zombieIds)
        {
            int processedThisFrame = 0;
            foreach (ulong id in zombieIds)
            {
                SafeDestroyZombie(id);
                if (++processedThisFrame >= 12)
                {
                    processedThisFrame = 0;
                    yield return null;
                }
            }
            _destroyCoroutine = null;
        }

        private void CmdSiegeAdmin(Oxide.Core.Libraries.Covalence.IPlayer iPlayer, string command, string[] args)
        {
            if (!iPlayer.IsAdmin) return;
            bool forceElite = false;
            int count = 3;

            if (args.Length > 0)
            {
                if (args[0].ToLowerInvariant() == "elite")
                {
                    forceElite = true;
                    if (args.Length >= 2 && !int.TryParse(args[1], out count)) count = 3;
                }
                else if (!int.TryParse(args[0], out count)) count = 3;
            }

            var rustPlayer = iPlayer.Object as BasePlayer;
            if (rustPlayer == null) return;
            var fakeRec = new BaseRecord { Position = rustPlayer.transform.position, Tier = DifficultyTier.Medium, TCEntity = rustPlayer };
            
            for (int i = 0; i < Clamp(count, 1, 10); i++)
            {
                Vector3 pos = GetSafeSpawnPosition(rustPlayer.transform.position, 3f, 6f);
                if (pos == Vector3.zero) pos = rustPlayer.transform.position + rustPlayer.transform.forward * 2f;
                SpawnSingleZombie(pos, fakeRec, 0UL, forceElite);
            }
            iPlayer.Reply($"[夜间尸潮] 成功在周围测试生成了纯正木乃伊怪群！");
        }

        private ulong GetEntityId(BaseNetworkable entity) => entity?.net == null ? 0UL : (ulong)entity.net.ID.Value;
        private int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);
        private void LogInfo(string message) => Puts(message);
    }
}