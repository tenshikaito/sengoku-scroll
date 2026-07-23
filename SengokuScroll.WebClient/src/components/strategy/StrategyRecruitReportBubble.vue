<script setup lang="ts">
import type { StrategyEvent } from "@/api/strategyTypes";

const props = defineProps<{
  visible: boolean;
  characterName: string;
  message: string;
  event?: StrategyEvent;
}>();

const emit = defineEmits<{
  openDetail: [event: StrategyEvent];
  dismiss: [];
}>();

function onOpenDetail() {
  if (!props.event) return;
  emit("openDetail", props.event);
}

function avatarInitial(name: string): string {
  const trimmed = name.trim();
  return trimmed ? trimmed.slice(0, 1) : "将";
}
</script>

<template>
  <div v-if="visible" class="recruit-report-bubble" @pointerdown.stop @click.stop>
    <div class="avatar" :title="characterName" aria-hidden="true">
      {{ avatarInitial(characterName) }}
    </div>
    <div
      class="speech-bubble"
      :class="{ 'speech-bubble--static': !event }"
      :role="event ? 'button' : undefined"
      :tabindex="event ? 0 : undefined"
      @click="onOpenDetail"
      @keydown.enter.prevent="onOpenDetail"
      @keydown.space.prevent="onOpenDetail"
    >
      <span class="speaker">{{ characterName }}</span>
      <span class="speech-text">{{ message }}</span>
    </div>
    <button type="button" class="dismiss-btn" title="关闭" aria-label="关闭汇报" @click="emit('dismiss')">
      ×
    </button>
  </div>
</template>

<style scoped>
.recruit-report-bubble {
  display: flex;
  align-items: flex-end;
  gap: 8px;
  width: 100%;
  max-width: min(420px, 100%);
  align-self: flex-start;
  pointer-events: auto;
}

.avatar {
  flex-shrink: 0;
  width: 52px;
  height: 52px;
  border-radius: 50%;
  border: 2px solid #f8fafc;
  background: linear-gradient(145deg, #475569, #1e293b);
  color: #f8fafc;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-family: "Yu Mincho", "MS Mincho", "SimSun", serif;
  font-size: 1.35rem;
  font-weight: 700;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.35);
}

.speech-bubble {
  position: relative;
  flex: 1;
  min-width: 0;
  margin: 0;
  padding: 10px 12px;
  border: 2px solid #0f172a;
  border-radius: 14px 14px 14px 4px;
  background: #fffef8;
  color: #1e293b;
  text-align: left;
  cursor: pointer;
  box-shadow: 0 3px 10px rgba(0, 0, 0, 0.25);
}

.speech-bubble--static {
  cursor: default;
}

.speech-bubble::before {
  content: "";
  position: absolute;
  left: -10px;
  bottom: 14px;
  width: 0;
  height: 0;
  border-top: 8px solid transparent;
  border-bottom: 8px solid transparent;
  border-right: 10px solid #0f172a;
}

.speech-bubble::after {
  content: "";
  position: absolute;
  left: -7px;
  bottom: 15px;
  width: 0;
  height: 0;
  border-top: 7px solid transparent;
  border-bottom: 7px solid transparent;
  border-right: 9px solid #fffef8;
}

.speaker {
  display: block;
  font-size: 0.72rem;
  font-weight: 700;
  color: #64748b;
  margin-bottom: 4px;
}

.speech-text {
  display: block;
  font-family: "Yu Mincho", "MS Mincho", "SimSun", serif;
  font-size: 0.88rem;
  line-height: 1.55;
  color: #1e293b;
}

.dismiss-btn {
  flex-shrink: 0;
  width: 24px;
  height: 24px;
  padding: 0;
  border: 1px solid #64748b;
  border-radius: 999px;
  background: rgba(15, 23, 42, 0.75);
  color: #e2e8f0;
  font-size: 1rem;
  line-height: 1;
  cursor: pointer;
}

.dismiss-btn:hover {
  background: rgba(51, 65, 85, 0.9);
}
</style>
