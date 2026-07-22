# 策略模式：战争迷雾与情报设计

> 版本：2026-07-21

## 1. 难度档位（3 模板 + Custom）

| 难度 | Fog | Intel | Control | Instant UI | AllyVision | CharVision |
|------|-----|-------|---------|------------|------------|------------|
| Easy | None | Full | FullDirect | ON（强制） | ON | ON |
| Normal | Force | ForceIntel | DirectiveOnly | OFF | OFF | OFF |
| Hard | Character | ForceIntel | DirectiveOnly | OFF | N/A | N/A |
| Custom | 可逐项配置 | | | | 仅 Force 模式 | 仅 Force 模式 |

**玩家当主角色**在任何迷雾模式下均提供视野（外出/随军时），不受 `CharacterSharedVision` 影响。

**难度仅影响迷雾与消息/情报获取方式，不影响战斗伤亡下限、追击脱离率等战斗数值。**

旧字符串 `Legendary` 解析为 `Hard`。

## 2. 视野

- 默认半径：**2 格**（单位、据点、角色均如此）。
- 形状：轴对齐方形，格 `(x,y)` 可见当且仅当 `|x-x0| ≤ range` 且 `|y-y0| ≤ range`。
- Known 据点（如剧本 `knownStrongholdIds` 中的挂川 Id=6）：单格 gray 标记，不提供 sight。

### 2.1 文书载体（IMessageCarrier）

「信使」不是独立地图种族，而是 **单位/角色携带文书的载体抽象**（当前过渡实现为 `MessageCarrier` + `MessagePayload` + `CarrierKind`）：

| CarrierKind | 典型来源 | 势力迷雾视野 | 地图显示 |
|-------------|----------|--------------|----------|
| `UnitEscort` | 战报/前线情报（单位编制护送） | **贡献视野**（同军事单位） | 仅 **亮格** |
| `Character` | 方针/税令/匿名信差 | **不贡献视野** | 仅 **亮格** |

灰格/黑格上不显示任何在途文书载体位置；玩家通过消息区/列表获知文书状态。

## 3. 开局 UI

主界面「开局设置」对话框确认后 `POST /load` 传入 `difficulty` 与可选 `customStartOptions`。

详见 `SengokuScroll.Strategy/Vision/` 与 `Models/GameStartOptions.cs`。

## 4. 谍报情报（ForceIntel / Hard，2026-07 实装）

**核心规则**：进入 visible 格 **≠** 看到具体数值。非自势力（含内藩）据点/部队须 **谍报成功** 后才按登记范围展示。

| 组件 | 职责 |
|------|------|
| `StrategyEspionageIntelLedger` | 按 `(TargetKind, TargetId)` 存 scope/精度/过期日 |
| `EspionageIntelRules` | DTO masking：无记录→「未知」；Fuzzy→高/中/低；Exact→具体数值 |
| `StrategyVisionSystem` | 日推进 `PruneExpired`（**获得日起 2 游戏月**） |
| `ForceIntelPolicy` | **no-op**（不再因视野自动 `****` 兵数） |
| `POST /strategy/espionage-intel` | 开发/任务登记 API（忍者玩法待实装） |

### 4.1 Scope 与 Precision

| Scope | 可见字段（敌方） |
|-------|------------------|
| `Military` | 兵数、士气、训练、城防、守备设施 |
| `Domestic` | 人口、储粮、金钱、治安、税率等 |
| `Both` | 军事 + 内政 |

| Precision | 展示 |
|-----------|------|
| `Fuzzy` | 高 / 中 / 低 档位（阈值见 `EspionageIntelRules`） |
| `Exact` | 具体数值（在 scope 允许范围内） |

### 4.2 前端

- `espionageIntel` / `espionage*Band` 字段经 `normalizeStrategyWorldState` 解析。
- `strategyIntelDisplay.isForeignIntelRestricted` 按 **势力圈**（非仅 `forceId !== player`）判断。
- 悬浮框/情报 Tab 无谍报时显示「未知」。

## 5. 时间控制 UI（WebClient）

| 元素 | 行为 |
|------|------|
| **战略** | 暂停自动 `advance-day` |
| **进行** | 按左上角 ▶/▶▶/▶▶▶ 倍速循环推进 |
| **倍速** | 1×≈2s/日，2×≈1s/日，4×≈0.5s/日（`AUTO_DAY_BASE_MS=2000`） |
| **失败** | API 推进失败自动切回战略 |

详见 [`strategy-ui-design.md`](strategy-ui-design.md) §2.2 / §14.3。
