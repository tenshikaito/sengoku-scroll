<template>
  <div class="not-found-container">
    <el-card class="not-found-card">
      <div class="error-code">401</div>
      <div class="error-message">您没有权限访问页面，请登录</div>
      <el-button type="primary" @click="goHome" class="back-home-btn">
        返回首页
      </el-button>
    </el-card>
  </div>
</template>

<script>
import { ElButton, ElCard } from 'element-plus'
import { useRouter } from 'vue-router'

export default {
  name: 'NotFound',
  components: {
    ElButton,
    ElCard
  },
  setup() {
    const router = useRouter()

    // 返回首页
    const goHome = () => {
      router.push('/')
    }

    return {
      goHome
    }
  }
}
</script>

<style scoped>
.not-found-container {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100vh;
  background-color: #f5f5f5;
}

.not-found-card {
  width: 400px;
  padding: 40px;
  text-align: center;
}

.error-code {
  font-size: 80px;
  font-weight: bold;
  color: #ff6f61;
}

.error-message {
  font-size: 24px;
  margin: 20px 0;
}

.back-home-btn {
  margin-top: 20px;
}
</style>
