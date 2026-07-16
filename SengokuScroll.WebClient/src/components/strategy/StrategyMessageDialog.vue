<script setup lang="ts">
import { computed } from "vue";
import type { StrategyEvent } from "@/api/strategyTypes";
import { formatEventsAsPlainText } from "@/utils/messageCategories";

const props = defineProps<{
  visible: boolean;
  events: StrategyEvent[];
}>();

defineEmits<{
  "update:visible": [value: boolean];
}>();

const messageText = computed(() => formatEventsAsPlainText(props.events));
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
    <textarea
      v-if="messageText"
      class="message-textarea"
      readonly
      tabindex="-1"
      :value="messageText"
      aria-label="完整消息记录"
    />
    <el-empty v-else description="当前筛选无消息" :image-size="64" />

    <template #footer>
      <el-button type="primary" @click="$emit('update:visible', false)">关闭</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.message-textarea {
  display: block;
  width: 100%;
  min-height: min(420px, 55vh);
  max-height: min(420px, 55vh);
  margin: 0;
  padding: 10px 12px;
  box-sizing: border-box;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  resize: none;
  outline: none;
  background: #f8fafc;
  color: #1e293b;
  font-family: "Yu Mincho", "MS Mincho", "SimSun", serif;
  font-size: 0.88rem;
  line-height: 1.55;
  white-space: pre-wrap;
  overflow-y: auto;
}

.strategy-message-dialog :deep(.el-dialog__title) {
  color: #0f172a;
}
</style>
