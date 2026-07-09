<script setup lang="ts">
import { computed } from "vue";
import type { StrategyMapColorMode } from "@/utils/mapEntityColors";

const props = defineProps<{
  modelValue: StrategyMapColorMode;
}>();

const emit = defineEmits<{
  "update:modelValue": [StrategyMapColorMode];
}>();

const mode = computed({
  get: () => props.modelValue,
  set: (value: StrategyMapColorMode) => emit("update:modelValue", value),
});
</script>

<template>
  <div class="map-view-controls" @pointerdown.stop @click.stop @wheel.stop>
    <span class="map-view-label">地图视图</span>
    <el-radio-group v-model="mode" size="small" class="map-view-radios">
      <el-radio-button label="Realm">势力</el-radio-button>
      <el-radio-button label="Force">封地</el-radio-button>
      <el-radio-button label="Diplomacy">外交</el-radio-button>
    </el-radio-group>
  </div>
</template>

<style scoped>
.map-view-controls {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 6px;
  padding: 8px 10px;
  border-radius: 8px;
  background: rgba(15, 23, 42, 0.88);
  border: 1px solid rgba(148, 163, 184, 0.35);
  backdrop-filter: blur(6px);
  pointer-events: auto;
  flex-shrink: 0;
}

.map-view-label {
  font-size: 0.72rem;
  color: #94a3b8;
  letter-spacing: 0.02em;
}

.map-view-radios :deep(.el-radio-button__inner) {
  padding: 5px 10px;
  font-size: 0.75rem;
}
</style>
