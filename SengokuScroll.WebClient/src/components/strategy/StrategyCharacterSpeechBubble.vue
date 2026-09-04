<script setup lang="ts">
withDefaults(
  defineProps<{
    characterName: string;
    message: string;
    tone?: "default" | "warning" | "muted";
  }>(),
  {
    tone: "default",
  },
);

function avatarInitial(name: string): string {
  const trimmed = name.trim();
  return trimmed ? trimmed.slice(0, 1) : "将";
}
</script>

<template>
  <div class="character-speech">
    <div class="avatar" :title="characterName" aria-hidden="true">
      {{ avatarInitial(characterName) }}
    </div>
    <div
      class="speech-bubble speech-bubble--static"
      :class="{
        'speech-bubble--warning': tone === 'warning',
        'speech-bubble--muted': tone === 'muted',
      }"
    >
      <span class="speaker">{{ characterName }}</span>
      <span class="speech-text">{{ message }}</span>
    </div>
  </div>
</template>

<style scoped>
.character-speech {
  display: flex;
  align-items: flex-end;
  gap: 10px;
  width: 100%;
  margin-bottom: 14px;
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
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.12);
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
  box-shadow: 0 3px 10px rgba(0, 0, 0, 0.08);
}

.speech-bubble--static {
  cursor: default;
}

.speech-bubble--warning {
  border-color: #dc2626;
  background: #fff7f7;
}

.speech-bubble--warning::before {
  border-right-color: #dc2626;
}

.speech-bubble--warning::after {
  border-right-color: #fff7f7;
}

.speech-bubble--muted .speech-text {
  color: #64748b;
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

.speech-bubble--warning .speech-text {
  color: #b91c1c;
}
</style>
