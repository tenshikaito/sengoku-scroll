<script setup lang="ts">
import { computed } from "vue";
import type { StrategyEvent } from "@/api/strategyTypes";
import { messageCategoryLabel } from "@/utils/messageCategories";
import {
  eventHasDetail,
  messengerFeedBrief,
  notificationFromEventDetail,
} from "@/utils/strategyNotifications";
import type { StrategyPendingNotification } from "@/components/strategy/StrategyNotificationTray.vue";

const props = defineProps<{
  visible: boolean;
  events: StrategyEvent[];
  playerForceId?: number;
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
  "open-detail": [notification: StrategyPendingNotification];
}>();

interface MessageRow {
  key: string;
  label: string;
  text: string;
  detailAvailable: boolean;
  event: StrategyEvent;
}

const rows = computed<MessageRow[]>(() =>
  props.events.map((event, index) => ({
    key: `${event.category}-${index}-${event.message.slice(0, 24)}`,
    label: messageCategoryLabel(event.category),
    text: messengerFeedBrief(event).replace(/^\[[^\]]+\]\s*/, ""),
    detailAvailable: eventHasDetail(event),
    event,
  })),
);

function openDetail(row: MessageRow) {
  if (!row.detailAvailable) return;
  const notification = notificationFromEventDetail(row.event, props.playerForceId);
  if (!notification) return;
  emit("open-detail", notification);
  emit("update:visible", false);
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    title="消息记录"
    width="620px"
    align-center
    destroy-on-close
    class="strategy-message-dialog strategy-dialog-centered-footer"
    @update:model-value="$emit('update:visible', $event)"
  >
    <ul v-if="rows.length" class="message-list">
      <li v-for="row in rows" :key="row.key" class="message-row">
        <span class="message-category">[{{ row.label }}]</span>
        <button
          v-if="row.detailAvailable"
          type="button"
          class="message-link"
          @click="openDetail(row)"
        >
          {{ row.text }}
        </button>
        <span v-else class="message-plain">{{ row.text }}</span>
      </li>
    </ul>
    <el-empty v-else description="当前筛选无消息" :image-size="64" />

    <template #footer>
      <el-button type="primary" @click="$emit('update:visible', false)">关闭</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.message-list {
  list-style: none;
  margin: 0;
  padding: 0;
  max-height: min(420px, 55vh);
  overflow-y: auto;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  background: #f8fafc;
}

.message-row {
  padding: 8px 12px;
  border-bottom: 1px solid #e2e8f0;
  font-family: "Yu Mincho", "MS Mincho", "SimSun", serif;
  font-size: 0.88rem;
  line-height: 1.55;
  color: #1e293b;
}

.message-row:last-child {
  border-bottom: none;
}

.message-category {
  color: #64748b;
  margin-right: 0.35rem;
}

.message-link {
  padding: 0;
  border: none;
  background: none;
  color: #2563eb;
  font: inherit;
  text-align: left;
  cursor: pointer;
  text-decoration: underline;
  text-underline-offset: 2px;
}

.message-link:hover {
  color: #1d4ed8;
}

.message-plain {
  color: #1e293b;
}

.strategy-message-dialog :deep(.el-dialog__title) {
  color: #0f172a;
}
</style>
