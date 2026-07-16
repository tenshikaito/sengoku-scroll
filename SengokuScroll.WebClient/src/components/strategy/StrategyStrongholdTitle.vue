<script setup lang="ts">
import { computed } from "vue";
import type { StrategyStrongholdState, StrategyWorldState } from "@/api/strategy";
import { strongholdGovernanceBadge } from "@/utils/strategyStrongholdLabels";

const props = defineProps<{
  stronghold: StrategyStrongholdState;
  worldState?: StrategyWorldState;
  /** 主标题字号（rem），副标记自动小一号。 */
  size?: "hover" | "dialog";
}>();

const badge = computed(() => strongholdGovernanceBadge(props.stronghold, props.worldState));
</script>

<template>
  <span class="sh-title" :class="size ?? 'hover'">
    <span class="sh-name">🏯 {{ stronghold.name }}</span>
    <span class="sh-badge">{{ badge }}</span>
  </span>
</template>

<style scoped>
.sh-title {
  display: inline-flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 0.35em 0.5em;
}

.sh-title.hover .sh-name {
  font-weight: 600;
  font-size: 0.88rem;
}

.sh-title.dialog .sh-name {
  font-weight: 600;
  font-size: 1rem;
}

.sh-badge {
  font-weight: 500;
  color: #94a3b8;
  white-space: nowrap;
}

.sh-title.hover .sh-badge {
  font-size: 0.76rem;
}

.sh-title.dialog .sh-badge {
  font-size: 0.82rem;
  color: #64748b;
}
</style>
