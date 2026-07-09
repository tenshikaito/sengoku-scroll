<script setup lang="ts">
import type { StrategyBattleResult, StrategyEvent } from "@/api/strategyTypes";

export type StrategyNotificationKind = "economy" | "battle" | "message";

export interface StrategyPendingNotification {
  id: string;
  kind: StrategyNotificationKind;
  icon: string;
  brief: string;
  event?: StrategyEvent;
  battleResult?: StrategyBattleResult;
}

const props = defineProps<{
  notifications: StrategyPendingNotification[];
}>();

const emit = defineEmits<{
  open: [notification: StrategyPendingNotification];
}>();

function onClick(notification: StrategyPendingNotification) {
  emit("open", notification);
}
</script>

<template>
  <div v-if="notifications.length" class="notification-tray" aria-label="消息通知">
    <button
      v-for="item in notifications"
      :key="item.id"
      type="button"
      class="notification-icon"
      :title="item.brief"
      @click="onClick(item)"
    >
      <span class="notification-glyph" aria-hidden="true">{{ item.icon }}</span>
    </button>
  </div>
</template>

<style scoped>
.notification-tray {
  display: flex;
  flex-direction: row-reverse;
  flex-wrap: nowrap;
  align-items: center;
  gap: 6px;
  padding: 0;
  min-height: 0;
  background: transparent;
  border: none;
  flex-shrink: 0;
}

.notification-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  padding: 0;
  border: 1px solid #475569;
  border-radius: 8px;
  background: #1e293b;
  cursor: pointer;
  transition:
    background 0.15s ease,
    border-color 0.15s ease,
    transform 0.1s ease;
}

.notification-icon:hover {
  background: #334155;
  border-color: #94a3b8;
  transform: translateY(-1px);
}

.notification-glyph {
  font-size: 1.25rem;
  line-height: 1;
}
</style>
