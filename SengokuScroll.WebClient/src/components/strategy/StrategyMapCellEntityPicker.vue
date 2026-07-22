<script setup lang="ts">
import type { MapCellEntityOption } from "@/utils/mapCellEntityPicker";
import { mapCellEntityKindIcon } from "@/utils/mapCellEntityPicker";

defineProps<{
  entities: MapCellEntityOption[];
}>();

const emit = defineEmits<{
  pick: [entity: MapCellEntityOption];
  cancel: [];
}>();

function swallowPointer(event: Event) {
  event.stopPropagation();
}
</script>

<template>
  <div
    class="map-popup"
    @pointerdown.stop="swallowPointer"
    @pointerup.stop="swallowPointer"
    @click.stop
    @contextmenu.stop.prevent
  >
    <div class="title">选择对象</div>
    <div class="entity-list">
      <button
        v-for="entity in entities"
        :key="`${entity.kind}-${entity.id}`"
        type="button"
        class="entity-item"
        @click.stop="emit('pick', entity)"
      >
        <span class="entity-icon">{{ mapCellEntityKindIcon(entity.kind) }}</span>
        <span class="entity-text">
          <span class="entity-label">{{ entity.label }}</span>
          <span v-if="entity.subtitle" class="entity-subtitle">{{ entity.subtitle }}</span>
        </span>
      </button>
    </div>
    <div class="actions">
      <button type="button" class="map-action map-action--default" @click.stop="emit('cancel')">
        取消
      </button>
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
  min-width: 220px;
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
}

.entity-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-bottom: 10px;
}

.entity-item {
  margin: 0;
  width: 100%;
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 10px;
  border-radius: 4px;
  border: 1px solid #475569;
  background: #334155;
  color: #e2e8f0;
  cursor: pointer;
  text-align: left;
}

.entity-item:hover {
  background: #475569;
}

.entity-icon {
  flex: 0 0 auto;
  font-size: 1rem;
}

.entity-text {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.entity-label {
  font-size: 0.84rem;
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.entity-subtitle {
  font-size: 0.74rem;
  color: #94a3b8;
}

.actions {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.map-action {
  margin: 0;
  width: 100%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 7px 12px;
  border-radius: 4px;
  font-size: 0.82rem;
  cursor: pointer;
  border: 1px solid transparent;
}

.map-action--default {
  background: #334155;
  border-color: #475569;
  color: #e2e8f0;
}

.map-action--default:hover {
  background: #475569;
}
</style>
