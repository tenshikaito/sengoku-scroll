<script setup lang="ts">
import { computed, ref, watch } from "vue";

const props = defineProps<{ visible: boolean; spectator?: boolean }>();

const emit = defineEmits<{
  "update:visible": [visible: boolean];
  finish: [];
}>();

const step = ref(0);

const steps = computed(() => [
  {
    title: props.spectator ? "欢迎进入观战模式" : "欢迎来到战国绘卷",
    body: props.spectator
      ? "本局所有势力都由 AI 控制。选择倍速并点击“进行”，即可观察各家经营、外交与战争。"
      : "目标是统一地图上的全部据点。失去全部领地则战败；你可以随时暂停时间思考和下令。",
  },
  {
    title: "战略与进行",
    body: "右下角“进行”会按左上角倍速自动推进日期；再次切回“战略”即可暂停。空格键也能快速暂停或继续。",
  },
  {
    title: "查看与下令",
    body: props.spectator
      ? "点击地图实体查看情报；观战局中的玩家势力同样由 AI 接管，建议不要手动干预。"
      : "点击据点、部队或人物打开指令。可进行移动、攻城、征募、治理、贸易、谍报与外交。",
  },
  {
    title: "战争迷雾与情报",
    body: "看不见不等于不存在。通过人物视野、同盟情报与谍报逐步掌握敌情；情报窗口可集中查看人物、势力和据点。",
  },
  {
    title: "存档与战局进度",
    body: "右上角“系统”可以存读档。日期旁会持续显示据点进度和领先势力；首次试玩建议使用标准难度。",
  },
]);

watch(
  () => props.visible,
  (visible) => {
    if (visible) step.value = 0;
  },
);

function close() {
  emit("update:visible", false);
}

function next() {
  if (step.value < steps.value.length - 1) {
    step.value += 1;
    return;
  }
  emit("finish");
  close();
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    width="520px"
    :close-on-click-modal="false"
    title="新手引导"
    @close="close"
  >
    <div class="tutorial-card">
      <div class="tutorial-progress" aria-label="教程进度">
        <span
          v-for="(_, index) in steps"
          :key="index"
          :class="{ active: index <= step }"
        />
      </div>
      <h3>{{ steps[step].title }}</h3>
      <p>{{ steps[step].body }}</p>
      <p class="shortcut">第 {{ step + 1 }} / {{ steps.length }} 步</p>
    </div>
    <template #footer>
      <el-button @click="close">稍后再看</el-button>
      <el-button v-if="step > 0" @click="step -= 1">上一步</el-button>
      <el-button type="primary" @click="next">
        {{ step === steps.length - 1 ? "开始" : "下一步" }}
      </el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.tutorial-card {
  min-height: 150px;
  padding: 4px 6px;
}

.tutorial-card h3 {
  margin: 22px 0 10px;
  color: var(--el-text-color-primary);
}

.tutorial-card p {
  margin: 0;
  line-height: 1.75;
  color: var(--el-text-color-regular);
}

.tutorial-card .shortcut {
  margin-top: 18px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.tutorial-progress {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 6px;
}

.tutorial-progress span {
  height: 4px;
  border-radius: 999px;
  background: var(--el-fill-color-darker);
}

.tutorial-progress span.active {
  background: var(--el-color-primary);
}
</style>
