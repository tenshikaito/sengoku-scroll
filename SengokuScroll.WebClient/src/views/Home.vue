<script setup lang="ts">
import { onMounted, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import StrategyGameStartDialog from "@/components/strategy/StrategyGameStartDialog.vue";
import type { GameStartSettings } from "@/utils/strategyGameStartSettings";
import { writeGameStartSettings, buildGameStartNavigationState } from "@/utils/strategyGameStartSettings";

const router = useRouter();
const route = useRoute();
const dialogVisible = ref(false);

function openDialog() {
  dialogVisible.value = true;
}

function closeDialog() {
  dialogVisible.value = false;
  if (route.query.configure) {
    void router.replace({ name: "Home" });
  }
}

function onConfirm(settings: GameStartSettings) {
  writeGameStartSettings(settings);
  dialogVisible.value = false;
  void router.push({
    name: "strategy",
    state: buildGameStartNavigationState(settings),
  });
}

onMounted(() => {
  if (route.query.configure === "1") {
    openDialog();
  }
});

watch(
  () => route.query.configure,
  (value) => {
    if (value === "1") openDialog();
  }
);
</script>

<template>
  <div class="home">
    <h1>战国绘卷 · SengokuScroll</h1>
    <p>策略模式 M2-b 纵切：PixiJS 主地图 + 单位移动。</p>
    <el-button type="primary" size="large" @click="openDialog">进入策略模式</el-button>

    <StrategyGameStartDialog
      :visible="dialogVisible"
      allow-cancel
      scenario-id="mini_kanto"
      @confirm="onConfirm"
      @cancel="closeDialog"
    />
  </div>
</template>

<style scoped>
.home {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
  padding: 48px 24px;
  text-align: center;
}
</style>
