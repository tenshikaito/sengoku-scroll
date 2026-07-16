<script setup lang="ts">
import { ElMessage } from "element-plus";
import StrategyMapActionButton from "./StrategyMapActionButton.vue";

defineProps<{
  forceName?: string;
  tooltipSide?: "left" | "right" | "auto";
}>();

defineEmits<{
  showIntel: [];
  cancel: [];
}>();

function swallowPointer(event: Event) {
  event.stopPropagation();
}

function showUnavailableTip(reason: string) {
  ElMessage({
    message: reason,
    type: "info",
    duration: 2800,
    showClose: true,
  });
}
</script>

<template>
  <div
    class="map-popup force-command-popup"
    @pointerdown.stop="swallowPointer"
    @pointerup.stop="swallowPointer"
    @click.stop
    @contextmenu.stop.prevent
  >
    <div class="title">{{ forceName ?? "势力" }}</div>
    <div class="subtitle">势力指令</div>
    <div class="actions actions--vertical">
      <StrategyMapActionButton
        variant="muted"
        :tooltip-side="tooltipSide"
        tooltip="设定势力默认方针功能尚未实装"
        @click="showUnavailableTip('设定势力默认方针功能尚未实装')"
      >
        📜 设定势力默认方针
      </StrategyMapActionButton>
      <StrategyMapActionButton
        variant="muted"
        :tooltip-side="tooltipSide"
        tooltip="提议同盟功能尚未实装"
        @click="showUnavailableTip('提议同盟功能尚未实装')"
      >
        🤝 提议同盟
      </StrategyMapActionButton>
      <StrategyMapActionButton
        variant="muted"
        :tooltip-side="tooltipSide"
        tooltip="宣战功能尚未实装"
        @click="showUnavailableTip('宣战功能尚未实装')"
      >
        🤝 宣战
      </StrategyMapActionButton>
      <StrategyMapActionButton
        variant="muted"
        :tooltip-side="tooltipSide"
        tooltip="议和功能尚未实装"
        @click="showUnavailableTip('议和功能尚未实装')"
      >
        🤝 议和
      </StrategyMapActionButton>
      <StrategyMapActionButton
        variant="muted"
        :tooltip-side="tooltipSide"
        tooltip="调整税率功能尚未实装"
        @click="showUnavailableTip('调整税率功能尚未实装')"
      >
        💰 调整税率
      </StrategyMapActionButton>
      <StrategyMapActionButton
        variant="muted"
        :tooltip-side="tooltipSide"
        tooltip="贸易功能尚未实装"
        @click="showUnavailableTip('贸易功能尚未实装')"
      >
        💰 贸易
      </StrategyMapActionButton>
      <StrategyMapActionButton
        variant="muted"
        :tooltip-side="tooltipSide"
        tooltip="投资功能尚未实装"
        @click="showUnavailableTip('投资功能尚未实装')"
      >
        💰 投资
      </StrategyMapActionButton>
      <div class="divider" />
      <button type="button" class="map-action map-action--default" @click.stop="$emit('showIntel')">
        📋 势力情报
      </button>
      <button type="button" class="map-action map-action--default" @click.stop="$emit('cancel')">取消</button>
    </div>
  </div>
</template>

<style scoped>
.map-popup {
  padding: 10px 12px;
  background: #1e293b;
  border: 1px solid #475569;
  border-radius: 8px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.45);
  min-width: 200px;
  max-width: 280px;
}

.title {
  font-size: 0.95rem;
  font-weight: 600;
  color: #f1f5f9;
  margin-bottom: 4px;
}

.subtitle {
  font-size: 0.8rem;
  color: #94a3b8;
  margin-bottom: 10px;
  line-height: 1.4;
}

.actions {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.actions--vertical {
  flex-direction: column;
  align-items: stretch;
}

.map-action {
  margin: 0;
  width: 100%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 7px 12px;
  border-radius: 4px;
  font-size: 0.82rem;
  line-height: 1.2;
  cursor: pointer;
  border: 1px solid transparent;
  transition: background 0.15s ease, color 0.15s ease, border-color 0.15s ease;
}

.map-action--default {
  background: #334155;
  border-color: #475569;
  color: #e2e8f0;
}

.map-action--default:hover {
  background: #475569;
}

.divider {
  height: 1px;
  margin: 4px 0;
  background: rgba(148, 163, 184, 0.35);
}
</style>
