<script setup lang="ts">
import { computed, reactive, ref, watch } from "vue";
import {
  cloneDifficultyStartOptions,
  DEFAULT_INTEL_DEBUG_MODE,
  enforceCharacterFogControl,
  GAME_START_PRESETS,
  INTEL_DEBUG_START_OPTION_VISIBLE,
  resolveDifficultyFromOptions,
  type GameStartSettings,
  type PresetDifficultyId,
  type StrategyDifficultyId,
} from "@/utils/strategyGameStartSettings";
import {
  CONTROL_MODE_HINTS,
  FOG_MODE_HINTS,
  INSTANT_EVENT_MESSAGES_HINT,
  INTEL_MODE_HINTS,
  PRESET_SUMMARIES,
  resolveGameStartOptionUiRules,
} from "@/utils/strategyGameStartOptionRules";

export type { GameStartSettings, StrategyDifficultyId };

const props = defineProps<{
  visible: boolean;
  loading?: boolean;
  scenarioId?: string;
  allowCancel?: boolean;
}>();

const emit = defineEmits<{
  confirm: [settings: GameStartSettings];
  cancel: [];
}>();

const form = reactive<GameStartSettings>({
  scenarioId: props.scenarioId ?? "mini_kanto",
  difficulty: "Normal",
  customStartOptions: cloneDifficultyStartOptions(GAME_START_PRESETS.Normal),
  intelDebugMode: DEFAULT_INTEL_DEBUG_MODE,
  allForcesAiControlled: false,
});

const applyingPreset = ref(false);
const advancedExpanded = ref<string[]>([]);

watch(
  () => props.scenarioId,
  (id) => {
    if (id) form.scenarioId = id;
  },
);

watch(
  () => form.difficulty,
  (difficulty) => {
    if (difficulty === "Custom") {
      advancedExpanded.value = ["advanced"];
      return;
    }
    applyingPreset.value = true;
    const preservedDebugMode = form.intelDebugMode;
    Object.assign(form.customStartOptions, GAME_START_PRESETS[difficulty as PresetDifficultyId]);
    form.intelDebugMode = preservedDebugMode;
    applyingPreset.value = false;
  },
);

watch(
  () => ({ ...form.customStartOptions }),
  () => {
    if (applyingPreset.value) return;
    if (enforceCharacterFogControl(form.customStartOptions)) return;

    const resolved = resolveDifficultyFromOptions(form.customStartOptions);
    if (form.difficulty !== resolved) {
      form.difficulty = resolved;
      if (resolved === "Custom") advancedExpanded.value = ["advanced"];
    }
  },
  { deep: true },
);

const uiRules = computed(() =>
  resolveGameStartOptionUiRules({
    ...form.customStartOptions,
    intelDebugMode: form.intelDebugMode,
  }),
);

const presetSummary = computed(() => {
  if (form.difficulty === "Custom") return null;
  return PRESET_SUMMARIES[form.difficulty as PresetDifficultyId];
});

const fogModeHint = computed(
  () => FOG_MODE_HINTS[form.customStartOptions.fogMode] ?? "",
);

const intelModeHint = computed(
  () => INTEL_MODE_HINTS[form.customStartOptions.intelMode] ?? "",
);

const controlModeHint = computed(
  () => CONTROL_MODE_HINTS[form.customStartOptions.controlMode] ?? "",
);

const fogModeOptions = [
  { label: "无迷雾", value: "None" },
  { label: "势力迷雾", value: "Force" },
  { label: "角色视野", value: "Character" },
];

const intelModeOptions = [
  { label: "显示所有情报", value: "Full" },
  { label: "仅显示已知情报", value: "ForceIntel" },
];

const controlModeOptions = [
  { label: "全势力单位", value: "FullDirect" },
  { label: "仅角色", value: "DirectiveOnly" },
];

const difficultyOptions = [
  { label: "简易", value: "Easy" },
  { label: "标准", value: "Normal" },
  { label: "困难", value: "Hard" },
  { label: "自定义", value: "Custom" },
];

function onConfirm() {
  const options = cloneDifficultyStartOptions(form.customStartOptions);
  enforceCharacterFogControl(options);
  const difficulty = resolveDifficultyFromOptions(options);

  emit("confirm", {
    scenarioId: form.scenarioId,
    difficulty,
    customStartOptions: options,
    intelDebugMode: form.intelDebugMode,
    allForcesAiControlled: form.allForcesAiControlled,
  });
}

function onClose() {
  if (props.allowCancel) emit("cancel");
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    title="游戏设置"
    width="560px"
    :close-on-click-modal="allowCancel"
    :close-on-press-escape="allowCancel"
    :show-close="allowCancel"
    @close="onClose"
  >
    <el-form label-width="96px" label-position="left" class="start-form">
      <el-form-item label="游戏难度" class="compact-item">
        <div class="control-stack">
          <el-radio-group v-model="form.difficulty" class="option-radios">
            <el-radio
              v-for="opt in difficultyOptions"
              :key="opt.value"
              :value="opt.value"
            >
              {{ opt.label }}
            </el-radio>
          </el-radio-group>
          <p v-if="presetSummary" class="field-hint">{{ presetSummary }}</p>
          <p v-else class="field-hint">当前选项与预设不完全一致。</p>
        </div>
      </el-form-item>

      <el-form-item label="游玩方式" class="compact-item">
        <div class="control-stack">
          <div class="sub-option">
            <div class="sub-option-row">
              <span class="sub-option-label">全势力 AI 观战</span>
              <el-switch v-model="form.allForcesAiControlled" />
            </div>
          </div>
          <p class="field-hint">
            开启后本家也由 AI 接管，适合观看战局、长局压测与检验数值平衡。
          </p>
        </div>
      </el-form-item>

      <el-form-item
        v-if="INTEL_DEBUG_START_OPTION_VISIBLE"
        label="调试模式"
        class="compact-item"
      >
        <div class="control-stack">
          <div class="sub-option-row">
            <span class="sub-option-label">情报调试</span>
            <el-switch v-model="form.intelDebugMode" />
          </div>
          <p class="field-hint">
            开启后情报对话框显示「调试模式」checkbox；该 checkbox 才控制隐藏人物与性情等字段的显示。
          </p>
        </div>
      </el-form-item>

      <el-collapse v-model="advancedExpanded" class="advanced-collapse">
        <el-collapse-item name="advanced" title="高级设置">
          <el-form-item label="地图视野" class="compact-item nested-item">
            <div class="control-stack">
              <el-radio-group v-model="form.customStartOptions.fogMode" class="option-radios">
                <el-radio v-for="o in fogModeOptions" :key="o.value" :value="o.value">
                  {{ o.label }}
                </el-radio>
              </el-radio-group>
              <p class="field-hint">{{ fogModeHint }}</p>
              <div v-if="uiRules.showAllySharedVision" class="sub-option">
                <div class="sub-option-row">
                  <span class="sub-option-label">同盟共享视野</span>
                  <el-switch v-model="form.customStartOptions.allySharedVision" />
                </div>
              </div>
              <div v-if="uiRules.showCharacterSharedVision" class="sub-option">
                <div class="sub-option-row">
                  <span class="sub-option-label">角色共享视野</span>
                  <el-switch v-model="form.customStartOptions.characterSharedVision" />
                </div>
                <p class="field-hint muted">
                  地图上的 AI 角色 avatar 是否扩视野；玩家当主在任何模式下均提供视野。
                </p>
              </div>
            </div>
          </el-form-item>

          <el-form-item label="情报谍报" class="compact-item nested-item">
            <div class="control-stack">
              <el-radio-group v-model="form.customStartOptions.intelMode" class="option-radios">
                <el-radio v-for="o in intelModeOptions" :key="o.value" :value="o.value">
                  {{ o.label }}
                </el-radio>
              </el-radio-group>
              <p class="field-hint">{{ intelModeHint }}</p>
              <div v-if="uiRules.showAllyIntel" class="sub-option">
                <div class="sub-option-row">
                  <span class="sub-option-label">显示同盟情报</span>
                  <el-switch v-model="form.customStartOptions.showAllyIntel" />
                </div>
              </div>
            </div>
          </el-form-item>

          <el-form-item label="对象控制" class="compact-item nested-item">
            <div class="control-stack">
              <el-radio-group
                v-if="uiRules.showControlMode"
                v-model="form.customStartOptions.controlMode"
                class="option-radios"
              >
                <el-radio v-for="o in controlModeOptions" :key="o.value" :value="o.value">
                  {{ o.label }}
                </el-radio>
              </el-radio-group>
              <p v-if="uiRules.showControlMode" class="field-hint">{{ controlModeHint }}</p>
              <p v-else class="field-hint muted">{{ uiRules.controlModeLockedHint }}</p>
            </div>
          </el-form-item>

          <el-form-item label="事件消息" class="compact-item nested-item">
            <div class="control-stack">
              <div class="sub-option">
                <div class="sub-option-row">
                  <span class="sub-option-label">即时事件摘要</span>
                  <el-switch v-model="form.customStartOptions.instantEventMessages" />
                </div>
              </div>
              <p class="field-hint">{{ INSTANT_EVENT_MESSAGES_HINT }}</p>
            </div>
          </el-form-item>
        </el-collapse-item>
      </el-collapse>

      <p class="preset-hint">
        修改任一高级选项后，若与简易/标准/困难不完全一致，难度会自动标记为「自定义」。
      </p>
    </el-form>

    <template #footer>
      <el-button v-if="allowCancel" @click="emit('cancel')">取消</el-button>
      <el-button type="primary" :loading="loading" @click="onConfirm">开始游戏</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.start-form {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.compact-item {
  margin-bottom: 8px;
  align-items: flex-start;
}

.compact-item :deep(.el-form-item__label) {
  font-weight: 600;
  color: var(--el-text-color-primary);
  line-height: 1.4;
  height: auto;
  padding-top: 2px;
  padding-right: 8px;
  align-self: flex-start;
}

.compact-item :deep(.el-form-item__content) {
  line-height: 1.4;
  justify-content: flex-start;
  align-items: flex-start;
  text-align: left;
}

.control-stack {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 6px;
  width: 100%;
  text-align: left;
}

.nested-item:first-of-type {
  margin-top: 4px;
}

.option-radios {
  display: flex;
  flex-direction: row;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px 14px;
}

.option-radios :deep(.el-radio) {
  margin-right: 0;
  height: auto;
}

.sub-option {
  width: 100%;
  padding: 6px 10px;
  border-radius: 6px;
  background: var(--el-fill-color-light);
  text-align: left;
}

.sub-option-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.sub-option-label {
  font-size: 13px;
  color: var(--el-text-color-regular);
}

.field-hint {
  margin: 0;
  width: 100%;
  font-size: 12px;
  line-height: 1.4;
  text-align: left;
  color: var(--el-text-color-secondary);
}

.field-hint.muted {
  color: var(--el-text-color-placeholder);
}

.preset-hint {
  margin: 8px 0 0;
  width: 100%;
  font-size: 12px;
  text-align: left;
  color: var(--el-text-color-secondary);
  line-height: 1.4;
}

.advanced-collapse {
  border: none;
  margin-top: 4px;
}

.advanced-collapse :deep(.el-collapse-item__header) {
  font-size: 14px;
  font-weight: 600;
  border-bottom: none;
  height: 32px;
  line-height: 32px;
  padding-left: 0;
}

.advanced-collapse :deep(.el-collapse-item__wrap) {
  border-bottom: none;
}

.advanced-collapse :deep(.el-collapse-item__content) {
  padding: 0 0 4px;
}
</style>
