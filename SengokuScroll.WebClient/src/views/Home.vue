<script setup lang="ts">
import { nextTick, ref } from "vue";
import StrategyPanel from "@/views/Strategy.vue";
import StrategyGameStartDialog from "@/components/strategy/StrategyGameStartDialog.vue";
import type { GameStartSettings } from "@/utils/strategyGameStartSettings";
import { writeGameStartSettings } from "@/utils/strategyGameStartSettings";

const dialogVisible = ref(false);
const gameActive = ref(false);
const strategyRef = ref<InstanceType<typeof StrategyPanel> | null>(null);

function openGameStartDialog() {
  dialogVisible.value = true;
}

function closeGameStartDialog() {
  dialogVisible.value = false;
}

async function onConfirm(settings: GameStartSettings) {
  writeGameStartSettings(settings);
  dialogVisible.value = false;
  gameActive.value = true;
  await nextTick();
  await strategyRef.value?.startGameWithSettings(settings);
}
</script>

<template>
  <div class="home">
    <section v-if="!gameActive" class="home-landing">
      <h1>战国绘卷 · SengokuScroll</h1>
      <p>策略模式 M2-b 纵切：PixiJS 主地图 + 单位移动。</p>
      <el-button type="primary" size="large" @click="openGameStartDialog">启动游戏</el-button>
    </section>

    <StrategyPanel
      v-else
      ref="strategyRef"
      @request-game-start="openGameStartDialog"
    />

    <StrategyGameStartDialog
      :visible="dialogVisible"
      allow-cancel
      scenario-id="mini_kanto"
      @confirm="onConfirm"
      @cancel="closeGameStartDialog"
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
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 16px;
  padding: 48px 24px;
  text-align: center;
}
</style>
