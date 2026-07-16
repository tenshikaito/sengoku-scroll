<script setup lang="ts">
import { computed } from "vue";

const showPlayer = defineModel<boolean>("showPlayer", { default: true });
const showWorld = defineModel<boolean>("showWorld", { default: true });

defineEmits<{
  "open-dialog": [];
}>();

const allChecked = computed(() => showPlayer.value && showWorld.value);

const allIndeterminate = computed(
  () => (showPlayer.value || showWorld.value) && !allChecked.value
);

function onAllChange(checked: boolean) {
  showPlayer.value = checked;
  showWorld.value = checked;
}
</script>

<template>
  <div class="message-feed-toolbar" @pointerdown.stop @click.stop @wheel.stop>
    <el-button size="small" type="primary" plain @click="$emit('open-dialog')">
      📋 消息
    </el-button>
    <div class="scope-checks" role="group" aria-label="消息筛选">
      <el-checkbox
        :model-value="allChecked"
        :indeterminate="allIndeterminate"
        @change="(val: boolean) => onAllChange(val)"
      >
        全部
      </el-checkbox>
      <el-checkbox v-model="showPlayer">我方消息</el-checkbox>
      <el-checkbox v-model="showWorld">世界消息</el-checkbox>
    </div>
  </div>
</template>

<style scoped>
.message-feed-toolbar {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 6px;
  flex-shrink: 0;
  pointer-events: auto;
  width: 100%;
}

.scope-checks {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.scope-checks :deep(.el-checkbox) {
  margin-right: 0;
  height: auto;
}

.scope-checks :deep(.el-checkbox__label) {
  font-size: 0.78rem;
  color: #cbd5e1;
  padding-left: 6px;
}
</style>
