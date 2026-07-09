<script setup lang="ts">
import { ref, onMounted } from "vue";

const barInfo = ref({
  name: "织田信长",
  forceName: "织田家",
  location: "大阪",
  hp: 100,
  act: 100,
  goldCoin: 1,
  silverCoin: 10,
  copperCoin: 1000,
});

const init = async () => {
  console.log("init");
};

onMounted(async () => {});

defineExpose({
  init,
});
</script>

<template>
  <div>
    <span>{{ barInfo.name }}</span>
    <span>{{ barInfo.forceName }}</span>
    <span>{{ barInfo.location }}</span>
  </div>
  <div>
    <span>生命值: {{ barInfo.hp }}</span>
    <span>行动力: {{ barInfo.act }}</span>
    <span>金币: {{ barInfo.goldCoin }}</span>
    <span>银币: {{ barInfo.silverCoin }}</span>
    <span>铜币: {{ barInfo.copperCoin }}</span>
  </div>
</template>

<style scoped></style>
