<script setup lang="ts">
import { computed, ref, watch } from "vue";
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
  expeditionTooltip?: string;
  /** 当主是否位于本家居城（据点指令总开关） */
  lordAtResidence?: boolean;
  strongholdCommandsTooltip?: string;
  /** 是否可调整税率（己方非内藩据点） */
  canAdjustTax?: boolean;
  /** 是否可设置政务方针（有代官或非直辖领主） */
  canSetGovernancePolicy?: boolean;
  /** 方针按钮说明 */
  governancePolicyTooltip?: string;
  /** 税率指令说明（直辖即时 / 非直辖信使） */
  taxRateTooltip?: string;
  /** 可对当前格敌方据点展开谍报 */
  canEspionage?: boolean;
  /** 双弹窗模式下隐藏据点侧谍报（改由角色菜单操作） */
  hideStrongholdEspionage?: boolean;
  /** 当主在城内 */
  lordInStronghold?: boolean;
  /** 可出城 */
  canLeaveStronghold?: boolean;
  /** 可移动（在地图） */
  canCharacterMove?: boolean;
  /** 可入城 */
  canEnterStronghold?: boolean;
  /** 城内有其它角色可拜访 */
  canVisitOthers?: boolean;
  /** 角色菜单谍报（他势力据点） */
  canCharacterEspionage?: boolean;
  /** 出入城 AP 消耗 */
  gateApCost?: number;
  /** 当主当前 AP */
  lordAp?: number;
  /** 据点正被围攻/包围 */
  isStrongholdBesieged?: boolean;
  /** 外政据点显示「方针」按钮（有城主或内藩当主居城） */
  showStrongholdDirective?: boolean;
  /** 内藩据点：仅显示方针（隐藏本家据点指令） */
  strongholdDirectiveOnly?: boolean;
  /** 本家直属兵队可移动 */
  canUnitMove?: boolean;
  /** 本家直属兵队可攻城 */
  canUnitSiege?: boolean;
  /** 单位可入城（同格据点） */
  canUnitEnterStronghold?: boolean;
  /** 单位可出城 */
  canUnitExitStronghold?: boolean;
  /** 单位可建制解散（Home 据点） */
  canUnitDisband?: boolean;
  /** 商队可在城内打开市场 */
  canUnitOpenMarket?: boolean;
  /** 当主据点可交易（城内须有商队） */
  canStrongholdTrade?: boolean;
  strongholdTradeTooltip?: string;
  /** 角色所在据点可查看市场 */
  canViewPersonalMarket?: boolean;
  personalMarketTooltip?: string;
  /** 据点商家列表（Merchant cityActors） */
  merchantShops?: { id: number; name: string }[];
  /** 角色在城内可执行个人军事/内政指令（领主/代官/当主） */
  canExecutePersonalCommands?: boolean;
}>();

const emit = defineEmits<{
  beginMove: [];
  beginAttack: [];
  beginDirective: [];
  beginMerge: [];
  beginSplit: [];
  beginExpedition: [];
  beginTaxRate: [];
  beginGovernancePolicy: [];
  beginRecruit: [];
  beginMercenaryRecruit: [];
  beginPersonalRecruit: [];
  beginPersonalMercenaryRecruit: [];
  beginEspionage: [];
  beginAppointLord: [];
  beginTransferCharacter: [];
  beginRecallCharacter: [];
  beginLeaveStronghold: [];
  beginEnterStronghold: [];
  beginVisit: [];
  siegeAssault: [];
  siegeEncircle: [];
  beginUnitEnterStronghold: [];
  beginUnitExitStronghold: [];
  beginUnitDisband: [];
  openUnitMarket: [];
  openStrongholdMarket: [];
  openPersonalMarket: [];
  openMerchantShop: [actorId: number];
  showIntel: [];
  cancel: [];
}>();

type StrongholdMenuView = "categories" | "military" | "domestic" | "personnel" | "merchants";

const strongholdMenuView = ref<StrongholdMenuView>("categories");
const characterMenuView = ref<StrongholdMenuView>("categories");

const merchantShopList = computed(() => props.merchantShops ?? []);

function openMerchantEntry() {
  if (blockIfStrongholdBesieged()) return;
  const shops = merchantShopList.value;
  if (shops.length === 0) {
    showUnavailableTip("该据点暂无商家");
    return;
  }
  if (shops.length === 1) {
    emit("openMerchantShop", shops[0]!.id);
    return;
  }
  characterMenuView.value = "merchants";
}

function onMerchantShopClick(actorId: number) {
  emit("openMerchantShop", actorId);
}

watch(
  () => props.mode,
  (mode) => {
    if (mode !== "strongholdCommand") {
      strongholdMenuView.value = "categories";
    }
    if (mode !== "characterCommand") {
      characterMenuView.value = "categories";
    }
  },
);

function openCharacterCategory(view: Exclude<StrongholdMenuView, "categories">) {
  if (blockIfStrongholdBesieged()) return;
  if (!props.canExecutePersonalCommands) {
    showUnavailableTip("须在城内且为当主、领主或代官方可执行个人指令");
    return;
  }
  characterMenuView.value = view;
}

function backToCharacterCategories() {
  characterMenuView.value = "categories";
}

function onPersonalMercenaryRecruitClick() {
  if (blockIfStrongholdBesieged()) return;
  if (!props.canExecutePersonalCommands) {
    showUnavailableTip("须在城内且为当主、领主或代官方可执行个人指令");
    return;
  }
  emit("beginPersonalMercenaryRecruit");
}

function onPersonalRecruitClick() {
  if (blockIfStrongholdBesieged()) return;
  if (!props.canExecutePersonalCommands) {
    showUnavailableTip("须在城内且为当主、领主或代官方可执行个人指令");
    return;
  }
  emit("beginPersonalRecruit");
}

function onPersonalMutedAction(tip: string) {
  if (blockIfStrongholdBesieged()) return;
  if (!props.canExecutePersonalCommands) {
    showUnavailableTip("须在城内且为当主、领主或代官方可执行个人指令");
    return;
  }
  showUnavailableTip(tip);
}

function openStrongholdCategory(view: Exclude<StrongholdMenuView, "categories">) {
  if (blockIfStrongholdBesieged()) return;
  if (strongholdCommandsUnavailable.value) {
    showUnavailableTip(strongholdCommandsTip.value);
    return;
  }
  strongholdMenuView.value = view;
}

function backToStrongholdCategories() {
  strongholdMenuView.value = "categories";
}

function cancelPopup() {
  strongholdMenuView.value = "categories";
  characterMenuView.value = "categories";
  emit("cancel");
}

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
  attackBlockReason.value ?? `对相邻敌方据点或部队下达攻城指令（消耗 ${ATTACK_AP_COST} AP）`
);

const siegeBlockReason = computed(() => siegeApBlockReason(props.unit));
const siegeUnavailable = computed(() => siegeBlockReason.value !== null);
const siegeTooltip = computed(() =>
  siegeBlockReason.value ?? `对相邻敌方据点下达攻城指令（消耗 ${SIEGE_AP_COST} AP）`
);

const expeditionUnavailable = computed(
  () => !props.canExpedition || besieged.value,
);
const expeditionTip = computed(() => {
  if (besieged.value && props.mode === "strongholdCommand") return strongholdBesiegedBlockTip;
  return props.expeditionTooltip ?? "从当主居城分配 SubUnit 与将领组建部队（默认在城中）";
});

const besieged = computed(() => props.isStrongholdBesieged === true);

const strongholdBesiegedBlockTip = "据点被围，无法执行命令";

function blockIfStrongholdBesieged(): boolean {
  if (
    (props.mode === "strongholdCommand" || props.mode === "characterCommand")
    && besieged.value
  ) {
    showUnavailableTip(strongholdBesiegedBlockTip);
    return true;
  }
  return false;
}

const strongholdCommandsUnavailable = computed(
  () => props.lordAtResidence === false || besieged.value,
);
const strongholdCommandsTip = computed(() => {
  if (besieged.value && props.mode === "strongholdCommand") return strongholdBesiegedBlockTip;
  return props.strongholdCommandsTooltip ?? "当主须在本家居城方可下达据点指令";
});

const taxRateUnavailable = computed(
  () =>
    besieged.value
    || props.lordAtResidence === false
    || props.canAdjustTax === false,
);
const taxRateTip = computed(
  () =>
    props.taxRateTooltip
    ?? (props.lordAtResidence === false
      ? strongholdCommandsTip.value
      : "调整人头/农/商/关税；非直辖城经信使传达后生效")
);

const governancePolicyUnavailable = computed(
  () =>
    besieged.value
    || props.lordAtResidence === false
    || props.canSetGovernancePolicy === false,
);
const governancePolicyTip = computed(
  () =>
    props.governancePolicyTooltip
    ?? (props.canSetGovernancePolicy === false
      ? "仅本家据点可设置方针"
      : "设定自由决策/军事/内政优先，每月自动向待命将领派任务")
);

function onGovernancePolicyClick() {
  if (blockIfStrongholdBesieged()) return;
  if (governancePolicyUnavailable.value) {
    showUnavailableTip(
      props.lordAtResidence === false ? strongholdCommandsTip.value : governancePolicyTip.value
    );
    return;
  }
  emit("beginGovernancePolicy");
}

function onTaxRateClick() {
  if (blockIfStrongholdBesieged()) return;
  if (taxRateUnavailable.value) {
    showUnavailableTip(
      props.lordAtResidence === false ? strongholdCommandsTip.value : taxRateTip.value
    );
    return;
  }
  emit("beginTaxRate");
}

function onMercenaryRecruitClick() {
  if (blockIfStrongholdBesieged()) return;
  if (strongholdCommandsUnavailable.value) {
    showUnavailableTip(strongholdCommandsTip.value);
    return;
  }
  emit("beginMercenaryRecruit");
}

function onRecruitClick() {
  if (blockIfStrongholdBesieged()) return;
  if (strongholdCommandsUnavailable.value) {
    showUnavailableTip(strongholdCommandsTip.value);
    return;
  }
  emit("beginRecruit");
}

function onExpeditionClick() {
  if (blockIfStrongholdBesieged()) return;
  if (expeditionUnavailable.value) {
    showUnavailableTip(expeditionTip.value);
    return;
  }
  emit("beginExpedition");
}

function onAppointClick() {
  if (blockIfStrongholdBesieged()) return;
  if (strongholdCommandsUnavailable.value) {
    showUnavailableTip(strongholdCommandsTip.value);
    return;
  }
  emit("beginAppointLord");
}

function onTransferClick() {
  if (blockIfStrongholdBesieged()) return;
  if (strongholdCommandsUnavailable.value) {
    showUnavailableTip(strongholdCommandsTip.value);
    return;
  }
  emit("beginTransferCharacter");
}

function onRecallClick() {
  if (blockIfStrongholdBesieged()) return;
  if (strongholdCommandsUnavailable.value) {
    showUnavailableTip(strongholdCommandsTip.value);
    return;
  }
  emit("beginRecallCharacter");
}

function onStrongholdMutedAction(tip: string) {
  if (blockIfStrongholdBesieged()) return;
  if (strongholdCommandsUnavailable.value) {
    showUnavailableTip(strongholdCommandsTip.value);
    return;
  }
  showUnavailableTip(tip);
}

const gateApCost = computed(() => Math.max(1, props.gateApCost ?? 1));
const lordApValue = computed(() => props.lordAp ?? 0);
const apInsufficient = computed(() => lordApValue.value < gateApCost.value);

const leaveAvailable = computed(
  () => props.canLeaveStronghold && !apInsufficient.value && !besieged.value,
);
const enterAvailable = computed(
  () => props.canEnterStronghold && !apInsufficient.value && !besieged.value,
);

const visitAvailable = computed(
  () => props.canVisitOthers && !besieged.value,
);

const characterEspionageAvailable = computed(
  () => props.canCharacterEspionage && !besieged.value,
);

const leaveTip = computed(() => {
  if (besieged.value) return strongholdBesiegedBlockTip;
  if (!props.canLeaveStronghold) return "仅在城内可出城";
  if (apInsufficient.value) return `行动力不足（需要 ${gateApCost.value} AP）`;
  return `离开据点出现在地图格（消耗 ${gateApCost.value} AP）`;
});

const enterTip = computed(() => {
  if (besieged.value) return strongholdBesiegedBlockTip;
  if (!props.canEnterStronghold) return "须在据点格上且处于地图方可入城";
  if (apInsufficient.value) return `行动力不足（需要 ${gateApCost.value} AP）`;
  return `进入同格据点（消耗 ${gateApCost.value} AP）`;
});

function onLeaveClick() {
  if (blockIfStrongholdBesieged()) return;
  if (leaveAvailable.value) {
    emit("beginLeaveStronghold");
    return;
  }
  showUnavailableTip(leaveTip.value);
}

function onCharacterMoveClick() {
  if (blockIfStrongholdBesieged()) return;
  if (props.canCharacterMove) {
    emit("beginMove");
    return;
  }
  showUnavailableTip("须先出城方可移动");
}

function onEnterClick() {
  if (blockIfStrongholdBesieged()) return;
  if (enterAvailable.value) {
    emit("beginEnterStronghold");
    return;
  }
  showUnavailableTip(enterTip.value);
}

function onVisitClick() {
  if (blockIfStrongholdBesieged()) return;
  if (visitAvailable.value) {
    emit("beginVisit");
    return;
  }
  showUnavailableTip("拜访功能尚未实装（RPG 模式扩展）");
}

function onCharacterEspionageClick() {
  if (blockIfStrongholdBesieged()) return;
  if (characterEspionageAvailable.value) {
    emit("beginEspionage");
    return;
  }
  showUnavailableTip(strongholdBesiegedBlockTip);
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
      <div class="actions actions--vertical">
        <StrategyMapActionButton
          variant="primary"
          :tooltip-side="tooltipSide"
          tooltip="设定部队作战方针（待机/移动/进攻/撤退/支援等）；与当主同格即时生效，否则派出信使传达"
          @click="emit('beginDirective')"
        >
          📜 方针
        </StrategyMapActionButton>
        <StrategyMapActionButton
          variant="muted"
          :tooltip-side="tooltipSide"
          tooltip="设定部队作战姿态（尚未实装）"
          @click="showUnavailableTip('姿态设定功能尚未实装')"
        >
          🛡 姿态
        </StrategyMapActionButton>
        <StrategyMapActionButton
          v-if="canUnitMove"
          variant="primary"
          :tooltip-side="tooltipSide"
          tooltip="在地图上规划路径并移动到目标格"
          @click="emit('beginMove')"
        >
          移动
        </StrategyMapActionButton>
        <StrategyMapActionButton
          v-if="canUnitSiege"
          :variant="attackUnavailable ? 'muted' : 'primary'"
          :tooltip="attackTooltip"
          :tooltip-side="tooltipSide"
          @click="emit('beginAttack')"
        >
          ⚔ 攻城
        </StrategyMapActionButton>
        <StrategyMapActionButton
          v-if="canUnitMove"
          variant="primary"
          :tooltip-side="tooltipSide"
          tooltip="同格友军合并或分割编制（尚未实装）"
          @click="emit('beginMerge')"
        >
          🏴 军团
        </StrategyMapActionButton>
        <StrategyMapActionButton
          v-if="canUnitEnterStronghold"
          variant="primary"
          :tooltip-side="tooltipSide"
          tooltip="进入同格据点，不占地图格"
          @click="emit('beginUnitEnterStronghold')"
        >
          🏯 入城
        </StrategyMapActionButton>
        <StrategyMapActionButton
          v-if="canUnitExitStronghold"
          variant="primary"
          :tooltip-side="tooltipSide"
          tooltip="离开据点，出现在地图格上"
          @click="emit('beginUnitExitStronghold')"
        >
          🚪 出城
        </StrategyMapActionButton>
        <StrategyMapActionButton
          v-if="canUnitDisband"
          variant="primary"
          :tooltip-side="tooltipSide"
          tooltip="在 Home 据点建制解散，兵力与物资归还据点"
          @click="emit('beginUnitDisband')"
        >
          📤 解散
        </StrategyMapActionButton>
        <StrategyMapActionButton
          v-if="canUnitOpenMarket"
          variant="primary"
          :tooltip-side="tooltipSide"
          tooltip="以商队库存在大宗市场买卖粮食"
          @click="emit('openUnitMarket')"
        >
          📈 交易
        </StrategyMapActionButton>
        <div class="divider" />
        <button type="button" class="map-action map-action--default" @click.stop="emit('showIntel')">
          📋 情报
        </button>
        <button type="button" class="map-action map-action--default" @click.stop="emit('cancel')">取消</button>
      </div>
    </template>

    <template v-else-if="mode === 'characterCommand'">
      <div class="actions actions--vertical">
        <template v-if="characterMenuView === 'merchants'">
          <button type="button" class="map-action map-action--default map-action--back" @click.stop="characterMenuView = 'categories'">
            ← 返回
          </button>
          <StrategyMapActionButton
            v-for="shop in merchantShopList"
            :key="shop.id"
            variant="primary"
            :tooltip-side="tooltipSide"
            :tooltip="`拜访 ${shop.name}`"
            @click="onMerchantShopClick(shop.id)"
          >
            🏪 {{ shop.name }}
          </StrategyMapActionButton>
          <div class="divider" />
          <button type="button" class="map-action map-action--default" @click.stop="emit('cancel')">取消</button>
        </template>

        <template v-else>
        <template v-if="canExecutePersonalCommands">
          <template v-if="characterMenuView === 'categories'">
            <StrategyMapActionButton
              variant="primary"
              :tooltip-side="tooltipSide"
              tooltip="以个人金库出资，亲自募兵/征兵"
              @click="openCharacterCategory('military')"
            >
              ⚔ 军备
            </StrategyMapActionButton>
            <StrategyMapActionButton
              variant="primary"
              :tooltip-side="tooltipSide"
              tooltip="个人内政指令（部分尚未实装）"
              @click="openCharacterCategory('domestic')"
            >
              🏛 内政
            </StrategyMapActionButton>
            <StrategyMapActionButton
              variant="primary"
              :tooltip-side="tooltipSide"
              tooltip="个人人事指令（部分尚未实装）"
              @click="openCharacterCategory('personnel')"
            >
              👤 人事
            </StrategyMapActionButton>
          </template>

          <template v-else-if="characterMenuView === 'military'">
            <button type="button" class="map-action map-action--default map-action--back" @click.stop="backToCharacterCategories">
              ← 返回
            </button>
            <StrategyMapActionButton
              variant="primary"
              :tooltip-side="tooltipSide"
              tooltip="以本人资金在城内募兵（60 日期限）"
              @click="onPersonalMercenaryRecruitClick"
            >
              💰 募兵
            </StrategyMapActionButton>
            <StrategyMapActionButton
              variant="primary"
              :tooltip-side="tooltipSide"
              tooltip="亲自在城内征兵（消耗民心/治安）"
              @click="onPersonalRecruitClick"
            >
              ⚔ 征兵
            </StrategyMapActionButton>
          </template>

          <template v-else-if="characterMenuView === 'domestic'">
            <button type="button" class="map-action map-action--default map-action--back" @click.stop="backToCharacterCategories">
              ← 返回
            </button>
            <StrategyMapActionButton
              variant="muted"
              :tooltip-side="tooltipSide"
              tooltip="个人建设功能尚未实装"
              @click="onPersonalMutedAction('个人建设功能尚未实装')"
            >
              🏗 建设
            </StrategyMapActionButton>
            <StrategyMapActionButton
              variant="muted"
              :tooltip-side="tooltipSide"
              tooltip="个人运输功能尚未实装"
              @click="onPersonalMutedAction('个人运输功能尚未实装')"
            >
              📦 运输
            </StrategyMapActionButton>
          </template>

          <template v-else-if="characterMenuView === 'personnel'">
            <button type="button" class="map-action map-action--default map-action--back" @click.stop="backToCharacterCategories">
              ← 返回
            </button>
            <StrategyMapActionButton
              variant="muted"
              :tooltip-side="tooltipSide"
              tooltip="个人人事功能尚未实装"
              @click="onPersonalMutedAction('个人登庸功能尚未实装')"
            >
              📜 登庸
            </StrategyMapActionButton>
          </template>

          <div class="divider" />
        </template>

        <StrategyMapActionButton
          v-if="canViewPersonalMarket"
          variant="primary"
          :tooltip-side="tooltipSide"
          :tooltip="personalMarketTooltip || '查看大宗市场行情（不可交易）'"
          @click="emit('openPersonalMarket')"
        >
          📈 市场
        </StrategyMapActionButton>
        <StrategyMapActionButton
          v-if="merchantShopList.length > 0"
          variant="primary"
          :tooltip-side="tooltipSide"
          tooltip="拜访城内商家（个人物品买卖尚未实装）"
          @click="openMerchantEntry"
        >
          🏪 商家
        </StrategyMapActionButton>

        <StrategyMapActionButton
          :variant="leaveAvailable ? 'primary' : 'muted'"
          :tooltip-side="tooltipSide"
          :tooltip="leaveTip"
          @click="onLeaveClick"
        >
          🚪 出城
        </StrategyMapActionButton>
        <StrategyMapActionButton
          :variant="canCharacterMove && !besieged ? 'primary' : 'muted'"
          :tooltip-side="tooltipSide"
          :tooltip="besieged ? strongholdBesiegedBlockTip : canCharacterMove ? '在地图上规划路径并移动' : '须先出城方可移动'"
          @click="onCharacterMoveClick"
        >
          移动
        </StrategyMapActionButton>
        <StrategyMapActionButton
          :variant="enterAvailable ? 'primary' : 'muted'"
          :tooltip-side="tooltipSide"
          :tooltip="enterTip"
          @click="onEnterClick"
        >
          🏯 入城
        </StrategyMapActionButton>
        <StrategyMapActionButton
          :variant="visitAvailable ? 'primary' : 'muted'"
          :tooltip-side="tooltipSide"
          :tooltip="besieged ? strongholdBesiegedBlockTip : '拜访城内将领：登庸、密谈、计谋等（RPG 模式扩展）'"
          @click="onVisitClick"
        >
          🤝 拜访
        </StrategyMapActionButton>
        <StrategyMapActionButton
          v-if="canCharacterEspionage"
          :variant="characterEspionageAvailable ? 'primary' : 'muted'"
          :tooltip-side="tooltipSide"
          :tooltip="besieged ? strongholdBesiegedBlockTip : '对该据点展开间谍搜索（从角色菜单下达）'"
          @click="onCharacterEspionageClick"
        >
          🕵 谍报
        </StrategyMapActionButton>
        <div class="divider" />
        <button type="button" class="map-action map-action--default" @click.stop="emit('showIntel')">
          📋 情报
        </button>
        <button type="button" class="map-action map-action--default" @click.stop="emit('cancel')">取消</button>
        </template>
      </div>
    </template>

    <template v-else-if="mode === 'foreignCommand'">
      <div class="actions actions--vertical">
        <StrategyMapActionButton
          variant="muted"
          :tooltip-side="tooltipSide"
          tooltip="计略功能尚未实装"
          @click="showUnavailableTip('计略功能尚未实装')"
        >
          🕵 计略
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
          v-if="showStrongholdDirective"
          variant="muted"
          :tooltip-side="tooltipSide"
          :tooltip="strongholdCommandsUnavailable ? strongholdCommandsTip : '外政据点方针设定尚未实装'"
          @click="onStrongholdMutedAction('外政据点方针设定尚未实装')"
        >
          📜 方针
        </StrategyMapActionButton>

        <template v-if="strongholdMenuView === 'categories'">
          <template v-if="!strongholdDirectiveOnly">
            <StrategyMapActionButton
              :variant="governancePolicyUnavailable ? 'muted' : 'primary'"
              :tooltip-side="tooltipSide"
              :tooltip="governancePolicyUnavailable ? governancePolicyTip : '设定自由决策/军事/内政优先，每月自动向待命将领派任务'"
              @click="onGovernancePolicyClick"
            >
              📋 方针
            </StrategyMapActionButton>
            <StrategyMapActionButton
              :variant="strongholdCommandsUnavailable ? 'muted' : 'primary'"
              :tooltip-side="tooltipSide"
              :tooltip="strongholdCommandsTip"
              @click="openStrongholdCategory('military')"
            >
              ⚔ 军备
            </StrategyMapActionButton>
            <StrategyMapActionButton
              :variant="strongholdCommandsUnavailable ? 'muted' : 'primary'"
              :tooltip-side="tooltipSide"
              :tooltip="strongholdCommandsTip"
              @click="openStrongholdCategory('domestic')"
            >
              🏛 内政
            </StrategyMapActionButton>
            <StrategyMapActionButton
              :variant="strongholdCommandsUnavailable ? 'muted' : 'primary'"
              :tooltip-side="tooltipSide"
              :tooltip="strongholdCommandsTip"
              @click="openStrongholdCategory('personnel')"
            >
              👤 人事
            </StrategyMapActionButton>
          </template>
        </template>

        <template v-else-if="strongholdMenuView === 'military' && !strongholdDirectiveOnly">
          <button type="button" class="map-action map-action--default map-action--back" @click.stop="backToStrongholdCategories">
            ← 返回
          </button>
          <StrategyMapActionButton
            :variant="expeditionUnavailable ? 'muted' : 'primary'"
            :tooltip-side="tooltipSide"
            :tooltip="expeditionTip"
            @click="onExpeditionClick"
          >
            🚩 组建
          </StrategyMapActionButton>
          <StrategyMapActionButton
            :variant="strongholdCommandsUnavailable ? 'muted' : 'primary'"
            :tooltip-side="tooltipSide"
            :tooltip="strongholdCommandsUnavailable ? strongholdCommandsTip : '从据点府库拨付预算，向将领发布募兵任务'"
            @click="onMercenaryRecruitClick"
          >
            💰 募兵
          </StrategyMapActionButton>
          <StrategyMapActionButton
            :variant="strongholdCommandsUnavailable ? 'muted' : 'primary'"
            :tooltip-side="tooltipSide"
            :tooltip="strongholdCommandsUnavailable ? strongholdCommandsTip : '向将领发布征兵任务'"
            @click="onRecruitClick"
          >
            ⚔ 征兵
          </StrategyMapActionButton>
        </template>

        <template v-else-if="strongholdMenuView === 'domestic' && !strongholdDirectiveOnly">
          <button type="button" class="map-action map-action--default map-action--back" @click.stop="backToStrongholdCategories">
            ← 返回
          </button>
          <StrategyMapActionButton
            :variant="taxRateUnavailable ? 'muted' : 'primary'"
            :tooltip-side="tooltipSide"
            :tooltip="taxRateTip"
            @click="onTaxRateClick"
          >
            💰 税率
          </StrategyMapActionButton>
          <StrategyMapActionButton
            :variant="canStrongholdTrade ? 'primary' : 'muted'"
            :tooltip-side="tooltipSide"
            :tooltip="canStrongholdTrade ? '以官府库在本城大宗市场买卖' : (strongholdTradeTooltip || '当前无法交易')"
            @click="canStrongholdTrade ? emit('openStrongholdMarket') : showUnavailableTip(strongholdTradeTooltip || '当前无法交易')"
          >
            📈 交易
          </StrategyMapActionButton>
          <StrategyMapActionButton
            variant="muted"
            :tooltip-side="tooltipSide"
            tooltip="据点建设功能尚未实装"
            @click="onStrongholdMutedAction('据点建设功能尚未实装')"
          >
            🏗 建设
          </StrategyMapActionButton>
          <StrategyMapActionButton
            variant="muted"
            :tooltip-side="tooltipSide"
            tooltip="据点运输功能尚未实装"
            @click="onStrongholdMutedAction('据点运输功能尚未实装')"
          >
            📦 运输
          </StrategyMapActionButton>
        </template>

        <template v-else-if="strongholdMenuView === 'personnel' && !strongholdDirectiveOnly">
          <button type="button" class="map-action map-action--default map-action--back" @click.stop="backToStrongholdCategories">
            ← 返回
          </button>
          <StrategyMapActionButton
            :variant="strongholdCommandsUnavailable ? 'muted' : 'primary'"
            :tooltip-side="tooltipSide"
            :tooltip="strongholdCommandsUnavailable ? strongholdCommandsTip : '选派将领担任领主或代官'"
            @click="onAppointClick"
          >
            👤 任命
          </StrategyMapActionButton>
          <StrategyMapActionButton
            :variant="strongholdCommandsUnavailable ? 'muted' : 'primary'"
            :tooltip-side="tooltipSide"
            :tooltip="strongholdCommandsUnavailable ? strongholdCommandsTip : '自本据点派遣或将其它据点将领召集至本据点'"
            @click="onTransferClick"
          >
            🚶 调动
          </StrategyMapActionButton>
          <StrategyMapActionButton
            :variant="strongholdCommandsUnavailable ? 'muted' : 'primary'"
            :tooltip-side="tooltipSide"
            :tooltip="strongholdCommandsUnavailable ? strongholdCommandsTip : '中断外派任务，令其尽快回城（效果减半，未用资金退回）'"
            @click="onRecallClick"
          >
            ↩ 召回
          </StrategyMapActionButton>
        </template>

        <div class="divider" />
        <button type="button" class="map-action map-action--default" @click.stop="emit('showIntel')">
          📋 情报
        </button>
        <button type="button" class="map-action map-action--default" @click.stop="cancelPopup">取消</button>
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
          v-if="canEspionage && !hideStrongholdEspionage"
          variant="primary"
          :tooltip-side="tooltipSide"
          tooltip="对该据点展开间谍搜索，揭示内政或军事情报（约 2 个月有效）"
          @click="emit('beginEspionage')"
        >
          🕵 间谍
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
      <div class="title">选择攻城目标</div>
      <div class="subtitle hint">点击相邻敌军或敌方据点所在格</div>
      <div class="subtitle hint">右键取消并返回指令菜单</div>
      <div class="actions actions--vertical">
        <button type="button" class="map-action map-action--default" @click.stop="emit('cancel')">取消</button>
      </div>
    </template>

    <template v-else-if="mode === 'mergeSelect'">
      <div class="title">选择军团目标</div>
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

.map-action--back {
  margin-bottom: 2px;
  color: #cbd5e1;
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
