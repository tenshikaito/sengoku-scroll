<script setup lang="ts">
import { computed } from "vue";
import type {
  StrategyMessengerState,
  StrategyStrongholdState,
  StrategySupplyConvoyState,
  StrategyUnitState,
  StrategyBattlefieldState,
  StrategyWorldState,
} from "@/api/strategy";
import type { AnchorSide } from "@/utils/mapCellAnchor";
import type { CompactCellEntityEntry } from "@/utils/strategyIntelRows";
import StrategyConvoyIntelSummary from "./StrategyConvoyIntelSummary.vue";
import StrategyIntelPanel from "./StrategyIntelPanel.vue";
import StrategyMessengerIntelSummary from "./StrategyMessengerIntelSummary.vue";
import StrategyStrongholdIntelSummary from "./StrategyStrongholdIntelSummary.vue";
import StrategyUnitIntelSummary from "./StrategyUnitIntelSummary.vue";
import StrategyBattlefieldIntelSummary from "./StrategyBattlefieldIntelSummary.vue";
import StrategyCellEntitiesCompactSummary from "./StrategyCellEntitiesCompactSummary.vue";

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

type MilitaryEntry = { kind: "unit"; key: string; unit: StrategyUnitState };

type CivilEntry =
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

const battlefieldAtCell = computed((): StrategyBattlefieldState | null => {
  const bf = props.worldState.battlefields?.find((b) => b.x === props.x && b.y === props.y);
  return bf ?? null;
});

const siegeBattlefield = computed((): StrategyBattlefieldState | null =>
  battlefieldAtCell.value?.kind === "Siege" ? battlefieldAtCell.value : null
);

const fieldBattlefield = computed((): StrategyBattlefieldState | null =>
  battlefieldAtCell.value?.kind === "Field" ? battlefieldAtCell.value : null
);

/** 兵队（军事单位）；已入战场的单位由战场面板汇总，未参战同格部队仍单独展示。 */
const militaryEntries = computed((): MilitaryEntry[] => {
  const bf = battlefieldAtCell.value;
  const units = atCell(props.worldState.units);
  if (!bf) {
    return units.map((unit) => ({
      kind: "unit" as const,
      key: `u-${unit.id}`,
      unit,
    }));
  }

  const inBattleIds = new Set(bf.unitIds ?? []);
  return units
    .filter((unit) => !inBattleIds.has(unit.id))
    .map((unit) => ({
      kind: "unit" as const,
      key: `u-${unit.id}`,
      unit,
    }));
});

/** 运输队、信使等非兵队实体。 */
const civilEntries = computed((): CivilEntry[] => {
  const list: CivilEntry[] = [];
  for (const convoy of atCell(props.worldState.supplyConvoys)) {
    list.push({ kind: "convoy", key: `c-${convoy.id}`, convoy });
  }
  for (const messenger of atCell(props.worldState.messengers)) {
    list.push({ kind: "messenger", key: `m-${messenger.id}`, messenger });
  }
  return list;
});

/** 同格全部可移动实体（兵队、运输、信使），按 id 排序。 */
const allEntityEntries = computed((): CompactCellEntityEntry[] => {
  const list: CompactCellEntityEntry[] = [];
  for (const entry of militaryEntries.value) {
    list.push({
      kind: "unit",
      key: entry.key,
      forceId: entry.unit.forceId,
      unit: entry.unit,
    });
  }
  for (const entry of civilEntries.value) {
    if (entry.kind === "convoy") {
      list.push({
        kind: "convoy",
        key: entry.key,
        forceId: entry.convoy.forceId,
        convoy: entry.convoy,
      });
    } else {
      list.push({
        kind: "messenger",
        key: entry.key,
        forceId: entry.messenger.forceId,
        messenger: entry.messenger,
      });
    }
  }
  return list.sort((a, b) => a.key.localeCompare(b.key));
});

/** 仅一个兵队且无其他同格实体时，保持原有详细悬浮布局。 */
const useCompactEntityPanel = computed(() => allEntityEntries.value.length > 1);

const singleMilitaryEntry = computed(() =>
  !useCompactEntityPanel.value && militaryEntries.value.length === 1
    ? militaryEntries.value[0]
    : null
);

const singleCivilEntry = computed(() =>
  !useCompactEntityPanel.value && civilEntries.value.length === 1 && militaryEntries.value.length === 0
    ? civilEntries.value[0]
    : null
);

const panelCount = computed(() => {
  let count = 0;
  if (strongholdEntries.value.length) count += 1;
  if (siegeBattlefield.value) count += 1;
  if (fieldBattlefield.value) count += 1;
  if (useCompactEntityPanel.value && allEntityEntries.value.length) count += 1;
  else {
    if (singleMilitaryEntry.value) count += 1;
    if (singleCivilEntry.value) count += 1;
  }
  return count;
});

const multiBoxLayout = computed(() => panelCount.value > 1);

const stackClass = computed(() => {
  const side = props.anchorSide ?? "right";
  const vAlign = props.verticalAlign ?? "start";
  return [
    "cell-intel-stack",
    `cell-intel-stack--${side}`,
    multiBoxLayout.value ? "cell-intel-stack--multi" : "",
    multiBoxLayout.value && vAlign === "end" ? "cell-intel-stack--valign-end" : "",
  ];
});
</script>

<template>
  <div :class="stackClass">
    <StrategyIntelPanel
      v-if="strongholdEntries.length"
      variant="stronghold"
      ariaLabel="据点情报"
    >
      <template v-for="(entry, index) in strongholdEntries" :key="entry.key">
        <div v-if="index > 0" class="entity-divider" role="separator" />
        <div class="block">
          <StrategyStrongholdIntelSummary
            :world-state="worldState"
            :stronghold="entry.stronghold"
          />
        </div>
      </template>
    </StrategyIntelPanel>

    <StrategyIntelPanel
      v-if="siegeBattlefield"
      variant="battlefield"
      ariaLabel="围城战场情报"
    >
      <StrategyBattlefieldIntelSummary :battlefield="siegeBattlefield" />
    </StrategyIntelPanel>

    <StrategyIntelPanel
      v-if="fieldBattlefield"
      variant="battlefield"
      ariaLabel="野战战场情报"
    >
      <StrategyBattlefieldIntelSummary :battlefield="fieldBattlefield" />
    </StrategyIntelPanel>

    <StrategyIntelPanel
      v-if="useCompactEntityPanel && allEntityEntries.length"
      variant="military"
      ariaLabel="同格单位情报"
    >
      <StrategyCellEntitiesCompactSummary
        :world-state="worldState"
        :entries="allEntityEntries"
      />
    </StrategyIntelPanel>

    <StrategyIntelPanel
      v-else-if="singleMilitaryEntry"
      variant="military"
      ariaLabel="兵队情报"
    >
      <StrategyUnitIntelSummary
        :world-state="worldState"
        :unit="singleMilitaryEntry.unit"
      />
    </StrategyIntelPanel>

    <StrategyIntelPanel
      v-else-if="singleCivilEntry"
      variant="civil"
      ariaLabel="运输与信使"
    >
      <StrategyConvoyIntelSummary
        v-if="singleCivilEntry.kind === 'convoy'"
        :world-state="worldState"
        :convoy="singleCivilEntry.convoy"
      />
      <StrategyMessengerIntelSummary
        v-else-if="singleCivilEntry.kind === 'messenger'"
        :world-state="worldState"
        :messenger="singleCivilEntry.messenger"
      />
    </StrategyIntelPanel>
  </div>
</template>

<style scoped>
.cell-intel-stack {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  width: max-content;
}

.cell-intel-stack--multi {
  flex-direction: row;
}

.cell-intel-stack--multi.cell-intel-stack--left,
.cell-intel-stack--multi.cell-intel-stack--top {
  flex-direction: row-reverse;
}

.cell-intel-stack--multi.cell-intel-stack--bottom,
.cell-intel-stack--multi.cell-intel-stack--top,
.cell-intel-stack--multi.cell-intel-stack--valign-end {
  align-items: flex-end;
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

.cell-intel-stack .intel-box--military {
  border-color: #fbbf24;
}

.cell-intel-stack .intel-box--civil {
  border-color: #4ade80;
}

.cell-intel-stack .intel-box--battlefield {
  border-color: #f87171;
}
</style>
