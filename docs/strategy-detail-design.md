# SengokuScroll（战国绘卷）大战略模式详细设计

> 版本：1.6 | 日期：2026-07-17 | 索引：[设计文档索引](./README.md) | 开发计划：[strategy-development-plan.md](./strategy-development-plan.md) | 上一级：[基本设计](./design-document.md) | 共享：[共同详细设计](./shared-detail-design.md) | 界面：[大战略模式界面设计](./strategy-ui-design.md) | 概念：[game-concepts.md](./game-concepts.md)

---

## 目录

1. [核心体验](#1-核心体验)
2. [系统架构](#2-系统架构)
3. [半回合制时间系统](#3-半回合制时间系统)
4. [经济系统](#4-经济系统)
5. [军事系统](#5-军事系统)（含 [§5.1 同格堆叠/战场/围城](#51-同格堆叠战场与围城2026-07-15-冻结--待实装)）
6. [自动战斗系统](#6-自动战斗系统)
7. [方针系统](#7-方针系统)
8. [胜率预测系统](#8-胜率预测系统)
9. [自动战斗战斗结算](#9-自动战斗战斗结算)
10. [外交系统](#10-外交系统)
11. [多人联机架构](#11-多人联机架构)
12. [策略专属事件](#12-策略专属事件)

---

## 1. 核心体验

参照信长之野望·革新 + 欧陆风云式半回合制：

- 玩家控制一个势力（或多个单位）
- 时间在所有玩家无异议时自动推进
- 任何玩家可暂停时间进行部署
- 支持多人联机（1-8人）

> **实装顺序**：多人联机设计见 §11，**代码实装延后至 RPG 模式完成之后**（全项目顺序见 [基本设计 §1.7](./design-document.md#17-开发顺序与当前阶段)）。策略单机阶段（M3）仅预埋确定性结算与指令数据结构。

---

## 2. 系统架构

```
StrategyGameEngine : GameEngineBase
├── StrategyClimateSystem    气候系统
│   ├── 季节变化影响农业/移动
│   └── 灾害事件（台风/地震/饥荒）
├── StrategyMarketSystem     市场系统（M4+）
│   ├── 连续撮合、日 K 线
│   ├── 贸易税台账（MerchantTaxLedger）
│   └── 行情情报写入 StrategyIntelligenceLedger
├── StrategyEconomySystem    经济系统（核心）
│   ├── 生产/口粮、税收与府库收支
│   ├── Region 收粮与农业税
│   ├── 贡赋义务与欠账
│   └── 官办/民间库存分层
├── StrategyMilitarySystem   军事系统（核心）
│   ├── 单位招募与维护
│   ├── ZOC（控制区）计算
│   └── 战斗触发与结算入口
├── StrategyDiplomacySystem  外交系统（核心）
│   ├── 外交提议与回应
│   ├── 宣战理由计算
│   ├── 战争分数与和谈
│   └── 同盟/联姻/朝贡
├── StrategyCharacterSystem  角色系统
│   ├── 家臣管理
│   ├── 角色任命与调配
│   └── 角色忠诚与叛变
├── StrategyAISystem         AI系统
│   ├── 势力AI决策
│   ├── 军事AI
│   ├── 外交AI
│   └── 经济AI
├── StrategySupplySystem     补给系统
│   ├── 自动派遣 SupplyConvoy（运输队实体，地图可见）
│   ├── 运输队沿路径移动、到达卸粮
│   ├── 补给三态（充足/紧张/断绝）为衍生展示
│   └── 断补给惩罚（士气/战力修正）
└── StrategyMessengerSystem  信使系统
    ├── 同格（含同据点）指令即时生效
    ├── 异格：方针/远程指令/战报经信使实体传递
    └── 到达后生效或解锁战报详情
```

> **玩家体验**：玩家 **扮演势力当主**（如织田信长），而非逐支微操地图上每一支部队。当主通过方针、攻击命令与战略目标 **间接指挥**；本势力所有军事单位（含非当主直辖的先锋、城代军等）均按已生效 **方针 + AI 规则** 自主接敌、追击与败退。后勤由 **运输队实体** 自动补给；远程 **方针与战报** 遵循 **信使制度**（同格免信使）。实装范围见 [策略开发计划 §6](./strategy-development-plan.md#6-系统实现要点)。

### 2.1 当主视角与间接指挥

```
玩家身份
├── 控制对象：势力（Force）当主，非地图上每一支 Unit
├── 地图单位：织田先锋、柴田队等由 AI 按方针执行（接敌/占领/撤退/追击）
├── 玩家操作：设方针（可经信使）、下达攻击命令、查看情报与战报
├── 当主同格：方针/部分指令可即时生效（免信使）
├── 战败处理：无论单位是否「玩家曾选中过」，败方一律 AI 败退（方针→Retreat + 脱离接触）
└── 战报投递：参战部队战报同时发往当主当前位置、当主居城；若部队将领有封地则另发该据点
```

**虚构据点**：剧本 JSON 未声明 `isHistorical` 时，按同格是否存在地图 **地标** 推断；DTO `isHistorical=false` 时 UI 在「居城/直辖」后显示 **·虚构**。

---

## 3. 半回合制时间系统

```
StrategyTimeController
├── 时间状态
│   ├── Running     时间推进中
│   ├── Paused      已暂停
├── 推进规则
│   ├── 任何玩家可随时暂停
│   ├── 暂停后所有玩家需"同意继续"才恢复
│   ├── 默认速度：1天/2秒
│   └── 可调速度：1天/1秒 ~ 1天/10秒（多人游戏仅在主机端可调）
├── 暂停触发条件
│   ├── 玩家手动暂停
│   ├── 重要事件弹出时
│   └── 外交提议到达时
└── 多人同步
    ├── 主机控制时间推进
    ├── 客机发送暂停/继续请求
    └── 所有指令在时间推进前同步
```

---

## 4. 经济系统

> **实装阶段**：M3-d 已实现军队日耗粮、月维持费、运输队贡纳（过渡方案）；**M4 起**按本节重构为「民间—府库—市场—贡赋」模型。数值计算**一律整型**（文、合、万分比），避免 `double`。

### 4.1 库存分层

```
Stronghold
├── CivilianActor      市民整体（钱粮、日耗、被征税）
├── ForceActor         官府府库（税入、购粮、平抑、贡赋出库）
├── MerchantActors[]   商人势力在本据点的店铺仓库（Force 类型为 Merchant）
├── ReligionActors[]   （远期）寺社等，可参与市场
└── Market             每据点一个订单簿市场（需 Market 设施方可挂单）
```

| 层级 | 字段 | 说明 |
|------|------|------|
| 市民 | `CivilianActor.Food/Money` | 聚合人口；日耗粮按市民口粮系数 |
| 官府 | `ForceActor.Food/Money` | 府库；势力 `Force.Money/Food` = Σ 旗下府库 |
| 店铺 | `MerchantActors[].Food/Money` | 商人 Force 开设；同一商人 Force 在同一据点仅 1 店 |
| 军队 | `Unit.Food` | 携行粮，与民间市场隔离 |

**商人势力**：`Force` 的一种（`ActorType.Merchant`）；缴费获开店许可；可持私兵；极端情况下可夺城建立世俗势力（见 shared-detail-design 角色/势力扩展，M5+ 事件）。

### 4.2 生产与消耗（每日）

| 操作 | 对象 | 公式（整型） |
|------|------|--------------|
| 农业产出入库 | `CivilianActor.Food` | `AgricultureProduction / 30`（合/日，按 Region 季修正） |
| 商业产出入库 | `CivilianActor.Money` | `CommerceProduction / 30`（文/日） |
| 市民口粮 | `CivilianActor.Food` | `ceil(Population × DailyCivilianRationMilliGo / 1000)` |
| 士兵口粮 | `Unit.Food` | `ceil(Soldier × DailySoldierRationMilliGo / 1000)`（默认约 3 合/人/日） |
| 运输队在途 | `CargoFoodGo` | 人夫 + 护卫口粮 |

口粮不足时不自动替市民/官府抢购；**民心**持续下降，阈值触发暴动等（M5+）。官府缺粮时可主动向市场挂买单（**免税**）。

### 4.3 税收

#### 4.3.1 征收模式

| 模式 | 说明 |
|------|------|
| **比例制（默认）** | 按产出/人口/商业值 × 税率 × 征收效率 |
| **定额制（可选政策）** | 太阁检地式固定石高；歉年仍按比例缺口影响民心 |

#### 4.3.2 税种与频率

| 税种 | 基数 | 频率 | 入库 |
|------|------|------|------|
| 人头税 | `Population` | 每月 1 日 | `ForceActor.Money` |
| 基础商业税 | `CommerceValue` | 每月 1 日 | `ForceActor.Money` |
| 贸易税 | 本据点市场**商户**成交额 | 成交时记账，每月 1 日汇总征收 | `ForceActor.Money` |
| 关税 | 途经/进出本据点的**运输队载货价值**（`CargoMoney` + 粮折钱） | 到港/过境日或每月汇总（M4-c） | `ForceActor.Money` |
| 农业税 | 当次 `Harvest` 产出 | **Region 日历**（见 §4.4） | `ForceActor.Food` |

**商业相关三项（勿混淆）**：

| 项目 | 性质 | 对象 |
|------|------|------|
| 基础商业税 | 税 | 按据点 `CommerceValue` 向市民聚合征收 |
| 贸易税 | 税 | 市场成交向**卖方商户**计提（`MerchantTaxLedger`） |
| 关税 | 税 | 跨据点 `Trade` 运输队过路/入境，按载货价值向官府征收 |

- 贸易税**仅向商户**计提，不向市民个人收交易税；**官府、市民挂单免税**（`TaxExempt`）。
- 关税与贸易税独立：前者针对**运输队过境货值**，后者针对**本城市场内商户成交**。
- 征收效率：`Authority`↑、`Corruption`↓、`IsHistorical` 修正（`EconomyCalculator.CalculateCollectionEfficiency`）。
- 征收为**比例划扣**，不在征收层出现「税额 > 100%」；过度征收通过民生与市场反馈体现。
- M3 过渡：`CalculateStrongholdMonthlyTaxMoney` 仍用 `Population × TariffTaxRate / 40` 近似关税并入钱纳总额；M4-c 改为运输队货值计税后移除该近似。

#### 4.3.3 收粮后分配（Harvest 当日）

```
grossHarvest
  → taxToForce   = gross × 农业税率 × 效率  → ForceActor.Food
  → civilianRemain = gross - taxToForce       → CivilianActor.Food
```

Merchant 仓库**不**参与产量划分。

#### 4.3.4 府库收入（除税以外）

| 类别 | 说明 | 入库 | 阶段 |
|------|------|------|------|
| **税收** | §4.3.2 五税 | `ForceActor` 钱/粮 | M4-a/b（关税 M4-c） |
| **贸易收入** | 官府/商户在本据点市场**粮食卖单**成交后的货款 | `ForceActor.Money` | M4-c ✅ |
| **贡赋收入** | 势力内据点、内藩/外藩向宗主/当主居城上缴的粮钱；**外交「朝贡」条约下附庸/藩属的定期贡纳亦归入此类**（义务、运输、欠账见 §4.5，不设独立「朝贡收入」科目） | 宗主/当主 `ForceActor` | M4-b+ |
| **掠夺收入** | 战斗缴获、抄略等 | `ForceActor` | M5+ |

贸易收入与贸易税不同：前者是**卖货所得**，后者是对商户成交的**征税**。

#### 4.3.5 府库支出

| 项目 | 基数 | 频率 | 出处 | 阶段 |
|------|------|------|------|------|
| 军队维护费 | 单位规模/兵种 | 每月 1 日 | `ForceActor.Money` | M3-d ✅ |
| 据点维持费 | 据点等级/设施 | 每月 1 日 | `ForceActor.Money` | M3-d ✅ |
| 商户店铺维持费 | 固定月费 | 每月 1 日 | 商户 `Money`（不足关店） | M4-b ✅ |
| 角色俸禄 | `Character.Salary` | 每月/每年 | `ForceActor.Money` | M5+ |
| 建设投资 | 设施/扩建 | 指令触发 | `ForceActor` 钱粮物资 | M5+ |
| 外交支出 | 赠金、和谈赔款等 | 事件触发 | `ForceActor.Money` | M5+ |

#### 4.3.6 经济周期

| 周期 | 内容 |
|------|------|
| **每日** | 农业/商业日产、市民/士兵/运输队口粮、市场连续撮合、生产事件 |
| **每月 1 日** | 人头税/基础商业税/贸易税汇总、军队与据点维持费、钱纳贡赋运输队、商户维持费 |
| **收粮日** | Region 日历触发 Harvest、农业税、贡粮运输（与 §4.4–§4.5 同频） |
| **每年** | 年度财政报告（`EconomyAnnual`）、人口/民心等大项调整（M5+ 细化） |

### 4.4 Region 收粮日历

| Region 配置 | 示例 |
|-------------|------|
| `CropPattern.Single` | 北方：`HarvestMonth=11, Day=1` |
| `CropPattern.Double` | 早稻 6/1、晩稻 9/1 |
| `CropPattern.Triple` | 暖地扩展（6/9/11 等，剧本配置） |

每次 Harvest 与征税、贡赋义务计算**同频**（见 §4.5）。

### 4.5 贡赋与欠账

**贡赋收入**（§4.3.4）统一涵盖：势力内直辖据点向当主居城、内藩/外藩向宗主居城的上缴，以及外交 **朝贡** 关系下附庸对宗主的定期贡纳（粮/钱比例由 `Diplomacy` / 剧本模板配置，Gameplay 上不单独记「朝贡收入」账目）。

- 义务 = **当次产出 × 贡赋比例**（太阁检地式，**非**府库快照，防转移资产卡 bug）。
- 粮、钱**分开**记账；默认大名→幕府以**粮**为主（模板可配置）。
- 势力内据点 → 当主居城（**粮**：收粮日运输；**钱**：每月 1 日运输队 `TransportPurpose.TaxMoney`）。
- 内藩/外藩居城 → 宗主居城（规则同上，义务比例可高于直辖）。
- 实际运送不足部分记入 **`Diplomacy` 从属关系**上的 `ArrearsFoodGo` / `ArrearsMoney`；**势力内直辖→居城**不足记入 **`Force.InternalArrearsFoodGo/Money`**（外交变更时义务主体随关系迁移）。
- 运输队载货量受人夫/护卫规模限制；运输节点与征税节点一致。

### 4.6 市场（M4+）

> **贸易 / 商家 / 商队 UI 与权限**：见 [strategy-trade-market-design.md](./strategy-trade-market-design.md)

```
StrongholdMarket（每据点）
├── Orders[]              连续撮合订单簿（按 MarketCommodityType 分品类）
├── PriceHistoryByCommodity[]  分品类日 K：Open/High/Low/Close，保留 2 年
└── LastClose（按品类）   最近收盘价
```

| 规则 | 说明 |
|------|------|
| 撮合 | **连续撮合**；独立 Market 相位（日推进中在单位移动前），稳定 tie-break 保证确定性 |
| 参与者 | 官府、Merchant、Civilian（聚合）、远期 Religion 等；**官府购粮/平抑挂单 TaxExempt**；官办企业**卖单**成交货款入府库（§4.3.4 贸易收入） |
| 店铺 | `MaxShops = CommerceValue / K`（线性）；月 **维持费**（单一费用项，游戏内不拆分地租/铺租明细）→ 不足则**关店**；重开加惩罚费（M5+ 可扩展设施荒废） |
| 粮价 | 供需订单簿撮合；秋收供应增加 → 价格下跌；政策可托底/平抑（自由贸易 vs 兜底） |
| 跨据点贸易 | 复用 **运输队**（`ConvoyPurpose.Trade`）；道路连通；利润须覆盖路程与拦截风险 |
| 情报 | 独立 **`StrategyIntelligenceLedger`**：各势力对市场的**已知行情**（可延迟、可过时）；信使/旅人/移民/兵队均可传播；记录不过期，AI 按时间降权 |

> **2026-07-27 实装说明**：粮食 + **马匹** 双品类大宗撮合；`CommodityDefinition` master 驱动 UI。做市 AI 已多档，但 AI 严格挂于中枢 ±N 时 **日更仍可能零成交**；2026-07-27 起 AI 可感知最低卖价并在砸单后刷新。战争/谣言/围城抢粮等 **事件粮价 modifier 未接**。诊断与全因素表见 [strategy-trade-market-design.md §14–§15](./strategy-trade-market-design.md)。

### 4.7 运输与拦截

| 类型 | 占格 | 拦截 |
|------|------|------|
| 军事单位 | **本势力 ∪ 同战共战方可叠**；其余挡路绕行；敌对同格 → Battlefield | 战斗（同格 BF） |
| 运输队 | 目标并入 Unit（非军事）；遇**任何敌军军事**即停止 | 非交战敌可遭遇缴获；交战中敌军可只挡不抢 |
| 贸易商队 / 流民 | 一律 Unit kind；规则同运输（可调拦截率） | 见上 |

运输队具备攻防属性（M4+），`IsMilitary=false` 区分 ZOC；**遇敌按遭遇战结算（大概率被抢）**。详见 [game-concepts.md §3.0](./game-concepts.md)。

### 4.8 系统链与日顺序（目标）

```
StrategyTimeSystem
StrategyMarketSystem      ← M4+ 连续撮合、日 K、贸易税台账
StrategyEconomySystem     ← 生产、市民/军队口粮、月钱税、Harvest 粮税
StrategySupplySystem      ← 税赋/贡赋/补给/贸易运输队、软拦截
StrategyUnitSystem        ← 移动、swap、战斗
StrategyMessengerSystem   ← 方针/战报/情报（含物价大幅波动）
```

### 4.9 M3-d 过渡（部分已迁移）

| 能力 | 状态 |
|------|------|
| 日耗粮、军队/据点月维持费 | ✅ 保留 |
| 月钱税（人头/基础商业/贸易税汇总） | ✅ M4-b `ApplyMonthlyMoneyTaxes` |
| Region 收粮、农业税、贡粮运输 | ✅ M4-b |
| 钱纳运输队（`TaxMoney`） | ✅ M4-b；义务 = 当月钱税 × 贡赋比例（M4-c） |
| 市场撮合、贸易税台账 | ✅ M4-b |
| **关税（运输队货值）** | ✅ M4-c |
| **官办企业贸易收入** | ✅ M4-c |
| **跨据点贸易 AI** | ✅ M4-c |
| **`Diplomacy.Arrears*` 欠账写入** | ✅ M4-c/d（内藩→Diplomacy；直辖→`Force.InternalArrears*`） |
| **Market 设施 / 商户 AI / 同盟贸易** | ✅ M4-d |
| **拦截缴获 / 势力内欠账 / 俸禄 / 民心人口** | ✅ M4-d |
| 定额制、民心爆发 | M5+ |

仍保留运输队实体与 `StrategyTributeLedger` / `EconomyMonthly` 报告模式。

### 4.10 实装进度

| 模块 | 阶段 | 状态 |
|------|------|------|
| 市民/士兵口粮、整型常量 | M4-a | ✅ |
| `StrongholdMarket` 实体、贡赋欠账字段 | M4-a | ✅ |
| `StrategyIntelligenceLedger` 脚手架 | M4-a | ✅ |
| `StrategyMarketSystem` 连续撮合、日 K、贸易税台账 | M4-b | ✅ |
| Region 收粮日历、农业税、贡粮运输 | M4-b | ✅ |
| 月钱税（市民→府库）、商户维持费/关店 | M4-b | ✅ |
| 运输队软拦截、`TransportPurpose` | M4-b | ✅ |
| 关税（运输队货值计税） | M4-c | ✅ |
| 官办企业卖单、贸易收入入账 | M4-c | ✅ |
| 跨据点贸易 AI、Trade 运输队 | M4-c | ✅ |
| 市民缺粮自动买单 | M4-b | ✅ |
| `Diplomacy.Arrears*` 欠账累加（运送不足） | M4-c | ✅ |
| Market 设施绑定 | M4-d | ✅ |
| 商户卖单、同盟跨势力贸易 | M4-d | ✅ |
| 拦截缴获、势力内欠账、俸禄、民心/人口 | M4-d | ✅ |
| 定额制/民心爆发 | M5+ | 待做 |

### 4.11 远期（M5+）

**资源类型**（`Actor` 已有字段，策略玩法逐步启用）：金钱、粮草、铁、木材、马、铁炮/大炮等；官办特产种类与产能表。

**尚未实装**：掠夺收入、角色俸禄、建设投资、外交赠金/赔款、定额制歉年事件、店铺设施荒废、AI 经济政策、年度人口细则。

---

## 5. 军事系统

```
StrategyMilitary
├── 单位招募
│   ├── 从据点人口中征兵
│   ├── 兵种决定于设施和资源
│   ├── 招募消耗金钱+资源+时间
│   └── 新兵训练度低，需时间提升
├── 单位维护
│   ├── 每日消耗粮草
│   ├── 每月消耗维护费
│   ├── 出征状态额外消耗
│   └── 断补给：士气下降→逃兵（运输队被截/路径切断时系统自动重派或进入紧张/断绝）
│   └── 补给三态（M3-c）：`SupplyStatusEvaluator` 衍生 Sufficient/Strained/CutOff；DTO 含 `inTransitSupplies[]`
├── ZOC（控制区）
│   ├── 军事单位对相邻格子施加ZOC
│   ├── 敌方单位进入ZOC需额外AP
│   ├── ZOC重叠区域有叠加效果
│   └── 城塞ZOC范围更大
├── 战斗触发 → 战斗方式选择
│   ├── 单机：⚡ 瞬间（自动）战斗（M3） / ⚔ 亲自指挥（后期，M3 占位）
│   ├── M3-b：攻击 **命令排队** → **日推进** 结算（非选格即时开战）
│   ├── 自动路径：触发前胜率预测 → 玩家确认 → attack-order
│   ├── 结算后：战报信使自部队格向当主回程（战斗当日不移动，次日启程）
│   └── 战报 **详情** 在信使抵达当主（或同格即时送达）后由 UI 弹出；此前不可见
└── 战斗结算
    ├── 瞬间计算伤亡/胜负
    ├── 战斗结果影响战争分数
    └── 战败方损失兵力/士气
├── 指挥编制（地图实体 = Unit，团内 = SubUnit）
│   ├── 战略地图：一军一团（参照信长之野望·革新 / EU4 栈）
│   │   ├── 地图上 selectable 实体仅为 <Unit>（兵队）
│   │   └── 移动/攻击/方针/运输目标均指向 Unit
│   ├── 总将（Unit.LeaderId）
│   │   ├── **出征编组时确定**（指令「出征」或剧本开局）
│   │   ├── 战略层唯一下令对象；远程方针经信使传递
│   │   └── 瞬间战/胜率预测读取既有总将，**非**临战任命
│   ├── 子编制（SubUnit：兵种/备队）
│   │   ├── 挂在 Unit.SubUnitIds，**不**独立占格移动
│   │   ├── 出征编组时写入兵种构成（足轻/弓/骑/铁炮等）
│   │   ├── 可选队将（SubUnit.LeaderId）；省略则归总将统辖
│   │   └── 战力 = Σ(各段兵数 × 训练度 × 士气 × 兵种系数)
│   ├── 军师/奉行等职位
│   │   └── 独立「任命」指令，提供数值修正，与队将可分离
│   └── 亲自指挥（RPG 战术，后期）
│       └── 允许临战部署；**不属于**大战略默认编组流程
├── 运输队（SupplyConvoy，非军事单位）
│   ├── 概念上等同 <Unit> 且 <see cref="Unit.IsMilitary"/> = false
│   ├── 占格/遇敌：见 §5.1（遇敌军即挡，非交战可缴获）
│   ├── 具备 <see cref="SupplyConvoy.Name"/>、<see cref="SupplyConvoy.LeaderId"/>（总将/奉行）
│   ├── 兵数 = 人夫 + 护卫；粮草字段 = 载粮
│   └── API 情报字段与军事单位 DTO 对齐（`StrategySupplyConvoyStateDto`）
├── 信使（迁移中）
│   ├── **点对点**：Character（内置 NPC/具名）持文书移动；遇敌可俘截
│   ├── **传闻 TTL 网**：据点/路人广播（见 game-concepts §8.3）
│   └── 现行 `Messenger` 实体 API 仍可用，目标模型见上
├── 兵种类型（UnitTypeDefinition，M4+ 配置化）
│   ├── 当前：`StrategyTroopTypes` 硬编码足轻/弓/骑/铁炮（M3-b 最小集）
│   ├── 目标：剧本 JSON / `GameMasterData.UnitTypes` 定义世界中可用兵种及攻/防/移动力等
│   └── 用途：玩家自制剧本/世界；`SubUnit.TypeId` 引用配置表；战力公式读兵种系数
├── 据点领主（城主）与内藩（国主）
│   ├── `Stronghold.LordId`：领主角色 Id；**0 = 当主直辖**（展示势力当主名，必非空）
│   ├── 领主居城须为本据点；`LordId>0` 时同步角色 `StrongholdId`/坐标
│   ├── `Stronghold.LeaderId`：代官（与领主可分离）
│   ├── **Province** 仍为地理单元（国/道），不用于册封；任命**国主** = 新建 `Force`（`InnerVassal`）
│   ├── 内藩 `SuzerainForceId` 指向宗主；地图可切换「本家/内藩」视角；**可撤销**内藩（并回宗主）
│   └── 城主（同势力领主）≠ 国主（内藩势力）；太阁4 式分层
├── 地图着色（WebClient · M3）
│   ├── **势力**：每势力 Id 独立色（含内藩）
│   ├── **封地**：内藩归并宗主根势力同色
│   ├── **外交**：相对玩家根势力 — 蓝自/绿盟/红敌/橘不敌对（均含对方内藩）
│   └── DTO `diplomacies[]`：玩家势力 `Diplomacies` 摘要
├── 经济与贡赋（M4 · 详见 §4）
│   ├── 每日：口粮、市场撮合、日产
│   ├── 每月 1 日：钱税汇总、维持费、**钱纳**运输队（`TransportPurpose.TaxMoney`）
│   ├── 收粮日：农业税、**贡粮**运输（与 Region 日历同频）
│   ├── 月度/年度报告：`StrategyDayOutcomeBuffer`（`EconomyMonthly` / `EconomyAnnual`）
│   └── API：`GET /strategy/save`、`POST /strategy/restore-save`（JSON 存档）
├── 简单 AI（M3-d）
│   └── 非玩家闲置单位每 3 日向最近玩家据点寻路（弱扩张/边境压力）
├── 道路系统（M3-d）
│   ├── 剧本 JSON：`roadTypes` / `roadTemplates` / `placedRoads`（模板可复用编辑）
│   ├── 类型 → `GameMapMasterData.Roads`；实例格点 → `GameMapData.Roads`（稀疏 `tileIndex → typeId`）
│   ├── 寻路 `MovementRules` 读道路类型 `SpeedBonus` 或 `MovementCostOverride`
│   ├── DTO `map.roadCells[]`；前端以 **枚举贴图 overlay** 绘制（独立于地形 autotile，M4 渲染管线）
│   └── **不**写入 `TileMap.region`
├── **地图区域**（M3-d · 气候/收粮）
│   ├── 剧本 JSON：`regions[]` + `regionGrid[]`（行优先）
│   ├── 定义 → `GameMapMasterData.Regions`；格点归属 → `TileMap.region` 层
│   ├── 收粮/气候 `HarvestRules`、`RegionLocationHelper` 读 `TileMap.GetRegion`
│   └── 底栏显示：`地形 · 区域名`（如 `平地 · 尾张`）；DTO `tileRegionNames[]`
├── **地图地标**（M3-d · `GameMapMasterData.Landmarks`）
│   ├── **与 playable 据点分离**：地标存于地图主数据 `Landmarks`（剧本 JSON `map.landmarks[]`），**不**写入 `GameData.Strongholds`
│   ├── **独立格点**：可与据点同格或独占一格；API `map.landmarks[]` + 逐格 `tileTerrainNames` / `tileRegionNames`
│   ├── **UI**：地图格内 ◆ 标记；底栏 **独立段落** `📍 地标名`（不与 `🏯 据点` 用「·」拼接）
│   └── **据点类型（M4 规划，未实装）**
│       ├── **史实据点**：玩家在地标格 **建造/放置** 据点 → 认定与地标一致；**不影响**该格税收/收入公式
│       └── **虚拟据点**：无地标背书的新建/虚构城名 → 按 **游戏难度** 对据点收入施加不同比例 **减益**（具体系数 M4 平衡）
```

---


### 5.1 同格堆叠、战场与围城（2026-07-15 冻结 · 待实装）

权威概念表：[game-concepts.md §3.0 / §5.5 / §6](./game-concepts.md)。

```
堆叠与索引
├── 每格 List<unitId>（旧一格一 Id 存档不兼容）
├── 军事可叠：同势力 ∪ 同一 War 共战方（已 Join）
├── 平时同盟 / 中立：不可军事同格，挡路绕行（据点同理）
└── Support + TargetUnitId 同格 → 跟随移动（不替代索引）

Battlefield 容器
├── Field：敌对军事同格创建（取消邻格对峙/邻格开战）
├── Siege：下达攻城令且同格加入，与野战同一套容器抽象
├── 两侧列表；入格顺序定主战；主战歼灭 ≠ 整场必败（士气冲击换主战）
├── 地图折叠显示交战双方；战记/回放 → 战争情报
├── 入场方向 → 溃逃原路；不可穿敌对交战格；中立不得入交战格
└── 速决 / 对峙 / Commit / 30 日强制战：思想流不变，日数记在 BF 上

围城与据点
├── 仅攻城令算包围；兵力 = 同格围城侧（含共战盟友）
├── 必要兵力 ∝ 据点规模；pressure = clamp(兵力/必要, 0, 1)
├── 充分：满压制 + 禁出城野战；不充分：可出城；信使/运输成功率随 pressure↓
├── 城内：本势力兵数 + Support；盟友解围 = 外打围军（不入城）
└── 出城编队上图 = 野战；破围后城方可再出击

战后
├── 可在 BF 内追击 → 击溃 or 败方 Routing 离场 + 追方修正 1 日
└── 当主阵亡：冲击 + 临时指挥/溃走检定 + 继承（不整场秒崩）

IMoveable
├── Unit：军 / 运输 / 商队 / 流民…
└── Character：将领、点对点信使（内置 NPC 可）
```

地图编制与运输/信使树状说明（下节）中，**占格与接敌以本小节为准**；下列旧「同盟 swap / 邻格对峙」描述作废。

## 6. 自动战斗系统

### 6.1 设计理由

在多人策略模式中，如果两个玩家的势力发生战争并进入战术地图：
- 其他玩家必须等待战斗结束，游戏进程被阻塞
- 战斗时间不可控，可能长达数十分钟
- 多场战斗并行时无法协调参与程度
- 与半回合制的流畅体验冲突

因此策略模式采用类似 EU4/CK3 的方式：战斗在战略地图上即时结算，玩家的决策重心在于**是否开战、何时开战、如何部署**，而非战术微操。

### 6.2 核心机制：方针驱动（Directive-Driven）

**关键设计**：防守方不需要实时应对，而是通过预设的**方针（Directive）**自动响应事件。这彻底消除了玩家间的直接交互等待，使多人游戏完全异步化。

每个玩家在非战斗时为自己的单位设定各种情境下的方针，当事件发生时系统根据方针自动执行。方针可能成功也可能失败（如"逃跑"方针在敌军包围下可能逃跑失败），增加策略深度。

### 6.3 自动战斗类型

```
InstantEventType（自动战斗类型）
│   ├── 野战           敌对军事同格进入 Field Battlefield（无邻格开战）
│   ├── 攻城战         攻城令 + Siege Battlefield（据点格）
│   └── 伏击战         埋伏单位触发
```

### 6.4 自动战斗流程

```
InstantEventFlow
├── 1. 触发阶段
│   ├── 玩家/AI 移动后相邻接敌，或主动下达攻击令
│   └── 相邻即接敌：不要求双方合计 ≥6000 才进入战斗流程
├── 2. 预览阶段（攻方，玩家主动发起时）
│   ├── 计算并显示胜率预测（详见第8章）
│   ├── 显示双方参战力量对比
│   ├── 显示防守方可能采取的方针（基于情报）
│   ├── 显示可能的后果范围
│   └── 玩家选择：确认发起 / 取消
├── 3. 当日野战分流（FieldBattleAutoResolver，M3 实装）
│   ├── 小股（合计兵数 < 6000）：前 `SmallArmyProbeDays=2` 日默认对峙试探，之后倾向决战
│   ├── 大股（合计 ≥ 6000）：默认当日「对峙」（低伤亡、无胜负）
│   │   ├── 双方仍每日接敌，但仅列阵僵持
│   │   ├── 对峙信使/事件仅在第 3/5/10/15/20/30 日发送（非每日 spam）
│   │   └── 对峙日数由 StrategyFieldEngagementRegistry 追踪
│   └── 强袭决战（ShouldCommitDecisiveAssault）
│       ├── 一方调整后胜率 ≥ 58% 时判定适合强袭
│       ├── 强袭因子：敌军补给断绝/紧张、携粮日数、指挥官性格（胆气/野心/慎重）
│       ├── 预留：计谋成功、天气突变等 hook
│       └── 对峙累计 ≥30 日强制决战
├── 4. 决战日战术模拟（TacticalBattleSimulator）
│   ├── 先手：互攻时比较移动力，高者先进攻并担任攻方
│   ├── 参战：以守方主队为中心，曼哈顿 ≤4 格内双方兵队入场
│   ├── 围攻：守方上下左右四邻皆为攻方势力单位 → 围攻态势
│   ├── 展开：各队 SubUnit 全部展开，按移动力高低依次行动
│   ├── 将领：双方主将按性格/智谋/士气判定强攻/坚守/侧击/鼓舞/脱离
│   ├── 多回合交锋：子队互击结算伤亡，写回 SubUnit
│   └── 战报：过程叙述（接触/布阵/将领/回合/交锋），不含因素修正百分比描述
├── 5. 结算与广播
│   ├── 决战：生成战报信使（BattleReportDispatchRules 过滤 spam）
│   ├── 对峙：仅在里程碑日发信使 + StandoffReport 事件
│   └── 更新世界状态（据点归属/单位位置/外交关系）
```

### 6.5 野战自动战斗模块（M3 实装映射）

| 组件 | 职责 |
|------|------|
| `StrategyMoveEngagementSystem` | 移动后扫描相邻格，持进攻方针则 `QueueAttack` |
| `StrategyBattleResolutionSystem` | 日末处理攻击令，调用 `FieldBattleAutoResolver` |
| `FieldBattleAutoResolver` | 当日结果：`Standoff` 或 `Decisive` |
| `BattleCommitRules` | 强袭判定：胜率阈值、补给、性格、30 日强制 |
| `InstantBattleCalculator` | 决战日确定性伤亡与胜负 |
| `StrategyFieldEngagementRegistry` | （迁移中）对峙日数；目标记入 Battlefield |
| `BattleReportDispatchRules` | 战报/对峙信使派遣条件 |
| `BattleFactorEvaluator` | 全因素评估（见 [`strategy-auto-battle-factors.md`](strategy-auto-battle-factors.md)） |
| `BattleMoraleRules` | 战后士气涨跌与低士气禁战 |

### 6.5b 战争迷雾与情报（2026-07 实装）

详见 [`strategy-fog-of-war-design.md`](strategy-fog-of-war-design.md)。

| 组件 | 职责 |
|------|------|
| `GameStartOptions` / `GameStartOptionsPresets` | 难度模板与 Custom 快照 |
| `IVisionPolicy` / `IIntelPolicy` | 可见格计算；ForceIntel 下 IIntelPolicy 为 no-op |
| `StrategyEspionageIntelLedger` | 谍报台账（2 月过期；scope/精度） |
| `EspionageIntelRules` | 非自势力 DTO masking（视野≠具体数值） |
| `StrategyVisibilityLedger` | explored bitset、known 据点、日重算 |
| `StrategyVisionSystem` | 日推进：剔除过期谍报 + 重算 visible |
| `StrategyFogDtoRules` | 过滤单位/据点/战场 DTO |
| `InstantEventMessages` | UI 摘要提前；信使/Message 权威不变 |
| `POST espionage-intel` | 登记谍报（忍者任务玩法 📋） |

### 6.5c 实体看法 / 影响 / 任务（2026-07 实装）

> 与 §6.5b **谍报 intel**（非自势力数值 masking）不同：本节为 Domain 实体上的 **ViewEffects / ActiveEffects / IntelTasks**，经 DTO 直出情报 UI。

| 组件 | 职责 |
|------|------|
| `IntelEntityBootstrapHelper` | 剧本加载后：ServiceDate、IntelTasks、亲属关系基线、兵力/维护/技术缓存、**演示看法 seed** |
| `CharacterRelationshipBootstrapHelper` | 父母/配偶/仇敌等默认 Relationship、Trust |
| `CharacterRelationsHelper` | DTO Relations（亲属图 + 五档亲疏，供人际关系 Tab） |
| `CharacterIntelTasksHelper` | 运行时派生 activeTasks；与 Character.IntelTasks 双轨 |
| `EntityEffectHelper` | Magnitude 汇总、Loyalty 有效值、三套 formatter |
| `StrategyWorldStateDto` | MapDiplomacyViewEffects / MapCharacterViewEffects / MapEntityEffects |
| `StrategyWorldSaveService` | Relationships.ViewEffects、IntelTasks、Diplomacy.ViewEffects 存档 |
| `strategyIntelSystemData.ts` | 情报 Tab 行数据；DTO 为空时 mock fallback（待移除） |

**势力看法 Tab 规则：**

- 选中 **本家根势力** → 隐藏「本家看法」「对本家的看法」
- 选中 **内藩** → 显示 Tab，数据来自 `diplomacies[isInnerVassal]`
- 外交看法 TargetStat：`Diplomacy` / `Relationship` → UI「外交关系」

**人物看法 Tab 规则：**

- 选中 **当主本人** → 隐藏「本人看法」「对本人的看法」
- 视角固定为 **玩家当主** ↔ 列表选中角色
- 角色看法 TargetStat：`Relationship`→亲疏、`Trust`→信赖、`PersonalOpinion`→个人观感；**不含外交关系**

**演示 seed（mini_kanto）：** 桶狭间、世仇（势力）；骏河继承之争、杀害本家当主（角色）。待事件系统接管后删除 `SeedDemo*Views`。

详见 [`shared-detail-design.md` §1.2.1a](./shared-detail-design.md) 与 [`strategy-ui-design.md` §7.2 / §9.9](./strategy-ui-design.md)。

### 6.6 自动战斗的多人同步

```
InstantEventMultiplayer
├── 完全异步设计（核心优势）
│   ├── 自动战斗不暂停游戏时间
│   ├── 防守方无需实时响应，方针自动执行
│   ├── 其他玩家完全不受影响，可继续操作
│   ├── 多个自动战斗可并行处理
│   └── 玩家离线时方针照常执行
├── 方针预设
│   ├── 玩家在非战斗时为单位设定方针
│   ├── 方针存储在单位数据中，随游戏同步
│   ├── 方针变更即时生效（下一个事件即使用新方针）
│   └── 未设定方针的单位使用势力默认方针
├── 事件队列
│   ├── 同一单位同时只能参与一个自动战斗
│   ├── 多个事件针对同一势力时排队处理
│   └── 优先级：攻城 > 野战 > 谍报 > 外交
└── 结果一致性
    ├── 所有自动战斗由主机/服务器结算
    └── 客机只负责展示，不参与计算
```

### 6.7 野战遭遇的现实模型（设计目标）

两军相邻即进入**战斗事件**（Field Engagement），但「接敌」≠「立刻决战」。现实逻辑如下：

```
FieldEngagementReality
├── 0. 触发
│   ├── 移动/接敌后相邻 → 进入战斗事件（每日仍接敌扫描）
│   └── 双方进入「对峙接触」状态，互相试探底细
├── 1. 对峙期（默认，尤其大军 / 初期）
│   ├── 双方未摸清实力前，绝大多数日子「按兵不动」
│   ├── 无纠缠、无伤亡、无胜负（Standoff）
│   ├── 情报随对峙日数逐渐改善（待实装：侦察/谍报 hook）
│   └── 信使仅在里程碑日通报（3/5/10/15/20/30）
├── 2. 贸然进攻（低概率）
│   ├── 将领特殊心态：轻敌、复仇、鲁莽等 → 无视胜率直接 Commit
│   ├── 常规性格仅作小幅修正（胆气/野心/慎重）
│   └── 或客观窗口：敌粮尽、计谋得手、备队抵达（高概率 Commit）
├── 3. 决战（Commit / 纠缠）
│   ├── **纠缠特性**：一旦发起决战，双方进入「纠缠态」
│   │   ├── 无第三方备队介入时，劣势方**不能**再拖入长期对峙
│   │   └── 当日必须产出：一方胜 / 一方负（或极少数同归于尽）
│   ├── 由 InstantBattleCalculator 一次性结算胜负与伤亡
│   └── 小股遭遇可视为默认当日即纠缠（兵力少、试探空间小）
├── 4. 决战后（士气链）
│   ├── 胜方：士气大涨 → 后续胜率/追击意愿上升
│   ├── 负方：士气低落 → **短期内不宜再战**
│   │   ├── 极低士气时 Commit 阈值极高（将领不会送死）
│   │   └── 方针倾向撤退/重整，而非再次 QueueAttack
│   └── 负方若未溃灭：当日原地重整（方针=Retreat，补给 AP），次日由 AI/玩家自行撤离；不强制后撤一格
└── 5. 未决战日结局
    └── 继续对峙（平手），次日重复 1→2→3 判定
```

**与 EU4/CK3 式自动战斗的关系**：玩家决策重心仍是「是否接敌、何时强袭、是否投入备队」，而非战术微操；纠缠态把「决战日」与「对峙日」在规则上明确分开。

### 6.8 实装对照（M3 当前代码 vs 设计目标）

| 环节 | 设计目标 | 当前实装 | 差距 |
|------|----------|----------|------|
| 相邻接敌 | 进入战斗事件 | `MoveEngagementRules` + `QueueAttack` | ✅ 已对齐 |
| 初期对峙 | 前几天多数不进攻 | 大军 Standoff + 小股 `SmallArmyProbeDays=2` | ✅ |
| 贸然进攻 | 轻敌/复仇/鲁莽 | 性格 + `BattleDirectiveRules` + 中计 ForceCommit | ⚠️ 复仇事件未建 |
| 客观强袭窗口 | 粮尽、计谋、天气 | 补给/携粮 + `BattleStratagemEvaluator` + `BattleWeatherEvaluator` | ✅ 基础 |
| 胜方士气涨 / 负方跌 | 战后 ±N | `BattleMoraleRules` | ✅ |
| 低士气禁战 | 拒绝 Commit/接敌 | `CanUnitEngage` + BlockCommit | ✅ |
| 败方撤退 | 全势力 AI 败退 | `BattleRetreatRules` + `BattleAftermathHelper` | ✅ |
| 当主视角 | 玩家势力部队非当主直辖亦 AI 行动 | `BattleAftermathHelper` 不区分 PlayerForceId | ✅ |
| 战报信使 | 抵达后解锁详情 | `BattleReportDeliveryHelper` + Events[].BattleResult | ✅ |
| 胜率全因素 | §8 公式 | `BattleFactorEvaluator` | ✅ |
| 方针系统 | 死守/迎击/撤退 | `BattleDirectiveRules` | ✅ 基础 |
| 兵种构成 | SubUnit 加权 | `BattleCompositionCalculator` | ✅ |
| 30 日强制战 | 长期对峙终局 | `StandoffForceBattleDays = 30` | ✅ 已实装 |
| 阵型 / 装备 | FormationId、WeaponId/ArmorId | `BattleFormationRules` / `BattleEquipmentRules` | ✅ |
| 接敌类型 | 野战/伏击/攻城 | `BattleEngagementClassifier` + `SiegeBattleRules` | ✅ 城防系数 + 占领 |
| 战报因素明细 | Breakdown → DTO/UI | `BattleFactorMapper` + 前端战报对话框 | ✅ |
| 玩家视角胜负 | 当前势力视角 | `AttackerForceId`/`DefenderForceId` + 前端 `playerWonBattle` | ✅ |

**建议实装顺序（供后续迭代）**

1. ~~士气链、小股试探、全因素 Evaluator~~ ✅  
2. ~~方针 / SubUnit / 计略天气 hook~~ ✅  
3. ~~**阵型、装备**接入 `BattleFactorEvaluator`~~ ✅  
4. ~~**攻城/伏击** 接敌分类与系数~~ ⚠️ 完整攻城 Resolver 待做  
5. **ClimateSystem** 替换季节近似  
6. **Formation/装备 MasterData**、复仇事件、SubUnit 分段伤亡  

---

## 7. 方针系统

```
Directive（方针）
├── 战斗方针 — 遭遇敌军攻击时
│   ├── 死守           不撤退，战斗到底
│   │   ├── 效果：防御+20%，士气下降减缓
│   │   ├── 失败条件：无（必定执行）
│   │   └── 后果：高伤亡，但可能拖延敌军
│   ├── 坚守           尽可能抵抗
│   │   ├── 效果：先正常战斗2轮，然后尝试撤退
│   │   ├── 撤退成功率：基于剩余兵力比和地形
│   │   └── 后果：中等伤亡，可能保全部分兵力
│   ├── 迎击           主动出击
│   │   ├── 效果：攻击+15%，防御-10%
│   │   ├── 失败条件：无
│   │   └── 后果：快速决战，胜负取决于战力差
│   └── 逃跑           立即尝试撤退
│       ├── 效果：不战斗，直接尝试脱离
│       ├── 成功率：基于移动力差、地形、是否被包围
│       ├── 失败后果：被追击，伤害×1.5，士气大降甚至可能被击溃被俘
│       └── 成功后果：保全兵力
└── 攻城方针 — 攻击据点时
    ├── 包围           将敌军包围在据点内但不进攻
    └── 强攻           猛攻据点图谋快速占领城池
```

## 8. 胜率预测系统

> **因素全表与公式实装细节**见 [`strategy-auto-battle-factors.md`](strategy-auto-battle-factors.md)。

玩家在触发自动战斗前，系统会计算并展示胜率预测，帮助玩家做出战略决策。胜率预测会考虑防守方可能采取的方针。

```
WinRatePrediction
├── 胜率计算公式（M3+ 由 BattleFactorEvaluator 实装）
│   ├── 基础战力 = Soldier×(Attack+Defense)/20 × TrainingScale × LeadershipScale
│   ├── 基础胜率 = 攻方有效战力 / (攻+守) × 100
│   ├── 修正因素（详见 strategy-auto-battle-factors.md §2~§3）
│   │   ├── 士气、训练、统率/武力/智谋、生病、疲劳
│   │   ├── 姿态/状态（坚守/埋伏/高昂/恐惧/混乱…）
│   │   ├── 地形（守方山地/丘陵）、补给、携粮、对峙情报、附近友军
│   │   ├── 方针、兵种构成、阵型、装备、天气、计略、接敌类型
│   │   └── 详见 strategy-auto-battle-factors.md §2~§3
│   ├── 强袭判定：AdjustedWinRate ≥ 58% 或 ForceCommit 或 ≥30 对峙日
│   ├── 防守方方针预估
│   │   ├── 情报充足：显示防守方实际方针
│   │   ├── 情报一般：显示最可能方针（概率分布）
│   │   └── 情报不足：显示默认方针（可能不准确）
│   └── 最终胜率 = clamp(基础胜率 + Σ修正, 5%, 95%)
│       （胜率永远在5%~95%之间，不存在必胜/必败）
├── 预览信息展示
│   ├── 胜率百分比（如：攻方胜率 62%）
│   ├── 双方战力对比条形图
│   ├── 防守方预估方针（基于情报精度）
│   ├── 关键加成/减益列表
│   ├── 预估伤亡范围（如：攻方损失 15~30%）
│   └── 可能后果（如：胜则占领据点/败则撤退至XX）
└── 胜率精度
    ├── 胜率是估计值，非精确值
    ├── 实际结果有随机波动（±10%）
    ├── 情报不足时胜率精度降低（如未侦察到敌方增援）
    ├── 防守方方针可能改变实际胜率（如逃跑成功则攻方0伤亡）
    └── 谍报系统可提高胜率预测精度和方针情报获取
```


---

## 9. 自动战斗战斗结算

```
InstantBattleResolution
├── 接敌当日分流（FieldBattleAutoResolver）
│   ├── Standoff（对峙）
│   │   ├── 无伤亡结算、无胜负
│   │   ├── standoffDays += 1
│   │   └── 仅里程碑日发信使（3/5/10/15/20/30）
│   └── Decisive（强袭决战）
│       ├── 选定 CommittedAggressor（强袭方）
│       └── 进入下方 InstantBattleCalculator 流程
├── 结算输入（决战日）
│   ├── 攻方单位列表（兵数/类型/士气/训练度/指挥官）
│   ├── 守方单位列表（同上）
│   ├── 地形信息
│   ├── 攻方策略（正面攻击/包围/突袭）
│   ├── 守方方针（自动匹配，含条件判断）
│   ├── 强袭原因（CommitReason：胜率/补给/性格等）
│   └── 修正因素（补给/阵型/特性等）
├── 强袭判定（BattleCommitRules，大股专用）
│   ├── 基础胜率 = InstantBattleCalculator.ComputeAttackerWinRatePercent
│   ├── 调整后胜率 += 敌军补给断绝(+15)/紧张(+8)
│   ├── 调整后胜率 += 敌军携粮 ≤3 日(+12) / ≤7 日(+6)
│   ├── 调整后胜率 += 指挥官胆气/野心/慎重修正
│   ├── 阈值：≥58% 则 ShouldCommit = true
│   └── 对峙 ≥30 日：强制 ShouldCommit = true
├── 结算过程（确定性 + 随机种子）
│   ├── 0. 方针匹配
│   │   ├── 查找守方单位预设方针
│   │   ├── 检查方针附加条件
│   │   ├── 条件满足则使用该方针，否则使用默认
│   │   └── 计算方针执行成功率
│   ├── 1. 方针执行判定
│   │   ├── 成功：按方针效果结算
│   │   └── 失败：按方针失败后果结算
│   ├── 2. 计算双方有效战力（含方针效果修正）
│   ├── 3. 根据策略/方针计算交锋轮次（1~5轮）
│   ├── 4. 每轮计算双方伤害
│   │   ├── 伤害 = 战力比 × 策略/方针系数 × 随机(0.8~1.2)
│   │   ├── 死守方伤害减免 ×0.8
│   │   ├── 逃跑失败方伤害增加 ×1.5
│   │   ├── 伏击成功方首轮伤害 ×2.0
│   │   └── 突围方伤害波动 ×(0.5~1.5)
│   ├── 5. 更新双方兵数/士气
│   ├── 6. 检查撤退条件（士气<30%可能溃逃）
│   └── 7. 判定胜负
├── 结算输出（M3 实装）
│   ├── 胜负结果（`AttackerWon`；前端按 `playerForceId` 显示胜利/失利）
│   ├── 双方伤亡（`InstantBattleCalculator`；`PercentOf` 小股至少扣 1）
│   ├── 接敌类型（`EngagementKind`：FieldBattle / Ambush / Siege）
│   ├── 因素明细（`FactorNotes` + 战报日志「因素」段）
│   ├── 战斗过程日志（`LogEntries`）
│   └── 战后士气 / 方针 / 败退（`BattleMoraleRules`、`BattleRetreatRules`）
├── 结算输出（设计扩展，未实装）
│   ├── 方针执行结果（成功/失败/条件触发）
│   ├── 指挥官战果（负伤/阵亡/被俘）
│   ├── 战利品（金钱/物资）
│   ├── 战争分数变化
│   └── 多轮交锋模拟（当前为单轮 Instant 结算）
└── 结算确定性
    ├── 使用共享随机种子确保多人一致性
    ├── 种子 = 事件ID + 游戏日期 + 参战单位ID哈希
    └── 所有客户端可独立验证结果
```

---

## 10. 外交系统

```
StrategyDiplomacy
├── 外交行动
│   ├── 提议同盟
│   ├── 请求联姻
│   ├── 要求朝贡
│   ├── 宣战（需宣战理由）→ 创建 War，宣战方 = WarAggressor
│   ├── 召盟 / 应召 JoinWar（参战国）
│   ├── 求和 / 参战国单独议和（信使送达后生效）
│   ├── 贸易协定
│   ├── 军事通行权
│   └── 赠送金钱
├── 宣战理由（Casus Belli）
│   ├── 核心领土（拥有核心的据点被占）
│   ├── 侮辱（外交事件触发）
│   ├── 宗教冲突（不同宗教间）
│   ├── 贸易争端
│   └── 无理由（大幅降低声望/稳定度）
├── 战争实体 War（📋 2026-07-15）
│   ├── 主战国 + 参战国；战争情报汇战报与 BF 记录
│   ├── 对同一交战敌军：只能 Join，不可平行第二 War 挂同一 Battlefield
│   ├── 禁止战时势力倒戈；避免与交战双方同时同盟
│   ├── 参战国可单独与敌议和退出；主战国议和结束整场战争
│   └── 议和信使在途：战场可续打；送达 → 关联 BF 强制停火
├── 战争分数（War Score）
│   ├── 战斗胜利 +5~15
│   ├── 占领据点 +10~30
│   ├── 海战胜利 +5~10
│   └── 战争分数达到100可强制和谈
└── 和谈条件
    ├── 割让据点
    ├── 赔款
    ├── 朝贡
    ├── 释放附庸
    └── 割让核心
```

概念词典：[game-concepts.md §5.5](./game-concepts.md)。平时同盟 **不** 授予军事同格权（仅同战共战方可叠）。

---

## 11. 多人联机架构

策略模式采用**主机-客机**架构：

```
StrategyMultiplayer
├── 主机（Host）
│   ├── 运行完整游戏引擎
│   ├── 控制时间推进
│   ├── 处理所有AI决策
│   └── 广播状态更新
├── 客机（Client）
│   ├── 发送玩家指令
│   ├── 接收状态更新
│   ├── 请求暂停/继续
│   └── 本地预测+服务端校验
└── 同步机制
    ├── 指令式同步（只同步指令，不同步状态）
    ├── 每个时间步收集所有玩家指令
    ├── 确认后统一执行
    └── 断线重连通过快照恢复
```

网络协议详见 [共同详细设计 §8.1](./shared-detail-design.md#81-策略模式多人协议)。

---

## 12. 策略专属事件

```
StrategyEvent（策略事件）
├── WarDeclaredEvent            宣战
├── PeaceTreatyEvent            和谈
├── AllianceFormedEvent         同盟成立
├── MonthlySettlementEvent      月度结算
├── InstantEventTriggeredEvent  自动战斗触发
├── InstantEventResolvedEvent   自动战斗结算
└── WinRatePreviewEvent         胜率预测请求
```

---

> 本文档为战国绘卷大战略模式的详细设计，共享系统详见[共同详细设计](./shared-detail-design.md)，界面设计详见[大战略模式界面设计](./strategy-ui-design.md)。
