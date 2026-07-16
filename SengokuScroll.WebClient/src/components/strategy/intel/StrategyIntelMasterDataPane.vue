<script setup lang="ts">
import { computed, ref } from "vue";
import type { StrategyWorldState } from "@/api/strategy";
import StrategyIntelSystemTable from "../StrategyIntelSystemTable.vue";
import {
  MASTER_DATA_LIST_COLUMN_PRESETS,
  masterDataPresetTabs,
  type MasterDataListPreset,
} from "@/utils/strategyIntelSystemColumns";
import { masterDataIntelRows } from "@/utils/strategyIntelSystemData";

const props = defineProps<{
  worldState: StrategyWorldState;
}>();

const presetTabs = masterDataPresetTabs();
const listPreset = ref<MasterDataListPreset>(presetTabs[0]?.name ?? "cultureGroups");

const listColumns = computed(() => MASTER_DATA_LIST_COLUMN_PRESETS[listPreset.value]);
const listRows = computed(
  () =>
    masterDataIntelRows(props.worldState, listPreset.value) as unknown as Array<
      Record<string, unknown>
    >
);
</script>

<template>
  <div class="intel-pane">
    <p class="master-hint">以下为当前剧本 Master Data 全量快照，便于删减字段与分类。</p>

    <el-tabs v-model="listPreset" class="layer-tabs layer-tabs--list master-tabs">
      <el-tab-pane
        v-for="tab in presetTabs"
        :key="tab.name"
        :label="tab.label"
        :name="tab.name"
      />
    </el-tabs>

    <StrategyIntelSystemTable
      :rows="listRows"
      :columns="listColumns"
      :highlight-current="false"
      empty-text="暂无 Master Data"
      :max-height="360"
    />
  </div>
</template>

<style scoped>
.intel-pane {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.master-hint {
  margin: 0;
  font-size: 0.8rem;
  color: #64748b;
}

.layer-tabs :deep(.el-tabs__header) {
  margin-bottom: 8px;
}

.layer-tabs :deep(.el-tabs__item) {
  font-size: 0.82rem;
}

.master-tabs :deep(.el-tabs__nav-wrap) {
  overflow-x: auto;
}
</style>
