<script setup lang="ts">
import { computed } from "vue";

export type StrategyMapLoadingPhase = "map" | "state" | "error";

const props = defineProps<{
  phase: StrategyMapLoadingPhase;
  mapName?: string;
  error?: string;
}>();

const emit = defineEmits<{
  retry: [];
}>();

const phaseLabel = computed(() => {
  if (props.phase === "error") return "加载失败";
  if (props.phase === "state") return "正在同步世界状态…";
  return "正在加载地图数据…";
});
</script>

<template>
  <div class="map-loading-scene" role="status" aria-live="polite">
    <div class="map-loading-scene__panel">
      <div class="map-loading-scene__emblem" aria-hidden="true">戦</div>
      <h2 class="map-loading-scene__title">{{ mapName ?? "战略地图" }}</h2>
      <p class="map-loading-scene__phase">{{ phaseLabel }}</p>
      <div v-if="phase !== 'error'" class="map-loading-scene__bar" aria-hidden="true">
        <span class="map-loading-scene__bar-fill" />
      </div>
      <ul v-if="phase !== 'error'" class="map-loading-scene__steps">
        <li :class="{ done: phase === 'state' || phase === 'map', active: phase === 'map' }">
          地图主数据
        </li>
        <li :class="{ active: phase === 'state' }">世界状态</li>
      </ul>
      <p v-if="phase === 'error'" class="map-loading-scene__error">{{ error }}</p>
      <button
        v-if="phase === 'error'"
        type="button"
        class="map-loading-scene__retry"
        @click="emit('retry')"
      >
        重试
      </button>
    </div>
  </div>
</template>

<style scoped>
.map-loading-scene {
  position: absolute;
  inset: 0;
  z-index: 20;
  display: flex;
  align-items: center;
  justify-content: center;
  background:
    radial-gradient(ellipse at center, rgba(20, 32, 18, 0.92) 0%, rgba(8, 12, 8, 0.98) 70%),
    linear-gradient(160deg, #1a2e16 0%, #0d1510 100%);
}

.map-loading-scene__panel {
  min-width: 280px;
  max-width: 360px;
  padding: 2rem 2.25rem;
  text-align: center;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.06);
  border: 1px solid rgba(255, 255, 255, 0.12);
  box-shadow: 0 12px 40px rgba(0, 0, 0, 0.35);
  color: #f5f5f4;
}

.map-loading-scene__emblem {
  font-size: 2.5rem;
  font-weight: 700;
  color: #d4a574;
  margin-bottom: 0.5rem;
  letter-spacing: 0.2em;
}

.map-loading-scene__title {
  margin: 0 0 0.75rem;
  font-size: 1.15rem;
  font-weight: 600;
}

.map-loading-scene__phase {
  margin: 0 0 1.25rem;
  font-size: 0.95rem;
  color: rgba(245, 245, 244, 0.82);
}

.map-loading-scene__bar {
  height: 4px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.12);
  overflow: hidden;
  margin-bottom: 1rem;
}

.map-loading-scene__bar-fill {
  display: block;
  height: 100%;
  width: 40%;
  border-radius: inherit;
  background: linear-gradient(90deg, #8b6914, #d4a574);
  animation: map-loading-slide 1.4s ease-in-out infinite;
}

@keyframes map-loading-slide {
  0% {
    transform: translateX(-120%);
  }
  100% {
    transform: translateX(320%);
  }
}

.map-loading-scene__steps {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  justify-content: center;
  gap: 1.5rem;
  font-size: 0.85rem;
  color: rgba(245, 245, 244, 0.45);
}

.map-loading-scene__steps li.active {
  color: #d4a574;
  font-weight: 600;
}

.map-loading-scene__steps li.done {
  color: rgba(212, 165, 116, 0.75);
}

.map-loading-scene__error {
  margin: 0 0 1rem;
  color: #fca5a5;
  font-size: 0.9rem;
  line-height: 1.5;
}

.map-loading-scene__retry {
  padding: 0.45rem 1.25rem;
  border-radius: 8px;
  border: 1px solid rgba(212, 165, 116, 0.5);
  background: rgba(212, 165, 116, 0.15);
  color: #f5f5f4;
  cursor: pointer;
  font-size: 0.9rem;
}

.map-loading-scene__retry:hover {
  background: rgba(212, 165, 116, 0.28);
}
</style>
