# SengokuScroll（战国绘卷）共同详细设计

> 版本：1.1 | 日期：2026-07-22 | 索引：[设计文档索引](./README.md) | 上一级：[基本设计](./design-document.md)

---

## 目录

1. [共享领域模型](#1-共享领域模型)
2. [共享规则体系](#2-共享规则体系)
3. [GameDate 时间系统](#3-gamedate-时间系统)
4. [事件系统与数据流](#4-事件系统与数据流)
5. [指令系统详细设计](#5-指令系统详细设计)
6. [时间推进逻辑](#6-时间推进逻辑)
7. [战术地图战斗系统](#7-战术地图战斗系统)
8. [网络与多人架构](#8-网络与多人架构)
9. [存档系统](#9-存档系统)

---

## 1. 共享领域模型

### 1.0 数据定义概览

> 本节内容基于 `data.xlsx` 和 `model.xlsx` 整理。

#### 1.0.1 游戏设置

| 参数 | 值 | 说明 |
|------|-----|------|
| `turn_per_day` | 30 | 每天回合数 |
| `longevity` | 0 | 长寿模式开关 |

#### 1.0.2 类型定义

| 类型 | 值 | 说明 |
|------|-----|------|
| `气候` | 温暖/寒冷/热带 | 影响农业产出、人口增长 |
| `文化` | 日本/朝鲜/琉球/虾夷 | 影响外交、文化事件 |
| `宗教` | 神道/佛教/基督教 | 影响民心、外交关系 |
| `地形` | 平原/山地/森林/水域/道路 | 影响移动成本、战斗加成 |
| `建筑` | 城郭/支城/港口/集市/寺院 | 影据点功能 |
| `货币` | 金/银/铜 | 经济系统基础 |
| `原料` | 铁/木材/石材/粮食 | 生产资源 |
| `兵装` | 刀/枪/弓/铁炮/马 | 军备资源 |

#### 1.0.3 气候类型

| 类型 | 影响农业 | 影响人口 | 特殊事件 |
|------|----------|----------|----------|
| 温暖 | +10% | +5% | 台风、洪水 |
| 寒冷 | -10% | -5% | 雪灾、冻害 |
| 热带 | +20% | +10% | 瘟疫、旱灾 |

#### 1.0.4 文化类型

| 类型 | 外交倾向 | 特殊能力 | 文化事件 |
|------|----------|----------|----------|
| 日本 | 中立 | 武士道 | 武士道事件 |
| 朝鲜 | 保守 | 儒学 | 儒学事件 |
| 琉球 | 开放 | 海贸 | 海贸事件 |
| 虾夷 | 独立 | 狩猎 | 狩猎事件 |

#### 1.0.5 宗教类型

| 类型 | 民心影响 | 外交影响 | 特殊事件 |
|------|----------|----------|----------|
| 神道 | +5% | 无 | 祭祀事件 |
| 佛教 | +10% | 同宗教友好 | 寺院建设 |
| 基督教 | -5% | 西方势力友好 | 传教事件 |

#### 1.0.6 地形类型

| 地形 | 移动成本 | 攻击加成 | 防御加成 | 特殊效果 |
|------|----------|----------|----------|----------|
| 平原 | 1 | 0 | 0 | 无 |
| 山地 | 3 | -10% | +20% | 视野受限 |
| 森林 | 2 | -5% | +10% | 可埋伏 |
| 水域 | 4 | -20% | +0% | 需船只 |
| 道路 | 0.5 | 0 | -5% | 移动加速 |

#### 1.0.7 建筑类型

| 建筑 | 建设成本 | 维护成本 | 功能 |
|------|----------|----------|------|
| 城郭 | 1000金 | 10金/月 | 防御+20% |
| 支城 | 500金 | 5金/月 | 防御+10% |
| 港口 | 300金 | 3金/月 | 海贸+20% |
| 集市 | 200金 | 2金/月 | 商业+15% |
| 寺院 | 100金 | 1金/月 | 民心+5% |

#### 1.0.8 货币类型

| 货币 | 价值比例 | 用途 |
|------|----------|------|
| 金 | 1 | 高价值交易、外交 |
| 银 | 10银=1金 | 中等交易、俸禄 |
| 铜 | 100铜=1银 | 日常交易、税收 |

#### 1.0.9 原料类型

| 原料 | 生产方式 | 用途 |
|------|----------|------|
| 铁 | 矿山开采 | 兵装生产 |
| 木材 | 森林采伐 | 建筑、船只 |
| 石材 | 山地开采 | 建筑 |
| 粮食 | 农田生产 | 人口消耗、军粮 |

#### 1.0.10 兵装类型

| 兵装 | 生产成本 | 效果 |
|------|----------|------|
| 刀 | 10铁 | 步兵攻击+5% |
| 枪 | 15铁 | 步兵攻击+8% |
| 弓 | 5铁+5木材 | 弓兵攻击+10% |
| 铁炮 | 20铁+5木材 | 铁炮兵攻击+15% |
| 马 | 50金 | 骑兵移动+20% |

#### 1.0.11 游戏规则配置

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `EnterStrongholdAp` | 5 | 进入据点消耗AP |
| `AttackAp` | 5 | 攻击消耗AP |
| `NextTurnApRecovery` | 3 | 每时段AP恢复量 |

#### 1.0.12 指令类型（基于 `data.xlsx` 指令表）

| 分类 | 指令 | 执行权限 | 说明 |
|------|------|----------|------|
| 个人 | 移动 | 全员 | 移动到其他据点 |
| 个人 | 宴会 | 全员 | 消耗金钱恢复体力 |
| 个人 | 修炼 | 全员 | 提升能力熟练度 |
| 个人 | 交际 | 全员 | 提升好感度 |
| 个人 | 赠礼 | 全员 | 提升好感度 |
| 个人 | 拜访 | 全员 | 拜访其他角色 |
| 个人 | 隐居 | 全员 | 退休让位 |
| 个人 | 出奔 | 全员 | 离开势力 |
| 会议 | 方针 | 君主 | 修改势力方针 |
| 会议 | 战略 | 君主 | 修改势力战略 |
| 会议 | 会议 | 君主 | 发起会议 |
| 会议 | 宣战 | 君主 | 发起战争 |
| 会议 | 停战 | 君主 | 结束战争 |
| 会议 | 同盟 | 君主 | 结成同盟 |
| 会议 | 解盟 | 君主 | 解除同盟 |
| 会议 | 从属 | 君主 | 成为从属 |
| 会议 | 支配 | 君主 | 使其从属 |
| 会议 | 赠礼 | 君主 | 向其他势力赠礼 |
| 会议 | 贡品 | 君主 | 向其他势力进贡 |
| 会议 | 联姻 | 君主 | 政治婚姻 |
| 会议 | 侮辱 | 君主 | 外交侮辱 |
| 人事 | 晋升 | 君主·领主 | 官职上升 |
| 人事 | 降职 | 君主·领主 | 官职下降 |
| 人事 | 任免 | 君主·领主 | 任免职位 |
| 人事 | 登用 | 君主·领主 | 登用浪人 |
| 人事 | 流放 | 君主·领主 | 流放家臣 |
| 人事 | 处刑 | 君主·领主 | 处刑俘虏 |
| 人事 | 赏赐 | 君主·领主 | 赏赐家臣 |
| 人事 | 惩罚 | 君主·领主 | 惩罚家臣 |
| 外交1 | 宣战 | 领主·代官和执行命令的人物 | 变更外交关系 |
| 外交1 | 停战 | 领主·代官和执行命令的人物 | 结束战争 |
| 外交1 | 同盟 | 领主·代官和执行命令的人物 | 结成同盟 |
| 外交1 | 解盟 | 领主·代官和执行命令的人物 | 解除同盟 |
| 外交2 | 赠礼 | 领主·代官和执行命令的人物 | 向其他势力赠礼 |
| 外交2 | 贡品 | 领主·代官和执行命令的人物 | 向其他势力进贡 |
| 外交2 | 联姻 | 领主·代官和执行命令的人物 | 政治婚姻 |
| 外交2 | 侮辱 | 领主·代官和执行命令的人物 | 外交侮辱 |
| 军事 | 出征 | 领主·代官 | 派出兵队出击 |
| 军事 | 撤退 | 领主·代官 | 撤回兵队 |
| 军事 | 补给 | 领主·代官 | 补给兵队 |
| 军事 | 训练 | 领主·代官 | 训练兵队 |

#### 1.0.13 任务类型（基于 `data.xlsx` 任务表）

| 分类 | 任务 | 说明 |
|------|------|------|
| 内政 | 开垦 | 建设据点农田 |
| 内政 | 商业 | 建设据点商业 |
| 内政 | 建筑 | 建设据点建筑 |
| 内政 | 修补 | 修补据点建筑 |
| 内政 | 治安 | 提升据点治安 |
| 内政 | 民心 | 提升据点民心 |
| 外政 | 外交 | 执行外交任务 |
| 外政 | 计略 | 执行计略任务 |
| 外政 | 谍报 | 执行谍报任务 |
| 军备 | 训练 | 训练兵队 |
| 军备 | 补给 | 补给兵队 |
| 军备 | 征兵 | 征召士兵 |
| 计略 | 放火 | 降低据点物资士气并造成混乱 |
| 计略 | 破坏 | 降低据点防御 |
| 计略 | 流言 | 降低据点民心 |
| 计略 | 暗杀 | 暗杀目标人物 |
| 计略 | 煽动 | 煽动叛乱 |
| 会战 | 出征 | 派出兵队出击 |
| 会战 | 撤退 | 撤回兵队 |
| 会战 | 攻城 | 攻击敌方据点 |
| 会战 | 防守 | 防守己方据点 |

---

### 1.1 实体继承体系

```
Entity（抽象基类）
├── Actor（抽象行动体）
│   ├── Character（角色）
│   ├── Unit（单位）
│   └── Stronghold（据点）
├── Force（势力）
├── Diplomacy（外交关系）
└── GameWorld（游戏世界）
```

---

### 1.2 核心实体详细定义

#### 1.2.1 Character（角色）

> 基于 `SengokuScroll.Domain.Entities.Character.cs` 和 `CharacterDefinition.cs`

**定义数据（CharacterDefinition）** - 静态属性：

| 字段 | 类型 | 说明 |
|------|------|------|
| `Id` | int | 角色唯一标识 |
| `Name` | string | 角色名 |
| `Sex` | bool | 性别（true=男） |
| `Age` | byte | 年龄 |
| `PortraitId` | int | 肖像ID |
| `BirthDate` | GameDate | 出生日期 |
| `DeathDate` | GameDate? | 死亡日期（null=存活） |
| `CultureId` | int | 文化ID |
| `ReligionId` | int | 宗教ID |
| `HomeId` | int | 家乡据点ID |
| `HometownId` | int? | 出生地据点ID（需补充） |
| `FamilyId` | int | 家族ID |
| `FatherId` | int? | 父亲ID |
| `MotherId` | int? | 母亲ID |
| `SpouseId` | int? | 配偶ID |
| `ChildrenIds` | int[] | 子女ID列表 |
| `PersonalityData` | PersonalityData | 性格数据 |
| `ProficiencyData` | ProficiencyData | 能力熟练度 |

**性格数据（PersonalityData）**：

| 字段 | 类型 | 范围 | 说明 |
|------|------|------|------|
| `Temper` | byte | 0-100 | 性情：0=急躁，100=温和 |
| `Courage` | byte | 0-100 | 勇气：0=胆小，100=钢胆 |
| `Principle` | byte | 0-100 | 主义：0=现实，100=理想 |
| `Action` | byte | 0-100 | 慎重：0=轻率，100=慎重；保留 Action 名称兼容存档 |
| `Friendship` | byte | 0-100 | 情义：0=薄情，100=重义；不是 Character.Loyalty |
| `Ambition` | byte | 0-100 | 野心：0=知足，100=野心勃勃 |
| `Hobby` | byte | 0-100 | 喜好标量；五分类尚非独立玩法属性 |
| `Desire` | byte | 0-100 | 物欲：0=清廉，100=贪婪 |
| `Drinking` | byte | 0-100 | 饮酒：0=禁酒，100=嗜酒 |
| `Fortune` | byte | 0-100 | 运势：0=厄运，100=幸运 |

**能力熟练度（ProficiencyData）** - Level+Exp结构：

| 字段 | 类型 | 说明 |
|------|------|------|
| `Infantry` | ProficiencyStats | 步兵熟练度 |
| `Ride` | ProficiencyStats | 骑马熟练度 |
| `Archery` | ProficiencyStats | 弓术熟练度 |
| `Firearm` | ProficiencyStats | 火枪熟练度 |
| `Navigation` | ProficiencyStats | 航海熟练度 |
| `Strategy` | ProficiencyStats | 军略熟练度 |
| `Combat` | ProficiencyStats | 战斗熟练度 |
| `Espionage` | ProficiencyStats | 谍报熟练度 |
| `Agriculture` | ProficiencyStats | 农业熟练度 |
| `Commerce` | ProficiencyStats | 商业熟练度 |
| `Construction` | ProficiencyStats | 建筑熟练度 |
| `Smithing` | ProficiencyStats | 冶炼熟练度 |
| `Speech` | ProficiencyStats | 辩才熟练度 |
| `Court` | ProficiencyStats | 宫廷熟练度 |
| `Social` | ProficiencyStats | 交际熟练度 |
| `Medicine` | ProficiencyStats | 医术熟练度 |

**运行时数据（Character）** - 动态属性：

| 字段 | 类型 | 说明 |
|------|------|------|
| `Location` | Point3 | 当前位置（地图坐标） |
| `Ap` | int | 行动力 |
| `Hp` | int | 体力 |
| `Money` | int | 个人金钱 |
| `Emotion` | int | 心情（-100～100） |
| `ForceId` | int | 所属势力ID |
| `StrongholdId` | int | 所在据点ID |
| `PositionId` | int | 职位ID |
| `Salary` | int | 俸禄 |
| `TaskId` | int? | 当前任务ID（旧字段，逐步由 IntelTasks 取代） |
| `TaskProgress` | int | 任务进度 |
| `ServiceDate` | GameDate | 仕官开始日期（情报 Tab 派生仕官年数） |
| `Loyalty` | byte | 基础忠诚 0–100；DTO 下发有效值含 ActiveEffects 叠加 |
| `Relationships` | List&lt;CharacterRelationship&gt; | 角色间数值关系 + ViewEffects（本人/对本人看法） |
| `ActiveEffects` | List&lt;EntityEffect&gt; | 灾害/政策等增减益（影响 Tab） |
| `IntelTasks` | List&lt;CharacterIntelTask&gt; | 持久化任务（任务 Tab；bootstrap 写入） |
| `Introduction` | string? | 介绍 Tab 文案 |
| `ActionPlan` | CharacterActionPlan | 行动计划 |
| `ActionStatus` | CharacterActionStatus | 行动状态 |
| `ForceStatus` | CharacterForceStatus | 势力状态 |
| `LocationType` | CharacterLocationType | 位置类型 |
| `ActionTarget` | CharacterActionTarget | 行动目标 |

**行动目标（CharacterActionTarget）**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `ForceId` | int | 目标势力ID |
| `StrongholdId` | int | 目标据点ID |
| `UnitId` | int | 目标单位ID |
| `CharacterId` | int | 目标角色ID |
| `RoutePoints` | Queue<Point2> | 移动路径队列 |

**行动计划枚举（CharacterActionPlan）**：

| 值 | 说明 |
|------|------|
| `Rest` | 休息：角色计划休息、会去寻找休息的地方例如回家或附近的旅店 |
| `Meet` | 参加会议：角色计划参加会议、会去据点内的主家议事厅 |
| `Task` | 执行任务：角色计划执行任务、会优先去指定地点执行任务 |
| `Report` | 报告任务结果：角色计划汇报任务结果、会优先去据点内的主家 |

**行动状态枚举（CharacterActionStatus）**：

| 值 | 说明 |
|------|------|
| `Waiting` | 在当前地方待命 |
| `Resting` | 休息中：角色体力会快速上升、视所在位置可能需要同时消耗金钱 |
| `Moving` | 移动中：该状态表示正在按照指定的目标(据点或据点内设施)移动变化坐标 |
| `Acting` | 行动中：正在做事情 |

**势力状态枚举（CharacterForceStatus）**：

| 值 | 说明 |
|------|------|
| `Idle` | 空闲：表示角色没有任何任务、可以自由活动 |
| `Task` | 任务中：表示角色有任务需要执行、需要优先执行任务 |
| `UnitAction` | 单位行动中：表示角色正在参与单位行动、无法执行个人指令 |

---

#### 1.2.1a EntityEffect（增减益 / 看法条目）

> 基于 `SengokuScroll.Domain.Entities.EntityEffect.cs`、`EffectTargetStat.cs`、`EffectDurationKind.cs`

共用结构，挂载位置决定 UI 与 formatter：

| 挂载位置 | 列表字段 | 情报 UI | Strategy formatter |
|----------|----------|---------|-------------------|
| Force / Stronghold / Character | `ActiveEffects` | 影响 Tab | `FormatTargetStat` |
| `Diplomacy` | `ViewEffects` | 势力 · 本家看法 / 对本家的看法 | `FormatDiplomacyViewTargetStat` |
| `CharacterRelationship` | `ViewEffects` | 人物 · 本人看法 / 对本人的看法 | `FormatCharacterViewTargetStat` |

**字段：**

| 字段 | 类型 | 说明 |
|------|------|------|
| `Id` | int | 条目 Id |
| `Name` | string | 名称（如「桶狭间之战」） |
| `TargetStat` | EffectTargetStat | 作用属性（见下表） |
| `Magnitude` | int | 幅度；符号表示增减 |
| `Duration` | EffectDurationKind | Permanent / LongTerm / Temporary |
| `Description` | string? | 说明 |
| `ExpiresOn` | GameDate? | Temporary 到期日 |

**EffectTargetStat（节选）：**

| 值 | ActiveEffects | 外交看法 | 角色看法 |
|----|---------------|----------|----------|
| Relationship | — | 外交关系 | **亲疏** |
| Trust | — | 信赖 | 信赖 |
| Diplomacy | — | 外交关系 | （归并为亲疏，禁止新写入） |
| PersonalOpinion | — | — | 个人观感 |
| Loyalty | 忠诚 | — | — |
| Agriculture / Commerce / Morale | 农业/商业/士气 | — | — |

**CharacterRelationship**（`CharacterRelationship.cs`）：OwnerCharacterId → TargetCharacterId 视角；`Relationship`/`Trust` 为基线数值；`ViewEffects` 仅影响私人关系展示与后续玩法，**不**修改势力 `Diplomacy.Relation` 枚举。

**CharacterIntelTask**（`CharacterIntelTask.cs`）：`TaskCategory` = Personal | Life | Force | PartTime；供任务 Tab 与存档。

---

#### 1.2.2 Unit（单位）

> 基于 `SengokuScroll.Domain.Entities.Unit.cs`

**基础数据（Actor）**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `Id` | int | 单位唯一标识 |
| `Name` | string | 单位名 |
| `LeaderId` | int | 总将 Id |
| `ForceId` | int | 所属势力 Id |
| `SubUnitIds` | int[] | 子编制（兵种/备队）Id 列表 |

**子编制（SubUnit）**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `Id` | int | 子编制唯一标识 |
| `TypeId` | byte | 兵种类型 Id |
| `TypeName` | string | 兵种显示名 |
| `UnitId` | int | 所属 Unit Id |
| `Soldier` | int | 该段兵数 |
| `LeaderId` | int | 队将 Id（0 = 归总将统辖） |
| `ForceId` | int | 所属势力 Id |

> **指挥原则**：战略地图实体为 Unit；总将（`Unit.LeaderId`）在出征编组时确定；SubUnit 为团内兵种构成，不独立占格。瞬间战读取既有编制，非太阁式临战任命。

**资源数据**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `Soldier` | int | 士兵数 |
| `Morale` | byte | 士气（0-100） |
| `Food` | int | 军粮 |
| `Money` | int | 军资 |

**单位独有数据**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `Location` | Point3 | 当前位置 |
| `Ap` | int | 行动力 |
| `FormationId` | int | 阵型ID |
| `Attack` | int | 攻击力 |
| `Defense` | int | 防御力 |
| `AttackRange` | int | 攻击范围 |
| `Movement` | int | 移动力 |
| `Tiredness` | int | 疲劳度 |
| `IsSealing` | bool | 航行中 |
| `Directive` | UnitDirective | 方针 |
| `DirectiveTargetId` | int | 方针目标ID |
| `Stance` | UnitStance | 姿态 |
| `Status` | UnitStatus | 状态 |
| `IsMilitary` | bool | 是否军队 |
| `Direction` | Direction4 | 方向 |
| `ActionTarget` | UnitActionTarget | 行动目标 |

**行动目标（UnitActionTarget）**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `ForceId` | int | 目标势力ID |
| `StrongholdId` | int | 目标据点ID |
| `UnitId` | int | 目标单位ID |
| `CharacterId` | int | 目标角色ID |
| `RoutePoints` | Queue<Point2> | 移动路径队列 |

**方针枚举（UnitDirective）**：

| 值 | 说明 |
|------|------|
| `Move` | 移动 |
| `Occupy` | 占领 |
| `Raid` | 劫掠 |
| `Support` | 支援 |
| `Retreat` | 撤退 |

**姿态枚举（UnitStance）**：

| 值 | 说明 |
|------|------|
| `Normal` | 普通状态 |
| `Attacking` | 攻击中：下一回合会自动继续攻击目标 |
| `Surrounding` | 包围中：进入包围状态后被攻击方无法移动、且士气下降度提高直到包围状态解除 |
| `Maneuver` | 机动：移动上升，发现伏兵概率下降 |
| `Alert` | 警惕：移动下降，发现伏兵概率上升 |
| `Hold` | 坚守：无法移动，防御上升，疲劳度增加度降低 |

**状态枚举（UnitStatus）**：

| 值 | 说明 |
|------|------|
| `Waiting` | 原地待机：如果是普通状态、防御下降疲劳下降；如果是警惕状态、防御略微下降、疲劳略微下降 |
| `Moving` | 移动中 |
| `Inspiring` | 斗志高昂：士气上升、攻击上升 |
| `Fearful` | 恐惧：士气下降、攻击下降、防御下降、姿态可能变为机动 |
| `Chaos` | 混乱：士兵数缓慢降低、攻防极大降低 |
| `Ambushing` | 埋伏中 |
| `BeingSurround` | 被包围中 |

---

#### 1.2.3 Force（势力）

| 字段 | 类型 | 说明 |
|------|------|------|
| `Id` | int | 势力唯一标识 |
| `Name` | string | 势力名 |
| `LeaderId` | int | 势力领袖ID |
| `Color` | Color | 势力颜色 |
| `Treasury` | int | 金库 |
| `Strategy` | ForceStrategy | 战略方针 |
| `Tactics` | ForceTactics | 战术方针 |
| `DiplomacyIds` | int[] | 外交关系ID列表 |
| `StrongholdIds` | int[] | 据点ID列表 |
| `CharacterIds` | int[] | 角色ID列表 |
| `UnitIds` | int[] | 单位ID列表 |
| `Status` | ForceStatus | 势力身份（独立/外藩/内藩） |
| `SuzerainForceId` | int? | 宗主势力 Id；内藩/外藩时指向本家 |
| `Provinces` | Province[] | **地理单元**（国/道）；编排占领据点、核心判定；**不**用于册封国主 |

**势力身份枚举（ForceStatus）**：

| 值 | 说明 |
|------|------|
| `Independence` | 独立国家 |
| `OuterVassal` | 外藩：名义臣服、有独立外交/军事权、宗主无直接控制 |
| `InnerVassal` | 内藩：任命国主时新建；无独立外交/军事权；宗主可控制并可**撤销** |

---

#### 1.2.4 Stronghold（据点）

> 基于 `SengokuScroll.Domain.Entities.Stronghold.cs`

**基础数据**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `Id` | int | 据点唯一标识 |
| `Name` | string | 据点名 |
| `Location` | Point2 | 地图坐标 |
| `ForceId` | int | 所属势力ID |
| `LeaderId` | int | 代官ID（执行政务；与领主可分离） |
| `LordId` | int | 据点领主角色 Id；**0=当主直辖**（显示势力当主名） |
| `CultureId` | int | 文化ID |
| `ReligionId` | int | 宗教ID |
| `ClimateId` | int | 气候ID |
| `TerrainId` | int | 地形ID |

**政治属性**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `Authority` | byte | 统治力（0-100）：表示中央通过官僚系统对据点的控制程度、影响税收效率、政令执行的力量水平 |
| `Autonomy` | byte | 自治度（0-100）：地方势力可以自由制定政策的空间、如果统治力下降则会导致地方势力扩张权力、自治度自动扩大 |
| `AdminCost` | int | 行政损耗：维持统治力所需的成本 |

**经济属性**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `AgricultureTaxRate` | byte | 农业税率（0-100） |
| `CommerceTaxRate` | byte | 商业税率（0-100） |
| `TariffRate` | byte | 关税税率（0-100） |
| `SpecialTaxRate` | byte | 特产税率（0-100） |

**人口属性**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `Population` | int | 人口数 |
| `PopularFeelings` | byte | 民心（0-100） |
| `Security` | byte | 治安（0-100） |

**军事属性**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `Defense` | int | 防御力 |
| `Soldier` | int | 城兵数 |
| `Morale` | byte | 士气（0-100） |

**物资属性**（`Actor` 基类，据点/单位/商户等共用）：

| 字段 | 类型 | 说明 |
|------|------|------|
| `Food` | int | 粮食（合） |
| `Money` | int | 金钱（贯） |
| `Horse` | int | 所持马匹（匹）；Unit 载重/库存，**非**骑兵上限 |
| `Wood` / `Iron` / `Copper` / `Matchlock` / `Cannon` / `Boat` / `Ship` / `Fleet` | int | 建设/军备物资（M5+ 循环） |

骑兵编制见 **`SubUnit.TypeId`**（如 Cavalry）；已废弃用 `Horse` 限制骑兵数的 bootstrap 逻辑。

**StrongholdActor结构**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `CharacterIds` | int[] | 在据点内的角色列表 |
| `AgricultureProduction` | int | 农业产出 |
| `CommerceProduction` | int | 商业产出 |

---

### 1.3 关系模型

```
Force（势力）
├── LeaderId → Character（领袖）
├── StrongholdIds → Stronghold[]（据点）
├── CharacterIds → Character[]（家臣）
├── UnitIds → Unit[]（单位）
└── DiplomacyIds → Diplomacy[]（外交关系）

Stronghold（据点）
├── ForceId → Force（所属势力）
├── LeaderId → Character（领主）
├── MayorId → Character（代官）
└── StrongholdActor.CharacterIds → Character[]（在据点内的角色）

Unit（单位）
├── LeaderId → Character（总将）
├── SubUnitIds → SubUnit[]（兵种/备队构成）
├── ForceId → Force（所属势力）
└── ActionTarget.UnitId → Unit（目标单位）

Character（角色）
├── ForceId → Force（所属势力）
├── StrongholdId → Stronghold（所在据点）
├── PositionId → Position（职位）
├── Relationships → CharacterRelationship[]（私人关系 + ViewEffects）
├── ActiveEffects → EntityEffect[]（影响 Tab）
├── IntelTasks → CharacterIntelTask[]（任务 Tab）
├── TaskId → Task（当前任务，旧）
├── FamilyId → Family（家族）
├── FatherId → Character（父亲）
├── MotherId → Character（母亲）
├── SpouseId → Character（配偶）
└── ChildrenIds → Character[]（子女）

Force（势力）
└── Diplomacies → Diplomacy[]（含 ViewEffects → 势力看法 Tab）
```

---

## 2. 共享规则体系

### 2.1 规则层架构

```
Rules Layer
├── CommonRules         通用规则（边界检查、存在性检查）
├── MovementRules       移动规则（行动力检查、地形检查、敌军检查）
├── DiplomacyRules      外交规则（敌友判定、同盟判定）
├── UnitRules           单位规则（攻击检查、方针检查）
└── [模式专属规则]
    ├── RpgRules        RPG规则（个人行动、随从管理）
    ├── StrategyRules   策略规则（ZOC、补给线、包围）
    └── MmoRules        MMO规则（国战参战、据点攻防）
```

### 2.2 Evaluator体系

| 模式 | 评估器 | 说明 |
|------|--------|------|
| RPG | `RpgCharacterMoveEvaluator` | 角色移动+随从跟随+个人事件触发 |
| RPG | `RpgActionEvaluator` | 个人行动评估（修炼/任务/交际） |
| 策略 | `StrategyUnitMoveEvaluator` | 单位移动+补给线+ZOC |
| 策略 | `StrategyAttackEvaluator` | 攻击评估+地形加成+包围加成 |
| 策略 | `StrategyDiplomacyEvaluator` | 宣战理由/战争分数/和谈条件 |
| MMO | `MmoCharacterMoveEvaluator` | 角色移动+可见性+其他玩家交互 |
| MMO | `NationalWarEvaluator` | 国战参战资格+据点攻防规则 |

---

## 3. GameDate 时间系统

当前设计：1天 = 4时段（DayPhase），支持可配置的时间推演。

```
GameDate
├── DayPhasePerDay = 4    (时段/天)
├── DaysPerMonth = 30     (天/月)
├── MonthsPerYear = 12    (月/年)
├── DayPhase 枚举
│   ├── Morning     (清晨)
│   ├── Forenoon    (上午)
│   ├── Afternoon   (下午)
│   └── Evening     (傍晚)
└── 推演方法
    ├── AddDayPhase(int)  推进时段
    ├── AddDays(int)      推进天
    ├── AddMonths(int)    推进月
    └── AddYears(int)     推进年
```

各模式时间配置详见[基本设计 §2.4](./design-document.md#24-各模式时间配置)。

---

## 4. 事件系统与数据流

### 4.1 事件体系

```
GameEvent 体系
├── 领域事件（Domain Events）— 所有模式共享
│   ├── CharacterMovedEvent     角色移动
│   ├── UnitMovedEvent          单位移动
│   ├── BattleStartedEvent      战斗开始
│   ├── BattleEndedEvent        战斗结束
│   ├── StrongholdCapturedEvent 据点被占领
│   ├── ForceDestroyedEvent     势力灭亡
│   ├── DiplomacyChangedEvent   外交关系变化
│   ├── CharacterDiedEvent      角色死亡
│   └── DateAdvancedEvent       日期推进
├── RPG事件 → 详见[立志传模式详细设计](./rpg-detail-design.md)
├── 策略事件 → 详见[大战略模式详细设计](./strategy-detail-design.md)
└── MMO事件 → 详见[MMO模式详细设计](./mmo-detail-design.md)
```

### 4.2 数据流

```
玩家输入 → Command → CommandHandler → Evaluator → Service → Entity状态变更
                                                              ↓
时间推进 → System.Update() → Action.Update() → EventDispatcher.Publish → EventHandler → UI更新
```

---

## 5. 指令系统详细设计

> 本节描述指令从输入到执行的完整流程，各步骤涉及的数据字段参照 §1.2 核心实体详细定义。

### 5.1 指令处理流程概述

```
指令处理流程（Command Flow）
├── 1. 输入层
│   ├── 玩家通过UI/API发送指令
│   ├── 指令封装为指令对象（包含目标ID、目标位置等参数）
│   └── 游戏实例接收指令并转发至处理层
├── 2. 处理层
│   ├── 指令处理器接收指令
│   │   ├── 验证指令合法性（目标实体是否存在）
│   │   ├── 获取目标实体
│   │   ├── 调用评估器评估可行性
│   │   ├── 调用服务计算数据（如路径）
│   │   └── 修改实体状态（设置行动目标等）
│   └── 返回执行结果（成功/失败）
├── 3. 执行层（时间推进时）
│   ├── 游戏引擎推进时间 → 各系统依次更新
│   ├── 系统调用行动执行器
│   ├── 行动执行器从行动目标的路径队列取出路径点
│   ├── 行动执行器执行实际移动/攻击等操作
│   └── 行动执行器发布事件
└── 4. 事件层
    ├── 事件处理器处理事件
    ├── 更新UI/通知玩家
    └── 触发后续逻辑（如战斗结算）
```

**核心组件关系**：

| 组件 | 职责 | 说明 |
|------|------|------|
| 指令对象 | 指令数据结构 | 包含目标ID、目标位置等参数 |
| 指令处理器 | 指令处理入口 | 验证合法性、设置实体状态 |
| 评估器 | 可行性评估 | 检查边界、行动力、敌军等 |
| 服务 | 数据计算 | 如寻路服务计算路径 |
| 行动执行器 | 实际执行逻辑 | 执行移动、攻击等操作 |
| 系统 | 时间推进调度 | 在时间推进时调用行动执行器 |

### 5.2 移动指令详细设计

#### 5.2.1 角色移动指令

**指令参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| 目标角色ID | int | 要移动的角色唯一标识 |
| 目标位置 | Point2 | 移动目的地坐标 |

**处理流程**：

| 步骤 | 操作 | 涉及字段 | 说明 |
|------|------|----------|------|
| 1 | 获取角色实体 | `Character.Id` | 根据目标角色ID查找角色实体 |
| 2 | 计算路径 | `Character.Location` → 目标位置 | 使用寻路算法计算从当前位置到目标位置的路径 |
| 3 | 设置移动状态 | `Character.ActionStatus` | 将行动状态设置为"移动中" |
| 4 | 填充路径队列 | `Character.ActionTarget.RoutePoints` | 将计算出的路径点（跳过起点）放入路径队列 |

**路径计算算法**：

| 步骤 | 操作 | 说明 |
|------|------|------|
| 1 | 初始化 | 以角色当前位置为起点，目标位置为终点 |
| 2 | 探索邻居 | 检查当前位置的4方向相邻格子 |
| 3 | 检查地形 | 获取每个邻居的地形移动成本，不可通行则跳过 |
| 4 | 计算成本 | 累计移动成本 = 当前累计成本 + 地形移动成本 |
| 5 | 选择最优 | 使用启发式函数（曼哈顿距离）评估优先级 |
| 6 | 追溯路径 | 从终点回溯到起点，生成完整路径 |

**执行流程（时间推进时）**：

| 步骤 | 操作 | 涉及字段 | 说明 |
|------|------|----------|------|
| 1 | 检查状态 | `Character.ActionStatus` | 确认角色处于"移动中"状态 |
| 2 | 取出路径点 | `Character.ActionTarget.RoutePoints` | 从队列头部取出下一个路径点 |
| 3 | 评估可行性 | `Character.Ap`, 地形移动成本 | 检查行动力是否足够、是否相邻等 |
| 4 | 执行移动 | `Character.Location` | 将角色坐标更新为路径点坐标 |
| 5 | 扣除行动力 | `Character.Ap` | 减去地形移动成本 |
| 6 | 移除路径点 | `Character.ActionTarget.RoutePoints` | 从队列移除已执行的路径点 |
| 7 | 发布事件 | 角色移动事件 | 包含角色ID、起点坐标、终点坐标 |

**可行性评估检查项**：

| 检查项 | 涉及字段 | 失败条件 |
|------|----------|----------|
| 边界检查 | 目标位置 | 目标位置超出地图边界 |
| 行动力检查 | `Character.Ap`, 地形移动成本 | 行动力小于地形移动成本 |
| 相邻检查 | `Character.Location`, 目标位置 | 目标位置与当前位置不相邻 |
| 敌军检查 | 目标位置的单位, 外交关系 | 目标位置有敌军单位 |
| 敌城检查 | 目标位置的据点, 外交关系, `Character.Ap` | 目标位置有敌城且行动力不足 |

#### 5.2.2 单位移动指令

**指令参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| 目标单位ID | int | 要移动的单位唯一标识 |
| 目标位置 | Point2 | 移动目的地坐标 |

**处理流程**：

| 步骤 | 操作 | 涉及字段 | 说明 |
|------|------|----------|------|
| 1 | 获取单位实体 | `Unit.Id` | 根据目标单位ID查找单位实体 |
| 2 | 计算路径 | `Unit.Location` → 目标位置 | 使用寻路算法计算路径 |
| 3 | 设置移动状态 | `Unit.Status` | 将状态设置为"移动中" |
| 4 | 填充路径队列 | `Unit.ActionTarget.RoutePoints` | 将路径点放入路径队列 |

**与角色移动的差异**：

| 差异点 | 角色移动 | 单位移动 |
|--------|----------|----------|
| 状态字段 | `Character.ActionStatus` | `Unit.Status` |
| 敌军检查 | 有（不能进入敌军格子） | 无（可进入敌军格子触发战斗） |
| 执行系统 | 角色系统 | 单位系统 |

### 5.3 攻击指令详细设计

#### 5.3.1 单位攻击指令

**指令参数**：

| 参数 | 类型 | 说明 |
|------|------|------|
| 目标单位ID | int | 攻击方单位唯一标识 |
| 目标位置 | Point2 | 攻击目标位置（敌军或敌城） |

**处理流程**：

| 步骤 | 操作 | 涉及字段 | 说明 |
|------|------|----------|------|
| 1 | 获取单位实体 | `Unit.Id` | 根据目标单位ID查找单位实体 |
| 2 | 评估攻击可行性 | `Unit.Ap`, `Unit.Location`, 目标位置 | 检查行动力、攻击范围、攻击目标 |
| 3 | 设置攻击姿态 | `Unit.Stance` | 将姿态设置为"攻击中" |
| 4 | 设置攻击目标 | `Unit.ActionTarget.UnitId`, `Unit.ActionTarget.StrongholdId` | 记录攻击目标单位ID或据点ID |

**可行性评估检查项**：

| 检查项 | 涉及字段 | 失败条件 |
|------|----------|----------|
| 边界检查 | 目标位置 | 目标位置超出地图边界 |
| 行动力检查 | `Unit.Ap`, `GameRuleConfig.AttackAp` | 行动力小于攻击所需行动点（默认5） |
| 攻击范围检查 | `Unit.Location`, 目标位置 | 目标位置与当前位置不相邻 |
| 攻击目标检查 | 目标位置的单位/据点, 外交关系 | 目标位置无敌军或敌城 |

**执行流程（时间推进时）**：

| 步骤 | 操作 | 涉及字段 | 说明 |
|------|------|----------|------|
| 1 | 检查姿态 | `Unit.Stance` | 确认单位处于"攻击中"姿态 |
| 2 | 获取攻击目标 | `Unit.ActionTarget.UnitId` 或 `Unit.ActionTarget.StrongholdId` | 根据目标ID获取目标实体 |
| 3 | 计算伤害 | `Unit.Attack`, 目标防御 | 根据攻击力与防御力计算伤害值 |
| 4 | 应用伤害 | 目标 `Soldier`, 目标 `Morale` | 减少目标士兵数，降低士气 |
| 5 | 扣除行动力 | `Unit.Ap` | 减去攻击所需行动点 |
| 6 | 发布事件 | 单位攻击事件 | 包含攻击方ID、目标ID、伤害值 |

### 5.4 指令与属性变更对照表

| 指令 | 变更属性 | 变更时机 |
|------|----------|----------|
| 角色移动 | `Location`, `Ap`, `ActionStatus` | 处理层设置状态，执行层执行移动 |
| 单位移动 | `Location`, `Ap`, `Status` | 处理层设置状态，执行层执行移动 |
| 单位攻击 | `Ap`, `Stance`, `ActionTarget` | 处理层设置状态，执行层执行攻击 |
| 进入据点 | `Location`, `Ap`, 所在据点ID | 执行层额外扣除进入据点行动力（默认5） |

---

## 6. 时间推进逻辑

> 本节描述时间推进的执行流程和各系统的属性数值变动逻辑。

### 6.1 时间推进流程概述

```
时间推进流程（NextTime Flow）
├── 游戏循环（定时触发）
│   ├── 前置处理（可选）
│   ├── 时间推进 → 游戏引擎执行
│   └── 后置处理（可选）
├── 游戏引擎执行
│   ├── 按优先级排序各系统
│   └── 依次调用各系统的更新方法
└── 系统执行顺序（优先级值）
    ├── 1.  气候系统     季节/天气变化
    ├── 10. 经济系统     收入/支出结算
    ├── 20. 单位系统     单位行动执行
    ├── 30. 角色系统     角色行动执行+AP恢复
    └── 40. AI系统       NPC决策
```

### 6.2 各系统详细逻辑

#### 6.2.1 气候系统

**执行优先级**：1（最先执行）

**更新操作**：

| 操作 | 说明 | 状态 |
|------|------|------|
| 季节变化 | 根据日期更新当前季节 | 待实现 |
| 天气变化 | 根据季节和随机因素更新天气 | 待实现 |
| 地形影响 | 更新各地形的移动成本（如冬季道路成本增加） | 待实现 |
| 农业影响 | 更新各据点的农业产出系数 | 待实现 |

**设计目标**：

| 功能 | 属性影响 | 执行频率 |
|------|----------|----------|
| 季节变化 | 影响农业产出、地形移动成本 | 每月 |
| 天气变化 | 影响战斗效率、视野范围 | 每天 |
| 灾害事件 | 影响人口、治安、民心 | 随机触发 |

#### 6.2.2 经济系统

**执行优先级**：Market 8 → Economy 10 → Supply 15（策略模式，见 [strategy-detail-design §4.8](./strategy-detail-design.md#48-系统链与日顺序目标)）

**更新操作**：

| 操作 | 说明 | 状态 |
|------|------|------|
| 日产与口粮 | 农业/商业日产、市民/士兵/运输队日耗 | M4-a ✅ |
| 市场撮合 | `StrategyMarketSystem`、日 K、贸易税台账 | M4-b ✅ |
| 税收与维持费 | 人头/商业/贸易税、军队与据点月费 | M4-b ✅（关税 M4-c） |
| 收粮与贡赋 | Region Harvest、农业税、粮/钱贡赋运输 | M4-b ✅ |
| 官办/跨据点/奢侈品/商户贸易、关税、Arrears、拦截缴获 | 策略 M4-c/d ✅ |
| 角色俸禄、年度人口 | M4-d ✅ |
| 势力支出（俸禄等） | 角色俸禄、建设、外交支出 | M5+ |
| 物资生产（铁木马等） | 多资源类型 | M5+ |
| 补给与运输队 | 自动派遣 `SupplyConvoy` / `Messenger`（均 `IsMilitary=false`） | M3-c ✅ |

**设计目标**：

| 功能 | 属性影响 | 执行频率 |
|------|----------|----------|
| 收入结算 | `Force.Treasury`, `Stronghold.PopularFeelings` | 每月 |
| 支出结算 | `Force.Treasury`, `Character.Salary` | 每月 |
| 物资生产 | `StrongholdActor.AgricultureProduction`, `CommerceProduction` | 每月 |
| 补给线 | `Unit.Food`, `Unit.Morale` | 每时段 |

#### 6.2.3 单位系统

**执行优先级**：20（在角色系统之前）

**更新操作**：

| 操作 | 涉及字段 | 说明 |
|------|----------|------|
| 执行移动 | `Unit.Status`, `Unit.ActionTarget.RoutePoints` | 遍历所有"移动中"状态的单位，执行移动 |
| 执行攻击 | `Unit.Stance`, `Unit.ActionTarget` | 遍历所有"攻击中"姿态的单位，执行攻击（待实现） |
| 更新状态 | `Unit.Status`, `Unit.Morale`, `Unit.Tiredness` | 根据状态更新属性（待实现） |

**属性变更逻辑**：

| 状态 | 属性变更 | 说明 |
|------|----------|------|
| 移动中 | `Location`, `Ap`, `Tiredness` | 移动时扣除AP，增加疲劳 |
| 攻击中 | `Ap`, `Morale` | 攻击时扣除AP，士气变化 |
| 待机 | `Defense`, `Tiredness` | 待机时防御下降、疲劳下降（普通姿态） |
| 斗志高昂 | `Morale`, `Attack` | 斗志高昂时士气上升、攻击上升 |
| 恐惧 | `Morale`, `Attack`, `Defense`, `Stance` | 恐惧时士气下降、攻防下降、姿态可能变为机动 |
| 混乱 | `Soldier`, `Attack`, `Defense` | 混乱时士兵数缓慢降低、攻防极大降低 |
| 埋伏中 | 无变更 | 埋伏状态保持不变 |
| 被包围中 | `Morale` | 被包围时士气下降度提高 |

#### 6.2.4 角色系统

**执行优先级**：30（在单位系统之后）

**更新操作**：

| 操作 | 涉及字段 | 说明 |
|------|----------|------|
| 执行移动 | `Character.ActionStatus`, `Character.ActionTarget.RoutePoints` | 遍历所有"移动中"状态的角色，执行移动 |
| 恢复AP | `Character.Ap` | 所有角色的行动力增加恢复量（默认+3） |

**属性变更逻辑**：

| 状态 | 属性变更 | 说明 |
|------|----------|------|
| 待机 | `Ap` | 待机时AP恢复 |
| 休息 | `Hp`, `Ap`, `Money` | 休息时体力恢复、AP恢复、可能消耗金钱 |
| 移动中 | `Location`, `Ap` | 移动时坐标变化、AP扣除 |
| 行动中 | `Ap`, `Emotion` | 行动时AP扣除、心情变化 |

#### 6.2.5 AI系统

**执行优先级**：40（最后执行）

**更新操作**：

| 操作 | 说明 | 状态 |
|------|------|------|
| NPC角色决策 | 根据性格和状态选择行动（休息/修炼/移动） | 待实现 |
| 势力AI决策 | 根据战略方针选择外交和军事行动 | 待实现 |
| 单位AI决策 | 根据方针和姿态选择具体行动 | 待实现 |

**设计目标**：

| 功能 | 决策对象 | 执行频率 |
|------|----------|----------|
| NPC日常行为 | `Character` | 每时段 |
| 势力战略 | `Force.Strategy`, `Force.Tactics` | 每月 |
| 单位方针 | `Unit.Directive`, `Unit.Stance` | 每时段 |
| 外交决策 | `Diplomacy.Strategy` | 每季度 |

### 6.3 游戏规则配置

| 参数 | 默认值 | 说明 |
|------|--------|------|
| 进入据点消耗AP | 5 | 角色进入敌城或己方据点所需的行动力 |
| 攻击消耗AP | 5 | 单位执行攻击所需的行动力 |
| 每时段AP恢复量 | 3 | 角色每时段恢复的行动力 |

### 6.4 时间推进与属性变更对照表

| 属性 | 变更时机 | 变更规则 | 控制系统 |
|------|----------|----------|----------|
| `Ap`（角色） | 每时段 | 增加恢复量（默认+3） | 角色系统 |
| `Ap`（单位） | 每时段 | 待实现（可能不恢复或按姿态恢复） | 单位系统 |
| `Hp` | 休息时 | 增加恢复量 | 角色系统 |
| `Tiredness` | 移动时 | 增加疲劳值 | 单位系统 |
| `Morale` | 战斗时 | 根据胜负变化 | 单位系统 |
| `Soldier` | 战斗时 | 减少伤害值 | 单位系统 |
| `Food` | 每时段 | 减少消耗量 | 经济系统 |
| `Money`（势力） | 每月 | 增加收入减去支出 | 经济系统 |

---

## 7. 战术地图战斗系统

> 战术地图战斗系统用于RPG模式和MMO国战模式，详见各模式详细设计文档。

---

## 8. 网络与多人架构

### 8.1 策略模式多人协议

```
StrategyMultiplayerProtocol
├── 连接阶段
│   ├── Client → Host: JoinGame(playerId, forceId)
│   ├── Host → Client: GameSnapshot(worldState)
│   └── Host → All: PlayerJoined(playerId)
├── 游戏阶段
│   ├── Client → Host: Command(command)        // 玩家指令
│   ├── Client → Host: RequestPause()           // 请求暂停
│   ├── Client → Host: ApproveResume()          // 同意继续
│   ├── Host → All: TimeAdvanced(gameDate)      // 时间推进通知
│   ├── Host → All: StateUpdate(diff)           // 状态差异更新
│   └── Host → All: EventFired(gameEvent)       // 事件广播
├── 瞬间事件协议（方针驱动）
│   ├── Client → Host: TriggerInstantEvent(eventType, params)
│   ├── Host → Client: WinRatePreview(prediction)
│   ├── Client → Host: ConfirmInstantEvent(eventId)
│   ├── Host: 自动匹配防守方方针 + 执行结算
│   ├── Host → All: InstantEventResult(result)
│   └── 防守方离线时方针照常执行
├── 暂停同步
│   ├── 任何玩家可暂停
│   ├── 瞬间事件不暂停游戏时间
│   ├── 所有玩家同意后继续
│   └── 超时自动继续（30秒）
└── 断线处理
    ├── 客机断线：AI接管，5分钟内可重连
    ├── 主机断线：迁移主机或存档
    └── 重连：发送完整快照恢复
```

### 8.2 MMO服务器协议

```
MmoServerProtocol
├── 登录/角色
│   ├── Client → Server: Login(accountId)
│   ├── Client → Server: EnterWorld(characterId)
│   └── Server → Client: WorldSnapshot(characterState, nearbyEntities)
├── 日常行动
│   ├── Client → Server: Action(actionType, params)
│   ├── Server → Client: ActionResult(result)
│   ├── Server → Client: NearbyUpdate(entities)  // 周围实体更新
│   └── Server → All: WorldEvent(event)           // 世界事件
├── 国战
│   ├── Server → All: NationalWarAnnouncement(time, participants)
│   ├── Client → Server: JoinNationalWar()
│   ├── Server → Client: UnitAssigned(unit)       // 分配单位
│   ├── Client → Server: TacticalCommand(cmd)     // 战术指令
│   ├── Server → Client: TacticalUpdate(state)    // 战场状态
│   └── Server → All: NationalWarResult(result)
└── 心跳/同步
    ├── Client → Server: Heartbeat()  每5秒
    ├── Server → Client: Sync(state)  每10秒
    └── 断线30秒后角色自动回城
```

### 8.3 同步策略

```
SynchronizationStrategy
├── 策略模式：指令同步（Lockstep）+ 瞬间事件
│   ├── 只同步玩家指令，不同步状态
│   ├── 所有客户端运行相同引擎
│   ├── 瞬间事件由主机结算，结果广播
│   ├── 优点：带宽低、防作弊、不阻塞
│   └── 缺点：要求确定性计算
├── MMO日常：状态同步
│   ├── 服务器权威，客户端预测
│   ├── 定期发送状态差异
│   ├── 优点：容错性好
│   └── 缺点：带宽较高
└── MMO国战：混合同步
    ├── 战术地图内：指令同步（确定性战斗）
    ├── 战略层面：状态同步（据点归属等）
    └── 切换时发送完整快照
```

---

## 9. 存档系统

```
SaveSystem
├── RPG模式
│   ├── 本地文件存档
│   ├── 存档内容：GameWorld完整序列化
│   └── 自动存档（每月/每季度）
├── 策略模式
│   ├── 单机：本地文件存档
│   ├── 多人：主机存档 + 断线恢复快照
│   └── 存档内容：GameWorld + 多人状态
└── MMO模式
    ├── 服务器数据库持久化
    ├── 实时保存关键状态变更
    ├── 国战日志独立存储
    └── 定期世界快照（每日）
```

---

> 上一级：[基本设计](./design-document.md) | 相关文档：[立志传模式详细设计](./rpg-detail-design.md) | [大战略模式详细设计](./strategy-detail-design.md) | [MMO模式详细设计](./mmo-detail-design.md)
