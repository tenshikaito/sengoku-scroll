<script setup lang="ts">
import { nextTick, onMounted, ref } from "vue";
import StrategyPanel from "@/views/Strategy.vue";
import StrategyGameStartDialog from "@/components/strategy/StrategyGameStartDialog.vue";
import StrategyMultiplayerLobbyDialog from "@/components/strategy/StrategyMultiplayerLobbyDialog.vue";
import type { GameStartSettings } from "@/utils/strategyGameStartSettings";
import { writeGameStartSettings } from "@/utils/strategyGameStartSettings";
import {
  clearMultiplayerSession,
  readMultiplayerSession,
  reconnectMultiplayerSession,
  type MultiplayerSession,
} from "@/api/multiplayerClient";

const dialogVisible = ref(false);
const multiplayerVisible = ref(false);
const gameActive = ref(false);
const strategyRef = ref<InstanceType<typeof StrategyPanel> | null>(null);

function openGameStartDialog() {
  clearMultiplayerSession();
  dialogVisible.value = true;
}

function openMultiplayerDialog() {
  multiplayerVisible.value = true;
}

function closeGameStartDialog() {
  dialogVisible.value = false;
}

async function onConfirm(settings: GameStartSettings) {
  clearMultiplayerSession();
  writeGameStartSettings(settings);
  dialogVisible.value = false;
  gameActive.value = true;
  await nextTick();
  await strategyRef.value?.startGameWithSettings(settings);
}

async function onMultiplayerEntered(_session: MultiplayerSession) {
  multiplayerVisible.value = false;
  gameActive.value = true;
  await nextTick();
  await strategyRef.value?.resumeMultiplayerGame();
}

onMounted(async () => {
  if (!readMultiplayerSession()) return;
  try {
    const room = await reconnectMultiplayerSession();
    if (!room) return;
    gameActive.value = true;
    await nextTick();
    await strategyRef.value?.resumeMultiplayerGame();
  } catch {
    clearMultiplayerSession();
  }
});
</script>

<template>
  <div class="home">
    <section v-if="!gameActive" class="home-landing">
      <div class="landing-content">
        <p class="eyebrow">单机大战略原型</p>
        <h1>战国绘卷</h1>
        <p class="subtitle">经营领国、运筹外交、统率军势，在乱世中完成天下一统。</p>
        <div class="landing-actions">
          <el-button type="primary" size="large" @click="openGameStartDialog">开始新局</el-button>
          <el-button size="large" @click="openMultiplayerDialog">多人联机</el-button>
        </div>
        <p class="prototype-note">支持单机、全势力 AI 观战与 1–8 人房间联机</p>
      </div>
    </section>

    <StrategyPanel
      v-else
      ref="strategyRef"
      @request-game-start="openGameStartDialog"
      @exit-multiplayer="gameActive = false"
    />

    <StrategyGameStartDialog
      :visible="dialogVisible"
      allow-cancel
      scenario-id="mini_kanto"
      @confirm="onConfirm"
      @cancel="closeGameStartDialog"
    />

    <StrategyMultiplayerLobbyDialog
      :visible="multiplayerVisible"
      @entered="onMultiplayerEntered"
      @cancel="multiplayerVisible = false"
    />
  </div>
</template>

<style scoped>
.home {
  display: flex;
  flex-direction: column;
  flex: 1;
  height: 100%;
  min-height: 0;
  min-width: 0;
  overflow: hidden;
}

.home-landing {
  position: relative;
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  justify-content: center;
  padding: clamp(40px, 8vw, 112px);
  color: #f8f1df;
  background:
    linear-gradient(90deg, rgba(6, 12, 18, 0.88) 0%, rgba(8, 16, 24, 0.66) 38%, rgba(8, 16, 24, 0.08) 75%),
    url("/assets/prototype/landing-sengoku-landscape-v1.webp") center / cover no-repeat;
  overflow: hidden;
}

.home-landing::after {
  content: "";
  position: absolute;
  inset: 0;
  pointer-events: none;
  background: linear-gradient(180deg, rgba(2, 6, 12, 0.18), rgba(2, 6, 12, 0.45));
}

.landing-content {
  position: relative;
  z-index: 1;
  width: min(560px, 90vw);
  text-shadow: 0 2px 16px rgba(0, 0, 0, 0.65);
}

.eyebrow {
  margin: 0 0 12px;
  color: #d6b46d;
  font-size: 0.84rem;
  letter-spacing: 0.32em;
}

.landing-content h1 {
  margin: 0;
  font-family: "Noto Serif SC", "Songti SC", "SimSun", serif;
  font-size: clamp(3rem, 7vw, 6.5rem);
  font-weight: 700;
  letter-spacing: 0.12em;
  line-height: 1.08;
}

.subtitle {
  max-width: 500px;
  margin: 22px 0 0;
  color: rgba(248, 241, 223, 0.88);
  font-size: clamp(1rem, 1.7vw, 1.25rem);
  line-height: 1.8;
}

.landing-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  margin-top: 32px;
}

.landing-actions :deep(.el-button) {
  min-width: 148px;
  border-color: #b98a43;
  background: #8f3b2f;
  box-shadow: 0 10px 28px rgba(0, 0, 0, 0.32);
}

.landing-actions :deep(.el-button + .el-button) {
  margin-left: 0;
}

.landing-actions :deep(.el-button:hover) {
  border-color: #d6b46d;
  background: #a94d3e;
}

.prototype-note {
  margin: 16px 0 0;
  color: rgba(248, 241, 223, 0.62);
  font-size: 0.78rem;
}
</style>
