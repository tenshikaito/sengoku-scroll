import type { GameStartOptionsState } from "@/api/strategyTypes";
import type { PresetDifficultyId } from "@/utils/strategyGameStartSettings";

/** 开局选项 UI 可见性与说明（与 enforceCharacterFogControl 对齐）。 */
export interface GameStartOptionUiRules {
  showAllySharedVision: boolean;
  showCharacterSharedVision: boolean;
  showAllyIntel: boolean;
  showControlMode: boolean;
  controlModeLockedHint: string | null;
}

export function resolveGameStartOptionUiRules(
  options: GameStartOptionsState,
): GameStartOptionUiRules {
  const isCharacterFog = options.fogMode === "Character";
  const isForceFog = options.fogMode === "Force";
  const isForceIntel = options.intelMode === "ForceIntel";

  return {
    showAllySharedVision: isForceFog,
    showCharacterSharedVision: isForceFog,
    showAllyIntel: isForceIntel,
    showControlMode: !isCharacterFog,
    controlModeLockedHint: isCharacterFog ? "角色视野下固定为「仅角色」。" : null,
  };
}

export const PRESET_SUMMARIES: Record<PresetDifficultyId, string> = {
  Easy: "无迷雾 · 全情报 · 全控 · 即时消息",
  Normal: "势力迷雾 · 已知情报 · 仅角色指挥",
  Hard: "角色视野 · 已知情报 · 仅角色指挥",
};

export const FOG_MODE_HINTS: Record<string, string> = {
  None: "全图可见，适合熟悉地图。",
  Force: "本势力视野聚合；可选项下可让同盟共享开图。",
  Character: "仅当主与同行部队提供视野；对象控制固定为仅角色。",
};

export const INTEL_MODE_HINTS: Record<string, string> = {
  Full: "敌方据点与部队数值完全可见。",
  ForceIntel: "非己方须谍报；可选项下同盟及内藩可见具体数值。",
};

export const CONTROL_MODE_HINTS: Record<string, string> = {
  FullDirect: "可直控本家兵队移动与攻城；内藩兵队仍仅方针。",
  DirectiveOnly: "仅当主所在格或领兵时可直控；其余本家兵队仅方针/姿态。",
};

export const INSTANT_EVENT_MESSAGES_HINT =
  "开启后当日战报与事件在消息区即时摘要；完整详情仍经信使抵达后推送。";
