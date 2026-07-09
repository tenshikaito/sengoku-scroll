# SengokuScroll（战国绘卷）基本设计文档

> 版本：2.3 | 日期：2026-06-28

---

## 文档体系

> 完整索引、阅读顺序与实现对照见 **[设计文档索引](./README.md)**。

| 层级 | 文档 | 内容 | 状态 |
|------|------|------|------|
| 索引 | [README.md](./README.md) | 文档总览、依赖关系、阅读路径、实现进度 | 当前 |
| 基本设计 | [design-document.md](./design-document.md)（本文档） | 项目概述、架构总览、设计原则 | 当前 |
| 共同详细设计 | [shared-detail-design.md](./shared-detail-design.md) | 共享领域模型、规则、事件、战斗、网络 | 已有 |
| 模式详细设计 | [rpg-detail-design.md](./rpg-detail-design.md) | 立志传模式专属系统 | 已有 |
| | [strategy-detail-design.md](./strategy-detail-design.md) | 大战略模式专属系统 | 已有 |
| | [mmo-detail-design.md](./mmo-detail-design.md) | MMO模式专属系统 | 已有 |
| 共通界面设计 | [shared-ui-design.md](./shared-ui-design.md) | 共通UI/交互规则、主菜单、系统菜单 | 已有 |
| 模式界面设计 | [rpg-ui-design.md](./rpg-ui-design.md) | 立志传模式界面 | 已有 |
| | [strategy-ui-design.md](./strategy-ui-design.md) | 大战略模式界面 | 已有 |
| | [mmo-ui-design.md](./mmo-ui-design.md) | MMO模式界面 | 已有 |
| **开发计划** | [strategy-development-plan.md](./strategy-development-plan.md) | **策略模式里程碑与范围（待确认）** | **草案** |

**数据来源（项目根目录）**：`data.xlsx` · `model.xlsx` · `rule.xlsx` · `screen.xlsx`

---

## 目录

1. [项目概述与设计理念](#1-项目概述与设计理念)
2. [三模式架构总览](#2-三模式架构总览)
3. [附录：现有代码问题与重构计划](#3-附录现有代码问题与重构计划)
4. [附录：实现进度对照](#4-附录实现进度对照)

（§1 含 [玩家体验方针](#16-玩家体验方针)、[开发顺序](#17-开发顺序与当前阶段)、[RPG 与策略关系](#18-rpg-与策略的关系开发视角)）

---

## 1. 项目概述与设计理念

### 1.1 核心定位

SengokuScroll（战国绘卷）是一款以日本战国时代为背景的**多模式游戏引擎**，支持三种截然不同的游戏体验共享同一套领域模型：

| 模式 | 参照作品 | 核心体验 |
|------|----------|----------|
| 立志传模式（RPG） | 太阁立志传5 | 单角色+随从的人生模拟，从足轻到天下人 |
| 大战略模式（策略） | 信长之野望·革新 | 半回合制大战略，支持多人联机 |
| MMO模式 | 三国群英传Online | 日常RPG + 定时国战，改变世界格局 |

### 1.2 设计原则

1. **共享实体，独立行为**：三种模式共用 Domain 层的实体定义和基础规则，但各模式拥有独立的 System / Evaluator / Action
2. **可配置的时间尺度**：GameDate 系统支持不同粒度的时间推演（1回合/天、1回合/月等）
3. **战斗模式分离**：立志传模式使用战术地图（玩家直接操控）；大战略模式单人可选择亲自指挥或方针自动结算，多人模式仅方针自动结算（瞬间事件，不阻塞其他玩家）；MMO国战使用战术地图
4. **事件驱动**：所有模式间通过事件系统解耦，支持回放、存档、网络同步
5. **决策优先、减少微操**：方便玩家进行战略与角色决策，减少在运输、重复确认、中间状态管理等细节上的精力耗费（详见 §1.6）

### 1.6 玩家体验方针

> **方便玩家操作与决策，减少在细节上耗费精力。**

| 原则 | 说明 |
|------|------|
| 决策显性 | 高风险操作战前须可预览后果（胜率、收支、补给等） |
| 真实后勤 | **运输队实体**（与兵队同类）自动携带粮草补给；玩家看地图与在途状态，**不**手动操控运输队 |
| 信使制度 | 异格指令/战报/外交 **经信使** 传递；**同格（含同据点）免信使** |
| 间接指挥 | NPC 部队通过 **方针 + 战略目标** 间接控制，不逐格微操 |
| 战斗选择 | 单机战前可选 **瞬间（自动）** 或 **亲自指挥**（后者随 RPG 战术地图后期启用） |
| 合理默认 | 未配置项使用势力/系统默认值，减少必填项 |
| 信息分层 | 主界面摘要，详情进情报/战报；避免单屏信息过载 |

各模式实装细则见当前模式开发计划（策略：[strategy-development-plan.md](./strategy-development-plan.md) §2）。

### 1.7 开发顺序与当前阶段

| 顺序 | 阶段 | 状态 |
|------|------|------|
| **1** | **大战略（策略）单机** | **当前 — M0 已确认，M1 待启动** |
| 2 | 立志传（RPG） | 未开始 — **策略基础上的增量**（时间推进、城内场景、剧情） |
| 3 | **策略多人联机（1–8 人）** | 未开始 — **RPG 完成后** |
| 4 | MMO | 未开始 — RPG 可玩里程碑后 |

**规则**：同一时间只做一个阶段。策略单机计划见 **[strategy-development-plan.md](./strategy-development-plan.md)**。

### 1.8 RPG 与策略的关系（开发视角）

立志传（RPG）在实现上 **以策略单机为基线**，共用 Domain / 世界地图 / 据点 / 单位等核心，主要差异为：

| 维度 | 策略模式（先完成） | RPG 模式（增量） |
|------|-------------------|------------------|
| 时间推进 | 半回合，按天推进 | 时段（DayPhase），可暂停的实时推进 |
| 场景 | 全国战略地图 + Popup | 上述地图 + **据点内场景切换**（设施/交互） |
| 内容 | 系统驱动、无主线剧情 | **大量剧情演出**、任务、角色成长线 |
| 战斗 | 瞬间（自动）+ 亲自指挥 **占位**（M3）；战术地图 **RPG 阶段首做** | **战术地图**（RPG 阶段首做） |

详见 [rpg-detail-design.md §0](./rpg-detail-design.md#0-与策略模式的关系)、[strategy-development-plan §10](./strategy-development-plan.md#101-rpg-模式策略基础上的增量)。

### 1.3 技术栈

| 层级 | 技术 |
|------|------|
| 后端 | .NET 10 (C#) / ASP.NET Core |
| 前端 | Vue 3 + TypeScript + Vite |
| 地图/战斗渲染 | PixiJS（规划；当前原型为 Canvas 2D） |
| UI 组件 | Element Plus |
| 实时通信 | SignalR（多人联机/MMO） |
| 数据持久化 | 文件存档（单机）/ 数据库（MMO） |
| 测试 | xUnit v3 |
| 桌面分发（可选） | Tauri 包装 Web 客户端 |

### 1.4 目标平台

| 平台 | 优先级 | 说明 |
|------|--------|------|
| PC 浏览器 | 主平台 | 完整三模式体验 |
| 平板浏览器 | 次平台 | 大屏布局（≥768px），完整或接近完整体验 |
| 桌面独立应用 | 可选 | Tauri 壳，与 Web 共用同一前端 |
| 手机 | 非目标 | 信息密度与操作模型不适合；文档中触屏规则供平板参考 |

### 1.5 客户端架构

```
Web Client
├── Vue 3          面板/UI（菜单、情报、存档、外交）
├── PixiJS         世界地图、战术地图（规划）
├── Pinia          客户端状态（规划）
└── SignalR/HTTP   与 .NET 服务端通信

Backend
├── WebApi         REST（账号、存档元数据、命令）
├── SignalR Hub    实时联机/MMO（规划）
├── Game Server    独立进程：Tick、指令、AI（规划，MMO 必需）
└── Domain + Application   共享仿真核心
```

---

## 2. 三模式架构总览

### 2.1 项目结构

```
SengokuScroll.sln
├── Libraries/
│   ├── SengokuScroll.Common          # 通用工具（不变）
│   ├── SengokuScroll.Domain          # 领域层（共享实体+基础规则）
│   │   ├── Entities/                 # 共享实体定义
│   │   ├── Rules/                    # 基础规则（移动、外交等）
│   │   ├── Evaluators/               # 基础评估器
│   │   ├── Events/                   # 领域事件
│   │   └── Definitions/              # 定义数据
│   ├── SengokuScroll.Application     # 应用层（共享命令/查询框架）
│   └── SengokuScroll.Host            # 基础设施层
├── Modes/                            # 模式特定逻辑
│   ├── SengokuScroll.Rpg/            # 立志传模式
│   │   ├── Systems/                  # RPG专属系统
│   │   ├── Actions/                  # RPG专属行为
│   │   ├── Evaluators/              # RPG专属评估器
│   │   └── Cards/                    # 卡片系统（RPG交互）
│   ├── SengokuScroll.Strategy/       # 大战略模式
│   │   ├── Systems/                  # 策略专属系统
│   │   ├── Actions/                  # 策略专属行为
│   │   ├── Evaluators/              # 策略专属评估器
│   │   └── Multiplayer/             # 多人联机逻辑
│   └── SengokuScroll.Mmo/           # MMO模式
│       ├── Systems/                  # MMO专属系统
│       ├── Actions/                  # MMO专属行为
│       ├── NationalWar/             # 国战系统
│       └── Persistence/             # MMO持久化
├── Combat/                           # 战术地图战斗
│   └── SengokuScroll.Combat/        # 战术地图战斗引擎
├── Infrastructure/
│   ├── SengokuScroll.WebApi          # API层
│   └── SengokuScroll.WebClient       # 前端
└── UnitTests/
```

### 2.2 模式切换机制

```csharp
// GameMode 枚举扩展
public enum GameMode
{
    RolePlaying,      // 立志传模式（RPG）
    GrandStrategy,    // 大战略模式（策略）
    MassivelyMultiplayer  // MMO模式
}

// 各模式通过 DI 注册不同的 System/Evaluator/Action
// GameEngine 根据模式组合不同的系统集合
```

### 2.3 三模式对比矩阵

| 维度 | 立志传模式（RPG） | 大战略模式（策略） | MMO模式 |
|------|-----|------|-----|
| 玩家控制 | 单角色+随从 | 势力/多单位 | 单角色+自有单位 |
| 时间推进 | 实时+可暂停 | 半回合（EU4式） | 实时（国战时半回合） |
| 地图视角 | 角色周围局部 | 全国全图 | 角色周围+国战全图 |
| 战斗 | 战术地图（小规模） | 单人可亲自指挥/自动结算；多人仅自动结算 | 战术地图（国战超大规模） |
| 经济 | 个人收支 | 势力财政 | 个人+势力贡献 |
| 外交 | NPC好感度 | 势力间外交 | 势力间外交+玩家间 |
| 存档 | 本地文件 | 本地/主机 | 服务器数据库 |
| 人数 | 1人 | 1-8人 | 数百人 |

### 2.4 各模式时间配置

| 模式 | 默认推进粒度 | 推进速度 | 说明 |
|------|-------------|---------|------|
| RPG | 1 DayPhase | 1-4秒/时段 | 角色行动消耗时段 |
| 策略 | 1 Day | 可调（1-10秒/天） | 半回合制，可暂停 |
| MMO | 1 DayPhase | 1-2秒/时段 | 国战时切为策略模式速度 |

### 2.5 新增项目依赖关系

```
SengokuScroll.Common
    ↑
SengokuScroll.Domain
    ↑
SengokuScroll.Application
    ↑
┌───────┼───────┐
│       │       │
Rpg   Strategy  Mmo     ← 模式特定逻辑
│       │       │
└───────┼───────┘
        ↑
SengokuScroll.Combat     ← 战术地图战斗
        ↑
SengokuScroll.Host       ← 组装与DI
        ↑
SengokuScroll.WebApi     ← HTTP/SignalR
```

### 2.6 当前代码结构（2026-06-28）

设计目标见 §2.1。当前仓库已实现部分核心库与 Web 层，**模式专属项目（Rpg/Strategy/Mmo/Combat）尚未拆分**：

```
SengokuScroll.sln（当前）
├── SengokuScroll.Common
├── SengokuScroll.Domain          ← 实体、规则、System、Evaluator
├── SengokuScroll.Application     ← GameLoop、Command、Director
├── SengokuScroll.Host            ← DI 组装
├── SengokuScroll.WebApi          ← HTTP API
├── SengokuScroll.WebClient       ← Vue 3 前端原型
└── SengokuScroll.Application.Tests
```

---

## 3. 附录：现有代码问题与重构计划

### 3.1 需修复的Bug

| 优先级 | 问题 | 位置 | 修复方案 |
|--------|------|------|----------|
| 🔴 | CharacterMoveAction 使用 UnitMoveEvaluator 而非 CharacterMoveEvaluator | CharacterMoveAction.cs:8 | 替换为 CharacterMoveEvaluator |
| 🔴 | MovementRules.IsReadyToMove 重复条件 | MovementRules.cs:56 | 改为 `movable.IsReadyToMove && targetUnit.IsReadyToMove` |
| 🔴 | DiplomacyRules.IsAlly 错误码返回 NotEnemyForce | DiplomacyRules.cs:73 | 改为 `NotAllyForce` |
| 🟡 | DiplomacyRules.GetForces 返回可空值但签名不反映 | DiplomacyRules.cs:101 | 改为 `(Force?, Force?)` 并在调用方检查 |
| 🟡 | GameEventDispatcher 非线程安全 | GameEventDispatcher.cs | 改用 ConcurrentDictionary |

### 3.2 需重构的设计

| 优先级 | 问题 | 重构方案 |
|--------|------|----------|
| 🟡 | GameResult 用于布尔语义判断（IsEnemy返回Ok表示"是敌人"） | 引入 `DiplomacyCheckResult` 或改用 bool + out 参数 |
| 🟡 | GameLoop 使用 ManualResetEventSlim 阻塞线程池 | 改用 SemaphoreSlim.WaitAsync 或 Channel |
| 🟡 | RpgGameEngine 和 StrategyGameEngine 完全相同 | 合并为 ConfigurableGameEngine，通过配置注入系统列表 |
| 🟡 | DI 注册中 IGameWorldEventDispatcher 和 IGameEventDispatcher 重复实例 | 统一注册为同一 Singleton |
| 🟢 | EvaluatorBase.Evaluate 每次创建委托数组 | 改为虚方法链或编译时生成的规则链 |
| 🟢 | PathfindingService 只支持4方向 | 扩展支持8方向，并加入单位阻挡/地形限制 |

---

## 4. 附录：实现进度对照

| 设计模块 | 文档位置 | 代码状态 |
|----------|----------|----------|
| 共享实体 Character/Unit/Stronghold | shared-detail §1 | 🟡 部分实现 |
| 移动/外交规则 | shared-detail §2 | 🟡 部分实现 |
| GameDate 时间系统 | shared-detail §3 | 🟡 已实现类型 |
| 领域事件 | shared-detail §4 | 🟡 少量事件 |
| 角色/单位移动指令 | shared-detail §5 | 🟡 部分实现 |
| 时间推进 System 链 | shared-detail §6 | 🟡 Climate/Character/Unit 骨架 |
| 战术地图战斗 | shared-detail §7 | ❌ 未实现 |
| 策略多人 / MMO 网络 | shared-detail §8 | ❌ 未实现 |
| 存档系统 | shared-detail §9 | ❌ 未实现 |
| RPG 模式系统 | rpg-detail | ❌ 未拆分 |
| 大战略模式系统 | strategy-detail | ❌ 未拆分 |
| MMO 模式系统 | mmo-detail | ❌ 未实现 |
| 共通 UI | shared-ui-design | ❌ 前端原型 |
| 模式专属 UI | *-ui-design | ❌ 未实现 |

详细对照与开发阶段建议见 **[设计文档索引 §6](./README.md#6-设计与代码对照)**。

---

> 本文档为战国绘卷项目的基本设计概览。请先阅读 [设计文档索引](./README.md)，再按需查阅各模式详细设计文档。
