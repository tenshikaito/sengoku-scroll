<script setup lang="ts">
import { computed, ref, watch } from "vue";
import {
  createMultiplayerRoom,
  joinMultiplayerRoom,
  listMultiplayerRooms,
  listMultiplayerScenarioForces,
  type MultiplayerForce,
  type MultiplayerRoom,
  type MultiplayerSession,
} from "@/api/multiplayerClient";

const props = defineProps<{ visible: boolean }>();
const emit = defineEmits<{
  cancel: [];
  entered: [session: MultiplayerSession];
}>();

const tab = ref<"create" | "join">("create");
const loading = ref(false);
const error = ref("");
const rooms = ref<MultiplayerRoom[]>([]);
const roomName = ref("朋友联机房");
const playerName = ref("玩家");
const maxPlayers = ref(4);
const selectedRoomId = ref("");
const selectedForceId = ref<number | null>(null);
const createForceId = ref<number | null>(null);
const scenarioForces = ref<MultiplayerForce[]>([]);

const selectedRoom = computed(() =>
  rooms.value.find((room) => room.roomId === selectedRoomId.value) ?? null,
);
const availableForces = computed(() =>
  selectedRoom.value?.forces.filter((force) => !force.occupied) ?? [],
);

async function refreshRooms() {
  loading.value = true;
  error.value = "";
  try {
    [rooms.value, scenarioForces.value] = await Promise.all([
      listMultiplayerRooms(),
      listMultiplayerScenarioForces(),
    ]);
    if (!scenarioForces.value.some((force) => force.forceId === createForceId.value)) {
      createForceId.value = scenarioForces.value[0]?.forceId ?? null;
    }
    if (!rooms.value.some((room) => room.roomId === selectedRoomId.value)) {
      selectedRoomId.value = rooms.value[0]?.roomId ?? "";
    }
    if (!availableForces.value.some((force) => force.forceId === selectedForceId.value)) {
      selectedForceId.value = availableForces.value[0]?.forceId ?? null;
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : "读取房间失败";
  } finally {
    loading.value = false;
  }
}

async function createRoom() {
  if (!playerName.value.trim() || !roomName.value.trim() || createForceId.value == null) {
    error.value = "请填写房间名、玩家名并选择势力";
    return;
  }
  loading.value = true;
  error.value = "";
  try {
    const result = await createMultiplayerRoom({
      roomName: roomName.value,
      playerName: playerName.value,
      forceId: createForceId.value,
      maxPlayers: maxPlayers.value,
    });
    emit("entered", result.session);
  } catch (e) {
    error.value = e instanceof Error ? e.message : "创建房间失败";
  } finally {
    loading.value = false;
  }
}

async function joinRoom() {
  if (!selectedRoomId.value || selectedForceId.value == null || !playerName.value.trim()) {
    error.value = "请选择房间、势力并填写玩家名";
    return;
  }
  loading.value = true;
  error.value = "";
  try {
    const result = await joinMultiplayerRoom({
      roomId: selectedRoomId.value,
      playerName: playerName.value,
      forceId: selectedForceId.value,
    });
    emit("entered", result.session);
  } catch (e) {
    error.value = e instanceof Error ? e.message : "加入房间失败";
    await refreshRooms();
  } finally {
    loading.value = false;
  }
}

watch(
  () => props.visible,
  (visible) => {
    if (visible) void refreshRooms();
  },
);

watch(selectedRoomId, () => {
  selectedForceId.value = availableForces.value[0]?.forceId ?? null;
});
</script>

<template>
  <el-dialog
    :model-value="visible"
    title="多人大战略（1–8 人）"
    width="min(620px, 94vw)"
    :close-on-click-modal="false"
    @close="emit('cancel')"
  >
    <el-alert
      v-if="error"
      type="error"
      :title="error"
      show-icon
      :closable="false"
      class="lobby-alert"
    />

    <el-tabs v-model="tab">
      <el-tab-pane label="创建房间" name="create">
        <el-form label-width="90px">
          <el-form-item label="房间名称">
            <el-input v-model="roomName" maxlength="40" />
          </el-form-item>
          <el-form-item label="玩家名称">
            <el-input v-model="playerName" maxlength="24" />
          </el-form-item>
          <el-form-item label="最大人数">
            <el-input-number v-model="maxPlayers" :min="1" :max="8" />
          </el-form-item>
          <el-form-item label="初始势力">
            <el-select v-model="createForceId" placeholder="选择房主势力" style="width: 100%">
              <el-option
                v-for="force in scenarioForces"
                :key="force.forceId"
                :label="`${force.forceName}（${force.forceId}）`"
                :value="force.forceId"
              />
            </el-select>
          </el-form-item>
        </el-form>
        <p class="hint">房间保存在当前游戏进程内。服务器重启后房间会关闭。</p>
      </el-tab-pane>

      <el-tab-pane label="加入房间" name="join">
        <div class="refresh-row">
          <span>局域网内打开同一个服务器地址即可看到这些房间。</span>
          <el-button size="small" :loading="loading" @click="refreshRooms">刷新</el-button>
        </div>
        <el-form label-width="90px">
          <el-form-item label="玩家名称">
            <el-input v-model="playerName" maxlength="24" />
          </el-form-item>
          <el-form-item label="房间">
            <el-select v-model="selectedRoomId" placeholder="暂无可用房间" style="width: 100%">
              <el-option
                v-for="room in rooms"
                :key="room.roomId"
                :label="`${room.roomName} · ${room.playerCount}/${room.maxPlayers} · ${room.roomId}`"
                :value="room.roomId"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="势力">
            <el-select v-model="selectedForceId" placeholder="请选择未占用势力" style="width: 100%">
              <el-option
                v-for="force in availableForces"
                :key="force.forceId"
                :label="`${force.forceName}（${force.forceId}）`"
                :value="force.forceId"
              />
            </el-select>
          </el-form-item>
        </el-form>
      </el-tab-pane>
    </el-tabs>

    <template #footer>
      <el-button @click="emit('cancel')">取消</el-button>
      <el-button
        v-if="tab === 'create'"
        type="primary"
        :loading="loading"
        @click="createRoom"
      >
        创建并进入
      </el-button>
      <el-button
        v-else
        type="primary"
        :loading="loading"
        :disabled="!selectedRoomId || selectedForceId == null"
        @click="joinRoom"
      >
        加入房间
      </el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.lobby-alert {
  margin-bottom: 12px;
}

.refresh-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 16px;
  color: #64748b;
  font-size: 0.82rem;
}

.hint {
  margin: 4px 0 0;
  color: #64748b;
  font-size: 0.8rem;
}
</style>
