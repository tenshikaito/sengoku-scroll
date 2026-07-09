<script setup lang="ts">
import { computed } from "vue";
import type {
  StrategyMessengerState,
  StrategyStrongholdState,
  StrategySupplyConvoyState,
  StrategyUnitState,
  StrategyWorldState,
} from "@/api/strategy";
import type { AnchorSide } from "@/utils/mapCellAnchor";
import StrategyConvoyIntelSummary from "./StrategyConvoyIntelSummary.vue";
import StrategyMessengerIntelSummary from "./StrategyMessengerIntelSummary.vue";
import StrategyStrongholdIntelSummary from "./StrategyStrongholdIntelSummary.vue";
import StrategyUnitIntelSummary from "./StrategyUnitIntelSummary.vue";

const props = defineProps<{
  worldState: StrategyWorldState;
  x: number;
  y: number;
  /** 悬浮框相对格块的锚定方向，决定第二框的排列方向。 */
  anchorSide?: AnchorSide;
  /** 左右锚定时与格块的竖向对齐（start=顶，end=底）。 */
  verticalAlign?: "start" | "end";
}>();

type StrongholdEntry = { kind: "stronghold"; key: string; stronghold: StrategyStrongholdState };

type OtherEntry =
  | { kind: "unit"; key: string; unit: StrategyUnitState }
  | { kind: "convoy"; key: string; convoy: StrategySupplyConvoyState }
  | { kind: "messenger"; key: string; messenger: StrategyMessengerState };

function atCell<T extends { x: number; y: number }>(items: T[]) {
  return items.filter((item) => item.x === props.x && item.y === props.y);
}

const strongholdEntries = computed((): StrongholdEntry[] =>
  atCell(props.worldState.strongholds).map((stronghold) => ({
    kind: "stronghold" as const,
    key: `sh-${stronghold.id}`,
    stronghold,
  }))
);

const otherEntries = computed((): OtherEntry[] => {
  const list: OtherEntry[] = [];
  for (const unit of atCell(props.worldState.units)) {
    list.push({ kind: "unit", key: `u-${unit.id}`, unit });
  }
  for (const convoy of atCell(props.worldState.supplyConvoys)) {
    list.push({ kind: "convoy", key: `c-${convoy.id}`, convoy });
  }
  for (const messenger of atCell(props.worldState.messengers)) {
    list.push({ kind: "messenger", key: `m-${messenger.id}`, messenger });
  }
  return list;
});

const totalCount = computed(
  () => strongholdEntries.value.length + otherEntries.value.length
);

const dualLayout = computed(
  () => strongholdEntries.value.length > 0 && otherEntries.value.length > 0
);

const stackClass = computed(() => {
  const side = props.anchorSide ?? "right";
  const vAlign = props.verticalAlign ?? "start";
  return [
    "cell-intel-stack",
    `cell-intel-stack--${side}`,
    dualLayout.value ? "cell-intel-stack--dual" : "",
    dualLayout.value && vAlign === "end" ? "cell-intel-stack--valign-end" : "",
  ];
});
</script>

<template>
  <div :class="stackClass">
    <!-- 据点 + 同格其他实体：两框横向并排（据点靠格、其他向外） -->
    <template v-if="dualLayout">
      <div class="intel-box intel-box--stronghold" aria-label="据点">
        <div class="coord">格点 ({{ x }}, {{ y }}) · {{ totalCount }} 项</div>
        <div class="panel-title">据点</div>
        <template v-for="(entry, index) in strongholdEntries" :key="entry.key">
          <div v-if="index > 0" class="entity-divider" role="separator" />
          <div class="block">
            <StrategyStrongholdIntelSummary
              :world-state="worldState"
              :stronghold="entry.stronghold"
            />
          </div>
        </template>
      </div>

      <div class="intel-box intel-box--other" aria-label="单位与后勤">
        <div class="panel-title">单位</div>
        <template v-for="(entry, index) in otherEntries" :key="entry.key">
          <div v-if="index > 0" class="entity-divider" role="separator" />
          <div class="block">
            <StrategyUnitIntelSummary
              v-if="entry.kind === 'unit'"
              :world-state="worldState"
              :unit="entry.unit"
            />
            <StrategyConvoyIntelSummary
              v-else-if="entry.kind === 'convoy'"
              :world-state="worldState"
              :convoy="entry.convoy"
            />
            <StrategyMessengerIntelSummary
              v-else-if="entry.kind === 'messenger'"
              :world-state="worldState"
              :messenger="entry.messenger"
            />
          </div>
        </template>
      </div>
    </template>

    <!-- 单类实体：单个悬浮框 -->
    <div v-else class="intel-box">
      <div class="coord">格点 ({{ x }}, {{ y }}) · {{ totalCount }} 项</div>
      <template v-if="strongholdEntries.length">
        <template v-for="(entry, index) in strongholdEntries" :key="entry.key">
          <div v-if="index > 0" class="entity-divider" role="separator" />
          <div class="block">
            <StrategyStrongholdIntelSummary
              :world-state="worldState"
              :stronghold="entry.stronghold"
            />
          </div>
        </template>
      </template>
      <template v-else>
        <template v-for="(entry, index) in otherEntries" :key="entry.key">
          <div v-if="index > 0" class="entity-divider" role="separator" />
          <div class="block">
            <StrategyUnitIntelSummary
              v-if="entry.kind === 'unit'"
              :world-state="worldState"
              :unit="entry.unit"
            />
            <StrategyConvoyIntelSummary
              v-else-if="entry.kind === 'convoy'"
              :world-state="worldState"
              :convoy="entry.convoy"
            />
            <StrategyMessengerIntelSummary
              v-else-if="entry.kind === 'messenger'"
              :world-state="worldState"
              :messenger="entry.messenger"
            />
          </div>
        </template>
      </template>
    </div>
  </div>
</template>

<style scoped>
.cell-intel-stack {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  width: max-content;
}

.cell-intel-stack--dual {
  flex-direction: row;
}

.cell-intel-stack--dual.cell-intel-stack--left,
.cell-intel-stack--dual.cell-intel-stack--top {
  flex-direction: row-reverse;
}

/* 在格块下方/上方时：两框以底边对齐，避免矮框顶对齐产生与格块之间的空隙 */
.cell-intel-stack--dual.cell-intel-stack--bottom,
.cell-intel-stack--dual.cell-intel-stack--top,
.cell-intel-stack--dual.cell-intel-stack--valign-end {
  align-items: flex-end;
}

.coord {
  font-size: 0.72rem;
  color: #94a3b8;
  margin-bottom: 8px;
  padding-bottom: 6px;
  border-bottom: 1px solid #475569;
}

.panel-title {
  font-size: 0.68rem;
  font-weight: 600;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  color: #64748b;
  margin-bottom: 6px;
}

.intel-box--stronghold .panel-title {
  color: #7dd3fc;
}

.intel-box--other .panel-title {
  color: #fbbf24;
}

.entity-divider {
  height: 0;
  margin: 10px 0;
  border: none;
  border-top: 1px solid #94a3b8;
  opacity: 0.85;
}

.block {
  padding: 2px 0;
}
</style>

<style>
.cell-intel-stack .intel-box {
  width: max-content;
  max-width: min(280px, 38vw);
  padding: 10px 12px;
  box-sizing: border-box;
  background: #1e293b;
  border: 1px solid #38bdf8;
  border-radius: 8px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.5);
  overflow: visible;
  pointer-events: auto;
}

.cell-intel-stack .intel-box--stronghold {
  border-color: #38bdf8;
}

.cell-intel-stack .intel-box--other {
  border-color: #fbbf24;
}
</style>
