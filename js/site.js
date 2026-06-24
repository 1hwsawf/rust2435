const monuments = {
  ferry: {
    name: "渡轮码头",
    grid: "RustMaps 坐标 319,66",
    type: "water",
    x: 49.84,
    y: 10.31,
    loot: "海岸交通、基础补给、码头资源",
    card: "无需卡片",
    risk: "低风险，靠近海岸线",
    desc: "渡轮码头位于地图北部海岸，是海路转移和初期补给的可用节点。"
  },
  largeharbor: {
    name: "大型港口",
    grid: "RustMaps 坐标 574,488",
    type: "water",
    x: 89.69,
    y: 76.25,
    loot: "集装箱、回收机、海岸物资",
    card: "绿卡基础流程",
    risk: "低到中等风险，适合初中期搜刮",
    desc: "大型港口位于地图东南海岸，资源路线清晰，适合作为海岸起家和废料回收点。"
  },
  smallharbor: {
    name: "小型港口",
    grid: "RustMaps 坐标 584,140",
    type: "water",
    x: 91.25,
    y: 21.88,
    loot: "基础箱、回收机、绿卡流程",
    card: "绿卡基础流程",
    risk: "低风险，靠近海岸",
    desc: "小型港口位于东北海岸，适合快速搜集基础废料并进入卡片流程。"
  },
  fishinga: {
    name: "渔村 A",
    grid: "RustMaps 坐标 136,115",
    type: "safe",
    x: 21.25,
    y: 17.97,
    loot: "船只交易、潜水装备、任务与安全补给",
    card: "无需卡片",
    risk: "安全区",
    desc: "渔村 A 位于西北海岸，是购买船只、获取海上行动资源的安全据点。"
  },
  fishingb: {
    name: "渔村 B",
    grid: "RustMaps 坐标 527,355",
    type: "safe",
    x: 82.34,
    y: 55.47,
    loot: "船只交易、任务 NPC、海上补给",
    card: "无需卡片",
    risk: "安全区",
    desc: "渔村 B 位于东部海岸，适合连接港口、油井和海上路线。"
  },
  fishingc: {
    name: "渔村 C",
    grid: "RustMaps 坐标 316,467",
    type: "safe",
    x: 49.38,
    y: 72.97,
    loot: "船只、潜水装备、海上任务",
    card: "无需卡片",
    risk: "安全区",
    desc: "渔村 C 位于南部海岸，是南线海上行动的补给与交易点。"
  },
  outpost: {
    name: "前哨站",
    grid: "RustMaps 坐标 360,271",
    type: "safe",
    x: 56.25,
    y: 42.34,
    loot: "1/2 级工作台、回收机、资源商店、安全交易",
    card: "无需卡片，安全区禁止拔枪",
    risk: "0% 辐射，机枪塔防卫",
    desc: "全图核心补给与交易中心，适合独狼和新手中转。这里不是战场，是补给、维修、回收和重整节奏的安全锚点。"
  },
  bandit: {
    name: "盗匪营地",
    grid: "RustMaps 坐标 295,402",
    type: "safe",
    x: 46.09,
    y: 62.81,
    loot: "武器黑市、赌场、任务 NPC、废料交易",
    card: "无需卡片，黑市安全区",
    risk: "0% 辐射，守卫巡逻",
    desc: "隐蔽的废土黑市，适合小队补给和交易。玩家可以在这里用废料换取高级装备，也可以进行风险更高的经济博弈。"
  },
  powerplant: {
    name: "废弃发电厂",
    grid: "RustMaps 坐标 207,355",
    type: "danger",
    x: 32.34,
    y: 55.47,
    loot: "高阶军工箱、金属资源、红卡流程",
    card: "蓝卡 / 红卡联动解谜",
    risk: "25% 强辐射，高伏击概率",
    desc: "工业地形复杂，适合伏击与绕点。高收益对应高风险，是中后期玩家争夺红卡路线的重要节点。"
  },
  watertreatment: {
    name: "污水处理厂",
    grid: "RustMaps 坐标 457,221",
    type: "danger",
    x: 71.41,
    y: 34.53,
    loot: "军用箱、补给箱、蓝卡路线、回收机",
    card: "蓝卡解谜流程",
    risk: "中等辐射，中高交战率",
    desc: "污水处理厂位于地图东侧中部，是常见中期争夺点，资源稳定且路线较集中。"
  },
  sewer: {
    name: "下水道分支",
    grid: "RustMaps 坐标 452,474",
    type: "danger",
    x: 70.63,
    y: 74.06,
    loot: "绿卡、蓝卡过渡、补给箱、回收机",
    card: "绿卡流程",
    risk: "中等风险，适合小队快速搜刮",
    desc: "下水道分支位于东南区域，是前期进入卡片体系的关键路线。"
  },
  junkyard: {
    name: "垃圾场",
    grid: "RustMaps 坐标 518,130",
    type: "normal",
    x: 80.94,
    y: 20.31,
    loot: "废料、车辆部件、基础箱、回收机",
    card: "无需卡片",
    risk: "中等风险，适合前期积累",
    desc: "垃圾场位于东北区域，资源分散但废料稳定，适合前期过渡。"
  },
  satellite: {
    name: "卫星天线",
    grid: "RustMaps 坐标 385,128",
    type: "normal",
    x: 60.16,
    y: 20.0,
    loot: "基础箱、军用箱、蓝图碎片",
    card: "无需卡片",
    risk: "中等风险，视野开阔",
    desc: "卫星天线位于北部偏中区域，地形开阔，适合搜刮但容易被远点观察。"
  },
  spheretank: {
    name: "圆顶油罐",
    grid: "RustMaps 坐标 416,446",
    type: "danger",
    x: 65.0,
    y: 69.69,
    loot: "军用箱、油桶、爬塔资源",
    card: "无需卡片",
    risk: "高处坠落风险，中等交战率",
    desc: "圆顶油罐位于南部偏东，资源集中但路线暴露。"
  },
  largerig: {
    name: "大型油井",
    grid: "RustMaps 坐标 56,-47",
    type: "water",
    x: 8.75,
    y: 2.5,
    loot: "重装科学家箱、成品枪、火箭与高级物资",
    card: "红卡终局解谜",
    risk: "30% 致命威胁，海上终局战场",
    desc: "顶级海上资源点。建议小队携带快艇、直升机或充足弹药进入，启动流程后会触发高强度科学家防守。"
  },
  smallrig: {
    name: "小型油井",
    grid: "RustMaps 坐标 311,634",
    type: "water",
    x: 48.59,
    y: 97.5,
    loot: "科学家箱、蓝卡/红卡流程、海上物资",
    card: "蓝卡到红卡流程",
    risk: "高风险，海上交战点",
    desc: "小型油井位于南部海域，是中期海上争夺点，收益高但撤离路线容易被拦截。"
  }
};

const pluginCards = [
  ["GatherManager", "3x Gather Rate Manager", "全服核心倍率引擎，木材、矿石、硫磺、动物采集提升至 3 倍，黄金时间提升至 6 倍。", "Wipe: 3X / 6X", "core", "fa-wheat-awn"],
  ["Backpacks", "Extra Inventory Space", "额外随身背包，提升探索续航与物资转移效率，适合长途搜刮和团队补给。", "/backpack", "auto", "fa-briefcase"],
  ["CraftingController", "Instant Crafting Speed", "瞬间制作系统，减少等待，提升 PVP 与造家节奏，让玩家把时间花在对抗和运营上。", "Instant Craft", "core", "fa-hammer"],
  ["NightZombieSiege", "PvE Zombie Invasions", "夜间尸潮围攻系统，基地会在夜幕降临后遭遇木乃伊与精英怪进攻，并掉落奖励。", "Night Event", "combat", "fa-biohazard"],
  ["NoDemolishTime", "Unlimited Wall Demolition", "在建筑权限范围内重新激活整栋建筑的拆除与旋转菜单，解决误建焦虑。", "Hammer Hit", "core", "fa-trowel-bricks"],
  ["QuantumRelay", "Tactical Relay Network", "量子中继传送网络，支持安全区、睡袋、返回点跃迁，带战斗锁定和资源消耗。", "/tp /relay", "auto", "fa-circle-nodes"],
  ["SimpleSorter", "One-Click Sorter", "箱子与背包整理按钮，支持匹配收取、存入、全部转移与容器排序。", "UI Sort Button", "auto", "fa-filter"],
  ["StackSizeController", "Extended Stack Sizes", "堆叠上限控制，扩展资源、弹药和常用物资堆叠，显著减少箱子占用。", "Stack Rules", "core", "fa-boxes-stacked"],
  ["AutoDoors", "Automatic Door Closing", "自动关门系统，可设置门类、单门或全局延迟，防止漏关门被深潜。", "/ad /autodoor", "combat", "fa-door-closed"]
];

const sourceFiles = [
  "AutoDoors",
  "Backpacks",
  "CraftingController",
  "GatherManager",
  "NightZombieSiege",
  "NoDemolishTime",
  "QuantumRelay",
  "SimpleSorter",
  "StackSizeController"
];

const translations = {
  zh: {
    "nav.map": "战术地图",
    "nav.plugins": "插件能力",
    "nav.source": "开源代码",
    "hero.eyebrow": "2026 NEXT-GEN RUST SERVER SITE",
    "hero.desc": "免加速器、无付费特权、无 MOD 白名单。整站以 2026 风格玻璃拟态界面呈现，地图、插件和源码都集中在一个现代化页面里。",
    "action.copy": "复制",
    "action.viewMap": "查看地图",
    "hero.mapTitle": "地图",
    "hero.mapSub": "点击查看第三方地图",
    "stats.provider": "第三方地图",
    "stats.size": "地图大小",
    "stats.seed": "地图种子",
    "map.title": "真实地图视图",
    "map.open": "打开第三方地图",
    "map.note": "地图坐标以 RustMaps 页面为准",
    "plugins.title": "九大插件能力",
    "filter.all": "全部",
    "filter.core": "基础与倍率",
    "filter.auto": "智能收纳",
    "filter.combat": "防守战斗",
    "source.title": "插件源码开放查看",
    "source.download": "下载",
    "source.copy": "复制源码",
    "footer.note": "本服务器非 Facepunch Studios 官方赞助或关联。本页面用于展示服务器信息、地图与插件源码。",
    "toast.connect": "直连命令已复制",
    "toast.sourcePending": "源码还没有加载完成",
    "source.loading": "正在加载源码...",
    "source.preview": "预览源码",
    "source.opened": "已打开 {name}.cs 源码，可直接复制全部内容",
    "source.copied": "{name}.cs 源码已复制",
    "source.failed": "源码加载失败：plugins/{name}.cs\n请通过本地服务器访问页面，而不是直接双击 file:// 打开。"
  },
  en: {
    "nav.map": "Map",
    "nav.plugins": "Plugins",
    "nav.source": "Source",
    "hero.eyebrow": "2026 NEXT-GEN RUST SERVER SITE",
    "hero.desc": "No accelerator required, no paid privileges, and no mod whitelist. The site uses a 2026 glassmorphism interface that brings the map, plugins, and source code into one modern page.",
    "action.copy": "Copy",
    "action.viewMap": "View map",
    "hero.mapTitle": "MAP",
    "hero.mapSub": "Open the third-party map",
    "stats.provider": "Third-party map",
    "stats.size": "Map size",
    "stats.seed": "Map seed",
    "map.title": "Live map view",
    "map.open": "Open third-party map",
    "map.note": "Coordinates are based on the RustMaps page",
    "plugins.title": "Nine plugin features",
    "filter.all": "All",
    "filter.core": "Core",
    "filter.auto": "Utility",
    "filter.combat": "Defense",
    "source.title": "Plugin source viewer",
    "source.download": "Download",
    "source.copy": "Copy source",
    "footer.note": "This server is not sponsored by or affiliated with Facepunch Studios. This page displays server information, the map, and plugin source code.",
    "toast.connect": "Connection command copied",
    "toast.sourcePending": "Source code is still loading",
    "source.loading": "Loading source code...",
    "source.preview": "Preview source",
    "source.opened": "{name}.cs opened. You can copy the full source code.",
    "source.copied": "{name}.cs source copied",
    "source.failed": "Failed to load source: plugins/{name}.cs\nPlease access this page through a local server instead of opening it directly with file://."
  }
};

const pluginLocales = {
  zh: {
    GatherManager: ["3x Gather Rate Manager", "全服核心倍率引擎，木材、矿石、硫磺、动物采集提升至 3 倍，黄金时间提升至 6 倍。"],
    Backpacks: ["Extra Inventory Space", "额外随身背包，提升探索续航与物资转移效率，适合长途搜刮和团队补给。"],
    CraftingController: ["Instant Crafting Speed", "瞬间制作系统，减少等待，提升 PVP 与造家节奏，让玩家把时间花在对抗和运营上。"],
    NightZombieSiege: ["PvE Zombie Invasions", "夜间尸潮围攻系统，基地会在夜幕降临后遭遇木乃伊与精英怪进攻，并掉落奖励。"],
    NoDemolishTime: ["Unlimited Wall Demolition", "在建筑权限范围内重新激活整栋建筑的拆除与旋转菜单，解决误建焦虑。"],
    QuantumRelay: ["Tactical Relay Network", "量子中继传送网络，支持安全区、睡袋、返回点跃迁，带战斗锁定和资源消耗。"],
    SimpleSorter: ["One-Click Sorter", "箱子与背包整理按钮，支持匹配收取、存入、全部转移与容器排序。"],
    StackSizeController: ["Extended Stack Sizes", "堆叠上限控制，扩展资源、弹药和常用物资堆叠，显著减少箱子占用。"],
    AutoDoors: ["Automatic Door Closing", "自动关门系统，可设置门类、单门或全局延迟，防止漏关门被深潜。"]
  },
  en: {
    GatherManager: ["3x Gather Rate Manager", "Core gather-rate engine. Wood, ore, sulfur, and animal harvests are increased to 3x, with 6x during prime time."],
    Backpacks: ["Extra Inventory Space", "Adds a portable backpack for longer exploration runs and easier team logistics."],
    CraftingController: ["Instant Crafting Speed", "Removes long crafting waits so players can spend more time on fights, building, and progression."],
    NightZombieSiege: ["PvE Zombie Invasions", "Night raids bring mummies and elite enemies to player bases, with reward drops after defense."],
    NoDemolishTime: ["Unlimited Wall Demolition", "Re-enables demolish and rotate options inside building privilege to reduce misbuild frustration."],
    QuantumRelay: ["Tactical Relay Network", "Teleport network for safe zones, bags, and return points, with combat locks and resource costs."],
    SimpleSorter: ["One-Click Sorter", "Adds sorting buttons for boxes and backpacks, including matching, deposit, transfer-all, and container sorting."],
    StackSizeController: ["Extended Stack Sizes", "Raises stack limits for resources, ammo, and common items to reduce storage pressure."],
    AutoDoors: ["Automatic Door Closing", "Automatically closes doors with configurable delays for door types, individual doors, or global rules."]
  }
};

let activeSource = "AutoDoors";
let activeSourceCode = "";
let toastTimer;
let currentLang = "zh";
let map;
let mapMarkers = {};
let activeMarkerId = null;

const MAP_SIZE = 1440;
const MAP_BOUNDS = [[0, 0], [MAP_SIZE, MAP_SIZE]];

function byId(id) {
  return document.getElementById(id);
}

function t(key, vars = {}) {
  const value = translations[currentLang][key] || translations.zh[key] || key;
  return Object.entries(vars).reduce((text, [name, replacement]) => text.replace(`{${name}}`, replacement), value);
}

function applyLanguage(lang) {
  currentLang = lang;
  document.documentElement.lang = lang === "zh" ? "zh-CN" : "en";
  document.querySelectorAll("[data-i18n]").forEach(element => {
    element.textContent = t(element.dataset.i18n);
  });
  const toggle = byId("langToggle");
  if (toggle) toggle.textContent = lang === "zh" ? "EN" : "中";
  renderPlugins();
}

function initLanguage() {
  const saved = localStorage.getItem("site-lang");
  applyLanguage(saved === "en" ? "en" : "zh");
}

function initTheme() {
  const media = window.matchMedia("(prefers-color-scheme: light)");
  const apply = () => {
    document.documentElement.dataset.theme = media.matches ? "light" : "dark";
  };
  apply();
  media.addEventListener("change", apply);
}

function showToast(message) {
  const toast = byId("toast");
  byId("toastText").textContent = message;
  toast.classList.add("show");
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => toast.classList.remove("show"), 2400);
}

function copyText(text, message) {
  if (navigator.clipboard && window.isSecureContext) {
    navigator.clipboard.writeText(text).then(() => showToast(message));
    return;
  }
  const area = document.createElement("textarea");
  area.value = text;
  document.body.appendChild(area);
  area.select();
  document.execCommand("copy");
  area.remove();
  showToast(message);
}

function pctToCoord(pct) {
  return (pct / 100) * MAP_SIZE;
}

function updateMonument(id) {
  const item = monuments[id] || monuments.outpost;
  byId("monumentGrid").textContent = item.grid;
  byId("monumentName").textContent = item.name;
  byId("monumentLoot").textContent = item.loot;
  byId("monumentCard").textContent = item.card;
  byId("monumentRisk").textContent = item.risk;
  byId("monumentDesc").textContent = item.desc;

  if (activeMarkerId && mapMarkers[activeMarkerId]) {
    const el = mapMarkers[activeMarkerId].getElement();
    if (el) el.classList.remove("is-active");
  }
  activeMarkerId = id;
  if (mapMarkers[id]) {
    const el = mapMarkers[id].getElement();
    if (el) el.classList.add("is-active");
  }
}

function filterMarkers(type) {
  Object.entries(mapMarkers).forEach(([id, marker]) => {
    const item = monuments[id];
    const visible = type === "all" || item.type === type;
    if (visible) {
      marker.addTo(map);
    } else {
      marker.remove();
    }
  });

  document.querySelectorAll("[data-map-filter]").forEach(button => {
    button.classList.toggle("active", button.dataset.mapFilter === type);
  });
}

function createCustomIcon(item) {
  const classMap = {
    safe: "marker-safe",
    danger: "marker-danger",
    water: "marker-water",
    normal: "marker-normal"
  };
  const iconMap = {
    safe: "fa-shield-halved",
    danger: "fa-skull-crossbones",
    water: "fa-water",
    normal: "fa-location-dot"
  };
  const markerClass = classMap[item.type] || classMap.normal;
  const iconClass = iconMap[item.type] || iconMap.normal;
  return L.divIcon({
    className: "",
    html: `
      <div class="custom-marker-icon ${markerClass}">
        <span class="marker-symbol"><i class="fa-solid ${iconClass}"></i></span>
        <span class="marker-name">${item.name}</span>
      </div>
    `,
    iconSize: [42, 42],
    iconAnchor: [21, 21],
    popupAnchor: [0, -22]
  });
}

function initLeafletMap() {
  const container = byId("leafletMap");
  if (!container) return;

  map = L.map(container, {
    crs: L.CRS.Simple,
    minZoom: -2,
    maxZoom: 4,
    zoomControl: true,
    attributionControl: false,
    maxBounds: [[-200, -200], [MAP_SIZE + 200, MAP_SIZE + 200]],
    maxBoundsViscosity: 1.0
  });

  L.imageOverlay("assets/rust-map-107be62-icons.png", MAP_BOUNDS, {
    opacity: 1,
    interactive: false
  }).addTo(map);

  map.fitBounds(MAP_BOUNDS, { padding: [20, 20], animate: false });

  Object.entries(monuments).forEach(([id, item]) => {
    const lat = MAP_SIZE - pctToCoord(item.y);
    const lng = pctToCoord(item.x);
    const marker = L.marker([lat, lng], {
      icon: createCustomIcon(item),
      title: item.name
    }).addTo(map);

    marker.bindPopup(item.name, {
      closeButton: true,
      className: "rust-popup",
      offset: [0, -10]
    });

    marker.on("click", () => {
      updateMonument(id);
    });

    mapMarkers[id] = marker;
  });

  map.on("popupclose", () => {
    if (activeMarkerId && mapMarkers[activeMarkerId]) {
      const el = mapMarkers[activeMarkerId].getElement();
      if (el) el.classList.remove("is-active");
    }
  });
}

function renderPlugins() {
  const grid = byId("pluginGrid");
  grid.innerHTML = pluginCards.map(([name, tagline, desc, command, cat, icon]) => {
    const localized = pluginLocales[currentLang][name] || [tagline, desc];
    return `
    <article class="plugin-card glass" data-plugin-cat="${cat}" data-plugin-source="${name}" tabindex="0" role="button" aria-label="${currentLang === "zh" ? "查看" : "View"} ${name} ${currentLang === "zh" ? "源码" : "source"}">
      <div class="plugin-icon"><i class="fa-solid ${icon}"></i></div>
      <h3>${name}</h3>
      <p class="tagline">${localized[0]}</p>
      <p>${localized[1]}</p>
      <div class="plugin-card-footer">
        <span class="command-chip">${command}</span>
        <span class="source-jump">${t("source.preview")} <i class="fa-solid fa-arrow-right"></i></span>
      </div>
    </article>
  `;
  }).join("");

  grid.querySelectorAll("[data-plugin-source]").forEach(card => {
    const open = () => openPluginSource(card.dataset.pluginSource);
    card.addEventListener("click", open);
    card.addEventListener("keydown", event => {
      if (event.key === "Enter" || event.key === " ") {
        event.preventDefault();
        open();
      }
    });
  });
}

function filterPlugins(cat) {
  document.querySelectorAll("[data-plugin-cat]").forEach(card => {
    card.style.display = cat === "all" || card.dataset.pluginCat === cat ? "block" : "none";
  });
  document.querySelectorAll("[data-plugin-filter]").forEach(button => {
    button.classList.toggle("active", button.dataset.pluginFilter === cat);
  });
}

async function openPluginSource(name) {
  await loadSource(name);
  document.querySelectorAll("[data-plugin-source]").forEach(card => {
    card.classList.toggle("is-selected", card.dataset.pluginSource === name);
  });
  byId("source").scrollIntoView({ behavior: "smooth", block: "start" });
  showToast(t("source.opened", { name }));
}

function renderSourceTabs() {
  byId("sourceTabs").innerHTML = sourceFiles.map(name => `
    <button class="tab-btn ${name === activeSource ? "active" : ""}" data-source="${name}">${name}</button>
  `).join("");
}

async function loadSource(name) {
  activeSource = name;
  renderSourceTabs();
  byId("sourceFileName").textContent = `${name}.cs`;
  byId("sourceDownload").href = `plugins/${name}.cs`;
  byId("sourceDownload").download = `${name}.cs`;
  byId("sourceCode").innerHTML = `<pre>${t("source.loading")}</pre>`;

  try {
    const response = await fetch(`plugins/${name}.cs`, { cache: "no-store" });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    activeSourceCode = await response.text();
    byId("sourceCode").innerHTML = `<pre>${highlightCSharp(activeSourceCode)}</pre>`;
    document.querySelectorAll("[data-plugin-source]").forEach(card => {
      card.classList.toggle("is-selected", card.dataset.pluginSource === name);
    });
  } catch (error) {
    activeSourceCode = "";
    byId("sourceCode").innerHTML = `<pre>${t("source.failed", { name })}</pre>`;
  }
}

function highlightCSharp(code) {
  const escapeMap = { "&": "&amp;", "<": "&lt;", ">": "&gt;" };
  let html = code.replace(/[&<>]/g, ch => escapeMap[ch]);

  html = html.replace(/(\/\/.*?$)/gm, '<span class="tok-com">$1</span>');
  html = html.replace(/("(?:\\.|[^"\\])*")/g, '<span class="tok-str">$1</span>');

  const keywords = [
    "using", "namespace", "class", "public", "private", "protected", "internal",
    "static", "readonly", "const", "void", "return", "if", "else", "for",
    "foreach", "while", "switch", "case", "break", "continue", "new", "this",
    "base", "override", "virtual", "sealed", "abstract", "try", "catch",
    "finally", "throw", "true", "false", "null", "string", "int", "float",
    "double", "bool", "var", "object", "enum", "struct", "interface", "get",
    "set", "out", "ref", "in"
  ];

  html = html.replace(new RegExp(`\\b(${keywords.join("|")})\\b`, "g"), '<span class="tok-key">$1</span>');
  html = html.replace(/\b(\d+(?:\.\d+)?f?)\b/g, '<span class="tok-num">$1</span>');
  html = html.replace(/\b([A-Za-z_]\w*)(?=\s*\()/g, '<span class="tok-fn">$1</span>');
  return html;
}

function updateGoldenHour() {
  const now = new Date();
  const current = now.getHours() * 3600 + now.getMinutes() * 60 + now.getSeconds();
  const start = 18 * 3600;
  const end = 22 * 3600;
  const active = current >= start && current < end;
  const remaining = active ? end - current : (current < start ? start - current : 24 * 3600 - current + start);
  const h = String(Math.floor(remaining / 3600)).padStart(2, "0");
  const m = String(Math.floor((remaining % 3600) / 60)).padStart(2, "0");
  const s = String(remaining % 60).padStart(2, "0");

  byId("eventState").textContent = active ? "6X 黄金时间进行中" : "3X 常规倍率运行中";
  byId("eventTime").textContent = `${h}:${m}:${s}`;
  byId("eventHint").textContent = active ? "距离本轮黄金时间结束" : "距离下一轮黄金时间开始";
}

function bindEvents() {
  byId("copyConnect").addEventListener("click", () => {
    copyText("client.connect 183.214.37.205:48930", t("toast.connect"));
  });

  byId("langToggle").addEventListener("click", () => {
    const next = currentLang === "zh" ? "en" : "zh";
    localStorage.setItem("site-lang", next);
    applyLanguage(next);
  });

  document.querySelectorAll("[data-plugin-filter]").forEach(button => {
    button.addEventListener("click", () => filterPlugins(button.dataset.pluginFilter));
  });

  byId("sourceTabs").addEventListener("click", event => {
    const button = event.target.closest("[data-source]");
    if (button) loadSource(button.dataset.source);
  });

  byId("copySource").addEventListener("click", () => {
    if (!activeSourceCode) {
      showToast(t("toast.sourcePending"));
      return;
    }
    copyText(activeSourceCode, t("source.copied", { name: activeSource }));
  });
}

function boot() {
  initTheme();
  initLanguage();
  renderSourceTabs();
  bindEvents();
  filterPlugins("all");
  loadSource(activeSource);
}

document.addEventListener("DOMContentLoaded", boot);
