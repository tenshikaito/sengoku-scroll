# 策略模式 · 贸易 / 市场 / 商家（2026-07-26 冻结 · 已确认部分）

> 关联：[game-concepts.md §4.3](./game-concepts.md) · [strategy-detail-design.md §4.6](./strategy-detail-design.md)

本文档汇总玩家与 AI 在 **商家（MerchantActor）**、**市场（StrongholdMarket）**、**商队（UnitKind.Merchant）** 上的已确认规则；标注 **📋 待定** / **🔧 已知缺陷** 的条目见 §14–§16。

**最后更新**：2026-07-27（多品类市场：粮食 + 马匹；`CommodityDefinition` master 驱动 UI）

---

## 1.1 物资库存（Actor 字段 · 2026-07-27）

| 字段 | 语义 | 市场 |
|------|------|------|
| `Food` / `Money` | 粮、钱（合/贯） | 粮食可大宗撮合 |
| `Horse` | **所持马匹（匹）**；Unit/据点库存，**非**骑兵编制上限 | ✅ 马匹可大宗撮合 |
| `Wood` / `Iron` / `Copper` / `Matchlock` / `Cannon` / `Boat` / `Ship` / `Fleet` | 建设/军备物资 | 📋 M5+ |
| 骑兵 | **`SubUnit.TypeId=Cavalry`**（`IsMounted`） | 与 `Actor.Horse` 无关 |

- 已删除 **`LuxuryGoods`** 与 **`MarketCommodityType.Luxury`**（旧占位）。
- **运输队**：`UnitKind.Convoy`；**`Unit.Horse`** 表示队中持有的马匹，`Food`/`Money` 表示载重。
- **`CommodityDefinition` master**（`GameMasterData.Commodities`）：名称、描述、是否可交易、默认价、`UnitLabel`（粮食 UI=**石**，马=匹）；经 `WorldState.MasterData.Commodities` 下发前端。内部库存仍为合，展示按 `GO_PER_KOKU`（1 石 = 1000 合）换算。

---

## 1.2 可交易品类（`MarketCommodityType` · 2026-07-27）

| 枚举 | Master 名 | 库存字段 | 默认价 | UI 数量单位 | 砸单 API |
|------|-----------|----------|--------|-------------|----------|
| `Food` | 粮食 | `Food`（合） | 50 文/合（展示 **贯/石**） | **石** | `smash-*-food` |
| `Horse` | 马匹 | `Horse`（匹） | 120 贯/匹 | 匹 | `smash-*-horse` |

- 订单簿、日 K、昨收 **按品类隔离**（`StrongholdMarket.PriceHistoryByCommodity`、`ResolveLastClose(commodity)`）。
- `RemoveDeprecatedCommodityOrders` 仅清除 **未在枚举中定义的旧品类单**（修复：不再误删 Horse 等非 Food 挂单）。

---

## 1. 概念分层（太阁式）

| 层 | 实体 | 玩家入口 | 行为 |
|----|------|----------|------|
| **商家** | `Stronghold.MerchantActors[]`（= 情报 `cityActors` kind=Merchant） | 个人行动 · **商家** | 进店与商人对话；**个人物品**买卖（📋 后期） |
| **市场** | `Stronghold.Market`（订单簿 + 分品类日 K） | 个人 **市场**（仅看）/ 商队 **交易** | **大宗**撮合：`Food`、`Horse`（Tab 切换） |
| **商队** | `Unit` + `UnitKind.Merchant` | 单位菜单 **交易** / 据点 **交易**（须城内商队） | 带 **Unit.Money/Food/Horse** 砸单，**不可挂单** |

**粮食等大宗不在店内交易**；Merchant 作为 Actor 在 **市场订单簿** 上挂卖单（AI/系统），与太阁立志传一致。

---

## 2. 单位类型：商队 vs 运输队

| 类型 | `UnitKind` | 菜单与权限 | 说明 |
|------|------------|------------|------|
| **商队** | `Merchant` | 入城、**交易**、贸易策略、解散归库 | 可组 **护卫 SubUnit**；无护卫 = 纯运输载荷，攻防极低 |
| **运输队** | `Convoy` | 补给/贡赋/税赋运输 | **高荷载、低移速**；不参与市场 UI |

- 护卫队为 **独立 SubUnit 类型**（荷载、移速、战斗与运输段不同）。📋 SubUnit 荷载表与招募来源（据点商铺）后续配置。
- 当主在 **居城** 贸易 = 据点内政 **交易**（官府库）；前往 **其它据点** 须 **真商队** 行军（📋 组建 UI 待定）。
- 委任将领贸易任务时，操作在 **商队 Unit** 上，以 Unit 携带限额约束（📋 任务系统未实装）。

**实装阶段 1**：`UnitKind.Merchant` 方可执行 `UnitTradeActions`；运输仍可用 legacy `SupplyConvoy(Trade)` 直至迁移完成。

---

## 3. 创立商店（📋 策略模式暂不开放 UI）

| 规则 | 说明 |
|------|------|
| 资格 | **在野**（`Character.ForceId=0`）、**非武家**（不可当大名/领主）；策略模式扮演当主，**无创立入口** |
| 费用 | 从 **角色个人所持金** 扣除（大笔） |
| 结果 | 身份转入 **商人势力**；**不可从政**（阶级壁垒）；至多成为 **御用商人** 影响政策 📋 |
| 分店 | 商人势力 **会议评定** 决定 📋 |

南蛮商会等与国产 **MerchantActor** 同型，无特殊 UI。

---

## 4. 市场开放条件

- **硬条件**：据点已建成 **Market 设施**（`EconomyFacilityIds` 含 Market）；**无设施则无市场按钮、不撮合**。
- **关闭**：**围城/封锁**（`IsStrongholdBlockaded`）时市场 **关闭**，商队不可自由进出。
- **商业值**：用途 **📋 暂定**（开店上限、商业税、流动性等），**不**替代 Market 设施门槛。

---

## 5. UI 入口（策略地图 Popup）

### 5.1 个人行动区（角色菜单 **下方**，非「内政」子菜单）

| 按钮 | 显示 | 行为 |
|------|------|------|
| **市场** | 所在据点有 Market 且未关市 | 打开市场窗口，**只读**（看行情；买了没处放 📋 除后期流浪军） |
| **商家** | 据点存在 ≥1 `MerchantActor` | 1 店直接进入；多店 **子菜单** 列店名；暂为占位对话 📋 |

### 5.2 据点指令（当主 · 内政 · 交易）

- **交易**：当主 **直接以官府库** 在本城市场买卖（`StrongholdLordTradeActions`）；**无需** 城内商队。
- 将领执行贸易任务时，操作在 **商队 Unit** 上，以 Unit 携带限额约束（📋 任务系统未实装）。

### 5.3 商队 Unit 菜单

- **交易**：`UnitKind.Merchant` + InStronghold + 市场开放 + 外交允许 → 市场窗口 **可交易**。

### 5.4 市场窗口（炒股式 · 多品类 Tab）

```
┌─ [粮食] [马匹]  ← Tab 来自 MasterData.Commodities ─────────┐
│ 左：日K | 周K | 月K | 年K + 成交量/成交额（ECharts）          │
│     切换 Tab 时销毁并重建图表，避免 K 线/轴单位残留           │
│ 右：买卖各 5/10 档（聚合量；单位随 Tab：石 / 匹）             │
│ 下：买入/卖出（价格/数量标签来自 master，如贯/石、贯/匹）     │
└──────────────────────────────────────────────────────────────┘
```

- **粮食 / 马匹**：当主与商队均可 **限价砸单**（`SmashBuy/SellFood`、`SmashBuy/SellHorse`）；官府可 **挂单**（限价单列表 + 撤单）。
- **库存展示**：粮食 Tab 读 `Food`；马匹 Tab 读 `Horse`（官府库或商队 Unit）。
- **交易税**：成交时扣减展示（参考股票费用明细）📋 UI 细化。

**读 API**：

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/strongholds/{id}/market?commodity=Food\|Horse` | 快照：盘口、K 线、玩家挂单 |
| POST | `/strongholds/{id}/trade/smash-buy-food` | 官府购粮 |
| POST | `/strongholds/{id}/trade/smash-sell-food` | 官府卖粮 |
| POST | `/strongholds/{id}/trade/smash-buy-horse` | 官府购马 |
| POST | `/strongholds/{id}/trade/smash-sell-horse` | 官府卖马 |
| POST | `/strongholds/{id}/trade/cancel-order` | 撤单（body 含 `commodity`） |
| POST | `/units/{id}/trade/smash-buy-food` 等 | 商队对称 4 端点 |

写操作 POST 后返回 **`WorldState`**（含更新后的 `stronghold.horse` / `unit.horse` 等）。

**前端**：`StrategyMarketDialog.vue` + `strategyCommodityHelpers.ts`（解析 master）；图表 `StrategyMarketEchartsPanel.vue`（`chartKey` / `displayMeta`）。

---

## 6. 解散归库

仅 **Home 据点** 可解散。货物流向：

| 商队所属 | 据点条件 | 归库目标 |
|----------|----------|----------|
| 武家 `ForceId` = 据点 `ForceId` | — | **官府** `ForceActor` |
| 商人组织 | 同城有同 `ForceId` 的 **MerchantActor** | **该店** Actor |
| 寺社 | 同城有同 `ForceId` 的 **ReligionActor** | **该寺社** Actor |

---

## 7. 势力任务 · 交易

- 属 **人物 → 命令 Tab** 的 **势力任务**，**非** 任务·个人 Tab（暂定）。
- 当主或被委任 **交易任务** 的配下将才可代行据点 **交易** 📋 任务系统未实装前，仅当主 + 城内商队。

---

## 8. 关税与商队策略

- 过境关税在 **商队策略** 配置：一路全额缴纳 / 超过阈值 **确认**（拒缴则停止）📋 策略 UI 后续。
- 与 **贸易税**（市场内商户成交）独立。

---

## 9. 情报 / 迷雾

- 非己方据点进入市场 **📋 敌国是否可进待定（史实）**。
- 迷雾下对外城行情：保留该据点 **约 2 个月前** 的快照行情（`StrategyIntelligenceLedger` 扩展 📋）。

---

## 10. CityActor（情报与 UI 共用）

**CityActor 非独立实体**，是 `StrongholdIntelDtoHelper.MapCityActors` 输出的 **城内势力切片**：

| kind | 源 |
|------|-----|
| Government | ForceActor |
| Civilian | CivilianActor |
| Kokujin | 任命领主 |
| **Merchant** | **MerchantActors[]** |
| Religion | ReligionActors[] |

情报 **据点 → 商家 Tab** = `kind=Merchant` 行，与个人菜单 **商家** 子菜单同源。

---

## 11. API vs WorldState

| 内容 | 载体 |
|------|------|
| 菜单显隐（设施、cityActors、商队摘要） | **WorldState** |
| 订单簿 5 档、K 线、分时 | **GET Market API**（打开窗口时拉取） |
| 砸单买卖 | **POST** + 返回 WorldState |

---

## 12. 实装阶段与待定清单

### ✅ 阶段 1（已实装）

- Market 设施门槛 + 围城关市
- `UnitKind.Merchant` 贸易限制（现货砸单，**不可挂单**）
- 个人 **市场**（只读）+ **商家**（多店子菜单，占位）
- 商队 **交易** + 市场窗口（粮食）
- 当主 **内政·交易**（`StrongholdLordTradeActions` 限价砸单/挂单）
- 移除策略模式 **创立商店** UI

### ✅ 阶段 2（2026-07-27 · 做市 AI）

- **`MarketMakerAiHelper`**：以昨收为中枢，买 `中枢−N`、卖 `中枢+N`；推荐深度约 **20 档**（实际 8~24，随 Actor/预算浮动）
- **量分布非线性**：卖盘 ~1/L² 近价厚；耐心买盘 ~L² 远价厚；每档保底最小量后按权重分配
- **参与者**：官府 / 市民 / 商户 **均可多档挂单**；商户 **低买高卖**（同据点买卖双侧）；**与商业值、南蛮商会无关**
- **寺社** `ReligionTradeActions`：与商队相同，**仅现货**，不进订单簿
- **跨据点套利** `TradeMarketAiHelper`：比较两地中枢价差（已去掉市民 +10% 溢价）
- **演示 seed**：`MarketBootstrapHelper` 约 1 年随机 K 线 + 20 档演示单（日更 AI 会覆盖）

### ✅ 阶段 2.1（2026-07-27 · 成交与领队修正）

- **中枢穿越**：紧缺市民 / 有外部近价买盘时，AI 卖盘可挂中枢价促成交叉
- **撮合-结算原子化**：`MatchOrders` 不预扣量；`ApplyMatchResult` 结算成功后再填单
- **禁止自成交**：同一 Actor 买卖单不再互相撮合
- **商户无买盘抛售**：排除本店后若无外部买盘、或外部最高买 &lt; 中枢 → 撤本店买盘，卖盘挂到买一（或低于中枢≥捡漏折扣）引导跌价与次日捡漏
- **贸易队总将**：仅商家店员 / 组织角色；**禁止**回退武家代官（修复「今井屋商队显示酒井忠次」）
- **移民队**：`LeaderId=0`（无总将）；武家代官不会兼任移民队长
- **派出主体**：跨城贸易队仅商家组织派出；武家只派补给/贡赋
- **非史实收入**：固定 **80%**（`FictionalIncomePenaltyBp=8000`）

### ✅ 阶段 2.2（2026-07-28 · Actor 仓位 / 情绪 / 抢筹 / 邻城套利）

- **仓位平衡**：目标资金占比（默认 40%）；钱多物少且价合适 → 买到平衡；货多钱少 → 卖到平衡
- **机会抢筹**：相对公允价崩盘（≥10%）时，大单以卖一砸买；吃完后**同价挂买单承接**；过热则对称砸卖+挂卖
- **情绪**：敌对/敌军压境/围城 → 囤积（减卖加买）；收粮日及前瞻窗口 → 抛压（减买加卖、易 undercut）
- **邻城套利挂价**：扣每格运费后，出口抬高本城卖价锚、进口压低买价锚；跨城贸易队价差门槛计入运费
- **日更顺序**：机会砸单 → 挂单 → 撮合（砸单不走 Refresh，防递归）
- 参与方：市民 / 官府 / 商户 / 寺社 均可机会砸单

### ✅ 阶段 3（2026-07-27 · 多品类 + UI）

- **`CommodityDefinition` master** + `CommodityTradeModule` / `CommodityInventoryHelper` 统一读写库存
- **马匹市场**：独立 K 线、挂单 AI（`HorseMarketAiHelper`）、演示 seed（`SeedDemoHorseData`：~120 贯/匹、12 档买卖单）
- **分品类日 K**：`PriceHistoryByCommodity`；快照 `GET …/market?commodity=Horse`
- **AI 增强**：`ResolveBestAsk` 感知最低卖价；砸单后 `MarketAiRefreshHelper.RefreshAfterTrade` 重挂 AI 单
- **前端**：Tab 切换重建图表；单位/文案绑定 `MasterData.Commodities`（`strategyCommodityHelpers.ts`）

### 📋 待确认后再做

- 商队组建 UI（护卫 SubUnit、荷载表）
- 创立商店 / 在野商人 / 会议分店
- 交易势力任务与委任将
- 敌国市场准入
- 个人物品店、商家对话
- 关税策略 UI、2 月情报快照 API
- 分时/成交量 Tab 数据（现仅日 K）
- **事件驱动粮价**（战争、围城恐慌、谣言等，见 §15）

---

## 13. 日更流程与参与方（实装）

每日 `StrategyMarketSystem.Update`（气候之后、经济之前）：

```
1. RemoveZeroQuantityOrders
2. MarketPositionAiHelper      → 仓位/情绪机会砸单（可挂承接）
3. CivilianMarketAiHelper      → 多档买单（缺粮/囤积/捡漏；仅 Food）
4. GovernmentMarketAiHelper    → 多档买/卖（储备 + 情绪 + 邻城锚价）
5. MerchantMarketAiHelper      → 多档买/卖（做市 + 邻城套利 + 抛售）
6. HorseMarketAiHelper         → 马匹做市（Horse）
7. MatchOrders + ApplyMatchResult → 写分品类 K 线、LastClose
8. UnitTradeActions.ProcessAutoTradePolicies
```

| Actor | 订单簿 | 方向 | 现货砸单 | 说明 |
|-------|--------|------|----------|------|
| **ForceActor（官府）** | ✅ | 买+卖 | ✅ 当主交易 | 免税；储备线 2 万合 |
| **CivilianActor（市民）** | ✅ | 仅买 | ❌ | 聚合人口；余粮 &lt; 7 天挂买单（清洲开局粮极厚时几乎无民间需求） |
| **MerchantActors（商户）** | ✅ | 买+卖 | ❌ | 成交收 **贸易税**；无外部买盘时低价抛售 |
| **ReligionActors（寺社）** | ❌ | — | ✅ `ReligionTradeActions` | 不进 AI 日更挂单 |
| **Unit 商队** | ❌ | — | ✅ `UnitTradeActions` | `AllowRestingOrder=false`；总将=店员，非武家代官 |
| **Unit 移民** | ❌ | — | ❌ | `LeaderId=0`，无总将 |
| **玩家大名** | ✅ | 买+卖 | ✅ | 带 `MoneyCommitted`/`InventoryCommitted` 的限价单受保护 |

**中枢价**：`LastClosePriceMoneyPerGo`（昨收/最近有量成交的收盘价）。AI 挂单以此为基准 ±N 贯；紧缺/抢成交时可挂中枢或低于中枢（见 §14.2）。

---

## 14. 诊断：有挂单但不成交 / 价格看起来怪

### 14.1 现象

- 盘口两侧都有量，但 **日 K 成交量长期为 0**，收盘价几乎不动
- 或 K 线历史有波动，但 **当日/live 盘口与 K 线中枢不一致**
- 玩家 **砸单** 后价格突然跳变，AI 日更却不推动价格
- **清洲等粮仓城成交量特别少**：通常 **不是撮合坏了**，而是民间需求不足（市民余粮 ≥ 7 天不挂买单）+ 官府也不缺粮

### 14.2 根因 A：结构性买卖价差（✅ 已缓解）

| 侧 | AI 挂价 | 示例（中枢=100） |
|----|---------|------------------|
| 最高买价 | 中枢 − 1（最近一档） | 99 |
| 最低卖价 | 中枢 + 1（最近一档） | 101 |

撮合条件：`最高买价 ≥ 最低卖价`（`MarketCalculator`）。  
当所有 AI 严格分布在中枢两侧时，**最高买 &lt; 最低卖 → 日更可能零成交**。

**2026-07-27 缓解**：

- AI 买盘定价参考 **`ResolveBestAsk`**；紧缺时可挂中枢交叉
- 商户卖盘：有外部买盘且买价 ≥ 中枢时挂中枢交叉；**否则低价抢成交**（贴买一，或无买盘时低于中枢≥捡漏折扣）并撤本店买盘
- 撮合 **禁止同一 Actor 自成交**
- 玩家/当主 **砸单** 后 **`MarketAiRefreshHelper`** 按新品位重挂 AI 单

### 14.3 根因 B：演示 K 线与 live 订单簿脱节

- `MarketBootstrapHelper.SeedPriceHistory`：开局生成 **365 天随机游走** K 线（±3 贯/日）
- 日更 `MatchOrders` 若无成交：`VolumeGo=0`，`Close=LastClose` 平盘追加
- UI 左侧 K 线含 **历史随机波动**；右侧 live 盘口来自 **当前 AI 单**，二者可长期不一致

### 14.4 根因 C：撮合与结算两阶段不一致（✅ 已修）

旧缺陷：`MatchOrders` 先扣订单量，结算失败时量已丢失。  
**现况**：撮合仅在工作副本上扣量；`ApplyMatchResult` 结算成功后再写回订单 / MarkFilled。

### 14.5 根因 D：AI 不挂单的常见条件

| 条件 | 后果 |
|------|------|
| 市民余粮 ≥ 7 天 | 市民 **不挂买单**（清洲开局粮厚 → 成交量低属预期） |
| 官府粮 ≤ 储备 2 万合 | 官府 **不挂卖单** |
| 官府粮 ≥ 储备 | 官府 **不挂买单** |
| 商户 `Money` ≤ 营运准备金 | 商户 **不挂买单** |
| 商户 `Food` ≤ 储备 5000 合 | 商户 **不挂卖单** |
| 无外部买盘 / 买价 &lt; 中枢 | 商户 **低价抛售**（撤本店买盘） |
| 据点 **被围城** | `CanTrade=false`，**整个市场停更** |
| 无 Market 设施 | 不撮合、不显示市场 |

### 14.6 价格何时会变

| 来源 | 更新 `LastClose` | 写入 K 线 volume |
|------|------------------|------------------|
| AI 日更撮合 | 仅当 `TotalVolumeGo>0` | 同左 |
| 当主/玩家砸单 | ✅ 即时 | ✅ |
| 商队现货砸单 | ✅ 即时 | ✅ |
| 仅 AI 挂单、无交叉 | ❌ 不变 | 0 |

---

## 15. 粮价影响因素全表

> **图例**：✅ 已实装并影响价格或成交 · ⚠️ 已实装但不改价（关市/无交叉）· 📋 设计目标 / 未实装

### 15.1 订单簿与撮合（直接）

| 因素 | 方向/机制 | 状态 | 代码/备注 |
|------|-----------|------|-----------|
| 买卖盘交叉 | 最高买 ≥ 最低卖 → 连续撮合，成交价=卖单挂价 | ✅ | `MarketCalculator.MatchOrders` |
| **中枢 spread 无交叉** | AI 买&lt;中枢&lt;卖 → 日更零成交 | ✅ 已靠穿越/低价抛售缓解 | 见 §14.2 |
| 买方资金不足 | 撮合后结算跳过，可能不落 K 线 | ⚠️ | `ApplyMatchResult` |
| 卖方粮不足 | 同上 | ⚠️ | `MarketInventoryHelper` |
| 商户卖单 | 收 **贸易税** 5% | ✅ | `TradeTaxBasisPoints` |
| 官府/市民单 | **TaxExempt** | ✅ | `MarketActions.AddOrder` |
| 玩家限价挂单 | 带锁定标记，AI 不覆盖 | ✅ | `MarketRules.IsPlayerRestingOrder` |

### 15.2 做市 AI 与供需（直接/间接）

| 因素 | 对粮价影响 | 状态 | 说明 |
|------|------------|------|------|
| 昨收 `LastClose` | 次日 AI 中枢 | ✅ | 无成交则中枢冻结 |
| 市民缺粮（&lt;7 天） | 增加多档买单；紧缺时近价档加权 | ✅ | `CivilianMarketAiHelper` |
| 市民粮充足 | 撤买单，需求消失 | ✅ | |
| 官府余粮 &gt; 2 万合 | 多档卖单释粮 | ✅ | 单日卖量上限 5000 合 |
| 官府粮库不足 | 多档买单补库 | ✅ | |
| 商户做市 | 中枢下买、上卖；库存/资金定深度 | ✅ | `MerchantMarketAiHelper` |
| 商户补货 | 买单成交 → 商户 `Food` 增加 | ✅ | 依赖 §14.2 成交 |
| 收粮日 | 市民/官府粮增加 → 卖压潜力↑ | ✅ | `HarvestEconomyActions` |
| 市民日耗粮 | 库存下降 → 买压潜力↑ | ✅ | `StrongholdEconomyActions` |
| 跨据点运输队 | 粮直接入市民，**不经订单簿** | ✅ | `TradeEconomyActions` |
| 跨据点 AI 贸易 | 低价据点→高价据点（比较中枢价差） | ✅ | `TradeMarketAiHelper` |

### 15.3 市场开关与军事（间接 / 极端）

| 因素 | 对粮价影响 | 状态 | 说明 |
|------|------------|------|------|
| **围城 / 封锁** | 市场 **关闭**，无挂单无撮合 | ✅ | `GarrisonBehaviorRules.IsStrongholdBlockaded` |
| 围城恐慌抢粮 | 买单增加、价格飙升 | 📋 | 需与 §14.2 交叉成交 + 事件 |
| 附近 **战争 / 开战** | 预期供应中断、抢粮 | 📋 | 无专用粮价 modifier |
| 敌军压境 / 威胁 | 移民、弃城、粮外流 | 📋 | 部分移民逻辑已有，未接粮价 |
| 运输队被劫 | 在途粮损失，目的地供给↓ | ✅ | 遭遇战；到港量↓间接影响 |
| 商队砸单 | 短期冲击价格、清空可见档位 | ✅ | `MarketLimitOrderExecutor` |

### 15.4 经济、政策与税收（间接）

| 因素 | 对粮价影响 | 状态 | 说明 |
|------|------------|------|------|
| 人口 / 商业值 | 日产钱粮、税基；**不限制**做市深度 | ✅ | 商业值不 gate 挂单 |
| 人头税 / 商业税 | 市民钱↓ → 买单结算能力↓ | ✅ | 每月 1 日 |
| 农业税 / 收粮 | 供应侧 | ✅ | |
| 关税 | 过境货值，**非**本城订单簿 | ✅ | `TariffEconomyActions` |
| 自由贸易 vs 价格管制 | 官府托底/上限 | 📋 | 设计 §4.6 |
| 商户关店 | 做市深度↓ | ✅ | 维持费不足 |

### 15.5 情报、谣言与事件（设计目标）

| 因素 | 预期影响 | 状态 | 说明 |
|------|----------|------|------|
| **谣言**（假丰收/假缺粮） | 短期买/卖情绪、价格波动 | 📋 | 无 `Rumor` 系统 |
| 散布恐慌 | 市民近价抢买 | 📋 | 可映射为 `PreferNearReferenceBids` 强制 |
| 丰收公告 | 卖压、价格下跌 | 📋 | 可与 `HarvestEvent` 联动 |
| 歉收 / 气候 | 供应冲击 | ⚠️ | 收粮有气候；**未**直接改挂单策略 |
| 粮价情报 | 波动 &gt;15% 记入 `StrategyIntelligenceLedger` | ✅ | 需先有成交/波动 |
| 迷雾快照 | 外城见 ~2 月前行情 | 📋 | Ledger 脚手架已有 |
| 信使/旅人传播价格 | 跨据点 AI 套利信息 | 📋 | |

### 15.6 UI / 数据展示（易误解为「价格怪」）

| 因素 | 表现 | 状态 |
|------|------|------|
| 演示随机 K 线 | 历史像「在涨跌」，live 盘不动 | ✅ seed |
| UI 只显示 5 档 | 全簿 ~20 档，只见局部 | ✅ `MarketDepthDisplayHelper` |
| 空档占位 `—` | 看起来像「缺单」 | ✅ 设计 |
| `SessionPrice` vs `LastClose` | 中线与昨收可能不同 | ✅ 盘口 quote 逻辑 |

---

## 16. 后续修复优先级（建议）

| 优先级 | 项 | 说明 |
|--------|-----|------|
| ✅ | **中枢穿越 / 商户低价抢成交** | 已实装（§14.2 / 阶段 2.1） |
| ✅ | **撮合-结算原子化** | 已实装 |
| P1 | 演示 K 线与 live 盘对齐 | seed 后首日即用 AI 中枢，或 K 线从实装日起算 |
| P1 | 事件→粮价表（§15.5） | 围城/谣言/战争 modifier |
| P2 | 情报传播接 Trade AI | 套利不只看本地 LastClose |
| P2 | 分时/成交量 Tab | UI 数据 |

---

## 附录 A. 关键代码索引

| 模块 | 路径 |
|------|------|
| 日更入口 | `StrategyMarketSystem.cs` |
| 撮合 | `MarketCalculator.cs` |
| 结算 / K 线 | `MarketActions.cs` |
| 做市算法 | `MarketMakerAiHelper.cs` |
| 仓位/抢筹 | `MarketPositionAiHelper.cs` · `ActorMarketTradeActions.cs` |
| 情绪信号 | `MarketContextSignalsHelper.cs` |
| 邻城套利锚价 | `MarketRegionalArbitrageHelper.cs` |
| 官府/市民/商户 AI | `*MarketAiHelper.cs` |
| 玩家/商队/寺社现货 | `StrongholdLordTradeActions` / `UnitTradeActions` / `ReligionTradeActions` |
| 演示 seed | `MarketBootstrapHelper.cs`（`SeedDemoHorseData` / 粮食 K 线） |
| 分品类 K 线 | `StrongholdMarket.PriceHistoryByCommodity` |
| 统一贸易 | `CommodityTradeModule.cs` · `CommodityInventoryHelper.cs` |
| Master 定义 | `CommodityDefinition.cs` · `StrategyDefaultMasterDataSeed.CreateCommodities` |
| 前端单位/Tab | `strategyCommodityHelpers.ts` · `StrategyMarketDialog.vue` |
| 图表 | `StrategyMarketEchartsPanel.vue` |
| 关市 | `MarketRules.cs` + `GarrisonBehaviorRules` |
| 粮价情报 | `StrategyIntelligenceLedger.cs` |
