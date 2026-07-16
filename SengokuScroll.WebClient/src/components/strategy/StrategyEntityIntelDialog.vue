<script setup lang="ts">
import type {
  StrategyMessengerState,
  StrategyStrongholdState,
  StrategySupplyConvoyState,
  StrategyUnitState,
  StrategyWorldState,
} from "@/api/strategy";
import StrategyConvoyIntelDetail from "./StrategyConvoyIntelDetail.vue";
import StrategyMessengerIntelDetail from "./StrategyMessengerIntelDetail.vue";
import StrategyStrongholdIntelDetail from "./StrategyStrongholdIntelDetail.vue";
import StrategyUnitIntelDetail from "./StrategyUnitIntelDetail.vue";

export type EntityIntelTarget =
  | { kind: "unit"; unit: StrategyUnitState }
  | { kind: "stronghold"; stronghold: StrategyStrongholdState }
  | { kind: "convoy"; convoy: StrategySupplyConvoyState }
  | { kind: "messenger"; messenger: StrategyMessengerState };

defineProps<{
  visible: boolean;
  worldState: StrategyWorldState;
  target: EntityIntelTarget | null;
}>();

defineEmits<{
  "update:visible": [value: boolean];
}>();
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="
      target?.kind === 'unit'
        ? '单位情报'
        : target?.kind === 'stronghold'
          ? '据点情报'
          : target?.kind === 'convoy'
            ? '运输队情报'
            : target?.kind === 'messenger'
              ? '信使情报'
              : '情报'
    "
    width="min(720px, 92vw)"
    align-center
    destroy-on-close
    modal-class="entity-intel-dialog-modal"
    class="entity-intel-dialog strategy-dialog-centered-footer"
    @update:model-value="$emit('update:visible', $event)"
  >
    <StrategyUnitIntelDetail
      v-if="target?.kind === 'unit'"
      :world-state="worldState"
      :unit="target.unit"
    />
    <StrategyStrongholdIntelDetail
      v-else-if="target?.kind === 'stronghold'"
      :world-state="worldState"
      :stronghold="target.stronghold"
    />
    <StrategyConvoyIntelDetail
      v-else-if="target?.kind === 'convoy'"
      :world-state="worldState"
      :convoy="target.convoy"
    />
    <StrategyMessengerIntelDetail
      v-else-if="target?.kind === 'messenger'"
      :world-state="worldState"
      :messenger="target.messenger"
    />
    <template #footer>
      <el-button type="primary" @click="$emit('update:visible', false)">关闭</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.entity-intel-dialog :deep(.el-dialog__title) {
  color: #0f172a;
}
</style>

<style>
/* 遮罩层拦截指针移动，避免 window 级悬停监听误更新地图格点。 */
.entity-intel-dialog-modal {
  pointer-events: auto;
}
</style>
