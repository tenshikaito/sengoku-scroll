<script setup lang="ts">

import { computed, reactive, ref, watch } from "vue";

import {

  cloneGameStartOptions,

  enforceCharacterFogControl,

  GAME_START_PRESETS,

  resolveDifficultyFromOptions,

  type GameStartSettings,

  type PresetDifficultyId,

  type StrategyDifficultyId,

} from "@/utils/strategyGameStartSettings";



export type { GameStartSettings, StrategyDifficultyId };



const props = defineProps<{

  visible: boolean;

  loading?: boolean;

  scenarioId?: string;

  allowCancel?: boolean;

}>();



const emit = defineEmits<{

  confirm: [settings: GameStartSettings];

  cancel: [];

}>();



const form = reactive<GameStartSettings>({

  scenarioId: props.scenarioId ?? "mini_kanto",

  difficulty: "Normal",

  customStartOptions: cloneGameStartOptions(GAME_START_PRESETS.Normal),

});



const applyingPreset = ref(false);



watch(

  () => props.scenarioId,

  (id) => {

    if (id) form.scenarioId = id;

  },

);



watch(

  () => form.difficulty,

  (difficulty) => {

    if (difficulty === "Custom") return;

    applyingPreset.value = true;

    Object.assign(form.customStartOptions, GAME_START_PRESETS[difficulty as PresetDifficultyId]);

    applyingPreset.value = false;

  },

);



watch(

  () => ({ ...form.customStartOptions }),

  () => {

    if (applyingPreset.value) return;



    if (enforceCharacterFogControl(form.customStartOptions)) return;



    const resolved = resolveDifficultyFromOptions(form.customStartOptions);

    if (form.difficulty !== resolved) {

      form.difficulty = resolved;

    }

  },

  { deep: true },

);



const controlLockedByCharacterFog = computed(

  () => form.customStartOptions.fogMode === "Character",

);



const fogModeOptions = [

  { label: "无迷雾", value: "None" },

  { label: "势力迷雾", value: "Force" },

  { label: "角色视野", value: "Character" },

];



const intelModeOptions = [

  { label: "显示所有情报", value: "Full" },

  { label: "仅显示已知情报", value: "ForceIntel" },

];



const controlModeOptions = [

  { label: "全势力单位", value: "FullDirect" },

  { label: "仅角色", value: "DirectiveOnly" },

];



const difficultyOptions = [

  { label: "简易", value: "Easy" },

  { label: "标准", value: "Normal" },

  { label: "困难", value: "Hard" },

  { label: "自定义", value: "Custom" },

];



function onConfirm() {

  const options = cloneGameStartOptions(form.customStartOptions);

  enforceCharacterFogControl(options);

  const difficulty = resolveDifficultyFromOptions(options);



  emit("confirm", {

    scenarioId: form.scenarioId,

    difficulty,

    customStartOptions: options,

  });

}



function onClose() {

  if (props.allowCancel) emit("cancel");

}

</script>



<template>

  <el-dialog

    :model-value="visible"

    title="开局设置"

    width="520px"

    :close-on-click-modal="allowCancel"

    :close-on-press-escape="allowCancel"

    :show-close="allowCancel"

    @close="onClose"

  >

    <el-form label-width="120px" label-position="left">

      <el-form-item label="剧本">

        <el-input v-model="form.scenarioId" disabled />

      </el-form-item>

      <el-form-item label="难度">

        <el-select v-model="form.difficulty" style="width: 100%">

          <el-option

            v-for="opt in difficultyOptions"

            :key="opt.value"

            :label="opt.label"

            :value="opt.value"

          />

        </el-select>

      </el-form-item>



      <el-form-item label="迷雾视野">

        <el-select v-model="form.customStartOptions.fogMode" style="width: 100%">

          <el-option v-for="o in fogModeOptions" :key="o.value" :label="o.label" :value="o.value" />

        </el-select>

      </el-form-item>

      <el-form-item label="情报模式">

        <el-select v-model="form.customStartOptions.intelMode" style="width: 100%">

          <el-option v-for="o in intelModeOptions" :key="o.value" :label="o.label" :value="o.value" />

        </el-select>

      </el-form-item>

      <el-form-item label="控制模式">

        <el-select

          v-model="form.customStartOptions.controlMode"

          style="width: 100%"

          :disabled="controlLockedByCharacterFog"

        >

          <el-option v-for="o in controlModeOptions" :key="o.value" :label="o.label" :value="o.value" />

        </el-select>

      </el-form-item>

      <el-form-item label="同盟共享视野">

        <el-switch v-model="form.customStartOptions.allySharedVision" />

      </el-form-item>

      <el-form-item label="即时事件摘要">

        <el-switch v-model="form.customStartOptions.instantEventMessages" />

      </el-form-item>



      <p class="preset-hint">

        选项与某一预设完全一致时难度会自动匹配；否则视为「自定义」。角色视野下控制模式固定为「仅角色」。难度仅影响迷雾与消息获取方式，不影响战斗成功率。

      </p>

    </el-form>



    <template #footer>

      <el-button v-if="allowCancel" @click="emit('cancel')">取消</el-button>

      <el-button type="primary" :loading="loading" @click="onConfirm">开始游戏</el-button>

    </template>

  </el-dialog>

</template>



<style scoped>

.preset-hint {

  margin: 0;

  font-size: 13px;

  color: var(--el-text-color-secondary);

  line-height: 1.5;

}

</style>


