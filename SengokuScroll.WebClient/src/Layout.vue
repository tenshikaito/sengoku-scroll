<!-- Layout.vue -->
<template>
  <el-container class="layout-shell" :class="{ 'layout-shell--immersive': isImmersiveRoute }">
    <!-- 页面顶部导航栏 -->
    <el-header>
      <el-row type="flex" justify="space-between" align="middle">
        <el-col :span="12">
          <router-link to="/">
            <h2>我的网站</h2>
          </router-link>
        </el-col>
        <el-col :span="12" style="text-align: right">
          <template v-if="isLoggedIn">
            <el-dropdown trigger="click">
              <span class="el-dropdown-link">
                <el-avatar :src="userAvatar" size="small" />
                <span>{{ userName }}</span>
              </span>
              <template #dropdown>
                <el-dropdown-menu slot="dropdown">
                  <el-dropdown-item>个人中心</el-dropdown-item>
                  <el-dropdown-item divided @click="logout"
                    >注销</el-dropdown-item
                  >
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </template>
          <template v-else>
            <el-button @click="showLoginDialog">登录</el-button>
            <el-button @click="showRegisterDialog">注册</el-button>
          </template>
        </el-col>
      </el-row>
    </el-header>

    <!-- 页面主体内容 -->
    <el-main class="layout-main" :class="{ 'layout-main--immersive': isImmersiveRoute }">
      <router-view></router-view>
    </el-main>

    <!-- 登录和注册弹窗 -->
    <el-dialog title="登录" v-model="loginDialogVisible">
      <el-form :model="loginForm" label-width="80px">
        <el-form-item label="用户名">
          <el-input v-model="loginForm.username" />
        </el-form-item>
        <el-form-item label="密码">
          <el-input v-model="loginForm.password" type="password" />
        </el-form-item>
      </el-form> 
      <span slot="footer" class="dialog-footer">
        <el-button @click="loginDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleLogin">登录</el-button>
      </span>
    </el-dialog>

    <el-dialog title="注册" v-model="registerDialogVisible">
      <el-form :model="registerForm" label-width="80px">
        <el-form-item label="用户名">
          <el-input v-model="registerForm.username" />
        </el-form-item>
        <el-form-item label="密码">
          <el-input v-model="registerForm.password" type="password" />
        </el-form-item>
      </el-form>
      <span slot="footer" class="dialog-footer">
        <el-button @click="registerDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleRegister">注册</el-button>
      </span>
    </el-dialog>
  </el-container>
</template>

<script setup>
import { computed, ref } from "vue";
import { useRoute } from "vue-router";
import router from "./router";
import { login } from "@/api";

const route = useRoute();
const isImmersiveRoute = computed(() => route.name === "Home");

const isLoggedIn = ref(false);
const userName = ref("");
const userAvatar = ref("");
const loginForm = ref({ username: "testuser", password: "123" });
const registerForm = ref({ username: "testuser", password: "123" });
const loginDialogVisible = ref(false);
const registerDialogVisible = ref(false);

const showLoginDialog = () => {
  loginDialogVisible.value = true;
};

const showRegisterDialog = () => {
  registerDialogVisible.value = true;
};

const handleLogin = async () => {
  if (loginForm.value.username && loginForm.value.password) {
    try {
      const resp = await login(loginForm.value);
      console.log(resp);
      isLoggedIn.value = true;
      userName.value = resp.data.nickname;
      userAvatar.value = resp.data.portrait;
      loginDialogVisible.value = false;
      router.push("/game");
    } catch (error) {
      console.log(error);
    }
  }
};

const handleRegister = () => {
  //   if (registerForm.value.username && registerForm.value.password) {
  //     isLoggedIn.value = true;
  //     userName.value = registerForm.value.username;
  //     userAvatar.value = "https://i.pravatar.cc/150?img=5";
  //     registerDialogVisible.value = false;
  //   }
};

const logout = () => {
  isLoggedIn.value = false;
  userName.value = "";
  userAvatar.value = "";
  router.push("/");
};
</script>

<style scoped>
.layout-shell {
  height: 100vh;
  min-height: 0;
  overflow: hidden;
}

.layout-main {
  flex: 1;
  min-height: 0;
  overflow: auto;
  display: flex;
  flex-direction: column;
}

.layout-main--immersive {
  overflow: hidden;
  padding: 0;
  display: flex;
  flex-direction: column;
}

.layout-main--immersive > :deep(*) {
  flex: 1;
  min-height: 0;
  min-width: 0;
}
</style>
