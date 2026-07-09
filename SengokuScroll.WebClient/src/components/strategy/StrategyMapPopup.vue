<script setup lang="ts">
import type { StrategyMapPopupMode } from "@/strategyMapInteraction/types";

defineProps<{
  mode: Exclude<StrategyMapPopupMode, "none">;
  entityName?: string;
  x: number;
  y: number;
  isStronghold?: boolean;
}>();

defineEmits<{
  beginMove: [];
  beginAttack: [];
  beginDirective: [];
  showIntel: [];
  cancel: [];
}>();

function swallowPointer(event: Event) {
  event.stopPropagation();
}
</script>

<template>
  <div
    class="map-popup"
    @pointerdown.stop="swallowPointer"
    @pointerup.stop="swallowPointer"
    @click.stop
    @contextmenu.stop.prevent
  >
    <template v-if="mode === 'command'">
      <div class="title">{{ entityName ?? "单位" }}</div>
      <div class="subtitle">格点 ({{ x }}, {{ y }})</div>
      <div class="actions actions--vertical">
        <el-button type="primary" size="small" @click.stop="$emit('beginMove')">移动</el-button>
        <el-button type="primary" size="small" @click.stop="$emit('beginAttack')">攻击</el-button>
        <el-button type="primary" size="small" @click.stop="$emit('beginDirective')">方针</el-button>
        <el-button type="default" size="small" @click.stop="$emit('showIntel')">情报</el-button>
        <el-button type="default" size="small" @click.stop="$emit('cancel')">取消</el-button>
      </div>
    </template>

    <template v-else-if="mode === 'foreignCommand'">
      <div class="title">{{ entityName ?? "单位" }}</div>
      <div class="subtitle">格点 ({{ x }}, {{ y }})</div>
      <div class="actions actions--vertical">
        <el-button type="default" size="small" @click.stop="$emit('showIntel')">情报</el-button>
        <el-button type="default" size="small" @click.stop="$emit('cancel')">取消</el-button>
      </div>
    </template>

    <template v-else-if="mode === 'strongholdCommand'">
      <div class="title">🏯 {{ entityName ?? "据点" }}</div>
      <div class="subtitle">格点 ({{ x }}, {{ y }})</div>
      <div class="actions actions--vertical">
        <el-button type="default" size="small" @click.stop="$emit('showIntel')">情报</el-button>
        <el-button type="default" size="small" @click.stop="$emit('cancel')">取消</el-button>
      </div>
    </template>

    <template v-else-if="mode === 'foreignStrongholdCommand'">
      <div class="title">🏯 {{ entityName ?? "据点" }}</div>
      <div class="subtitle">格点 ({{ x }}, {{ y }})</div>
      <div class="actions actions--vertical">
        <el-button type="default" size="small" @click.stop="$emit('showIntel')">情报</el-button>
        <el-button type="default" size="small" @click.stop="$emit('cancel')">取消</el-button>
      </div>
    </template>

    <template v-else-if="mode === 'convoyCommand'">
      <div class="title">🌾 {{ entityName ?? "运输队" }}</div>
      <div class="subtitle">格点 ({{ x }}, {{ y }})</div>
      <div class="subtitle hint">移动由系统自动调度；抵达后卸粮并返回出发据点。</div>
      <div class="subtitle hint">若需改道，须由当主或据点派出信使传达新路径（后续实装）。</div>
      <div class="actions actions--vertical">
        <el-button type="default" size="small" @click.stop="$emit('showIntel')">情报</el-button>
        <el-button type="default" size="small" @click.stop="$emit('cancel')">取消</el-button>
      </div>
    </template>

    <template v-else-if="mode === 'moveSelect'">
      <div class="title">规划移动路径</div>
      <div class="subtitle hint">点击空格：追加路径段（从上一终点继续）</div>
      <div class="subtitle hint">再次点击同一格：确认移动</div>
      <div class="subtitle hint">橙色数字：已确认中继；青色「终」：当前终点（再点同格确认）</div>
      <div class="subtitle hint">点击已路过的格：截断并回到该中继点</div>
      <div class="subtitle hint">右键取消并返回指令菜单</div>
      <div class="actions actions--vertical">
        <el-button type="default" size="small" @click.stop="$emit('cancel')">取消</el-button>
      </div>
    </template>

    <template v-else-if="mode === 'attackSelect'">
      <div class="title">选择攻击目标</div>
      <div class="subtitle hint">点击相邻敌军所在格</div>
      <div class="subtitle hint">右键取消并返回指令菜单</div>
      <div class="actions actions--vertical">
        <el-button type="default" size="small" @click.stop="$emit('cancel')">取消</el-button>
      </div>
    </template>
  </div>
</template>

<style scoped>
.map-popup {
  padding: 10px 12px;
  background: #1e293b;
  border: 1px solid #475569;
  border-radius: 8px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.45);
  min-width: 200px;
  max-width: 280px;
}

.title {
  font-size: 0.95rem;
  font-weight: 600;
  color: #f1f5f9;
  margin-bottom: 4px;
}

.subtitle {
  font-size: 0.8rem;
  color: #94a3b8;
  margin-bottom: 10px;
  line-height: 1.4;
}

.subtitle.hint {
  color: #cbd5e1;
}

.actions {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.actions--vertical {
  flex-direction: column;
  align-items: stretch;
}

.actions--vertical :deep(.el-button) {
  margin: 0;
  width: 100%;
}
</style>
