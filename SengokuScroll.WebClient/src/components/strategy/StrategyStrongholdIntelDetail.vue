<script setup lang="ts">
import { ref } from "vue";
import type { StrategyStrongholdState, StrategyWorldState } from "@/api/strategy";
import { getForceColorCss } from "./forceColors";
import StrategyIntelFieldList from "./StrategyIntelFieldList.vue";
import StrategyStrongholdTitle from "./StrategyStrongholdTitle.vue";
import {
  strongholdDefenseIntelRows,
  strongholdDetailIntelRows,
} from "@/utils/strategyIntelRows";

defineProps<{
  worldState: StrategyWorldState;
  stronghold: StrategyStrongholdState;
}>();

const activeTab = ref("basic");
</script>

<template>
  <div class="stronghold-intel">
    <div class="header">
      <span class="name" :style="{ color: getForceColorCss(stronghold.forceId) }">
        <StrategyStrongholdTitle :stronghold="stronghold" :world-state="worldState" size="dialog" />
      </span>
    </div>

    <el-tabs v-model="activeTab" class="intel-tabs">
      <el-tab-pane label="基本信息" name="basic">
        <StrategyIntelFieldList
          variant="dialog"
          :columns="3"
          :rows="strongholdDetailIntelRows(worldState, stronghold)"
        />
      </el-tab-pane>
      <el-tab-pane label="城防信息" name="defense">
        <StrategyIntelFieldList
          variant="dialog"
          :columns="1"
          label-width="5em"
          :rows="strongholdDefenseIntelRows(stronghold)"
        />
      </el-tab-pane>
    </el-tabs>
  </div>
</template>

<style scoped>
.stronghold-intel {
  color: #1e293b;
  font-size: 0.9rem;
  line-height: 1.45;
}

.header {
  margin-bottom: 4px;
}

.name {
  display: block;
}

.intel-tabs :deep(.el-tabs__header) {
  margin-bottom: 12px;
}

.intel-tabs :deep(.el-tabs__item) {
  font-size: 0.88rem;
}
</style>
