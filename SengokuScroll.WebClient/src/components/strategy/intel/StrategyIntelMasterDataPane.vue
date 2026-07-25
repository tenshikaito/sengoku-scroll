<script setup lang="ts">
import { computed, ref, watch } from "vue";
import type { StrategyWorldState } from "@/api/strategy";
import StrategyIntelSystemTable from "../StrategyIntelSystemTable.vue";
import {
  MASTER_DATA_LIST_COLUMN_PRESETS,
  type MasterDataListPreset,
} from "@/utils/strategyIntelSystemColumns";
import { masterDataIntelRows } from "@/utils/strategyIntelSystemData";
import type {
  IntelExcludeEntity,
  IntelNavigateRequest,
  IntelNavigateTarget,
} from "@/utils/strategyIntelNavigation";

const props = defineProps<{
  worldState: StrategyWorldState;
  preset: MasterDataListPreset;
  navigateRequest?: IntelNavigateRequest | null;
}>();

const emit = defineEmits<{
  navigate: [target: IntelNavigateTarget];
}>();

const selectedRowId = ref<number | null>(null);

const listColumns = computed(() => MASTER_DATA_LIST_COLUMN_PRESETS[props.preset]);
const listRows = computed(
  () =>
    masterDataIntelRows(props.worldState, props.preset) as unknown as Array<
      Record<string, unknown>
    >
);

const excludeEntity = computed<IntelExcludeEntity | null>(() =>
  selectedRowId.value != null
    ? {
        kind: "masterData",
        entityId: selectedRowId.value,
        masterPreset: props.preset,
      }
    : null,
);

function onIntelNavigate(target: IntelNavigateTarget) {
  emit("navigate", target);
}

function onSelectRow(row: Record<string, unknown> | null) {
  selectedRowId.value = row ? Number(row.id) : null;
}

watch(
  () => props.preset,
  () => {
    selectedRowId.value = null;
  },
);

watch(
  () => props.navigateRequest,
  (request) => {
    if (!request || request.kind !== "masterData") return;
    if (request.masterPreset !== props.preset) return;
    selectedRowId.value = request.entityId;
  },
);
</script>

<template>
  <div class="intel-pane">
    <StrategyIntelSystemTable
      :rows="listRows"
      :columns="listColumns"
      :current-id="selectedRowId"
      :exclude-entity="excludeEntity"
      :highlight-current="false"
      empty-text="暂无 Master Data"
      :max-height="360"
      @current-change="onSelectRow"
      @navigate="onIntelNavigate"
    />
  </div>
</template>

<style scoped>
.intel-pane {
  display: flex;
  flex-direction: column;
  gap: 10px;
}
</style>
