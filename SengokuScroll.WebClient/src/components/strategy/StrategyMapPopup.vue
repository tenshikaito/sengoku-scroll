<script setup lang="ts">
import { computed } from "vue";
import { ElMessage } from "element-plus";
import type { StrategyUnitState } from "@/api/strategyTypes";
import type { StrategyMapPopupMode } from "@/strategyMapInteraction/types";
import { ATTACK_AP_COST, attackApBlockReason, SIEGE_AP_COST, siegeApBlockReason } from "@/utils/strategyActionRules";
import StrategyMapActionButton from "./StrategyMapActionButton.vue";

const props = defineProps<{
  mode: Exclude<StrategyMapPopupMode, "none">;
  entityName?: string;
  x: number;
  y: number;
  isStronghold?: boolean;
  unit?: StrategyUnitState | null;
  tooltipSide?: "left" | "right" | "auto";
  /** 可对当前格敌方据点下达攻城指令 */
  canSiege?: boolean;
  siegeStrongholdId?: number | null;
  /** 当主居城可出征 */
  canExpedition?: boolean;
}>();

const emit = defineEmits<{
  beginMove: [];
  beginAttack: [];
  beginDirective: [];
  beginMerge: [];
  beginSplit: [];
  beginExpedition: [];
  siegeAssault: [];
  siegeEncircle: [];
  showIntel: [];
  cancel: [];
}>();

function swallowPointer(event: Event) {
  event.stopPropagation();
}

function showUnavailableTip(reason: string) {
  ElMessage({
    message: reason,
    type: "info",
    duration: 2800,
    showClose: true,
  });
}

const attackBlockReason = computed(() => attackApBlockReason(props.unit));
const attackUnavailable = computed(() => attackBlockReason.value !== null);
const attackTooltip = computed(() =>
  attackBlockReason.value ?? `选择相邻敌军所在格发起攻击（消耗 ${ATTACK_AP_COST} AP）`
);

const siegeBlockReason = computed(() => siegeApBlockReason(props.unit));
const siegeUnavailable = computed(() => siegeBlockReason.value !== null);
const siegeTooltip = computed(() =>
  siegeBlockReason.value ?? `对相邻敌方据点下达攻城指令（消耗 ${SIEGE_AP_COST} AP）`
);
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
      <div class="actions actions--vertical">
        <StrategyMapActionButton
          variant="primary"
          :tooltip-side="tooltipSide"
          tooltip="设定部队作战方针（待机/移动/进攻/撤退/支援等）；与当主同格即时生效，否则派出信使传达"
          @click="emit('beginDirective')"
        >
          📜 下达方针
        </StrategyMapActionButton>
        <StrategyMapActionButton
          variant="muted"
          :tooltip-side="tooltipSide"
          tooltip="派遣信使功能尚未实装"
          @click="showUnavailableTip('派遣信使功能尚未实装')"
        >
          📨 派遣信使
        </StrategyMapActionButton>
        <StrategyMapActionButton
          variant="primary"
          :tooltip-side="tooltipSide"
          tooltip="在地图上规划路径并移动到目标格"
          @click="emit('beginMove')"
        >
          移动
        </StrategyMapActionButton>
        <StrategyMapActionButton
          :variant="attackUnavailable ? 'muted' : 'primary'"
          :tooltip="attackTooltip"
          :tooltip-side="tooltipSide"
          @click="emit('beginAttack')"
        >
          ⚔ 攻击目标
        </StrategyMapActionButton>
        <StrategyMapActionButton
          variant="primary"
          :tooltip-side="tooltipSide"
          tooltip="与同格或邻格友军合并子编制"
          @click="emit('beginMerge')"
        >
          🔀 合并
        </StrategyMapActionButton>
        <StrategyMapActionButton
          variant="primary"
          :tooltip-side="tooltipSide"
          tooltip="拆出子编制并在邻格生成新部队"
          @click="emit('beginSplit')"
        >
          ✂ 分兵
        </StrategyMapActionButton>
        <div class="divider" />
        <button type="button" class="map-action map-action--default" @click.stop="emit('showIntel')">
          📋 情报
        </button>
        <button type="button" class="map-action map-action--default" @click.stop="emit('cancel')">取消</button>
      </div>
    </template>

    <template v-else-if="mode === 'foreignCommand'">
      <div class="actions actions--vertical">
        <StrategyMapActionButton
          variant="muted"
          :tooltip-side="tooltipSide"
          tooltip="派遣忍者功能尚未实装"
          @click="showUnavailableTip('派遣忍者功能尚未实装')"
        >
          🕵 派遣忍者
        </StrategyMapActionButton>
        <div class="divider" />
        <button type="button" class="map-action map-action--default" @click.stop="emit('showIntel')">
          📋 情报
        </button>
        <button type="button" class="map-action map-action--default" @click.stop="emit('cancel')">取消</button>
      </div>
    </template>

    <template v-else-if="mode === 'strongholdCommand'">
      <div class="actions actions--vertical">
        <StrategyMapActionButton
          v-if="canExpedition"
          variant="primary"
          :tooltip-side="tooltipSide"
          tooltip="从当主居城分配城内兵与将领出征（据点格生成部队）"
          @click="emit('beginExpedition')"
        >
          🚩 出征
        </StrategyMapActionButton>
        <StrategyMapActionButton
          variant="muted"
          :tooltip-side="tooltipSide"
          tooltip="据点建设功能尚未实装"
          @click="showUnavailableTip('据点建设功能尚未实装')"
        >
          🏗 建设
        </StrategyMapActionButton>
        <StrategyMapActionButton
          variant="muted"
          :tooltip-side="tooltipSide"
          tooltip="征兵功能尚未实装"
          @click="showUnavailableTip('征兵功能尚未实装')"
        >
          ⚔ 征兵
        </StrategyMapActionButton>
        <StrategyMapActionButton
          variant="muted"
          :tooltip-side="tooltipSide"
          tooltip="任命城主功能尚未实装"
          @click="showUnavailableTip('任命城主功能尚未实装')"
        >
          👤 任命城主
        </StrategyMapActionButton>
        <StrategyMapActionButton
          variant="muted"
          :tooltip-side="tooltipSide"
          tooltip="据点方针设定尚未实装"
          @click="showUnavailableTip('据点方针设定尚未实装')"
        >
          📜 设定方针
        </StrategyMapActionButton>
        <StrategyMapActionButton
          variant="muted"
          :tooltip-side="tooltipSide"
          tooltip="据点运输功能尚未实装"
          @click="showUnavailableTip('据点运输功能尚未实装')"
        >
          📦 运输
        </StrategyMapActionButton>
        <div class="divider" />
        <button type="button" class="map-action map-action--default" @click.stop="emit('showIntel')">
          📋 情报
        </button>
        <button type="button" class="map-action map-action--default" @click.stop="emit('cancel')">取消</button>
      </div>
    </template>

    <template v-else-if="mode === 'foreignStrongholdCommand'">
      <div class="actions actions--vertical">
        <template v-if="canSiege && siegeStrongholdId">
          <StrategyMapActionButton
            :variant="siegeUnavailable ? 'muted' : 'primary'"
            :tooltip="siegeTooltip"
            :tooltip-side="tooltipSide"
            @click="emit('siegeAssault')"
          >
            ⚔ 强攻
          </StrategyMapActionButton>
          <StrategyMapActionButton
            :variant="siegeUnavailable ? 'muted' : 'primary'"
            :tooltip="siegeTooltip"
            :tooltip-side="tooltipSide"
            @click="emit('siegeEncircle')"
          >
            ⭕ 包围
          </StrategyMapActionButton>
          <div class="divider" />
        </template>
        <StrategyMapActionButton
          variant="muted"
          :tooltip-side="tooltipSide"
          tooltip="派遣忍者功能尚未实装"
          @click="showUnavailableTip('派遣忍者功能尚未实装')"
        >
          🕵 派遣忍者
        </StrategyMapActionButton>
        <div class="divider" />
        <button type="button" class="map-action map-action--default" @click.stop="emit('showIntel')">
          📋 情报
        </button>
        <button type="button" class="map-action map-action--default" @click.stop="emit('cancel')">取消</button>
      </div>
    </template>

    <template v-else-if="mode === 'convoyCommand'">
      <div class="title">🌾 {{ entityName ?? "运输队" }}</div>
      <div class="subtitle">格点 ({{ x }}, {{ y }})</div>
      <div class="subtitle hint">移动由系统自动调度；抵达后卸粮并返回出发据点。</div>
      <div class="actions actions--vertical">
        <StrategyMapActionButton
          variant="muted"
          :tooltip-side="tooltipSide"
          tooltip="运输队改道（信使）功能尚未实装"
          @click="showUnavailableTip('运输队改道（信使）功能尚未实装')"
        >
          📨 改道（信使）
        </StrategyMapActionButton>
        <div class="divider" />
        <button type="button" class="map-action map-action--default" @click.stop="emit('showIntel')">
          📋 情报
        </button>
        <button type="button" class="map-action map-action--default" @click.stop="emit('cancel')">取消</button>
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
        <button type="button" class="map-action map-action--default" @click.stop="emit('cancel')">取消</button>
      </div>
    </template>

    <template v-else-if="mode === 'attackSelect'">
      <div class="title">选择攻击目标</div>
      <div class="subtitle hint">点击相邻敌军所在格</div>
      <div class="subtitle hint">右键取消并返回指令菜单</div>
      <div class="actions actions--vertical">
        <button type="button" class="map-action map-action--default" @click.stop="emit('cancel')">取消</button>
      </div>
    </template>

    <template v-else-if="mode === 'mergeSelect'">
      <div class="title">选择合并目标</div>
      <div class="subtitle hint">点击同格或邻格友军部队</div>
      <div class="subtitle hint">右键取消并返回指令菜单</div>
      <div class="actions actions--vertical">
        <button type="button" class="map-action map-action--default" @click.stop="emit('cancel')">取消</button>
      </div>
    </template>

    <template v-else-if="mode === 'splitSelect'">
      <div class="title">选择分兵落点</div>
      <div class="subtitle hint">点击相邻且无军的空格</div>
      <div class="subtitle hint">右键取消并返回指令菜单</div>
      <div class="actions actions--vertical">
        <button type="button" class="map-action map-action--default" @click.stop="emit('cancel')">取消</button>
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

.map-action {
  margin: 0;
  width: 100%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: 7px 12px;
  border-radius: 4px;
  font-size: 0.82rem;
  line-height: 1.2;
  cursor: pointer;
  border: 1px solid transparent;
  transition: background 0.15s ease, color 0.15s ease, border-color 0.15s ease;
}

.map-action--default {
  background: #334155;
  border-color: #475569;
  color: #e2e8f0;
}

.map-action--default:hover {
  background: #475569;
}

.divider {
  height: 1px;
  margin: 4px 0;
  background: rgba(148, 163, 184, 0.35);
}
</style>
