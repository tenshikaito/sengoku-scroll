/** @deprecated Import from `@/gameStartOptions/GameStartOptionsProfile` instead. */
export {
  enforceCharacterFogControl,
  resolveGameStartOptionUiRules,
} from "@/gameStartOptions/GameStartOptionsProfile";

export type { GameStartOptionUiRules } from "@/gameStartOptions/types";

export const PRESET_SUMMARIES = {
  Easy: "无迷雾 · 全情报 · 全控 · 即时消息",
  Normal: "势力迷雾 · 已知情报 · 仅角色指挥",
  Hard: "角色视野 · 已知情报 · 仅角色指挥",
} as const;

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
