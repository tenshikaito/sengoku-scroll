<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { StrategyStrongholdState } from "@/api/strategy";
import type { StrategyWorldState } from "@/api/strategyTypes";
import { canLordCommandStronghold } from "@/utils/strategyPlayerCharacter";
import { canConfigureStrongholdGovernancePolicy } from "@/utils/strategyGovernancePolicy";

export type StrongholdGovernancePriorityValue = "Autonomous" | "Military" | "Domestic";

const props = defineProps<{
  visible: boolean;
  stronghold: StrategyStrongholdState | null;
  worldState: StrategyWorldState | null;
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
  confirm: [priority: StrongholdGovernancePriorityValue];
}>();

const priority = ref<StrongholdGovernancePriorityValue>("Autonomous");
const baseline = ref<StrongholdGovernancePriorityValue>("Autonomous");

const canConfigure = computed(() => {
  if (!props.worldState || !props.stronghold) return false;
  if (!canLordCommandStronghold(props.worldState, props.stronghold)) return false;
  return canConfigureStrongholdGovernancePolicy(props.worldState, props.stronghold);
});

watch(
  () => [props.visible, props.stronghold?.id] as const,
  ([visible, id]) => {
    if (!visible || !props.stronghold || id == null) return;
    const current = normalizePriority(props.stronghold.governancePriority);
    priority.value = current;
    baseline.value = current;
  },
);

const hasChange = computed(() => priority.value !== baseline.value);

function normalizePriority(value: string | undefined): StrongholdGovernancePriorityValue {
  if (value === "Military") return "Military";
  if (value === "Domestic") return "Domestic";
  return "Autonomous";
}

function close() {
  emit("update:visible", false);
}

function submit() {
  if (!hasChange.value || !canConfigure.value) return;
  emit("confirm", priority.value);
  close();
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="stronghold ? `方针 — ${stronghold.name}` : '方针'"
    width="360px"
    append-to-body
    class="strategy-dialog-centered-footer"
    @update:model-value="emit('update:visible', $event)"
  >
    <el-radio-group v-model="priority" class="priority-group" :disabled="!canConfigure">
      <el-radio value="Autonomous">自由决策</el-radio>
      <p class="option-desc">代官/领主根据据点状况与能力性格自动决定命令</p>
      <el-radio value="Military">军备优先</el-radio>
      <p class="option-desc">优先实行军备</p>
      <el-radio value="Domestic">内政优先</el-radio>
      <p class="option-desc">优先实行内政开发</p>
    </el-radio-group>

    <template #footer>
      <el-button type="default" @click="close">取消</el-button>
      <el-button type="primary" :disabled="!stronghold || !hasChange || !canConfigure" @click="submit">
        确认
      </el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.priority-group {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 4px;
}

.option-desc {
  margin: 0 0 10px 24px;
  font-size: 0.78rem;
  color: #64748b;
  line-height: 1.45;
}
</style>
