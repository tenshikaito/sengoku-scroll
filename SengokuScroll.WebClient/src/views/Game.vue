<script setup lang="ts">
import { ref, onMounted } from "vue";
import StatusBar from "@/components/game/components/StatusBar.vue";
import SceneMap from "@/components/game/SceneMap.vue";
import SceneStronghold from "@/components/game/SceneStronghold.vue";

const fullscreenLoading = ref(true);

const isOnMap = ref(false);
const isInStronghold = ref(false);

const sceneMap = ref<InstanceType<typeof SceneMap>>();

onMounted(async () => {
  console.log("game mounted");

  fullscreenLoading.value = true;

  // const initResp = await postCommand({
  //   name: "init",
  // });

  // console.log(initResp);

  // gameInfo.value = initResp.data;

  // sceneMap.value!.init();

  // const isOnMap = initResp.data.isOnMap;
  const isOnMap = true;

  if (isOnMap) {
    await switchMap();
  } else {
    await switchStronghold();
  }
});

const switchScene = (name: string) => {
  isOnMap.value = false;
  isInStronghold.value = false;
  switch (name) {
    case "map":
      isOnMap.value = true;
      break;
    case "stronghold":
      isInStronghold.value = true;
      break;
  }
};

const switchMap = async () => {
  fullscreenLoading.value = true;

  switchScene("map");

  // sceneMap.value!.init();

  fullscreenLoading.value = false;
};

const switchStronghold = async () => {
  fullscreenLoading.value = true;

  // const getStrongholdInfoResp = await postCommand({
  //   name: "getStrongholdInfo",
  // });

  switchScene("stronghold");

  // console.log(getStrongholdInfoResp);

  // strongholdInfo.value = getStrongholdInfoResp.data;

  fullscreenLoading.value = false;
};
</script>

<template>
  <div v-loading.fullscreen.lock="fullscreenLoading">
    <h1>game</h1>
    <status-bar ref="statusBar" />
    <scene-map ref="sceneMap" v-show="isOnMap" @switchScene="switchScene" />
    <scene-stronghold ref="sceneStronghold" v-show="isInStronghold" @switchScene="switchScene" />
  </div>
</template>

<style scoped></style>
