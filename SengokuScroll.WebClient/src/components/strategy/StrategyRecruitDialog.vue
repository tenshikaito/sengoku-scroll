<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { StrategyStrongholdState } from "@/api/strategy";

const props = defineProps<{
  visible: boolean;
  stronghold: StrategyStrongholdState | null;
  maxRecruitable?: number;
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
  confirm: [payload: { soldiers: number }];
}>();

const soldiers = ref(100);

watch(
  () => [props.visible, props.maxRecruitable] as const,
  ([visible, max]) => {
    if (!visible) return;
    soldiers.value = Math.min(100, max ?? 100);
  }
);

const cappedMax = computed(() => Math.max(0, props.maxRecruitable ?? 0));

function close() {
  emit("update:visible", false);
}

function submit() {
  if (soldiers.value <= 0 || soldiers.value > cappedMax.value) return;
  emit("confirm", { soldiers: soldiers.value });
  close();
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="stronghold ? `征兵 — ${stronghold.name}` : '征兵'"
    width="380px"
    append-to-body
    class="strategy-dialog-centered-footer"
    @update:model-value="emit('update:visible', $event)"
  >
    <p v-if="stronghold" class="hint">
      当前城内兵 {{ stronghold.garrisonSoldiers }} · 人口 {{ stronghold.population.toLocaleString() }}
    </p>
    <p class="hint">消耗人口、钱粮；当主须在居城下达。</p>
    <el-form label-width="80px">
      <el-form-item label="征兵数">
        <el-input-number v-model="soldiers" :min="1" :max="Math.max(1, cappedMax)" />
      </el-form-item>
    </el-form>
    <p class="hint subtle">本次最多可征 {{ cappedMax }} 人</p>
    <template #footer>
      <el-button type="default" @click="close">取消</el-button>
      <el-button type="primary" :disabled="!stronghold || cappedMax <= 0" @click="submit">确认</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.hint {
  margin: 0 0 12px;
  font-size: 0.82rem;
  color: #64748b;
  line-height: 1.45;
}

.hint.subtle {
  margin-top: 8px;
  margin-bottom: 0;
}
</style>
