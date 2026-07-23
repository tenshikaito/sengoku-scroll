<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { StrategyWorldState } from "@/api/strategy";
import StrategyIntelForcePane from "./intel/StrategyIntelForcePane.vue";
import StrategyIntelMasterDataPane from "./intel/StrategyIntelMasterDataPane.vue";
import StrategyIntelPersonPane from "./intel/StrategyIntelPersonPane.vue";
import StrategyIntelStrongholdPane from "./intel/StrategyIntelStrongholdPane.vue";
import {
  INTEL_REALM_FILTER_OPTIONS,
  type IntelRealmFilterMode,
} from "@/utils/intelRealmFilter";
import {
  isIntelDebugCheckboxVisible,
} from "@/utils/strategyIntelVisibility";
import "@/styles/intelCircleRadios.css";

const props = defineProps<{
  visible: boolean;
  worldState: StrategyWorldState | null;
  /** 打开时默认选中的 Tab（force | stronghold | person | character）。 */
  initialTab?: string;
  /** 打开时默认选中的势力范围过滤（all | realm | homeOnly）。 */
  initialRealmFilter?: IntelRealmFilterMode;
  /** 打开时预选实体 Id（据点/人物/势力）。 */
  initialSelectedEntityId?: number | null;
  /** 聚焦模式：仅显示当前实体的详情二级 Tab，隐藏总览列表与顶栏 Tab。 */
  focusMode?: boolean;
  /** 聚焦模式对话框标题。 */
  focusTitle?: string;
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
const intelDebugMode = ref(false);

const showIntelDebugCheckbox = computed(
  () => (props.worldState ? isIntelDebugCheckboxVisible(props.worldState) : false),
);

const dialogTitle = computed(() => {
  if (props.focusMode && props.focusTitle) return props.focusTitle;
  return "情报";
});

watch(
  () => props.visible,
  (open) => {
    if (open) {
      activeTab.value = normalizeTab(props.initialTab);
      realmFilter.value = props.initialRealmFilter ?? "all";
      intelDebugMode.value = false;
    }
  }
);

function close() {
  emit("update:visible", false);
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="dialogTitle"
    :width="focusMode ? 'min(640px, 92vw)' : 'min(920px, 96vw)'"
    append-to-body
    destroy-on-close
    class="intel-system-dialog strategy-dialog-centered-footer"
    @update:model-value="emit('update:visible', $event)"
  >
    <template v-if="worldState">
      <div v-if="!focusMode" class="intel-filter-bar">
        <el-radio-group v-model="realmFilter" class="intel-realm-filter intel-circle-radios">
          <el-radio
            v-for="option in INTEL_REALM_FILTER_OPTIONS"
            :key="option.value"
            :value="option.value"
          >
            {{ option.label }}
          </el-radio>
        </el-radio-group>
        <el-checkbox
          v-if="showIntelDebugCheckbox"
          v-model="intelDebugMode"
          class="intel-debug-checkbox"
        >
          调试模式
        </el-checkbox>
      </div>

      <template v-if="focusMode">
        <StrategyIntelForcePane
          v-if="activeTab === 'force'"
          :world-state="worldState"
          :realm-filter="realmFilter"
          :initial-selected-id="initialSelectedEntityId"
          :intel-debug-mode="intelDebugMode"
          detail-only
        />
        <StrategyIntelStrongholdPane
          v-else-if="activeTab === 'stronghold'"
          :world-state="worldState"
          :realm-filter="realmFilter"
          :initial-selected-id="initialSelectedEntityId"
          detail-only
        />
        <StrategyIntelPersonPane
          v-else-if="activeTab === 'person'"
          :world-state="worldState"
          :realm-filter="realmFilter"
          :initial-selected-id="initialSelectedEntityId"
          :intel-debug-mode="intelDebugMode"
          detail-only
        />
      </template>

      <el-tabs v-else v-model="activeTab" class="intel-tabs">
        <el-tab-pane label="势力" name="force">
          <StrategyIntelForcePane
            :world-state="worldState"
            :realm-filter="realmFilter"
            :intel-debug-mode="intelDebugMode"
          />
        </el-tab-pane>

        <el-tab-pane label="据点" name="stronghold">
          <StrategyIntelStrongholdPane
            :world-state="worldState"
            :realm-filter="realmFilter"
            :initial-selected-id="initialSelectedEntityId"
          />
        </el-tab-pane>

        <el-tab-pane label="人物" name="person">
          <StrategyIntelPersonPane
            :world-state="worldState"
            :realm-filter="realmFilter"
            :initial-selected-id="initialSelectedEntityId"
            :intel-debug-mode="intelDebugMode"
          />
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
  flex-wrap: wrap;
  gap: 12px;
  margin-bottom: 10px;
}

.intel-realm-filter {
  margin-bottom: 0;
}

.intel-debug-checkbox {
  flex-shrink: 0;
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
