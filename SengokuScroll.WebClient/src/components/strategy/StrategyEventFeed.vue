<script setup lang="ts">
import { computed } from "vue";
import type { StrategyEvent } from "@/api/strategyTypes";
import { messengerFeedBrief } from "@/utils/strategyNotifications";

const props = defineProps<{
  events: StrategyEvent[];
}>();

/** 简报区固定 8 行，超出从头部丢弃。 */
const MAX_LINES = 8;

const messageText = computed(() => {
  if (!props.events.length) return "";
  const lines = props.events.map((evt) => messengerFeedBrief(evt));
  if (lines.length <= MAX_LINES) return lines.join("\n");
  return lines.slice(-MAX_LINES).join("\n");
});
</script>

<template>
  <div v-if="events.length" class="message-log">
    <textarea
      class="message-textarea"
      readonly
      tabindex="-1"
      :value="messageText"
      aria-label="消息简报"
    />
  </div>
</template>

<style scoped>
.message-log {
  flex: 1;
  min-width: 0;
  width: 100%;
  pointer-events: none;
  user-select: none;
}

.message-textarea {
  display: block;
  width: 100%;
  height: calc(0.82rem * 1.55 * 8);
  margin: 0;
  padding: 0;
  border: none;
  resize: none;
  outline: none;
  background: transparent;
  color: #f8fafc;
  font-family: "Yu Mincho", "MS Mincho", "SimSun", serif;
  font-size: 0.82rem;
  line-height: 1.55;
  letter-spacing: 0.02em;
  white-space: pre-wrap;
  overflow: hidden;
  cursor: default;
  pointer-events: none;
  text-shadow:
    0 0 3px rgba(0, 0, 0, 1),
    0 1px 4px rgba(0, 0, 0, 0.95),
    1px 1px 0 rgba(0, 0, 0, 0.85);
}
</style>
