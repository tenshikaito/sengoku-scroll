<script setup lang="ts">
import { ref, watch } from "vue";
import type { StrategyWorldState } from "@/api/strategy";
import StrategyIntelForcePane from "./intel/StrategyIntelForcePane.vue";
import StrategyIntelMasterDataPane from "./intel/StrategyIntelMasterDataPane.vue";
import StrategyIntelPersonPane from "./intel/StrategyIntelPersonPane.vue";
import StrategyIntelStrongholdPane from "./intel/StrategyIntelStrongholdPane.vue";
import {
  INTEL_REALM_FILTER_OPTIONS,
  type IntelRealmFilterMode,
} from "@/utils/intelRealmFilter";

const props = defineProps<{
  visible: boolean;
  worldState: StrategyWorldState | null;
  /** 打开时默认选中的 Tab（force | stronghold | person | character）。 */
  initialTab?: string;
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
}>();

function normalizeTab(tab: string | undefined): string {
  if (tab === "character") return "person";
  return tab ?? "force";
}

const activeTab = ref(normalizeTab(props.initialTab));
const realmFilter = ref<IntelRealmFilterMode>("all");

watch(
  () => props.visible,
  (open) => {
    if (open) {
      activeTab.value = normalizeTab(props.initialTab);
      realmFilter.value = "all";
    }
  }
);

watch(
  () => props.initialTab,
  (tab) => {
    if (tab) activeTab.value = normalizeTab(tab);
  }
);

function close() {
  emit("update:visible", false);
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    title="情报"
    width="min(920px, 96vw)"
    append-to-body
    destroy-on-close
    class="intel-system-dialog strategy-dialog-centered-footer"
    @update:model-value="emit('update:visible', $event)"
  >
    <template v-if="worldState">
      <div class="intel-filter-bar">
        <el-radio-group v-model="realmFilter" size="small" class="intel-realm-filter">
          <el-radio-button
            v-for="option in INTEL_REALM_FILTER_OPTIONS"
            :key="option.value"
            :value="option.value"
          >
            {{ option.label }}
          </el-radio-button>
        </el-radio-group>
      </div>

      <el-tabs v-model="activeTab" class="intel-tabs">
        <el-tab-pane label="势力" name="force">
          <StrategyIntelForcePane :world-state="worldState" :realm-filter="realmFilter" />
        </el-tab-pane>

        <el-tab-pane label="据点" name="stronghold">
          <StrategyIntelStrongholdPane
            :world-state="worldState"
            :realm-filter="realmFilter"
          />
        </el-tab-pane>

        <el-tab-pane label="人物" name="person">
          <StrategyIntelPersonPane :world-state="worldState" :realm-filter="realmFilter" />
        </el-tab-pane>

        <el-tab-pane label="Master Data" name="masterData">
          <StrategyIntelMasterDataPane :world-state="worldState" />
        </el-tab-pane>
      </el-tabs>
    </template>

    <p v-else class="placeholder">暂无世界状态，无法加载情报。</p>

    <template #footer>
      <el-button type="primary" @click="close">关闭</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.intel-filter-bar {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 10px;
}

.filter-hint {
  display: none;
}

.intel-realm-filter :deep(.el-radio-button__inner) {
  font-size: 0.82rem;
  padding: 6px 10px;
}

.filter-hint {
  font-size: 0.78rem;
  color: #64748b;
}

.intel-tabs :deep(.el-tabs__header) {
  margin-bottom: 12px;
}

.intel-tabs :deep(.el-tabs__item) {
  font-size: 0.9rem;
}

.placeholder {
  margin: 8px 0 0;
  font-size: 0.88rem;
  color: #64748b;
  line-height: 1.5;
}

.intel-system-dialog :deep(.el-dialog__title) {
  color: #0f172a;
}
</style>
