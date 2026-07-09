<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { StrategyLordState, StrategyStrongholdState, StrategyUnitState } from "@/api/strategy";
import {
  UNIT_DIRECTIVE_OPTIONS,
  type UnitDirectiveValue,
} from "@/utils/unitDirective";

const props = defineProps<{
  visible: boolean;
  unit: StrategyUnitState | null;
  lord: StrategyLordState | null;
  strongholds: StrategyStrongholdState[];
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
  confirm: [payload: { directive: UnitDirectiveValue }];
}>();

const selected = ref<UnitDirectiveValue>("Move");

const lordAtUnit = computed(() => {
  if (!props.unit || !props.lord) return false;
  return props.lord.x === props.unit.x && props.lord.y === props.unit.y;
});

const lordLocationText = computed(() => {
  if (!props.lord) return "";
  if (props.lord.unitId === props.unit?.id) return `当主 ${props.lord.name} 与本队同格`;
  const stronghold = props.strongholds.find(
    (s) => s.x === props.lord!.x && s.y === props.lord!.y && s.forceId === props.unit?.forceId
  );
  if (stronghold) return `当主 ${props.lord.name} 在据点「${stronghold.name}」(${props.lord.x}, ${props.lord.y})`;
  return `当主 ${props.lord.name} 在 (${props.lord.x}, ${props.lord.y})`;
});

watch(
  () => [props.visible, props.unit?.id, props.unit?.directive] as const,
  ([visible, , directive]) => {
    if (!visible || !props.unit) return;
    selected.value = (directive as UnitDirectiveValue) ?? "Move";
  }
);

function close() {
  emit("update:visible", false);
}

function submit() {
  emit("confirm", { directive: selected.value });
  close();
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="unit ? `设定方针 — ${unit.name}` : '设定方针'"
    width="420px"
    append-to-body
    @update:model-value="emit('update:visible', $event)"
  >
    <p v-if="unit" class="hint">部队位置 ({{ unit.x }}, {{ unit.y }})</p>
    <p v-if="lord" class="hint">{{ lordLocationText }}</p>

    <el-radio-group v-model="selected" class="directive-list">
      <el-radio
        v-for="opt in UNIT_DIRECTIVE_OPTIONS"
        :key="opt.value"
        :value="opt.value"
        class="directive-item"
      >
        <span class="label">{{ opt.label }}</span>
        <span class="desc">{{ opt.description }}</span>
      </el-radio>
    </el-radio-group>

    <p v-if="lordAtUnit" class="hint">当主与本队同格，方针即时生效。</p>
    <p v-else-if="lord" class="hint">方针将从当主所在格派出信使，到达后生效。</p>

    <template #footer>
      <el-button type="default" @click="close">取消</el-button>
      <el-button type="primary" :disabled="!unit" @click="submit">确认</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.hint {
  margin: 0 0 12px;
  font-size: 0.82rem;
  color: #64748b;
}

.directive-list {
  display: flex;
  flex-direction: column;
  align-items: stretch;
  gap: 8px;
  width: 100%;
}

.directive-item {
  display: flex;
  align-items: flex-start;
  height: auto;
  margin: 0;
  padding: 8px 10px;
  border: 1px solid #e2e8f0;
  border-radius: 6px;
}

.directive-item :deep(.el-radio__label) {
  display: flex;
  flex-direction: column;
  gap: 2px;
  white-space: normal;
  line-height: 1.35;
}

.label {
  font-weight: 600;
  color: #1e293b;
}

.desc {
  font-size: 0.78rem;
  color: #64748b;
}
</style>
