<script setup lang="ts">
defineProps<{
  visible: boolean;
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
  "open-save-slots": [];
  "open-load-slots": [];
}>();

function close() {
  emit("update:visible", false);
}

function onSave() {
  emit("open-save-slots");
  close();
}

function onLoad() {
  emit("open-load-slots");
  close();
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    title="系统"
    width="360px"
    append-to-body
    class="strategy-dialog-centered-footer"
    @update:model-value="emit('update:visible', $event)"
  >
    <p class="hint">存档与读档使用服务器 10 个存档位（App_Data/strategy-saves）。</p>
    <div class="menu-list">
      <el-button class="menu-item" @click="onSave">💾 存档</el-button>
      <el-button class="menu-item" @click="onLoad">📂 读档</el-button>
      <el-button class="menu-item" disabled>⚙️ 游戏设置</el-button>
      <el-button class="menu-item" disabled>🏠 返回主菜单</el-button>
    </div>
    <template #footer>
      <el-button @click="close">关闭</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.hint {
  margin: 0 0 12px;
  font-size: 0.82rem;
  color: #64748b;
  line-height: 1.45;
}

.menu-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.menu-item {
  margin: 0;
  justify-content: flex-start;
  width: 100%;
}
</style>
