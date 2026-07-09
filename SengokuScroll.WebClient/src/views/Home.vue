<script setup lang="ts">
</script>

<template>
  <div class="home">
    <h1>战国绘卷 · SengokuScroll</h1>
    <p>策略模式 M2-b 纵切：PixiJS 主地图 + 单位移动。</p>
    <router-link to="/strategy">
      <el-button type="primary" size="large">进入策略模式</el-button>
    </router-link>
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
