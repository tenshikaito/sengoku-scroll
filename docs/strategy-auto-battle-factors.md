# 战略自动战斗因素全表与实现说明

> 本文档将**真实战场**中影响「是否开战 / 战斗过程 / 战斗结果」的因素，与 SengokuScroll 现有游戏模型字段一一映射，并说明 M3+ 自动战斗代码如何接入。  
> 流程总览见 [`strategy-detail-design.md`](strategy-detail-design.md) §5.1 / §6；概念见 [`game-concepts.md`](game-concepts.md) §3.0。  
> **2026-07-15**：邻格接敌废止，对峙迁入同格 Battlefield；本表因素公式暂不变，Engagement 触发条件待对齐。  
> 代码入口为 `SengokuScroll.Strategy/Battle/`。

---

## 1. 三阶段评估模型

| 阶段 | 枚举 | 回答的问题 | 主要输出 |
|------|------|------------|----------|
| 接敌 | `Engagement` | 是否进入战斗事件？ | `CanUnitEngage` |
| 强袭 | `Commit` | 对峙日是否发起纠缠决战？ | `ForceCommit` / `BlockCommit`、调整后胜率 |
| 结算 | `Resolve` | 纠缠当日谁胜、伤亡多少？ | 有效战力、胜率、伤亡系数 |
| 战后 | `Aftermath` | 士气/方针/追击如何变化？ | `BattleMoraleRules`、撤退/追击 |

**纠缠特性**：一旦 `Commit=true`，当日必须经 `InstantBattleCalculator` 分出胜负（无第三方备队介入时，劣势方不能退回「长期对峙」）。

---

## 2. 因素总览（按类别）

### 2.1 指挥官（Character）

| 因素 | 模型字段 | 影响阶段 | 作用（设计） | 代码 |
|------|----------|----------|--------------|------|
| 统率 | `Leadership` | Resolve | 有效战力缩放 0.88~1.12 | ✅ `BattleFactorEvaluator` |
| 武力 | `Power` | Resolve | 攻方战力缩放 | ✅ |
| 智谋 | `Strategy` | Resolve | 守方智谋差 → 守方胜率 | ✅ |
| 政治/魅力 | `Politics`, `Charm` | Commit/外交 | 同盟/劝降（待扩展） | ❌ |
| 胆气 | `Personality.Courage` | Commit | 强袭意愿 +5% | ✅ |
| 野心 | `Personality.Ambition` | Commit | 强袭意愿 +4%；鲁莽 | ✅ |
| 慎重/细心 | `Personality.Action` | Commit | 己方 -6%、敌慎重 +3% | ✅ |
| 性情 | `Personality.Temper` | Commit | 低 Temper+高 Courage → 轻敌莽撞 | ✅ 低概率 |
| 轻敌/鲁莽 | Temper+Courage+Ambition | Commit | `ForceCommit` | ✅ |
| 复仇/羁绊 | 事件标记（待建） | Commit | `ForceCommit` | ❌ 预留 |
| 生病 | `IsSick` | Resolve | 胜率 -8% | ✅ |
| 体力不支 | `Hp` &lt; 40 | Resolve | 胜率 -5% | ✅ |
| 军事熟练 | `Proficiency.Military.Level` | Resolve | 最多 +6% 胜率 | ✅ |
| 单挑/格斗 | `Proficiency.Fighting` | Resolve | 主将单挑（待扩展） | ❌ |
| 间谍/计略 | `Proficiency.Spy` | Commit/Resolve | 中计、假情报 | ❌ 仅信使迷惑运输队 |
| 装备 | `WeaponId`, `ArmorId` | Resolve | 攻防加成 | ✅ `BattleEquipmentRules` |
| 运势 | `Personality.Fortune` | Commit | 莽撞判定种子 | ✅ |

### 2.2 部队（Unit / Actor）

| 因素 | 模型字段 | 影响阶段 | 作用 | 代码 |
|------|----------|----------|------|------|
| 兵数 | `Soldier` | Resolve | 有效战力基数 | ✅ |
| 攻防 | `Attack`, `Defense` | Resolve | 有效战力 | ✅ |
| 训练度 | `Training` | Resolve | 战力缩放 0.72~1.28 | ✅ |
| 士气 | `Morale` | Commit/Resolve/Aftermath | 胜率 ±(Morale-50)/5；&lt;35 禁战；战后涨跌 | ✅ |
| 疲劳 | `Tiredness` | Resolve | 最多 -15% 胜率 | ✅ |
| 伤兵 | `Patient` | Aftermath | 恢复速度（待扩展） | ❌ |
| 军粮 | `Food` | Commit | 携粮天数 → 强袭窗口 | ✅ |
| 方针 | `Directive` | Engagement/Commit | 占领/劫掠接敌；撤退禁 Commit | ✅ |
| 姿态 | `Stance` | Resolve | 攻击/坚守/包围/机动/警惕 | ✅ 部分 |
| 状态 | `Status` | Engagement/Resolve | 高昂/恐惧/混乱/埋伏/被包围 | ✅ |
| 阵型 | `FormationId` | Resolve | 攻防/移动修正 | ✅ `BattleFormationRules` |
| 行动力 | `Ap` | 接敌角色 | 互攻时定攻方 | ✅ 流程 |
| 移动力 | `Movement` | Aftermath | 败退 AP 加成 | ✅ |
| 子编制 | `SubUnitIds` → `SubUnit` | Resolve | 兵种系数、分段伤亡 | ❌ 仅汇总兵数 |

### 2.3 兵种与编制（SubUnit / UnitType）

| 因素 | 模型字段 | 作用 | 代码 |
|------|----------|------|------|
| 兵种 | `SubUnit.TypeId`, `UnitTypeDefinition` | 步/弓/骑/铁炮攻防移动 | ✅ `BattleCompositionCalculator` |
| 兵种熟练 | `Proficiency.Infantry/Ride/Archery/Firelock` | 对应兵种战力 | ❌ |
| 骑兵在地形 | 地形 × 兵种 | 山地惩罚等 | ❌ |
| 器械 | `Cannon` 等物资 | 攻城/野战（待扩展） | ❌ |

### 2.4 后勤与情报

| 因素 | 模型/系统 | 影响阶段 | 作用 | 代码 |
|------|-----------|----------|------|------|
| 补给三态 | `SupplyStatusEvaluator` | Commit | 敌 CutOff +15%、Strained +8% | ✅ |
| 在途运输队 | `SupplyConvoy` | Commit | 评估 CutOff/Strained | ✅ |
| 携粮天数 | `EstimateFoodDaysRemaining` | Commit | ≤3 日 +12%、≤7 日 +6% | ✅ |
| 假情报 | `Messenger` FalseIntelligence | 间接 | 迷惑运输队 | ✅ 非野战 |
| 对峙情报 | `StandoffDays` | Commit | 每日改善评估（最多 +10） | ✅ |
| 据点粮库 | `Stronghold.ForceActor.Food` | Commit | 是否可续战 | ✅ 间接 |

### 2.5 战场环境与态势

| 因素 | 模型 | 影响阶段 | 作用 | 代码 |
|------|------|----------|------|------|
| 地形 | `TerrainDefinition.Type` | Resolve | 守方山地 +6%、丘陵 +4% 等 | ✅ 基础 |
| 道路 | `RoadDefinition` | Resolve | 增援速度（待扩展） | ❌ |
| 据点/城防 | `Stronghold.Defense` | 攻城 | 守城战 | ❌ 攻城未实装 |
| 天气/季节 | `ClimateSystem`, `RegionDefinition` | Commit/Resolve | 弓/火器/士气 | ⚠️ `BattleWeatherEvaluator`（区域+季节近似） |
| 灾害 | 区域灾害率 | Resolve | 混乱、补给 | ❌ |
| 攻守态势 | 攻方 `Attacking` / 守方 `Hold` | Resolve | 攻 +5% 战、守 +12% 防 | ✅ |
| 包围 | `Surrounding` / `BeingSurround` | Resolve | 守方胜率/伤亡 | ✅ |
| 埋伏 | `Ambushing` | Resolve | 攻方突袭 +18% | ✅ |
| 接敌类型 | `BattleEngagementKind` | Resolve | 野战/伏击/攻城（简）系数；**触发改为同格 BF**（§3.0） | ✅→📋 |
| 附近友军 | 同 BF / 同格共战侧计数 | Commit | 每队最多 +8% 胜率（原相邻格，待对齐） | ✅→📋 |
| 附近敌军备队 | 同上 | Commit | 威慑/支援（待细化） | ⚠️ 仅友军 |

### 2.6 外交与势力

| 因素 | 模型 | 作用 | 代码 |
|------|------|------|------|
| 敌友关系 | `Diplomacy.Relation` | 接敌合法性 | ✅ |
| 停战 | `IsTruce` | 是否可接敌 | ⚠️ 待查 |
| 势力战略/战术 | `Force.Strategy`, `Tactics` | AI 接敌倾向 | ⚠️ 部分 AI |
| 稳定/声望 | `Force.Stability`, `Prestige` | 全国士气（待扩展） | ❌ |

### 2.7 方针与计略（设计 §7）

| 方针 | 设计效果 | 代码 |
|------|----------|------|
| 死守/坚守/迎击/逃跑 | 攻防修正、撤退成功率 | ✅ `BattleDirectiveRules` |
| 包围/强攻 | 攻城 | ❌ |
| 火计/流言/煽动 | 混乱、士气、假情报 | ❌ |

---

## 3. 合成公式（M3+ 实装）

### 3.1 有效战力

```
BasePower(unit) = Soldier × (Attack + Defense) / 20   // 缺省攻防=10

EffectivePower = BasePower × TrainingScale × LeadershipScale × PowerScale × StanceScale × StatusScale

TrainingScale = 0.72 + Training/100 × 0.56
LeadershipScale = 0.88 + Leadership/100 × 0.24
```

### 3.2 胜率

```
BaseWinRate = EffectivePower(攻) / (EffectivePower(攻) + EffectivePower(守)) × 100

FinalWinRate = clamp(
    BaseWinRate
    + Σ(攻方胜率修正)
    - Σ(守方胜率修正),
    5%, 95%)
```

修正项包括：士气、疲劳、生病、智谋差、地形、埋伏、包围、补给窗口、对峙情报、附近友军等（见 `BattleFactorBreakdown.Notes`）。

### 3.3 强袭（Commit）判定

```
若 ForceCommit（轻敌/鲁莽低概率）且非 BlockCommit → 决战
若 合计兵数 < 6000 且非 BlockCommit → 决战（小股默认纠缠）
若 对峙日 ≥ 30 → 决战
若 AdjustedWinRate(自方) ≥ 58% 且 CanUnitEngage(自方) → 决战
否则 → 对峙（Standoff）
```

`BlockCommit`：士气 &lt; 35、混乱、撤退中、任一方 `CanUnitEngage=false`。

### 3.4 伤亡

```
基础伤亡% = 胜方 10~25% / 败方 30~60%（随机，种子固定）
实际伤亡 = PercentOf(兵数, 基础%) × CasualtyScale
PercentOf 对小股保底至少 1（避免残兵永不消灭）
```

### 3.5 战后士气

```
胜方 Morale += 12（决战）/ +6（非决战）
负方 Morale -= 18 / -9
Morale < 35 → 方针=Retreat，禁止再接敌
Morale ≥ 75 → Status=Inspiring；< 30 → Fearful
```

---

## 4. 代码架构

```
SengokuScroll.Strategy/Battle/
├── BattleEvaluationContext.cs
├── BattleFactorBreakdown.cs
├── BattleFactorEvaluator.cs      # 汇总入口
├── BattleMoraleRules.cs
├── BattleDirectiveRules.cs       # §7 战斗方针 → 战力/胜率/Commit
├── BattleCompositionCalculator.cs # SubUnit 兵种加权
├── BattleWeatherEvaluator.cs     # 季节 + 区域气候 hook
├── BattleStratagemEvaluator.cs   # 中计/迷惑粮道 hook
├── BattleEngagementClassifier.cs # 野战/伏击/攻城（简）
├── BattleFormationRules.cs       # FormationId 内置阵型
├── BattleEquipmentRules.cs       # WeaponId/ArmorId
└── BattleFactorMapper.cs         # Breakdown → DTO/战报

SengokuScroll.Strategy/Calculators/
├── FieldBattleAutoResolver.cs    # 对峙 / 决战分流
└── InstantBattleCalculator.cs    # 纠缠结算（读 Breakdown）

SengokuScroll.Strategy/Rules/
├── BattleCommitRules.cs          # 强袭判定（委托 Evaluator）
├── BattleRetreatRules.cs         # 败退
├── BattlePursuitRules.cs         # 追击
└── MoveEngagementRules.cs        # 接敌（含低士气禁战）
```

**扩展新因素步骤**

1. 在 §2 表中登记因素与字段  
2. 在 `BattleFactorEvaluator` 增加 `ApplyXxxFactors`  
3. 写入 `BattleFactorBreakdown.Add(...)` 供战报/UI  
4. 补充单元测试  

---

## 5. 实装状态汇总

| 状态 | 说明 |
|------|------|
| ✅ 已接入 | 兵数攻防、训练、士气、疲劳、将领、性格、姿态/状态、**战斗方针**、**SubUnit 兵种**、**阵型**、**装备**、补给、地形、**天气(区域+季节)**、**计略迷惑**、**接敌类型(野战/伏击/攻城简)**、对峙情报、友军、战后士气、低士气禁战、小股试探期、**战报因素明细 UI** |
| ⚠️ 部分 | 停战、AI 战略、复仇事件、ClimateSystem 精确天气、攻城完整 Resolver |
| ❌ 待做 | SubUnit 分段伤亡、全国稳定度、方针「逃跑成功脱离」独立分支、Formation/装备 MasterData |

---

## 6. 端到端流水线（M3 实装）

```
日推进 / 移动接敌
  StrategyMoveEngagementSystem
    → MoveEngagementRules.CanEngage（含低士气禁战）
    → QueueAttack

日末
  StrategyBattleResolutionSystem
    → BattleEngagementResolver（互攻比移动力定攻方）
    → FieldBattleAutoResolver.ResolveDailyEngagement
         Standoff：无伤亡、standoffDays++、里程碑信使
         Decisive：TacticalBattleSimulator
            ├─ 守方 4 格内兵队入场
            ├─ 四邻围攻判定
            ├─ SubUnit 展开按移动力行动
            ├─ 将领行动判定
            └─ 过程战报（无因素修正百分比）
    → ApplyCasualtiesToWorld / BattleMoraleRules / Aftermath
    → StrategyBattleResultDto.LogEntries（无 FactorNotes 过程段）
```

### 6.1 关键常量（`BattleConstants`）

| 常量 | 值 | 含义 |
|------|-----|------|
| `LargeArmySoldierThreshold` | 6000 | 大股对峙阈值 |
| `SmallArmyProbeDays` | 2 | 小股试探对峙天数 |
| `CommitAssaultWinRateThreshold` | 58 | 强袭胜率阈值（%） |
| `StandoffForceBattleDays` | 30 | 强制决战对峙日 |
| `LowMoraleEngageThreshold` | 35 | 低于此士气禁接敌 |
| `StandoffReportDays` | 3,5,10,15,20,30 | 对峙信使里程碑 |

### 6.2 API / DTO（`StrategyBattleResultDto`）

| 字段 | 说明 |
|------|------|
| `AttackerWon` | 攻方是否获胜（客观结果） |
| `AttackerForceId` / `DefenderForceId` | 供前端按当前势力判胜负 |
| `EngagementKind` | `FieldBattle` / `Ambush` / `Siege` |
| `FactorNotes` | 胜负因素明细（`FactorId`、`Label`、攻/守胜率修正） |
| `LogEntries` | 战斗过程 + 因素段（`BattleFactorMapper`） |
| `AttackerWinRatePercent` / `ResolutionRoll` | 战前胜率与判定骰 |

### 6.3 前端战报（WebClient）

| 组件 | 职责 |
|------|------|
| `StrategyBattleResultDialog` | 接敌类型、因素面板、过程日志；**按 `playerForceId` 显示胜利/失利** |
| `utils/battleResult.ts` | `normalizeBattleResult`、`playerWonBattle`、`battleOutcomeHeadline` |
| `utils/strategyNotifications.ts` | 通知摘要「胜利/失利」而非「攻方胜/守方胜」 |
| `StrategyBattleConfirmDialog` | 战前预览（既有） |

### 6.4 单元测试（`SengokuScroll.Strategy.Tests`）

| 测试类 | 覆盖 |
|--------|------|
| `FieldBattleAutoResolverTests` | 小股试探、大股对峙、30 日强袭 |
| `BattleFactorEvaluatorTests` | 低士气禁战、战后士气 |
| `BattleDirectiveAndCompositionTests` | 方针、兵种、中计 ForceCommit |
| `BattleFormationAndEngagementTests` | 阵型、接敌分类、FactorNotes |
| `InstantBattleTests` | 伤亡、`PercentOf` 残兵保底 |
| `BattleEngagementResolverTests` | 接敌解析 |

当前策略测试 **74 项**通过（截至 M3 自动战斗实装收尾）。

---

## 7. 建议迭代顺序

1. ~~**方针接入 Resolve**~~：`BattleDirectiveRules` ✅  
2. ~~**SubUnit 兵种系数**~~：`BattleCompositionCalculator` ✅  
3. ~~**计略/天气 hook**~~：`BattleStratagemEvaluator` / `BattleWeatherEvaluator` ✅  
4. ~~**阵型 `FormationId`**、装备 `WeaponId/ArmorId`~~ ✅  
5. ~~**攻城/伏击** 接敌分类与系数~~ ⚠️ 完整攻城 Resolver 待做  
6. **ClimateSystem** 替换季节近似值  
7. **Formation/装备 MasterData**、复仇事件

---

## 8. 相关文档

- [`strategy-detail-design.md`](strategy-detail-design.md) — §6 自动战斗、§7 方针、§8 胜率、§9 结算  
- [`shared-detail-design.md`](shared-detail-design.md) — 气候、计略、地形共享设计  
