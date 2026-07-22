<script setup lang="ts">
import type { StrategyBattlefieldState, StrategyWorldState } from "@/api/strategy";
import StrategyIntelFieldList from "./StrategyIntelFieldList.vue";
import {
  battlefieldParticipantFoodLabel,
  battlefieldParticipantMoneyLabel,
  battlefieldParticipantMoraleLabel,
  isForeignIntelRestricted,
  UNKNOWN_INTEL,
} from "@/utils/strategyIntelDisplay";
import { buildBattlefieldStatusRows, resolveBattlefieldPresentation } from "@/intelDisplay/BattlefieldKindPresentationBehavior";
import { formatSiegeSoldiers, formatSoldiers } from "@/utils/strategyDisplayUnits";

defineProps<{
  worldState: StrategyWorldState;
  battlefield: StrategyBattlefieldState;
  playerForceId: number;
}>();

function battlefieldStatusRows(battlefield: StrategyBattlefieldState) {
  return buildBattlefieldStatusRows(battlefield);
}

function participantSoldiersLabel(
  worldState: StrategyWorldState,
  battlefield: StrategyBattlefieldState,
  entry: StrategyBattlefieldState["participants"][number],
  playerForceId: number,
): string {
  if (isForeignIntelRestricted(worldState, entry.forceId)) {
    if (resolveBattlefieldPresentation(battlefield.kind).kind === "Siege") {
      return formatSiegeSoldiers(entry.soldiers, entry.forceId, playerForceId);
    }
    return UNKNOWN_INTEL;
  }
  return formatSoldiers(entry.soldiers);
}
</script>

<template>
  <div class="summary">
    <StrategyIntelFieldList variant="hover" :rows="battlefieldStatusRows(battlefield)" />
    <template v-for="(entry, index) in battlefield.participants" :key="entry.forceId">
      <div v-if="index > 0" class="entity-divider" role="separator" />
      <div class="force-block">
        <StrategyIntelFieldList
          variant="hover"
          :rows="[
            { label: '势力', value: entry.forceName },
            {
              label: '兵数',
              value: participantSoldiersLabel(worldState, battlefield, entry, playerForceId),
            },
            {
              label: '士气',
              value: battlefieldParticipantMoraleLabel(worldState, entry.forceId, entry.morale),
            },
            {
              label: '金钱',
              value: battlefieldParticipantMoneyLabel(worldState, entry.forceId, entry.money ?? 0),
            },
            {
              label: '粮草',
              value: battlefieldParticipantFoodLabel(worldState, entry.forceId, entry.food ?? 0),
            },
          ]"
        />
      </div>
    </template>
  </div>
</template>

<style scoped>
.summary {
  font-size: 0.78rem;
  line-height: 1.45;
  color: #e2e8f0;
}

.entity-divider {
  height: 0;
  margin: 10px 0;
  border: none;
  border-top: 1px solid #94a3b8;
  opacity: 0.85;
}
</style>
