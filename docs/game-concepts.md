# SengokuScroll 游戏概念词典（Game Concepts Reference）

> 版本：1.3 | 日期：2026-07-15 | 索引：[设计文档索引](./README.md)

本文档是 **游戏中所有已定义概念的权威清单**：每个概念给出中英名称、一句话定义、实装状态与关键代码/设计引用。

**与详细设计的关系**：

| 文档 | 职责 |
|------|------|
| [game-concepts.md](./game-concepts.md)（本文） | **概念是什么** — 名词解释、枚举值、实体关系 |
| [strategy-detail-design.md](./strategy-detail-design.md) 等 | **玩法怎么设计** — 流程、数值、UI、里程碑 |
| [shared-detail-design.md](./shared-detail-design.md) | **跨模式共享内核** — 实体字段、System 顺序 |

> 数值常量以代码为准（`BattleConstants`、`EconomyConstants`、`LogisticsConstants`、`GameRuleConfig` 等）。

---

## 0. 维护约定（变更时必须遵守）

### 0.1 何时更新本文档

在以下任一情况发生时，**必须**同步更新本文档对应章节：

- 新增或删除 **实体、枚举、状态、规则类、System**
- 改变概念的 **语义**（例如：驻军从地图单位改为城内兵数）
- 新增 **剧本 JSON 字段** 或 **存档字段**
- 前端 DTO / 情报面板展示的新概念
- 设计文档中已确认、并开始实装的新机制

以下情况 **可不** 单独更新概念条目（但应在 PR/提交说明中注明「无概念变更」）：

- 纯 Bug 修复、不改变语义
- 重构（类名/文件移动但概念不变）
- 仅调整数值常量、不改变概念定义

### 0.2 变更影响检查清单

每次改动游戏内容前，逐项自问：

| # | 检查项 | 若答案为「是」则… |
|---|--------|-------------------|
| 1 | 是否新增/删除了 Domain 实体或枚举？ | 更新 §1–§2 与附录 A |
| 2 | 是否改变了 Unit / Character / Stronghold / Force 的字段语义？ | 更新对应实体小节 + 存档 §10 |
| 3 | 是否新增或修改战斗/接敌/攻城流程？ | 更新 §3、§6、§7 |
| 4 | 是否影响经济产出、税种、市场或运输？ | 更新 §4 |
| 5 | 是否改变外交关系或从属规则？ | 更新 §5 |
| 6 | 是否改变 AI、信使或玩家间接指挥？ | 更新 §8 |
| 7 | 是否改变日推进顺序、AP 或移动占格？ | 更新 §9 |
| 8 | 是否改变剧本/存档/持久化字段？ | 更新 §10 |
| 9 | 是否新增前端可见标签或事件类型？ | 更新 §11 |
| 10 | 实装状态是否从「设计」变为「已实装」？ | 更新条目状态标记 ✅/📋 |

### 0.3 条目格式

每个概念条目应包含：

1. **名称**（中文 / English）
2. **定义**（一句话）
3. **实装状态**：✅ 已实装 · 📋 设计/部分实装 · 🔮 远期
4. **关键实现**（Rules / Systems / 实体文件）

代码中的业务逻辑注释规范见 `.cursor/rules/business-comments.mdc`（`///` 说明业务含义，`// 业务：` 标注规则分支）。

### 0.4 变更记录

| 日期 | 版本 | 变更摘要 |
|------|------|----------|
| 2026-07-15 | 1.3.1 | 劝降后降方离场+收编；胜方占敌城自动强攻；地图交战 BF 白底红字折叠图标 |
| 2026-07-14 | 1.2 | 难度框架、战报写实投递、逃入友城、本局固定种子、溃灭/逃城事件可见 |
| 2026-07-13 | 1.1 | 驻军简化为城内兵数 + Support 守城单位；移除野战驻军/议和退还 |
| 2026-07-13 | 1.0 | 初版：策略模式全量概念；含驻军、溃灭、Standoff、占城后果 |

---

## 概念总索引

| ID | 概念 | 章节 | 状态 |
|----|------|------|------|
| GW | 游戏世界 GameWorld | §1.1 | ✅ |
| GD | 运行时数据 GameData | §1.1 | ✅ |
| FRC | 势力 Force | §1.2 | ✅ |
| CHR | 角色 Character | §1.3 | ✅ |
| SH | 据点 Stronghold | §1.4 | ✅ |
| UNT | 地图单位 Unit | §1.5 | ✅ |
| SUB | 子编制 SubUnit | §1.5 | ✅ |
| MOV | 可移动抽象 IMoveable | §1.8 | 📋 |
| STK | 同格堆叠 Stacking | §3.0 | 📋 |
| BFD | 战场容器 Battlefield | §3.0 | 📋 |
| WAR | 战争 War | §5.5 | 📋 |
| GAR | 城内驻军 City Garrison | §6 | ✅ |
| DIR | 单位方针 UnitDirective | §2.1 | ✅ |
| STA | 单位姿态 UnitStance | §2.2 | ✅ |
| STS | 单位状态 UnitStatus | §2.3 | ✅ |
| SIE | 攻城模式 UnitSiegeMode | §2.4 | ✅ |
| ENG | 接敌类型 BattleEngagementKind | §3.1 | ✅ |
| SDO | 战场对峙 Standoff | §3.2 | 📋 |
| COM | 强袭决战 Commit | §3.2 | ✅ |
| SUR | 劝降 Surrender | §3.4 | ✅ |
| AFT | 战后处理 Aftermath | §3.5 | ✅ |
| DES | 部队溃灭 Unit Destruction | §3.5 | ✅ |
| RTG | 溃逃 Routing | §3.5 | 📋 |
| ECO | 经济与日结算 Economy | §4 | ✅ |
| MKT | 据点市场 Market | §4.3 | ✅ |
| CVY | 运输队 SupplyConvoy | §4.4 | ✅ |
| DIP | 外交 Diplomacy | §5 | 🟡 |
| OCC | 占城与议和 Occupation | §7 | ✅ |
| MSG | 信使 Messenger / Character | §8.3 | 📋 |
| AP | 行动力 AP / 移动 Movement | §9 | ✅ |
| SCN | 剧本 Scenario JSON | §10.1 | ✅ |
| SAV | 存档 Save Document | §10.3 | ✅ |

（完整枚举见附录 A。）

---

## 1. 核心世界实体（Core World Entities）

### 1.1 世界容器

| 概念 | 定义 | 状态 | 关键文件 |
|------|------|------|----------|
| **游戏世界 GameWorld** | 策略模式顶层容器：地图主数据 + 主数据表 + 运行时 `GameData`。 | ✅ | `SengokuScroll.Domain/GameWorld.cs` |
| **运行时数据 GameData** | 当前局内全部可变动实体（势力、据点、单位、子编制、角色、运输队、信使、日期、**SimulationSeed**）。 | ✅ | `SengokuScroll.Domain/GameData.cs` |
| **地图主数据 GameMapMasterData** | 静态地图：TileMap、地形、道路、政治区域网格、地标。 | ✅ | `SengokuScroll.Domain/GameMapMasterData.cs` |
| **主数据 GameMasterData** | 兵种、城防设施类型等配置表。 | ✅ | `SengokuScroll.Domain/GameMasterData.cs` |
| **世界快照 StrategyWorldStateDto** | 前端地图/面板用的只读摘要 DTO。 | ✅ | `SengokuScroll.Strategy/Models/StrategyWorldStateDto.cs` |

### 1.2 势力 Force

| 概念 | 定义 | 状态 | 关键文件 |
|------|------|------|----------|
| **势力 Force** | 政治-军事主体；聚合府库钱粮、外交、省份、战略/战术方针。 | ✅ | `Force.cs` |
| **势力身份 ForceStatus** | `Independence` 独立 / `InnerVassal` 内藩（无独立外交/军事）/ `OuterVassal` 外藩。 | ✅ | `Force.cs` |
| **宗主 SuzerainForceId** | 内藩/外藩指向的宗主势力 Id。 | ✅ | `Force.cs` |
| **势力战略 ForceStrategy** | `Hold` 守备 / `Area` 地区制霸 / `World` 天下制霸。 | 📋 | `Force.cs` |
| **势力战术 ForceTactics** | `War` 战争为主 / `Diplomacy` 外交为主。 | 📋 | `Force.cs` |
| **威望 Prestige / 正统 Orthodoxy** | 势力声望与合法性（0–100）。 | 📋 | `Force.cs` |
| **省份 Province** | 地理行政单元；编入据点的「核心」概念；失地可作宣战理由（远期）。 | 📋 | `Province.cs` |
| **内贡赋欠账 InternalArrears** | 直辖据点→当主居城未运完的欠粮/欠钱。 | ✅ | `Force.cs`；`TributeArrearsActions` |
| **Actor 类型 ActorType** | `Force` / `Merchant` / `Religion` / `Landlord`。 | ✅ | `Types/ActorType.cs` |

### 1.3 角色 Character

| 概念 | 定义 | 状态 | 相关文件 |
|------|------|------|----------|
| **角色 Character** | 可移动实体（`IMoveable`）；统率单位、任领主/代官、参与战斗与政务；**亦可作为信使本体**（革新式具名/内置 NPC）。 | ✅ | `Character.cs` |
| **势力内状态 CharacterForceStatus** | `Idle` 空闲 / `Task` 任务中 / `UnitAction` 随军 / `Prisoner` **被俘**。 | ✅ | `Character.cs` |
| **行动计划 CharacterActionPlan** | `Rest` / `Meet` / `Task` / `Report`。 | 📋 | `Character.cs` |
| **行动状态 CharacterActionStatus** | `Waiting` / `Resting` / `Moving` / `Acting`。 | 📋 | `Character.cs` |
| **位置类型 CharacterLocationType** | `Map` / `Stronghold` / `Unit`。 | ✅ | `Character.cs` |
| **当主 Lord** | 玩家扮演的势力君主；方针/战报信使出发点；同格指令免信使。 | ✅ | `StrategyLordHelper`；设计 §2.1 |
| **内置使者 NPC** | 固定编制的传令/杂役类 Character，承载点对点文书。 | 📋 | 设计：堆叠/信使规格 |

### 1.4 据点 Stronghold

| 概念 | 定义 | 状态 | 关键文件 |
|------|------|------|----------|
| **据点 Stronghold** | 地图 playable 城市；含人口、税率、城防、多层库存 Actor、市场。 | ✅ | `Stronghold.cs` |
| **领主 LordId** | 任命城主角色 Id；**0 = 当主直辖**。 | ✅ | `Stronghold.cs` |
| **代官 LeaderId** | 执行政务的代官（可与领主分离）。 | ✅ | `Stronghold.cs` |
| **统治力 Authority / 自治 Autonomy / 腐败 Corruption** | 中央控制力、地方自治、行政损耗（0–100）。 | 📋 | `Stronghold.cs` |
| **库存分层 Actor** | `ForceActor` 官府 / `CivilianActor` 市民 / `MerchantActors` / `ReligionActors`。 | ✅ | `Stronghold.cs`；`StrongholdActor.cs` |
| **史实据点 IsHistorical** | 非史实据点收入减益；未声明时按同格是否有 **Landmark** 推断。 | ✅ | `Stronghold.cs`；`StrategyScenarioLoader` |
| **保有核心 HasCoreForceIds** | 哪些势力宣称该据点为核心（宣战理由等）。 | ✅ | `Stronghold.cs` |
| **据点市场 StrongholdMarket** | 订单簿 + 日 K 线；需 Market 设施方可挂单。 | ✅ | `StrongholdMarket.cs` |
| **城防设施 DefenseFacilityIds** | Castle/Wall/Gate/Moat/Defender 等，累加城防值。 | 📋 | `StrongholdType.cs` |
| **经济设施 EconomyFacilityIds** | Market、奢侈品工坊等。 | ✅ | `EconomyFacilityRules.cs` |
| **据点类型 StrongholdType** | 平城/平山城/山城等地形建造限制。 | 📋 | `Types/StrongholdType.cs` |

### 1.5 地图单位 Unit 与子编制 SubUnit

| 概念 | 定义 | 状态 | 相关文件 |
|------|------|------|----------|
| **地图单位 Unit** | 地图上可移动的「队」实体（`IMoveable`）；军事兵团为默认形态；**特殊队一律归 Unit**（运输、商队、流民等），用 kind/职责区分。 | ✅→📋 | `Unit.cs` |
| **子编制 SubUnit** | 兵种段（足轻/弓/骑/铁炮）；挂于 `Unit.SubUnitIds`，不独立占格。 | ✅ | `SubUnit.cs` |
| **总将 LeaderId** | 出征编组时确定的战略层唯一下令对象（Character）。 | ✅ | `Unit.cs` |
| **守城单位** | 普通军事 `Unit`，方针 `Support`，驻守据点格；与城内兵数可并存；**盟友不得入城成建制协防**（解围见 §6）。 | ✅ | `StrongholdGarrisonRules.cs` |
| **行动目标 UnitActionTarget** | 目标势力/据点/单位/角色 + 路径队列 `RoutePoints`；Support 时可挂目标单位同格跟随。 | ✅→📋 | `Unit.cs` |
| **Actor 基类** | 钱粮物资、兵数、士气、训练度、伤兵等通用字段。 | ✅ | `Actor.cs` |
| **非军事标记 IsMilitary** | `false`：运输/商队等；遇敌军格仍触发遭遇（缴获/俘），见 §3.0。 | ✅→📋 | `Unit.cs` / `SupplyConvoy.cs` |
| **单位种类 UnitKind（目标）** | `Military` / `Convoy` / `Merchant` / `Migrant` 等；玩家默认可选中军事。 | 📋 | 设计规格 |

### 1.6 地图与地理

| 概念 | 定义 | 状态 | 相关文件 |
|------|------|------|----------|
| **地标 Landmark** | 与 playable 据点分离的地图标记（神社、名山等）；用于推断史实性。 | ✅ | `Landmark.cs` |
| **政治区域 Region** | 独立于道路层的收粮日历、气候区域。 | ✅ | `Region.cs`；`HarvestRules.cs` |
| **道路 Road** | 写入 TileMap；提供 `SpeedBonus` / 移动消耗 override。 | ✅ | `Road.cs`；`MovementRules.cs` |
| **设施 FacilityType** | 据点内设施：主家、兵营、市场、旅館等。 | 📋 | `Types/FacilityType.cs` |

### 1.7 非军事地图实体（迁移中）

| 概念 | 定义 | 状态 | 相关文件 |
|------|------|------|----------|
| **运输队 SupplyConvoy** | 粮秣/税赋/贸易载体；目标并入 `Unit`（kind=Convoy）；遇敌对军事即挡，非交战敌可缴获。 | ✅→📋 | `SupplyConvoy.cs` |
| **信使（实体） Messenger** | 现行独立实体；目标：**点对点文书由 Character（内置 NPC/具名）承载**；传闻见 §8.3 TTL 网。 | ✅→📋 | `Messenger.cs` |
| **商队 / 流民** | 未来地图队；一律 `Unit` + 对应 kind。 | 🔮 | 设计规格 |

### 1.8 可移动抽象 IMoveable

| 概念 | 定义 | 状态 | 相关文件 |
|------|------|------|----------|
| **IMoveable** | 地图需移动实体的共同抽象：**仅两大类** — `Unit`（一切「队」）与 `Character`（具名/NPC 人）。 | 📋 | 设计规格 |
| **寻路/占格** | 军事堆叠见 §3.0；中立与非共战方挡路则绕（单位与据点同理）。 | 📋 | `MovementRules`（待改） |

## 2. 单位状态、方针、姿态与攻城模式

### 2.1 战略方针 UnitDirective

| 枚举值 | 中文 | 定义 | 状态 |
|--------|------|------|------|
| `Move` | 移动 | 按路径行军，非主动接敌。 | ✅ |
| `Occupy` | 占领 | 进攻性目标（含攻城）；会主动接敌。 | ✅ |
| `Raid` | 劫掠 | 进攻性；接敌规则同 Occupy。 | ✅ |
| `Support` | 支援 | 防御/待命；**同格且 `TargetUnitId` 有效时跟随目标移动**，目标入战场则同侧挂入；决战时偏坚守。 | ✅→📋 |
| `Retreat` | 撤退 | 尝试脱离；阻断强袭 Commit；战后败方默认方针。 | ✅ |

**关键文件**：`Unit.cs` · `MoveEngagementRules.cs` · `BattleDirectiveRules.cs`

### 2.2 单位姿态 UnitStance

| 枚举值 | 中文 | 效果概要 | 状态 |
|--------|------|----------|------|
| `Normal` | 普通 | 基准移动/防御/疲劳。 | ✅ |
| `Attacking` | 攻击中 | 接敌/对峙时；下一回合继续攻击目标。 | ✅ |
| `Surrounding` | 包围中 | 被围方无法移动；攻城包围用。 | ✅ |
| `Maneuver` | 机动 | 移动力↑，发现伏兵概率↓。 | 📋 |
| `Alert` | 警惕 | 移动力↓，发现伏兵概率↑。 | 📋 |
| `Hold` | 坚守 | 无法移动，防御↑。 | ✅ |

**关键文件**：`Unit.cs` · `BattleFactorEvaluator.cs`

### 2.3 单位状态 UnitStatus

| 枚举值 | 中文 | 定义 | 状态 |
|--------|------|------|------|
| `Waiting` | 待机 | 原地；普通态。 | ✅ |
| `Moving` | 移动中 | 沿路径推进。 | ✅ |
| `Inspiring` | 斗志高昂 | 士气/攻击↑。 | 📋 |
| `Fearful` | 恐惧 | 士气/攻防↓。 | 📋 |
| `Chaos` | 混乱 | 兵数为 0 时进入；无法行动；**战后应触发溃灭移除**。 | ✅ |
| `Ambushing` | 埋伏 | 地图不可见；触发伏击战。 | 📋 |
| `BeingSurround` | 被包围 | 无法移动，士气下降加剧。 | ✅ |
| `Standoff` | 战场对峙 | **处于 Battlefield 内**、当日未 Commit 的接触态（无邻格对峙）；挂 `BattlefieldId`。 | ✅→📋 |
| `Routing` | 溃逃 | （目标枚举或衍生）战败未全灭后强制撤离；优先沿入场方向；再接敌易溃散。 | 📋 |

**关键文件**：`Unit.cs` · `BattlefieldEngagementRules.cs`

### 2.4 攻城模式 UnitSiegeMode

| 枚举值 | 中文 | 定义 | 状态 |
|--------|------|------|------|
| `None` | 无 | 非攻城上下文。 | ✅ |
| `Encircle` | 包围 | 对据点下达包围令并加入 **Siege Battlefield**；**仅同格**围城侧兵力计入；不足「必要兵力」则不充分（可出城野战，压制弱）。 | ✅→📋 |
| `Assault` | 强攻 | 对驻军/城防强攻决战；空城则占城。 | ✅ |

**关键文件**：`Unit.cs` · `SiegeOrderRules.cs` · `StrategySiegeSystem.cs`

### 2.5 决战战斗方针 BattleCombatDirective（由方针/姿态推断）

| 枚举值 | 中文 | 来源 | 状态 |
|--------|------|------|------|
| `HoldLine` | 坚守 | 守方默认 / Support | ✅ |
| `FightToDeath` | 死守 | `Stance=Hold` | ✅ |
| `CounterAttack` | 迎击 | Occupy/Raid 或攻方 | ✅ |
| `AttemptRetreat` | 逃跑 | `Directive=Retreat` | ✅ |

**关键文件**：`BattleDirectiveRules.cs`

---

## 3. 战斗概念（Battle Concepts）

### 3.0 同格堆叠与战场容器（2026-07-15 冻结规格 · 📋）

> 详细流程见 [strategy-detail-design.md §5.1 / §6.0 / §10](./strategy-detail-design.md)。**旧存档（一格一军索引）不兼容。**

| 概念 | 定义 | 状态 | 相关文件 |
|------|------|------|----------|
| **瓦片单位列表 Tile Unit Index** | 每格 `List<unitId>`（多军占格）；空间真相，不为 Support 目标所替代。 | 📋 | `GameMapData`（待改） |
| **军事同格堆叠** | 仅 **① 同一势力** 或 **② 同一场 War 的共战方（已 Join）** 可叠。平时同盟不可军事同格（挡路则绕）。 | 📋 | 设计规格 |
| **中立** | 双向：与交战任一方均非己非共战 → **不得进入已交战格**；与非共战方亦不可军事叠。 | 📋 | 设计规格 |
| **战场容器 Battlefield** | 敌对军事（或攻城令）同格创建；两侧各为列表；地图可折叠显示交战双方；存对峙日/战记/回放，汇总入战争情报。 | 📋 | 替代一对一 `ActionTarget` 对峙 |
| **战场类型** | `Field` 野战 / `Siege` 攻城（围城即一种 Battlefield）。 | 📋 | 设计规格 |
| **接敌触发** | **取消邻格对峙/邻格开战**；敌对进入同格（须已处于相关 War）→ 入或建 Battlefield。不可穿过敌对交战格。 | 📋 | 待改 `MoveEngagementRules` |
| **主战 / 支援** | 同侧按入格顺序定主战；主战被歼 ≠ 整场必败（士气冲击后可换主战）；当主阵亡另规（§3.5）。 | 📋 | 设计规格 |
| **入场方向** | 记录进入 BF 前所来格；溃逃优先原路；无方向时退向远离敌主战/最近友城。 | 📋 | 设计规格 |
| **Support 编制同调** | 单位独立；`Directive=Support` + 同格目标 → 跟随移动并可同侧入 BF；不负责占格索引。 | 📋 | 设计规格 |
| **路过共战友军** | 不因堆叠加罚；地形/道路 AP 照扣。 | 📋 | 设计规格 |
| **非军 Unit 遇敌** | 运输等入敌对军事格 → 遭遇战，大概率缴获；交战中敌军忙时可只挡不抢。 | 📋 | 设计规格 |
| **信使遇敌** | Character/信使入敌对军事 → 遭遇/俘截；点对点文书。 | 📋 | 设计规格 |

### 3.1 接敌类型 BattleEngagementKind

| 类型 | 中文 | 判定 | 状态 |
|------|------|------|------|
| `FieldBattle` | 野战 | **同格**进入/并入 Field Battlefield（不再默认邻格接敌）。 | ✅→📋 |
| `Ambush` | 伏击 | 任一方 `Status=Ambushing`。 | 📋 |
| `Siege` | 攻城战 | 攻方持攻城令并加入 Siege Battlefield（据点格）。 | ✅→📋 |

**相关文件**：`BattleEngagementClassifier.cs` · `SiegeBattleRules.cs`（待对齐同格 BF）

### 3.2 对峙 Standoff 与强袭决战 Commit

| 概念 | 定义 | 状态 | 相关文件 |
|------|------|------|----------|
| **战场对峙 Standoff** | **Battlefield 内**当日无伤亡无胜负的接触态；对峙逻辑全部在容器内（无邻格对峙）。 | ✅→📋 | `FieldBattleAutoResolver.cs` |
| **强袭决战 Commit** | 当日必须产出胜负；速决/持久思想流保持。 | ✅ | `BattleCommitRules.cs` |
| **小股试探** | 合计兵数 <6000 时前 2 日倾向对峙。 | ✅ | `BattleConstants` |
| **大军阈值** | 合计 ≥6000 默认对峙直至强袭或 30 日强制战。 | ✅ | `BattleConstants` |
| **对峙登记** | 目标：记在 Battlefield 上；现行相邻 registry 待迁移/删除。 | ✅→📋 | `StrategyFieldEngagementRegistry.cs` |
| **对峙信使里程碑** | 第 3/5/10/15/20/30 日发信使。 | ✅ | `BattleReportDispatchRules.cs` |

### 3.3 胜率与战斗因素

| 概念 | 定义 | 状态 | 关键文件 |
|------|------|------|----------|
| **胜率预测** | 开战前展示攻方胜率、伤亡区间、因素明细。 | ✅ | `InstantBattleCalculator` |
| **因素评估** | 统合士气、训练、地形、补给、方针、兵种、阵型等。 | ✅ | `BattleFactorEvaluator.cs` |
| **因素文档** | 全表与公式说明。 | ✅ | [strategy-auto-battle-factors.md](./strategy-auto-battle-factors.md) |

### 3.4 劝降 Surrender

| 概念 | 定义 | 状态 | 关键文件 |
|------|------|------|----------|
| **劝降提议** | 优势方胜率/兵力达阈值时提出。 | ✅ | `BattleSurrenderRules.cs` |
| **劝降成功** | 零伤亡；攻方收编约 30% 残部；**降方撤出地图**；若在敌城格则自动强攻/占城。 | ✅ | `BattleSurrenderRules` · `BattleAftermathHelper.ApplySurrender` |

### 3.5 战后处理 Aftermath 与部队溃灭

| 概念 | 定义 | 状态 | 关键文件 |
|------|------|------|----------|
| **战后士气** | 胜方涨士气，负方跌；低士气禁战。 | ✅ | `BattleMoraleRules.cs` |
| **败方重整** | 设 `Retreat` 方针、补 AP、清攻击令；**不强制后撤一格**。 | ✅ | `BattleRetreatRules.cs` |
| **逃入友城 Flee to Stronghold** | 败后邻格/同格友城且城格未被敌占 → 残部吸入城内守备并移除野战单位。 | ✅ | `BattleFleeToStrongholdRules.cs` |
| **败退残部下限** | 按难度保留战前兵力一定比例，避免追击必灭。 | ✅ | `StrategyDifficultyRules` · `BattleAftermathHelper` |
| **胜方追击** | 决战后可在 **Battlefield 内**再追：成功→击溃逻辑；失败→败方 Routing 离场，追方原地修正 1 日。 | ✅→📋 | `BattlePursuitRules.cs` |
| **溃逃 Routing** | 战败未全灭：强制撤离 BF，优先入场方向；Routing 中再接敌易溃散。 | 📋 | 设计规格 |
| **当主阵亡** | 本场大士气冲击 → 残余推临时总大将或溃走检定 → 政治层继承/灭；战术不自动整场秒崩。 | 📋 | `ForceSuccessionRules`；设计规格 |
| **战后总控** | 统合败退、占城、劝降、士气、溃灭。 | ✅ | `BattleAftermathHelper.cs` |
| **攻击令排队** | 移动后接敌 → 日末 `StrategyBattleResolutionSystem` 结算（非即时）。 | ✅ | `UnitBattleActions` · `StrategyMoveEngagementSystem` |
| **部队溃灭 Unit Destruction** | 败方 `Soldier ≤ 0` 时：从地图移除 Unit、处理将领命运、分配战利品。 | ✅ | `UnitDestructionRules.cs` |
| **战利品 Loot** | 胜方获得败方约 45% 粮草、35% 金钱（万分比可配置）。 | ✅ | `UnitDestructionRules` |
| **将领命运 Commander Fate** | 逃脱（撤至友方据点）/ 被俘（`Prisoner`）/ 阵亡。 | ✅ | `UnitDestructionRules` |
| **守城单位溃灭** | 方针 Support 的单位被击溃时同步据点士气。 | ✅ | `StrongholdGarrisonActions.OnGarrisonUnitDestroyed` |

> **注意**：溃灭仅在 **战斗结算后** 触发（`BattleAftermathHelper`），不在日末全局清扫零兵单位，以免误删非战斗场景单位。

### 3.6 战术模拟层（决战日内）

| 概念 | 定义 | 状态 | 关键文件 |
|------|------|------|----------|
| **阵位 BattleFormationSlot** | 前/中/后/左/右翼抽象战术格。 | ✅ | `BattleFormationSlotRules.cs` |
| **将领行动 BattleCommanderActionKind** | Assault/Hold/Flank/Rally/Withdraw。 | 📋 | `BattleCommanderActionRules.cs` |
| **瞬间战结算** | 确定性伤亡与胜负（ResolutionSeed 混入本局 SimulationSeed）。 | ✅ | `InstantBattleCalculator.cs` |
| **伤亡系数 CasualtyScale** | 因素分解写入并由战术模拟乘入突击伤害（含撤退减伤）。 | ✅ | `BattleFactorBreakdown` · `TacticalBattleSimulator` |
| **战报投递** | 异格须信使抵达后解锁详情；同格目击即时；**仅简易难度**可当日前线解锁。 | ✅ | `BattleReportDeliveryHelper.cs` · `StrategyDifficultyRules` |
| **难度 StrategyDifficulty** | Easy / Normal / Hard / Legendary；规则表由 `StrategyDifficultyRules` 扩展。 | ✅ | `StrategyDifficulty.cs` |
| **本局种子 SimulationSeed** | 开局固定；剧本可指定；战斗/命运/追击脱离等掷点混入以保证回放。 | ✅ | `GameData.SimulationSeed` |

---

## 4. 经济（Economy）

### 4.1 生产与消耗（每日）

| 概念 | 定义 | 状态 | 关键文件 |
|------|------|------|----------|
| **农业/商业日产** | 产出/30 入市民库存。 | ✅ | `StrategyEconomySystem` |
| **市民口粮** | 按人口 × 日耗系数扣 `CivilianActor.Food`。 | ✅ | `EconomyRules` |
| **士兵口粮** | 单位/运输队每日耗粮。 | ✅ | `LogisticsCalculator` |
| **Region 收粮 Harvest** | 按政治区域日历 bulk 收粮 + 农业税。 | ✅ | `HarvestRules.cs` |

### 4.2 税收 Taxes

| 税种 | 频率 | 入库 | 状态 |
|------|------|------|------|
| **人头税 Poll Tax** | 每月 1 日 | 官府 Money | ✅ |
| **商业税 Commerce Tax** | 每月 1 日 | 官府 Money | ✅ |
| **贸易税 Merchant Tax** | 月汇总 | 官府 Money | ✅ |
| **关税 Tariff** | 运输队过境 | 官府 Money | ✅ |
| **农业税 Agriculture Tax** | Region 收粮日 | 官府 Food | ✅ |

征收效率受 `Authority`、`Corruption`、`IsHistorical` 影响。

**关键文件**：`EconomyCalculator.cs` · `TariffEconomyActions.cs`

### 4.3 市场 Market

| 概念 | 定义 | 状态 | 关键文件 |
|------|------|------|----------|
| **连续撮合** | 日推进中、单位移动前独立相位。 | ✅ | `StrategyMarketSystem.cs` |
| **挂单 MarketOrder** | Buy/Sell；主体为 Actor。 | ✅ | `StrongholdMarket.cs` |
| **商品 MarketCommodityType** | `Food` / `Luxury`。 | ✅ | `MarketCommodityType.cs` |
| **日 K 线 DailyPriceBar** | OHLC，保留约 2 年。 | ✅ | `StrongholdMarket.cs` |

### 4.4 运输与经济任务 TransportPurpose

| 枚举值 | 中文 | 用途 | 状态 |
|--------|------|------|------|
| `Supply` | 军事补给 | 向野战单位送粮 | ✅ |
| `Tribute` | 贡赋上缴 | 收粮日粮纳 | ✅ |
| `Trade` | 跨据点贸易 | 商人/官府贸易队 | ✅ |
| `TaxMoney` | 钱税运输 | 每月 1 日钱纳 | ✅ |

**关键文件**：`TransportPurpose.cs` · `StrategySupplySystem.cs`

### 4.5 补给三态 Supply Status（衍生）

| 状态 | 条件概要 | 状态 |
|------|----------|------|
| `Sufficient` | 携粮充足 | ✅ |
| `Strained` | 粮低或有在途/被迷惑 | ✅ |
| `CutOff` | 无粮且无可用补给源 | ✅ |

**关键文件**：`SupplyStatusEvaluator.cs`

### 4.6 运输队状态 SupplyConvoyStatus

`Forming` → `Moving` → `Arrived`；异常：`Destroyed` / `Deceived`（假情报）。

---

## 5. 外交（Diplomacy）

### 5.1 外交关系 DiplomacyRelation

| 值 | 中文 | 说明 | 状态 |
|----|------|------|------|
| `Neutral` | 中立 | 默认 | ✅ |
| `Allied` | 同盟 | 可贸易；**平时同盟不可军事同格**；仅同战共战方可叠 | ✅→📋 |
| `Enemy` | 敌对 | 可接敌、拦截 | ✅ |

**关键文件**：`Diplomacy.cs` · `DiplomacyRules.cs`

### 5.2 外交策略 DiplomacyStrategy

`Maintain` / `Friend` / `Enemy` / `Ally` / `Marriage` / `Control` / `Submit`

### 5.3 远期外交概念 📋

宣战理由 Casus Belli、战争分数 War Score、和谈条款细节 — 见 [strategy-detail-design.md §10](./strategy-detail-design.md)。战争实体框架见下节。

### 5.4 贸易外交

| 概念 | 定义 | 状态 | 关键文件 |
|------|------|------|----------|
| **CanTradeForces** | 非敌对即可贸易。 | ✅ | `DiplomacyTradeRules.cs` |
| **AreAllied** | 判定同盟关系。 | ✅ | `DiplomacyTradeRules.cs` |

### 5.5 战争 War（2026-07-15 冻结规格 · 📋）

| 概念 | 定义 | 状态 | 相关文件 |
|------|------|------|----------|
| **战争实体 War** | 一场法理冲突：宣战方=`WarAggressor`；主战国 + 参战国名单；关联战报与战场。 | 📋 | 待建；设计 §10 |
| **召盟 / JoinWar** | 独立势力应召加入己方；接受则与对侧同盟破裂。对同一交战敌军 **只能入伙，不可平行另开第二场 War 挂同一 BF**。 | 📋 | 设计规格 |
| **战争情报** | EU3 式：宣战、参战、战报、谈判参考；战场记录可汇总入此。 | 📋 | 设计规格 |
| **禁止战时倒戈** | 已是某 War 参战方时，不可势力层突然投向对面或同格友→敌。 | 📋 | 设计规格 |
| **双盟倾向禁止** | 应避免同时与交战双方保持同盟；开战/召盟时撕对敌侧盟。 | 📋 | 设计规格 |
| **参战国单独议和** | 独立参战国可与敌单独和约退出；主战国议和 → **整场战争结束**。 | 📋 | 设计规格 |
| **议和信使** | 条款确认后信使送达日生效；关联 Battlefield **强制停火散场**（在途期间战场可继续打）。 | 📋 | 设计规格 |
| **法理攻方 vs 战术攻守** | `WarAggressor` ≠ 单场 Battlefield 攻守标签（驻军可能是战术守方）。 | 📋 | 设计规格 |

---

## 6. 驻军系统（Garrison）

| 概念 | 定义 | 状态 | 关键文件 |
|------|------|------|----------|
| **城内驻军 City Garrison** | `Stronghold.ForceActor.Soldier`；未编入地图单位的部分。剧本字段 `garrisonSoldiers`。 | ✅ | `StrongholdGarrisonRules.cs` |
| **守城单位** | 普通 `Unit`，方针 `Support` + 同格据守；无单独实体类型。 | ✅ | `StrongholdGarrisonRules.IsGarrisonUnit` |
| **总驻军** | 城内兵数 + 同格守城单位兵数。 | ✅ | `CountTotalGarrisonAt` |
| **EnsureDefenderUnit** | 攻城需接敌且仅有城内兵时，编组一支 Support 守城单位。 | ✅ | `StrongholdGarrisonActions.cs` |
| **威胁占格** | 敌对军事逼近时，日初可编组本势力 Support 守城单位；与堆叠/同格接敌规格对齐时再调邻域。 | ✅→📋 | `GarrisonBehaviorRules.TryPrepareGarrisonOnThreat` |
| **城下入城** | 城下野战击溃守城单位后，胜方进入据点格；空城占城，仍有城内兵则同格接敌。 | ✅ | `BattleAftermathHelper.TryAdvanceWinnerIntoStrongholdAfterGarrisonFight` |
| **封锁 / 包围充分度** | **仅下达攻城令**才算包围；`siegePressure = 同格围城侧（含共战盟友）兵力 / 据点规模必要兵力`。充分（≥1）可满额士气/粮压并 **禁出城野战**；不充分可出城野战，压制减弱；出城/入城支援类信使与运输成功率随 pressure 加深而降。站城格无令 ≠ 包围。 | ✅→📋 | `GarrisonBehaviorRules`（待改） |
| **占城条件 Capture** | 须持攻城指令（强攻/包围）+ 正确位置 + 守备崩溃（城内兵/士气或守城单位归零）。禁止踩格占城。 | ✅ | `StrongholdCaptureRules.CanTransferOwnership` |
| **占城诊断 CaptureDiagnostic** | 占城被拒绝时写入日事件，含 rejectReason 与守备快照。 | ✅ | `StrongholdCaptureHelper.CaptureStronghold` |
| **出城野战 / 解围** | 不充分包围时可编队上图野战。盟友协防 **仅方案 2**：外来与围城军同格开战解围，**不允许入城**成建制协防。城内仍以本势力兵数 + Support 为主。 | ✅→📋 | 设计规格 |
| **驻军崩溃 Garrison Broken** | 城内兵数 ≤0 且无敌方守城单位 → 可占空城。 | ✅ | `IsCityGarrisonBroken` / `IsAnyGarrisonPresent` |

**DTO**：`StrategyStrongholdStateDto.GarrisonSoldiers`（仅城内兵数；守城单位见 units 列表）

---

## 7. 占领与议和（Occupation / Peace）

| 概念 | 定义 | 状态 | 关键文件 |
|------|------|------|----------|
| **占城触发** | **禁止踩格自动占城**；仅攻城指令 + 战后/空城/劝降。 | ✅ | `StrategyStrongholdOccupationSystem` |
| **陷落后果** | 代官清空、居城迁移、在场将领俘虏、战时占领登记。 | ✅ | `StrongholdCaptureConsequenceRules.cs` |
| **战时占领登记** | 记录原主/占领者/日期（战史；和谈疆界以当前据点归属为准）。 | ✅ | `StrategyWarOccupationRegistry.cs` |
| **和谈疆界** | 停战默认 **维持当前据点归属**；割让须显式和谈条款（未实装 UI）。 | 📋 | 设计见 strategy-detail-design §10 |
| **议和生效** | 经信使送达后生效；关联战争之全部 Battlefield 强制停火。参战国单独退出仅拆其部队所在场。 | 📋 | 设计规格 §5.5 |
| **势力抵抗 ForceResistance** | 仍有据点或野战部队 → 势力未灭亡。 | ✅ | `ForceResistanceRules.cs` |
| **当主继承 Succession** | 当主被俘→势力灭亡；阵亡→有抵抗则立继承人（与 BF 内当主阵亡冲击衔接）。 | 📋 | `ForceSuccessionRules.cs` |

---

## 8. AI / 信使 / 派遣

### 8.1 玩家间接指挥

- 玩家控制 **势力当主**，非逐支微操全部 Unit。
- 本势力部队按 **方针 + AI 规则** 自主接敌/撤退/追击。
- 远程方针变更：`AppliedImmediately`（同格）或 `MessengerDispatched`（异格）。

**关键文件**：[strategy-detail-design.md §2.1](./strategy-detail-design.md) · `StrategyPolicyChangeResponseDto`

### 8.2 单位 AI

| 行为 | 说明 | 状态 |
|------|------|------|
| **跳过日 AI** | 非军事、Standoff、Chaos、BeingSurround、Attacking 等 | ✅ |
| **方针微调** | 低士气→Retreat；Occupy/Raid 接敌胜率门槛 | ✅ |
| **行军/撤退/占点** | 寻路、接敌 | ✅ |
| **非玩家扩张** | 每 3 日向最近玩家据点寻路（弱 AI） | ✅ |

**关键文件**：`StrategyUnitAIRules.cs` · `StrategyAISystem.cs`

### 8.3 信使制度

| 概念 | 定义 | 状态 | 相关文件 |
|------|------|------|----------|
| **同格免信使** | 下达方与目标同格 → 即时生效。 | ✅ | `MessengerRules.cs` |
| **点对点文书** | 方针/议和/指名战报等：由 **Character 信使**（含内置 NPC）单点投递；可被截获。 | ✅→📋 | `Messenger.cs`（迁移中） |
| **载荷 MessengerPayloadType** | PolicyChange / StrategicOrder / BattleReport / FalseIntelligence | ✅ | `MessengerPayloadType.cs` |
| **信使状态 MessengerStatus** | Moving / Arrived / Lost / Intercepted | ✅ | `MessengerStatus.cs` |
| **假情报** | 迷惑运输队改道/停留 | 📋 | `StrategyMessengerSystem` |
| **TTL 传闻网** | 败报/陷落/死讯等沿己/盟据点或道路广播，TTL 递减；据点路人/商旅 Unit 可作载体；敌对据点不中继或低概率泄漏。**指令类不用纯广播。** | 📋 | 设计规格 |
| **包围对通信** | 出城信使与入城运输成功率随包围充分度加深而降低。 | 📋 | 设计规格 |

### 8.4 运输队自动派遣

断粮/低粮阈值触发从最近友方据点派队；卸粮后空载返程。

**关键文件**：`SupplyConvoyDispatchHelper.cs`

---

## 9. 时间 / AP / 移动

### 9.1 半回合制时间

| 概念 | 定义 | 状态 | 关键文件 |
|------|------|------|----------|
| **StrategyTimeState** | `Paused` / `Running` | ✅ | `StrategyTimeState.cs` |
| **AdvanceDay** | 日期 +1 → 执行 System 链 | ✅ | `StrategyTimeController.cs` |

### 9.2 日推进 System 顺序（Strategy 层）

```
StrategyMarketSystem(8) → StrategyEconomySystem(10) → StrategySupplySystem(15)
→ StrategyAISystem(18) → StrategyUnitSystem(20) → StrategySiegeSystem(21)
→ StrategyMoveEngagementSystem(22) → StrategyMessengerSystem(25)
→ StrategyBattleResolutionSystem(26)
```

### 9.3 行动力 AP 与移动

| 概念 | 定义 | 状态 | 关键文件 |
|------|------|------|----------|
| **AP** | 当日剩余移动力；日初恢复（默认 +1） | ✅ | `Unit.Ap` · `GameRuleConfig` |
| **Movement** | 移动力上限（军事默认封顶 5） | ✅ | `Unit.Movement` |
| **日移动格数上限** | 单日最多 2 格（道路/AP 富余仍受限），约 **2 日 3 格** | ✅ | `GameRuleConfig.MaxTilesMovedPerDay` · `UnitMoveAction` |
| **地形消耗** | 地形 cost − 道路 bonus | ✅ | `MovementRules.cs` |
| **入城额外 AP** | 进入非己方/敌方据点格 | ✅ | `MovementRules` |
| **攻击 AP / 攻城 AP** | 攻击与攻城指令消耗 | ✅ | `GameRuleConfig` |
| **占格规则** | 军事：本势力∪同战共战方可叠（`List`）；其余挡路绕行。敌对同格→Battlefield。取消「仅同盟 swap / 一格一军」。 | ✅→📋 | `MovementRules` · `PathfindingService` |
| **ZOC 控制区** | 邻格额外 AP、重叠叠加 | 🔮 | 设计 §5 |

---

## 10. 持久化 / 剧本加载

### 10.1 剧本 JSON 结构

```
StrategyScenarioDocument
├── id, name, version
├── map（地形、道路、政治区域、landmarks）
└── scenario
    ├── startDate, playerForceId, lord
    ├── forces[], characters[]
    ├── strongholds[]（含 garrisonSoldiers）
    └── units[]（含 composition[]）
```

**示例**：`SengokuScroll.Strategy/Maps/mini_kanto.json`  
**加载器**：`StrategyScenarioLoader.cs` · `StrategyScenarioDocument.cs`

### 10.2 存档 StrategySaveDocument

| 字段域 | 内容 | 状态 |
|--------|------|------|
| forces | 府库 money/food | ✅ |
| strongholds | 归属、人口、府库、**GarrisonSoldiers** | ✅ |
| units | 坐标、兵数、状态、方针、路径 | ✅ |

**关键文件**：`Persistence/StrategyWorldSaveService.cs`

---

## 11. 其他概念

### 11.1 事件与 UI

| 概念 | 说明 | 状态 |
|------|------|------|
| **StrategyEventDto** | 日推进事件：信使、战报、经济结算、**UnitDestroyed** 溃灭 | ✅ |
| **StrategyAdvanceDayResponseDto** | state + events | ✅ |
| **地图着色** | 势力色；相对玩家外交色（自/盟/敌/非敌） | ✅ |
| **格内兵队列表** | 点击一格后在情报区下列出可操作兵队简表（EU3 式），可单选/取消选择；战场格可折叠显示交战双方。 | 📋 |
| **虚构据点标记** | `isHistorical=false` → UI「·虚构」 | ✅ |

### 11.2 兵种 UnitType（策略 M3）

足轻(1) / 弓(2) / 骑(3) / 铁炮(4) — `StrategyTroopTypes`；M4+ 配置化。

### 11.3 游戏模式 GameMode

Application 层区分策略 / RPG / MMO — `GameOptions.cs`。RPG/MMO 专属概念见各自 `*-detail-design.md`，实装后应追加到本文档 §12。

### 11.4 占位实体

**GameTask** — 任务系统占位，玩法未展开。

---

## 12. 其他模式概念（📋 设计阶段）

以下模式共享 §1 实体，但有专属玩法概念；详细定义见对应设计文档，实装时需回迁到本文档。

| 模式 | 文档 | 专属概念举例 |
|------|------|--------------|
| 立志传 RPG | [rpg-detail-design.md](./rpg-detail-design.md) | 职业、修炼、随从、城内场景、战术地图战斗 |
| MMO | [mmo-detail-design.md](./mmo-detail-design.md) | 国战排期、Zone 拆分、多人日常 |

---

## 附录 A：关键枚举速查

| 枚举 | 位置 | 主要值 |
|------|------|--------|
| `UnitStatus` | `Unit.cs` | Waiting, Moving, Inspiring, Fearful, Chaos, Ambushing, BeingSurround, Standoff；（目标）Routing |
| `UnitDirective` | `Unit.cs` | Move, Occupy, Raid, Support, Retreat |
| `UnitStance` | `Unit.cs` | Normal, Attacking, Surrounding, Maneuver, Alert, Hold |
| `UnitSiegeMode` | `Unit.cs` | None, Encircle, Assault |
| `CharacterForceStatus` | `Character.cs` | Idle, Task, UnitAction, Prisoner |
| `ForceStatus` | `Force.cs` | Independence, OuterVassal, InnerVassal |
| `DiplomacyRelation` | `Diplomacy.cs` | Neutral, Allied, Enemy |
| `DiplomacyStrategy` | `Diplomacy.cs` | Maintain, Friend, Enemy, Ally, Marriage, Control, Submit |
| `TransportPurpose` | `TransportPurpose.cs` | Supply, Tribute, Trade, TaxMoney |
| `MessengerPayloadType` | `MessengerPayloadType.cs` | PolicyChange, StrategicOrder, BattleReport, FalseIntelligence |
| `BattleEngagementKind` | `BattleEngagementClassifier.cs` | FieldBattle, Ambush, Siege |
| `BattleCombatDirective` | `BattleDirectiveRules.cs` | HoldLine, FightToDeath, CounterAttack, AttemptRetreat |
| `StrategyTimeState` | `StrategyTimeState.cs` | Paused, Running |
| `SupplyConvoyStatus` | `SupplyConvoyStatus.cs` | Forming, Moving, Arrived, Destroyed, Deceived |
| `MarketCommodityType` | `MarketCommodityType.cs` | Food, Luxury |

---

## 附录 B：Strategy Rules / Systems 索引

| 领域 | Rules | Systems |
|------|-------|---------|
| 移动接敌 | `MoveEngagementRules`, `MovementRules` | `StrategyUnitSystem`, `StrategyMoveEngagementSystem` |
| 战斗 | `BattleCommitRules`, `BattleSurrenderRules`, `BattleRetreatRules`, `BattlePursuitRules`, `UnitDestructionRules` | `StrategyBattleResolutionSystem` |
| 攻城 | `SiegeOrderRules`, `SiegeBattleRules` | `StrategySiegeSystem` |
| 驻军 | `StrongholdGarrisonRules` · `GarrisonBehaviorRules` | — |
| 占城 | `StrongholdCaptureConsequenceRules` | `BattleAftermathHelper` |
| 经济 | `EconomyRules`, `HarvestRules`, `MarketRules` | `StrategyEconomySystem`, `StrategyMarketSystem` |
| 后勤 | `TransportRules` | `StrategySupplySystem` |
| 信使 | `MessengerRules`, `BattleReportDispatchRules` | `StrategyMessengerSystem` |
| AI | `StrategyUnitAIRules` | `StrategyAISystem` |
| 外交 | `DiplomacyTradeRules` | 📋 远期 DiplomacySystem |
| 时间 | — | `StrategyTimeSystem`, `StrategyTimeController` |

---

## 附录 C：文档交叉引用

| 文档 | 内容 |
|------|------|
| [strategy-detail-design.md](./strategy-detail-design.md) | 大战略玩法权威设计 |
| [strategy-auto-battle-factors.md](./strategy-auto-battle-factors.md) | 自动战斗因素全表 |
| [strategy-development-plan.md](./strategy-development-plan.md) | 里程碑实装进度 |
| [shared-detail-design.md](./shared-detail-design.md) | 跨模式共享实体 |
| [design-document.md](./design-document.md) | 项目总览 |

---

*维护者：任何 PR 涉及游戏语义变更时，请更新本文档 §0.4 变更记录及对应章节。*
