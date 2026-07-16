<script setup lang="ts">
import { ref } from "vue";

const props = withDefaults(
  defineProps<{
    variant?: "primary" | "default" | "muted";
    tooltip?: string;
    tooltipSide?: "left" | "right" | "auto";
  }>(),
  {
    variant: "primary",
    tooltipSide: "auto",
  }
);

const emit = defineEmits<{
  click: [];
}>();

const hovered = ref(false);
const resolvedSide = ref<"left" | "right">("right");
const tooltipStyle = ref({ top: "0px", left: "0px" });

function resolveSide(rect: DOMRect): "left" | "right" {
  if (props.tooltipSide === "left") return "left";
  if (props.tooltipSide === "right") return "right";

  const estimatedWidth = 240;
  const gap = 10;
  const spaceRight = window.innerWidth - rect.right - gap;
  const spaceLeft = rect.left - gap;
  if (spaceRight >= estimatedWidth) return "right";
  if (spaceLeft >= estimatedWidth) return "left";
  return spaceRight >= spaceLeft ? "right" : "left";
}

function onMouseEnter(event: MouseEvent) {
  if (!props.tooltip) return;
  const btn = event.currentTarget as HTMLElement;
  const rect = btn.getBoundingClientRect();
  const gap = 10;
  const side = resolveSide(rect);
  resolvedSide.value = side;
  const top = rect.top + rect.height / 2;
  tooltipStyle.value = {
    top: `${top}px`,
    left: side === "right" ? `${rect.right + gap}px` : `${rect.left - gap}px`,
  };
  hovered.value = true;
}

function onMouseLeave() {
  hovered.value = false;
}

function onClick(event: MouseEvent) {
  event.stopPropagation();
  emit("click");
}
</script>

<template>
  <button
    type="button"
    class="map-action"
    :class="`map-action--${variant}`"
    @mouseenter="onMouseEnter"
    @mouseleave="onMouseLeave"
    @click="onClick"
  >
    <slot />
  </button>

  <Teleport to="body">
    <div
      v-if="hovered && tooltip"
      class="map-action-tooltip"
      :class="[`map-action-tooltip--${resolvedSide}`]"
      :style="tooltipStyle"
      role="tooltip"
    >
      {{ tooltip }}
    </div>
  </Teleport>
</template>

<style scoped>
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

.map-action--primary {
  background: #2563eb;
  border-color: #2563eb;
  color: #fff;
}

.map-action--primary:hover {
  background: #1d4ed8;
  border-color: #1d4ed8;
}

.map-action--default {
  background: #334155;
  border-color: #475569;
  color: #e2e8f0;
}

.map-action--default:hover {
  background: #475569;
}

.map-action--muted {
  background: #fff;
  border-color: #e2e8f0;
  color: #94a3b8;
}

.map-action--muted:hover {
  background: #f8fafc;
  color: #64748b;
}

.map-action-tooltip {
  position: fixed;
  z-index: 10050;
  max-width: 260px;
  padding: 8px 10px;
  background: #0f172a;
  border: 1px solid #64748b;
  border-radius: 6px;
  color: #e2e8f0;
  font-size: 0.78rem;
  line-height: 1.45;
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.45);
  pointer-events: none;
  white-space: normal;
}

.map-action-tooltip--right {
  transform: translateY(-50%);
}

.map-action-tooltip--left {
  transform: translate(-100%, -50%);
}
</style>
