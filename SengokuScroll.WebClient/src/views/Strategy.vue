<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { ElMessage, ElMessageBox } from "element-plus";
import {
  advanceDay,
  advanceDays,
  listStrategySaveSlots,
  saveStrategyToSlot,
  loadStrategyFromSlot,
  getMovementTrace,
  getStrategyState,
  getStrategyMapMaster,
  loadScenario,
  orderUnitAttack,
  orderUnitSiege,
  mergeUnits,
  splitUnit,
  deployFromStronghold,
  enterUnitStronghold,
  exitUnitStronghold,
  disbandUnitOrganizationally,
  recordEspionageIntel,
  previewDiplomacyMission,
  orderDiplomacyMission,
  previewPeaceSettlement,
  orderPeaceSettlement,
  setStrongholdTaxRates,
  setStrongholdGovernancePriority,
  recruitAtStronghold,
  mercenaryRecruitAtStronghold,
  personalRecruit,
  personalMercenaryRecruit,
  appointStrongholdLord,
  transferCharacterToStronghold,
  recallCharacter,
  moveUnit,
  leaveStrongholdAsCharacter,
  moveCharacter,
  enterStrongholdAsCharacter,
  interactWithCharacter,
  previewCharacterPath,
  previewBattle,
  previewUnitPath,
  setUnitDirective,
  setApiMode,
  strategyApiDiagnostics,
  type StrategyApiMode,
  type StrategyBattlePreview,
  type StrategyBattleResult,
  type StrategyEvent,
  type StrategyEconomySettlementDetail,
  type StrategyMovementTraceEntry,
  type StrategySaveSlotSummary,
  type StrategyUnitState,
  type StrategyWorldState,
  type StrategyMapMasterState,
  type MapPoint,
  type StrategyPeaceTermsPayload,
} from "@/api/strategy";
import { heartbeatMultiplayerRoom, getMultiplayerEvents, acknowledgeMultiplayerEvents,
  leaveMultiplayerRoom, readMultiplayerSession } from "@/api/multiplayerClient";
import StrategyMapCanvas from "@/components/strategy/StrategyMapCanvas.vue";
import StrategyMapCellEntityPicker from "@/components/strategy/StrategyMapCellEntityPicker.vue";
import StrategyMapLoadingScene, {
  type StrategyMapLoadingPhase,
} from "@/components/strategy/StrategyMapLoadingScene.vue";
import StrategyIntelBar from "@/components/strategy/StrategyIntelBar.vue";
import StrategyMapPopup from "@/components/strategy/StrategyMapPopup.vue";
import StrategyEntityIntelDialog, {
  type EntityIntelTarget,
} from "@/components/strategy/StrategyEntityIntelDialog.vue";
import StrategyBattleConfirmDialog, {
  type BattleConfirmPayload,
} from "@/components/strategy/StrategyBattleConfirmDialog.vue";
import StrategyBattleResultDialog from "@/components/strategy/StrategyBattleResultDialog.vue";
import StrategyDirectiveDialog from "@/components/strategy/StrategyDirectiveDialog.vue";
import StrategySplitDialog from "@/components/strategy/StrategySplitDialog.vue";
import StrategyExpeditionDialog from "@/components/strategy/StrategyExpeditionDialog.vue";
import StrategyDiplomacyDialog, {
  type DiplomacyMissionAction,
} from "@/components/strategy/StrategyDiplomacyDialog.vue";
import StrategyTaxRateDialog from "@/components/strategy/StrategyTaxRateDialog.vue";
import StrategyMarketDialog from "@/components/strategy/StrategyMarketDialog.vue";
import {
  isMerchantTradeUnit,
  isStrongholdMarketOpen,
  strongholdHasMarketFacility,
  strongholdMerchantActors,
} from "@/utils/strategyMarketHelpers";
import StrategyStrongholdGovernanceDialog, {
  type StrongholdGovernancePriorityValue,
} from "@/components/strategy/StrategyStrongholdGovernanceDialog.vue";
import StrategyRecruitDialog from "@/components/strategy/StrategyRecruitDialog.vue";
import StrategyMercenaryRecruitDialog from "@/components/strategy/StrategyMercenaryRecruitDialog.vue";
import StrategyAppointLordDialog from "@/components/strategy/StrategyAppointLordDialog.vue";
import StrategyTransferCharacterDialog from "@/components/strategy/StrategyTransferCharacterDialog.vue";
import StrategyRecallCharacterDialog from "@/components/strategy/StrategyRecallCharacterDialog.vue";
import StrategyEventFeed from "@/components/strategy/StrategyEventFeed.vue";
import StrategyNotificationTray, {
  type StrategyPendingNotification,
} from "@/components/strategy/StrategyNotificationTray.vue";
import StrategyMessageFeedToolbar from "@/components/strategy/StrategyMessageFeedToolbar.vue";
import StrategyMessageDialog from "@/components/strategy/StrategyMessageDialog.vue";
import StrategyRecruitReportBubble from "@/components/strategy/StrategyRecruitReportBubble.vue";
import {
  filterEventsByMessageScope,
} from "@/utils/strategyMessageScope";
import {
  handlePendingNotificationOpen,
  shouldSkipPendingNotification,
} from "@/eventNotifications/PendingNotificationBehaviors";
import StrategyEconomySettlementDialog from "@/components/strategy/StrategyEconomySettlementDialog.vue";
import StrategyIntelSystemDialog from "@/components/strategy/StrategyIntelSystemDialog.vue";
import StrategyOperableUnitList from "@/components/strategy/StrategyOperableUnitList.vue";
import StrategySystemMenuDialog from "@/components/strategy/StrategySystemMenuDialog.vue";
import StrategySaveSlotDialog from "@/components/strategy/StrategySaveSlotDialog.vue";
import StrategyForceCommandPopup from "@/components/strategy/StrategyForceCommandPopup.vue";
import StrategyCellIntelHover from "@/components/strategy/StrategyCellIntelHover.vue";
import StrategyMapViewControls from "@/components/strategy/StrategyMapViewControls.vue";
import StrategyTutorialDialog from "@/components/strategy/StrategyTutorialDialog.vue";
import type { MapViewportWorldRect } from "@/components/strategy/strategyMinimapTypes";
import type { MapRouteOverlay, MapMoveRelayMarker } from "@/components/strategy/mapRouteStyles";
import { getForceColorCss } from "@/components/strategy/forceColors";
import type { StrategyMapColorMode } from "@/utils/mapEntityColors";
import {
  countOwnCharacters,
  countOwnStrongholds,
  countRealmCharacters,
  countRealmStrongholds,
} from "@/utils/strategyRealmStats";
import { formatFoodKoku, formatMoneyKan, formatSoldiers } from "@/utils/strategyDisplayUnits";
import { useStrategyMapInteraction } from "@/composables/useStrategyMapInteraction";
import { characterSocialError } from "@/utils/characterSocialError";
import type { StrategyMoveTarget } from "@/strategyMapInteraction/types";
import {
  DEFAULT_ROUTE_VISIBILITY_POLICY,
  filterUnitsForRouteDisplay,
} from "@/strategyMapInteraction/routeVisibilityPolicy";
import {
  resolveCornerHintMode,
  resolveIntelMainTabForMenuPopup,
  resolveMenuPopupMode,
  resolvePrimaryPopupEntityName,
  popupUsesCorner as isCornerPopupMode,
} from "@/strategyMapInteraction/PopupModeBehavior";
import { resolveStrategyApiSourceInfo } from "@/api/StrategyApiSourceInfoBehavior";
import { resolveAnchoredPanelPlacement, resolveAnchoredPanelPlacementForSide, type AnchorSide, type AnchorVerticalAlign } from "@/utils/mapCellAnchor";
import { parseEconomySettlementFromEvent } from "@/utils/normalizeStrategyEvent";
import {
  findPointOnPath,
  truncateMovePathAtCell,
} from "@/utils/movePathPlanning";
import {
  clearMovePathDebug,
  formatPathPoints,
  logMovePath,
  movePathDebugEntries,
} from "@/utils/movePathDebug";
import type { UnitDirectiveValue } from "@/utils/unitDirective";
import { landmarkAtCell, mapTileInfo, roadAtCell } from "@/utils/mapTileLookup";
import { mapMasterMatchesScenario } from "@/utils/strategyMapDefaults";
import { logStrategyMapCoords, logHoverIntelLayoutDebug, rectToScreenDebug } from "@/utils/strategyMapDebug";
import { attackApBlockReason, siegeApBlockReason } from "@/utils/strategyActionRules";
import {
  applyStrategyApiErrorResolution,
  resolveStrategyApiError,
  type ApiErrorResolveContext,
} from "@/apiErrors/ApiErrorMessageBehaviors";
import { isForeignIntelRestricted } from "@/utils/strategyIntelDisplay";
import { validateDiplomacyMissionTarget } from "@/utils/strategyIntelSystemData";
import {
  isLordAtResidence,
  LORD_AT_RESIDENCE_REQUIRED_TIP,
  LORD_COMMAND_STRONGHOLD_TIP,
  resolveLordResidenceStronghold,
} from "@/utils/strategyLordCommands";
import {
  canConfigureStrongholdGovernancePolicy,
  governancePolicyBlockReason,
} from "@/utils/strategyGovernancePolicy";
import {
  canCharacterEspionageAtCell,
  canEnterStrongholdAtCell,
  canExecutePersonalStrongholdCommands,
  canLordCommandStronghold,
  canShowStrongholdDirectiveButton,
  countOtherCharactersInStronghold,
  CHARACTER_GATE_AP_COST,
  isLordDirectlyControlledUnit,
  isInnerVassalRealmStronghold,
  isLordInStronghold,
  isLordOnMap,
  isStrongholdBesieged,
  resolveCharacterGateStronghold,
  resolveCharacterStronghold,
  resolvePlayerLordCharacterId,
  strongholdAtLordCell,
} from "@/utils/strategyPlayerCharacter";
import {
  notificationFromEvent,
  recruitCompletionBubbleMessage,
  strategicReportDetailText,
} from "@/utils/strategyNotifications";
import { messageCategoryLabel } from "@/utils/messageCategories";
import {
  canShowCellHoverIntel,
  canShowTileMapIntel,
  convoysAtCellForIntel,
  isTileVisible,
  messengersAtCellForIntel,
  strongholdsAtCellForIntel,
  unitsAtCellForIntel,
} from "@/utils/strategyFogCell";
import { findOperableUnit, isMapOperableUnit, operableUnitAsMapState, resolveOperableUnitStrongholdId } from "@/utils/strategyOperableUnits";
import { collectMapCellEntityOptions } from "@/utils/mapCellEntityPicker";
import type { IntelRealmFilterMode } from "@/utils/intelRealmFilter";
import {
  buildLoadStartOptions,
  resolveDifficultyFromOptions,
  writeGameStartSettings,
  type GameStartSettings,
} from "@/utils/strategyGameStartSettings";

const emit = defineEmits<{
  "request-game-start": [];
  "exit-multiplayer": [];
}>();

const props = withDefaults(
  defineProps<{
    /** 挂载时自动恢复后端当前局（Home 页由用户确认后再启动，应为 false）。 */
    autoResume?: boolean;
  }>(),
  { autoResume: false },
);
const HOVER_INTEL_W = 280;
const HOVER_INTEL_H = 360;
const HOVER_INTEL_DUAL_GAP = 8;
const MENU_POPUP_W = 200;
const MENU_POPUP_H = 180;

const state = ref<StrategyWorldState | null>(null);
const mapMaster = ref<StrategyMapMasterState | null>(null);
const initialLoading = ref(true);
const lastGameStartSettings = ref<GameStartSettings | null>(null);
const initialLoadPhase = ref<StrategyMapLoadingPhase>("map");
const initialLoadError = ref("");
const loading = ref(false);
const error = ref("");
const info = ref("");
const multiplayerSession = ref(readMultiplayerSession());
const tutorialVisible = ref(false);
const TUTORIAL_STORAGE_KEY = "sengoku.strategy.tutorial.completed.v1";
const selectedUnitId = ref<number | null>(null);
const selectedCharacterId = ref<number | null>(null);
const selectedStrongholdId = ref<number | null>(null);
const selectedConvoyId = ref<number | null>(null);
const selectedCell = ref<{ x: number; y: number } | null>(null);
const hoverCell = ref<{ x: number; y: number; screenX: number; screenY: number } | null>(null);
const mapPanelRef = ref<HTMLElement | null>(null);
const mapCanvasRef = ref<InstanceType<typeof StrategyMapCanvas> | null>(null);
const minimapViewport = ref<MapViewportWorldRect | null>(null);
const menuPopupRef = ref<InstanceType<typeof StrategyMapPopup> | null>(null);
const cellEntityPickerRef = ref<InstanceType<typeof StrategyMapCellEntityPicker> | null>(null);
const cornerPopupRef = ref<InstanceType<typeof StrategyMapPopup> | null>(null);
const hoverIntelLayerRef = ref<HTMLElement | null>(null);
const mapTopOverlayRef = ref<HTMLElement | null>(null);
const mapTopOverlayHeight = ref(56);
/** 可操作部队列表与顶部 overlay（含情报/系统按钮、势力情报栏）之间的额外间距。 */
const UNIT_ROSTER_TOP_GAP = 8;
const UNIT_ROSTER_BOTTOM_RESERVE = 132;
let mapTopOverlayResizeObserver: ResizeObserver | null = null;
const intelPinnedCell = ref<{ x: number; y: number } | null>(null);
const intelLayerHovered = ref(false);
const movementTrace = ref<StrategyMovementTraceEntry[]>([]);
const moveCommittedWaypoints = ref<MapPoint[]>([]);
const movePendingRelay = ref<MapPoint | null>(null);

const diplomacyDialogVisible = ref(false);
const diplomacyAction = ref<DiplomacyMissionAction>("Ally");
const diplomacyCharacterId = ref<number | null>(null);
const diplomacyTargetForceId = ref<number | null>(null);
const diplomacyInitialTargetForceId = ref<number | null>(null);
const diplomacySuccessChance = ref<number | null>(null);
const diplomacyTravelDays = ref<number | null>(null);
const diplomacyPreviewLoading = ref(false);
const diplomacyForcePickActive = ref(false);
const diplomacyPeaceRequiredWarScore = ref<number | null>(null);
const diplomacyPeaceCanForceAcceptance = ref(false);
const diplomacyPeaceTerms = ref<Omit<StrategyPeaceTermsPayload, "characterId" | "targetForceId">>({
  cededStrongholdIds: [],
  reparationsMoney: 0,
  demandOuterVassalage: false,
});

const onDiplomacyForceStrongholdPickedRef = ref<(strongholdId: number) => boolean>(
  () => false
);
const onDiplomacyForcePickCancelledRef = ref<() => void>(() => {});

const mapInteraction = useStrategyMapInteraction({
  worldState: state,
  selectedUnitId,
  selectedCharacterId,
  selectedStrongholdId,
  selectedConvoyId,
  selectedCell,
  hoverCell,
  onDiplomacyForceStrongholdPicked: (strongholdId) =>
    onDiplomacyForceStrongholdPickedRef.value(strongholdId),
  onDiplomacyForcePickCancelled: () => onDiplomacyForcePickCancelledRef.value(),
});

const {
  menuAnchor,
  lockedCommand,
  stateId,
  mapUnitSelectionEnabled,
  mapStrongholdSelectionEnabled,
  mapConvoySelectionEnabled,
  mapCharacterSelectionEnabled,
  mapCellSelectionEnabled,
  mapRightClickEnabled,
  popupMode,
  cellEntityPickerOptions,
  onSelectUnit,
  onSelectStronghold,
  onSelectCharacter,
  onSelectConvoy,
  onSelectCellEntities,
  onPickCellEntity,
  onSelectCell,
  onHoverCell,
  onMapRightClick,
  onBeginMove,
  onBeginAttack,
  onBeginMerge,
  enterSplitSpawnSelection,
  enterDiplomacyForceSelection,
  pendingMergeTargetUnitId,
  pendingSplitSubUnitIds,
  onConfirmBattle,
  onBattlePreviewReady,
  onCancel,
  onMoveSucceeded,
  onMoveFailed,
  onBattleSucceeded,
  onBattleFailed,
  enterExecutingCommand,
} = mapInteraction;

const isDiplomacyForcePicking = computed(
  () => popupMode.value === "diplomacyForceSelect" || diplomacyForcePickActive.value
);

const previewRoutePoints = ref<MapPoint[]>([]);
const battlePreview = ref<StrategyBattlePreview | null>(null);
const battleConfirmVisible = ref(false);
const battleResult = ref<StrategyBattleResult | null>(null);
const battleResultVisible = ref(false);
const eventFeed = ref<StrategyEvent[]>([]);
const pendingNotifications = ref<StrategyPendingNotification[]>([]);
const settlementDialogVisible = ref(false);
const settlementDetail = ref<StrategyEconomySettlementDetail | null>(null);
const eventDetailVisible = ref(false);
const eventDetailTitle = ref("");
const eventDetailText = ref("");
const hoverIntelAnchorSide = ref<AnchorSide>("right");
const hoverIntelVerticalAlign = ref<AnchorVerticalAlign>("start");
const messageDialogVisible = ref(false);
interface RecruitReportBubbleState {
  characterName: string;
  message: string;
  event?: StrategyEvent;
}
const recruitReportBubble = ref<RecruitReportBubbleState | null>(null);
let recruitReportBubbleTimer: ReturnType<typeof setTimeout> | null = null;
const showPlayerMessages = ref(true);
const showWorldMessages = ref(true);
const directiveDialogVisible = ref(false);
const splitDialogVisible = ref(false);
const expeditionDialogVisible = ref(false);
const taxRateDialogVisible = ref(false);
const governancePolicyDialogVisible = ref(false);
const recruitDialogVisible = ref(false);
const mercenaryRecruitDialogVisible = ref(false);
const recruitDialogMode = ref<"assign" | "personal">("assign");
const mercenaryRecruitDialogMode = ref<"assign" | "personal">("assign");
const appointLordDialogVisible = ref(false);
const transferCharacterDialogVisible = ref(false);
const recallCharacterDialogVisible = ref(false);
const pendingSplitUnitName = ref<string | undefined>(undefined);
const intelSystemVisible = ref(false);
const intelSystemInitialTab = ref("force");
const intelSystemInitialRealmFilter = ref<IntelRealmFilterMode>("all");
const intelSystemInitialEntityId = ref<number | null>(null);
const intelSystemFocusMode = ref(false);
const intelSystemFocusTitle = ref("");
const systemMenuVisible = ref(false);
const saveSlotDialogVisible = ref(false);
const saveSlots = ref<StrategySaveSlotSummary[]>([]);
const saveSlotsLoading = ref(false);
const forceCommandVisible = ref(false);
const forceStatusRef = ref<HTMLElement | null>(null);
const forcePopupRef = ref<InstanceType<typeof StrategyForceCommandPopup> | null>(null);
const intelDialogVisible = ref(false);
const intelDialogTarget = ref<EntityIntelTarget | null>(null);
const hoverIntelStyle = ref<
  { display: "none" } | { position: "fixed"; left: string; top: string; zIndex: string }
>({ display: "none" });
let previewRequestSerial = 0;

const apiMode = computed(() => strategyApiDiagnostics.mode);
const lastRequest = computed(() => strategyApiDiagnostics.last);
const usingMockFallback = computed(() => strategyApiDiagnostics.usingMockFallback);

const dateText = computed(() => {
  if (!state.value) return " —";
  const d = state.value.date;
  const month = String(d.month).padStart(2, " ");
  const day = String(d.day).padStart(2, " ");
  return ` ${d.year}年${month}月${day}日`;
});

const campaignStatusText = computed(() => {
  const campaign = state.value?.campaignStatus;
  if (!campaign) return "";
  if (state.value?.allForcesAiControlled) {
    const leader = campaign.leadingForceName ? ` · 领先：${campaign.leadingForceName}` : "";
    return `观战 ${campaign.totalStrongholdCount} 城${leader}`;
  }
  if (campaign.state === "Victory") return "统一完成";
  if (campaign.state === "Defeat") return "本家覆灭";
  return `统一进度 ${campaign.playerStrongholdCount}/${campaign.totalStrongholdCount}`;
});

const campaignStatusType = computed<"success" | "danger" | "warning" | "info">(() => {
  const campaign = state.value?.campaignStatus;
  if (campaign?.state === "Victory") return "success";
  if (campaign?.state === "Defeat") return "danger";
  if (state.value?.allForcesAiControlled) return "warning";
  return "info";
});

function maybeShowTutorial() {
  try {
    tutorialVisible.value = localStorage.getItem(TUTORIAL_STORAGE_KEY) !== "1";
  } catch {
    tutorialVisible.value = true;
  }
}

function completeTutorial() {
  try {
    localStorage.setItem(TUTORIAL_STORAGE_KEY, "1");
  } catch {
    // 隐私模式下仍允许完成本次引导。
  }
}

function openTutorial() {
  gamePaused.value = true;
  tutorialVisible.value = true;
}

const selectedOperableUnit = computed(() => {
  if (!state.value || selectedUnitId.value == null) return null;
  return findOperableUnit(state.value, selectedUnitId.value);
});

const selectedOperableUnitDisplayName = computed(
  () => selectedOperableUnit.value?.unit.name ?? "",
);

const selectedUnit = computed(() => {
  const entry = selectedOperableUnit.value;
  if (!entry || !state.value) return null;
  return operableUnitAsMapState(state.value, entry);
});

const selectedStronghold = computed(
  () => state.value?.strongholds.find((s) => s.id === selectedStrongholdId.value) ?? null
);

const popupStronghold = computed(() => {
  if (!menuAnchor.value || !state.value) return null;
  return (
    state.value.strongholds.find(
      (s) => s.x === menuAnchor.value!.x && s.y === menuAnchor.value!.y
    ) ?? null
  );
});

const canSiegePopupStronghold = computed(() => {
  const unit = selectedUnit.value;
  const sh = popupStronghold.value;
  const playerForceId = state.value?.playerForceId;
  if (!unit || !sh || playerForceId == null) return false;
  if (sh.forceId === playerForceId) return false;
  const dist = Math.abs(unit.x - sh.x) + Math.abs(unit.y - sh.y);
  return dist <= 1;
});

const isLordAtOwnResidence = computed(() =>
  state.value ? isLordAtResidence(state.value) : false
);

const canLordCommandActiveStronghold = computed(() => {
  if (!state.value) return false;
  return canLordCommandStronghold(state.value, activeStrongholdForCommands.value);
});

const lordStrongholdAtCell = computed(() =>
  state.value ? strongholdAtLordCell(state.value) : null
);

const activeCharacterStronghold = computed(
  () => selectedStronghold.value ?? lordStrongholdAtCell.value ?? popupStronghold.value,
);

const activeCharacterForCommands = computed(() => {
  const ws = state.value;
  if (!ws) return null;
  const characterId =
    menuPopupMode.value === "characterCommand"
      ? selectedCharacterId.value ?? resolvePlayerLordCharacterId(ws)
      : resolvePlayerLordCharacterId(ws);
  if (characterId == null) return null;
  return ws.characters?.find((c) => c.id === characterId) ?? null;
});

const activeCharacterStrongholdForCommands = computed(() => {
  const ws = state.value;
  const character = activeCharacterForCommands.value;
  if (!ws || !character) return null;
  return resolveCharacterStronghold(ws, character.id);
});

const canExecutePersonalCommands = computed(() => {
  if (!state.value || menuPopupMode.value !== "characterCommand") return false;
  const characterId =
    selectedCharacterId.value ?? resolvePlayerLordCharacterId(state.value);
  return canExecutePersonalStrongholdCommands(state.value, characterId);
});

const lordAp = computed(() => state.value?.lord.ap ?? 0);

const characterPopupProps = computed(() => {
  const ws = state.value;
  if (!ws) {
    return {
      canLeaveStronghold: false,
      canCharacterMove: false,
      canEnterStronghold: false,
      canVisitOthers: false,
      canCharacterEspionage: false,
      isStrongholdBesieged: false,
    };
  }
  const inStronghold = isLordInStronghold(ws);
  const onMap = isLordOnMap(ws);
  const sh = activeCharacterStronghold.value;
  const besieged = isStrongholdBesieged(sh);
  return {
    canLeaveStronghold: inStronghold,
    canCharacterMove: onMap,
    canEnterStronghold: canEnterStrongholdAtCell(ws, sh),
    canVisitOthers: inStronghold && sh != null && countOtherCharactersInStronghold(ws, sh.id) > 0,
    canCharacterEspionage: canCharacterEspionageAtCell(ws, sh),
    isStrongholdBesieged: besieged,
  };
});

const primaryPopupEntityName = computed(() =>
  resolvePrimaryPopupEntityName(menuPopupMode.value, {
    lordName: state.value?.lord.name,
    strongholdName:
      activeStrongholdForCommands.value?.name ?? popupStronghold.value?.name,
    fallbackName: popupEntityName.value,
  }),
);

/** 据点指令（任命/征兵/税率等）的作用对象：地图据点菜单以弹窗格为准，避免情报面板选中它势力据点时误判。 */
const activeStrongholdForCommands = computed(() => {
  if (menuPopupMode.value === "strongholdCommand" && popupStronghold.value) {
    return popupStronghold.value;
  }
  return selectedStronghold.value ?? popupStronghold.value;
});

const showStrongholdDirectiveButton = computed(() => {
  if (!state.value) return false;
  return canShowStrongholdDirectiveButton(state.value, activeStrongholdForCommands.value);
});

/** 内藩据点仅显示方针，不显示本家据点内政/军事指令。 */
const strongholdDirectiveOnlyMenu = computed(() => {
  if (!state.value) return false;
  return isInnerVassalRealmStronghold(state.value, activeStrongholdForCommands.value);
});

const selectedUnitDirectlyControlled = computed(() => {
  if (!state.value) return false;
  return isLordDirectlyControlledUnit(state.value, selectedUnit.value);
});

function isPlayerAllyForce(ws: StrategyWorldState, forceId: number): boolean {
  if (forceId === ws.playerForceId) return true;
  return ws.diplomacies.some((d) => d.targetForceId === forceId && d.relation === "Allied");
}

const unitPopupStronghold = computed(() => {
  const entry = selectedOperableUnit.value;
  const ws = state.value;
  if (!entry || !ws) return null;

  const strongholdId = resolveOperableUnitStrongholdId(entry);
  if (strongholdId != null) {
    return ws.strongholds.find((s) => s.id === strongholdId) ?? null;
  }

  return popupStronghold.value;
});

const canUnitEnterStronghold = computed(() => {
  const unit = selectedUnit.value;
  const sh = popupStronghold.value;
  const ws = state.value;
  if (!unit || !sh || !ws || !selectedUnitDirectlyControlled.value) return false;
  if (unit.inStronghold || unit.soldiers <= 0) return false;
  if (unit.x !== sh.x || unit.y !== sh.y) return false;
  return isPlayerAllyForce(ws, sh.forceId);
});

const canUnitExitStronghold = computed(() => {
  const unit = selectedUnit.value;
  const sh = unitPopupStronghold.value;
  if (!unit || !sh || !selectedUnitDirectlyControlled.value) return false;
  return unit.inStronghold === true && unit.locationStrongholdId === sh.id && unit.soldiers > 0;
});

const canUnitDisband = computed(() => {
  const unit = selectedUnit.value;
  const sh = unitPopupStronghold.value;
  const ws = state.value;
  if (!unit || !sh || !ws || !selectedUnitDirectlyControlled.value) return false;
  return (
    unit.inStronghold === true
    && unit.homeStrongholdId != null
    && unit.homeStrongholdId === sh.id
    && unit.locationStrongholdId === sh.id
    && sh.forceId === ws.playerForceId
  );
});

const canUnitOpenMarket = computed(() => {
  const unit = selectedUnit.value;
  const sh = unitPopupStronghold.value;
  const ws = state.value;
  if (!unit || !sh || !ws || !selectedUnitDirectlyControlled.value) return false;
  if (!isMerchantTradeUnit(unit)) return false;
  if (!unit.inStronghold || unit.locationStrongholdId !== sh.id) return false;
  if (!isStrongholdMarketOpen(sh)) return false;
  return isPlayerAllyForce(ws, sh.forceId);
});

const popupMerchantShops = computed(() => {
  const ws = state.value;
  if (!ws) return [];
  const sh =
    menuPopupMode.value === "characterCommand"
      ? activeCharacterStronghold.value
      : menuPopupMode.value === "strongholdCommand"
        ? activeStrongholdForCommands.value
        : unitPopupStronghold.value;
  return strongholdMerchantActors(sh).map((a) => ({ id: a.id, name: a.name }));
});

const canViewPersonalMarket = computed(() => {
  const sh = activeCharacterStronghold.value;
  if (menuPopupMode.value !== "characterCommand" || !sh) return false;
  return isStrongholdMarketOpen(sh);
});

const personalMarketTooltip = computed(() => {
  const sh = activeCharacterStronghold.value;
  if (!sh) return "须在城内";
  if (!strongholdHasMarketFacility(sh)) return "该据点尚未建设市场设施";
  if (!isStrongholdMarketOpen(sh)) return "围城或封锁中，市场已关闭";
  return "查看大宗市场行情（个人不可交易）";
});

const canStrongholdTrade = computed(() => {
  if (!canLordCommandActiveStronghold.value || menuPopupMode.value !== "strongholdCommand") {
    return false;
  }
  const sh = activeStrongholdForCommands.value;
  return Boolean(sh && isStrongholdMarketOpen(sh));
});

const strongholdTradeTooltip = computed(() => {
  const sh = activeStrongholdForCommands.value;
  if (!canLordCommandActiveStronghold.value) return LORD_COMMAND_STRONGHOLD_TIP;
  if (!sh) return LORD_AT_RESIDENCE_REQUIRED_TIP;
  if (!strongholdHasMarketFacility(sh)) return "该据点尚未建设市场设施";
  if (!isStrongholdMarketOpen(sh)) return "围城或封锁中，市场已关闭";
  return "以官府库在本城大宗市场买卖";
});

const marketDialogVisible = ref(false);
const marketDialogTradeMode = ref<"view" | "lord" | "unit">("view");
const marketDialogStrongholdId = ref<number | null>(null);
const marketDialogStrongholdName = ref("");
const marketDialogTradeUnit = ref<StrategyUnitState | null>(null);

const marketDialogLordTreasury = computed(() => {
  const sh = state.value?.strongholds.find((s) => s.id === marketDialogStrongholdId.value);
  return { money: sh?.money ?? 0, food: sh?.food ?? 0, horse: sh?.horse ?? 0 };
});

const marketDialogTradeUnitResolved = computed(() => {
  if (!marketDialogTradeUnit.value || !state.value) return null;
  return state.value.units.find((u) => u.id === marketDialogTradeUnit.value!.id) ?? marketDialogTradeUnit.value;
});

function openMarketDialog(options: {
  strongholdId: number;
  strongholdName: string;
  mode: "view" | "lord" | "unit";
  tradeUnit?: StrategyUnitState | null;
}) {
  marketDialogStrongholdId.value = options.strongholdId;
  marketDialogStrongholdName.value = options.strongholdName;
  marketDialogTradeMode.value = options.mode;
  marketDialogTradeUnit.value = options.tradeUnit ?? null;
  marketDialogVisible.value = true;
  onCancel();
}

function handleOpenPersonalMarket() {
  const sh = activeCharacterStronghold.value;
  if (!sh || !canViewPersonalMarket.value) {
    void notifyActionBlocked("无法打开市场", personalMarketTooltip.value);
    return;
  }
  openMarketDialog({ strongholdId: sh.id, strongholdName: sh.name, mode: "view" });
}

function handleOpenUnitMarket() {
  const unit = selectedUnit.value;
  const sh = unitPopupStronghold.value;
  if (!unit || !sh || !canUnitOpenMarket.value) {
    void notifyActionBlocked("无法交易", "须为城内商队且市场开放");
    return;
  }
  openMarketDialog({
    strongholdId: sh.id,
    strongholdName: sh.name,
    mode: "unit",
    tradeUnit: unit,
  });
}

function handleOpenStrongholdMarket() {
  const sh = activeStrongholdForCommands.value;
  if (!sh || !canStrongholdTrade.value) {
    void notifyActionBlocked("无法交易", strongholdTradeTooltip.value || "当前无法交易");
    return;
  }
  openMarketDialog({
    strongholdId: sh.id,
    strongholdName: sh.name,
    mode: "lord",
  });
}

function handleOpenMerchantShop(actorId: number) {
  const shop = popupMerchantShops.value.find((s) => s.id === actorId);
  void notifyActionBlocked(
    "拜访商家",
    `${shop?.name ?? "商家"}：个人物品买卖与对话将在后续版本实装（参见 docs/strategy-trade-market-design.md）。`,
  );
  onCancel();
}

async function handleMarketTraded(nextState?: StrategyWorldState) {
  if (nextState) {
    state.value = nextState;
    info.value = "市场成交已更新";
    return;
  }

  try {
    state.value = await getStrategyState();
    info.value = "市场成交已更新";
  } catch (e) {
    const message = e instanceof Error ? e.message : "刷新状态失败";
    await handleStrategyApiError(message, message);
  }
}

const canExpeditionFromStronghold = computed(() => {
  const sh = activeStrongholdForCommands.value;
  const playerForceId = state.value?.playerForceId;
  if (!sh || playerForceId == null) return false;
  if (sh.forceId !== playerForceId || !sh.isLordResidence) return false;
  return !state.value?.units.some((u) => u.soldiers > 0 && u.x === sh.x && u.y === sh.y);
});

const canExpedition = computed(
  () => canExpeditionFromStronghold.value && isLordAtOwnResidence.value
);

const expeditionTooltip = computed(() => {
  if (!isLordAtOwnResidence.value) return LORD_AT_RESIDENCE_REQUIRED_TIP;
  const sh = activeStrongholdForCommands.value;
  const playerForceId = state.value?.playerForceId;
  if (!sh || playerForceId == null) return LORD_AT_RESIDENCE_REQUIRED_TIP;
  if (sh.forceId !== playerForceId || !sh.isLordResidence) {
    const residence = state.value ? resolveLordResidenceStronghold(state.value) : null;
    return residence ? `仅可在当主居城 ${residence.name} 出征` : "仅当主居城可出征";
  }
  if (state.value?.units.some((u) => u.soldiers > 0 && u.x === sh.x && u.y === sh.y)) {
    return "据点格已有地图军，须先撤走或消灭驻留部队";
  }
  return "从当主居城分配 SubUnit 与将领组建部队（默认在城中，可选立即出城）";
});

const canAdjustTaxStronghold = computed(() => {
  const sh = activeStrongholdForCommands.value;
  const playerForceId = state.value?.playerForceId;
  if (!sh || playerForceId == null) return false;
  if (sh.forceId !== playerForceId || !sh.isDirectRule) return false;
  const force = state.value?.forces.find((f) => f.id === playerForceId);
  return force?.status !== "InnerVassal";
});

const taxRateTooltip = computed(() => {
  if (!canLordCommandActiveStronghold.value) return LORD_AT_RESIDENCE_REQUIRED_TIP;
  const sh = activeStrongholdForCommands.value;
  if (!sh) return LORD_AT_RESIDENCE_REQUIRED_TIP;
  if (sh.forceId !== state.value?.playerForceId) return "仅本家据点可调整税率";
  if (!sh.isDirectRule) return "已任命领主领地，税率由城主自行决定，当主不可干涉";
  return "仅直辖城可调整税率；税令将从当主居城派出信使，抵达后生效";
});

const canSetGovernancePolicyStronghold = computed(() => {
  const sh = activeStrongholdForCommands.value;
  if (!sh || state.value == null) return false;
  return canConfigureStrongholdGovernancePolicy(state.value, sh);
});

const governancePolicyTooltip = computed(() => {
  if (!canLordCommandActiveStronghold.value) return LORD_AT_RESIDENCE_REQUIRED_TIP;
  const sh = activeStrongholdForCommands.value;
  if (!sh || state.value == null) return LORD_AT_RESIDENCE_REQUIRED_TIP;
  if (!canConfigureStrongholdGovernancePolicy(state.value, sh)) {
    return governancePolicyBlockReason(state.value, sh);
  }
  return "设定自由决策/军事/内政优先；每月 1 日向待命将领自动发布任务令";
});

const canEspionageStronghold = computed(() => {
  const sh = popupStronghold.value ?? selectedStronghold.value;
  if (!sh || !state.value) return false;
  if (sh.forceId === state.value.playerForceId) return false;
  return isForeignIntelRestricted(state.value, sh.forceId);
});

const selectedConvoy = computed(
  () => state.value?.supplyConvoys.find((c) => c.id === selectedConvoyId.value) ?? null
);

const selectedEntityName = computed(
  () =>
    selectedUnit.value?.name ??
    selectedStronghold.value?.name ??
    (selectedConvoy.value ? `运输队 #${selectedConvoy.value.id}` : undefined)
);

const popupEntityName = computed(() => {
  if (menuPopupMode.value === "characterCommand") {
    return state.value?.lord.name ?? "当主";
  }
  return selectedEntityName.value;
});

const playerForce = computed(
  () =>
    state.value?.forces.find((f) => f.id === state.value!.playerForceId) ?? null
);

const lordResidenceName = computed(() => {
  if (!state.value) return null;
  const fromLord = state.value.lord.residenceStrongholdName?.trim();
  if (fromLord) return fromLord;
  const atLord = state.value.strongholds.find(
    (s) =>
      s.forceId === state.value!.playerForceId &&
      s.x === state.value!.lord.x &&
      s.y === state.value!.lord.y
  );
  return atLord?.name ?? null;
});

const playerLordName = computed(() => state.value?.lord.name?.trim() ?? null);

const playerForceStats = computed(() => {
  const force = playerForce.value;
  if (!force || !state.value) return null;
  const { forces, strongholds, units, characters, lord } = state.value;
  const lordName = lord.name;
  return {
    strongholdCount: countRealmStrongholds(force.id, forces, strongholds),
    ownStrongholdCount: countOwnStrongholds(force.id, strongholds),
    characterCount: countRealmCharacters(force.id, forces, strongholds, units, {
      characters,
      forceCharacterCount: force.characterCount,
      lordName,
    }),
    ownCharacterCount: countOwnCharacters(force.id, strongholds, units, {
      characters,
      lordName,
    }),
    prestige: force.prestige ?? 0,
    orthodoxy: force.orthodoxy ?? 0,
  };
});

/** 地图右下角：势力 / 封地 / 外交 着色模式。 */
const mapColorMode = ref<StrategyMapColorMode>("Realm");

/** 默认战略（暂停）；进行 = 按倍速自动推进。 */
const gamePaused = ref(true);

/** 1 / 2 / 4 倍速：进行模式下每推进 1 日的间隔 = 基准毫秒 ÷ 倍速。 */
const gameSpeed = ref<1 | 2 | 4 | 8>(1);

/** 1 倍速下每游戏日的基础间隔（毫秒）；2 倍速 ≈ 1 秒/日，4 倍速 ≈ 0.5 秒/日。 */
const AUTO_DAY_BASE_MS = 2000;

let autoAdvanceTimer: ReturnType<typeof setTimeout> | null = null;
let multiplayerPollTimer: ReturnType<typeof setInterval> | null = null;
let multiplayerPollPending = false;
let multiplayerPollGeneration = 0;
let autoAdvanceGeneration = 0;

function clearAutoAdvanceTimer() {
  autoAdvanceGeneration += 1;
  if (autoAdvanceTimer !== null) {
    clearTimeout(autoAdvanceTimer);
    autoAdvanceTimer = null;
  }
}

function autoAdvanceDelayMs(): number {
  return AUTO_DAY_BASE_MS / gameSpeed.value;
}

function scheduleAutoAdvance(afterMs: number) {
  clearAutoAdvanceTimer();
  if (gamePaused.value || initialLoading.value) return;

  const generation = autoAdvanceGeneration;
  autoAdvanceTimer = setTimeout(() => {
    autoAdvanceTimer = null;
    if (generation !== autoAdvanceGeneration || gamePaused.value) return;
    void runAutoAdvanceTick(generation);
  }, afterMs);
}

async function runAutoAdvanceTick(generation: number) {
  if (generation !== autoAdvanceGeneration || gamePaused.value) return;
  if (initialLoading.value || !state.value) {
    scheduleAutoAdvance(200);
    return;
  }
  // 业务：玩家指令/API 请求进行中时不叠加重推进
  if (loading.value) {
    scheduleAutoAdvance(200);
    return;
  }

  if (state.value?.allForcesAiControlled && gameSpeed.value === 8)
    await onAdvanceDays(7);
  else
    await onAdvanceDay();

  if (generation !== autoAdvanceGeneration || gamePaused.value) return;
  scheduleAutoAdvance(autoAdvanceDelayMs());
}

function syncAutoAdvanceLoop() {
  if (gamePaused.value) {
    clearAutoAdvanceTimer();
    return;
  }
  scheduleAutoAdvance(autoAdvanceDelayMs());
}

/** M4 可改为从难度/设置读取；M3 起可接入同盟势力列表。 */
const routeVisibilityContext = computed(() => ({
  policy: DEFAULT_ROUTE_VISIBILITY_POLICY,
  playerForceId: playerForce.value?.id ?? 1,
  allyForceIds: [] as readonly number[],
}));

/** 底栏情报：跟随鼠标悬停格；栏位始终显示。 */
const intelBarX = computed(() => hoverCell.value?.x ?? null);
const intelBarY = computed(() => hoverCell.value?.y ?? null);

/** 悬浮框等仍用悬停 > 固定 > 选中格。 */
const intelBarCell = computed(() => {
  if (hoverCell.value) return { x: hoverCell.value.x, y: hoverCell.value.y };
  if (intelPinnedCell.value) return intelPinnedCell.value;
  if (selectedCell.value) return selectedCell.value;
  return null;
});

const intelX = computed(() => intelBarCell.value?.x ?? null);
const intelY = computed(() => intelBarCell.value?.y ?? null);

const intelStronghold = computed(() =>
  state.value && intelX.value !== null && intelY.value !== null
    ? strongholdsAtCellForIntel(state.value, intelX.value, intelY.value)[0] ?? null
    : null
);

const intelUnit = computed(() =>
  state.value && intelX.value !== null && intelY.value !== null
    ? unitsAtCellForIntel(state.value, intelX.value, intelY.value)[0] ?? null
    : null
);

function battlefieldAt(x: number, y: number) {
  return state.value?.battlefields?.find((b) => b.x === x && b.y === y) ?? null;
}

function fieldBattlefieldAt(x: number, y: number) {
  const bf = battlefieldAt(x, y);
  return bf?.kind === "Field" ? bf : null;
}

const hoverUnit = computed(() => intelUnit.value);

const hoverStronghold = computed(() => intelStronghold.value);

const hoverConvoy = computed(() => {
  if (!state.value || intelX.value === null || intelY.value === null) return null;
  return convoysAtCellForIntel(state.value, intelX.value, intelY.value)[0] ?? null;
});

const hoverUnitId = computed(() => hoverUnit.value?.id ?? null);
const hoverStrongholdId = computed(() => hoverStronghold.value?.id ?? null);
const hoverConvoyId = computed(() => hoverConvoy.value?.id ?? null);

const scopedEventFeed = computed(() =>
  filterEventsByMessageScope(eventFeed.value, {
    player: showPlayerMessages.value,
    world: showWorldMessages.value,
  })
);

const unitRosterFloatStyle = computed(() => {
  // 业务：随顶栏实测高度下移，避免势力情报折行时遮住「可操作部队」列表
  const top = mapTopOverlayHeight.value + UNIT_ROSTER_TOP_GAP;
  return {
    top: `${top}px`,
    maxHeight: `calc(100% - ${top}px - ${UNIT_ROSTER_BOTTOM_RESERVE}px)`,
  };
});

function updateMapTopOverlayHeight() {
  const el = mapTopOverlayRef.value;
  if (!el) return;
  const height = Math.ceil(el.getBoundingClientRect().height);
  if (height > 0) mapTopOverlayHeight.value = height;
}

function bindMapTopOverlayObserver() {
  mapTopOverlayResizeObserver?.disconnect();
  mapTopOverlayResizeObserver = null;

  const el = mapTopOverlayRef.value;
  if (!el) return;

  updateMapTopOverlayHeight();
  mapTopOverlayResizeObserver = new ResizeObserver(() => updateMapTopOverlayHeight());
  mapTopOverlayResizeObserver.observe(el);
}

/** 浏览态 / 外交地图选势力：固定格点悬浮框，移入框内可滚动而不消失。 */
const showHoverIntel = computed(() => {
  const hoverIntelModeActive =
    (stateId.value === "navigate" && popupMode.value === "none") ||
    (stateId.value === "diplomacyForceSelect" && popupMode.value === "diplomacyForceSelect");
  return (
    hoverIntelModeActive &&
    !intelDialogVisible.value &&
    !messageDialogVisible.value &&
    !battleConfirmVisible.value &&
    !battleResultVisible.value &&
    !diplomacyDialogVisible.value &&
    intelPinnedCell.value !== null &&
    state.value !== null &&
    canShowCellHoverIntel(state.value, intelPinnedCell.value.x, intelPinnedCell.value.y)
  );
});

function intelBoxCountAt(x: number, y: number): number {
  if (!state.value) return 0;
  let count = 0;
  if (strongholdsAtCellForIntel(state.value, x, y).length > 0) count += 1;
  if (fieldBattlefieldAt(x, y) && isTileVisible(state.value, x, y)) count += 1;
  else if (unitsAtCellForIntel(state.value, x, y).length > 0) count += 1;
  if (
    convoysAtCellForIntel(state.value, x, y).length +
      messengersAtCellForIntel(state.value, x, y).length >
    0
  ) {
    count += 1;
  }
  return count;
}

function hasMultiIntelLayout(x: number, y: number): boolean {
  return intelBoxCountAt(x, y) > 1;
}

function estimateHoverIntelPopupSize(): { width: number; height: number } {
  const el = hoverIntelLayerRef.value;
  if (el && el.offsetWidth > 0 && el.offsetHeight > 0) {
    return { width: el.offsetWidth, height: el.offsetHeight };
  }
  if (intelPinnedCell.value && hasMultiIntelLayout(intelPinnedCell.value.x, intelPinnedCell.value.y)) {
    const boxCount = intelBoxCountAt(intelPinnedCell.value.x, intelPinnedCell.value.y);
    return {
      width: HOVER_INTEL_W * boxCount + HOVER_INTEL_DUAL_GAP * Math.max(0, boxCount - 1),
      height: HOVER_INTEL_H,
    };
  }
  return { width: HOVER_INTEL_W, height: HOVER_INTEL_H };
}

function measureHoverIntelPopup(): { width: number; height: number } {
  return estimateHoverIntelPopupSize();
}

function getCellViewportRect(x: number, y: number) {
  const panelRelative = mapCanvasRef.value?.getCellPanelRect(x, y, mapPanelRef.value);
  if (!panelRelative || !mapPanelRef.value) return null;

  const panelBBox = mapPanelRef.value.getBoundingClientRect();
  return {
    left: panelRelative.left + panelBBox.left,
    top: panelRelative.top + panelBBox.top,
    width: panelRelative.width,
    height: panelRelative.height,
  };
}

function getViewportBounds() {
  return {
    left: 0,
    top: 0,
    width: window.innerWidth,
    height: window.innerHeight,
  };
}

function getMapPanelViewportRect() {
  if (!mapPanelRef.value) return getViewportBounds();
  const bbox = mapPanelRef.value.getBoundingClientRect();
  return {
    left: bbox.left,
    top: bbox.top,
    width: bbox.width,
    height: bbox.height,
  };
}

function updateHoverIntelPosition() {
  if (!showHoverIntel.value || !intelPinnedCell.value || !mapPanelRef.value || !mapCanvasRef.value) {
    hoverIntelStyle.value = { display: "none" };
    return;
  }

  const cellRect = getCellViewportRect(intelPinnedCell.value.x, intelPinnedCell.value.y);
  if (!cellRect) {
    hoverIntelStyle.value = { display: "none" };
    return;
  }

  const panel = getViewportBounds();
  const mapPanelRect = getMapPanelViewportRect();

  const applyPosition = (popupSize: { width: number; height: number }, logLayout = false) => {
    const placement = resolveAnchoredPanelPlacement(
      cellRect,
      panel,
      popupSize,
      4,
      mapPanelRect
    );
    hoverIntelAnchorSide.value = placement.side;
    hoverIntelVerticalAlign.value = placement.verticalAlign;
    logStrategyMapCoords("hover-intel-position", {
      cell: { x: intelPinnedCell.value!.x, y: intelPinnedCell.value!.y },
      cellRect,
      panelSize: { width: panel.width, height: panel.height },
      mapPanelRect,
      popupSize,
      position: placement,
    });
    hoverIntelStyle.value = {
      position: "fixed",
      left: `${placement.left}px`,
      top: `${placement.top}px`,
      zIndex: "200",
    };

    if (logLayout) {
      logHoverIntelLayoutAfterLayout(placement, popupSize);
    }
  };

  applyPosition(estimateHoverIntelPopupSize());

  void nextTick(() => {
    if (!showHoverIntel.value) return;
    requestAnimationFrame(() => {
      if (!showHoverIntel.value) return;
      applyPosition(measureHoverIntelPopup(), true);
    });
  });
}

function panelCountFromDom(...elements: (Element | null)[]): number {
  return elements.filter(Boolean).length;
}

function logHoverIntelLayoutAfterLayout(
  placement: {
    left: number;
    top: number;
    side: string;
    rawTop: number;
    verticalAlign: AnchorVerticalAlign;
  },
  popupSize: { width: number; height: number }
) {
  if (!intelPinnedCell.value) return;

  const cellRect = getCellViewportRect(intelPinnedCell.value.x, intelPinnedCell.value.y);
  if (!cellRect) return;

  const layerEl = hoverIntelLayerRef.value;
  const strongholdEl = layerEl?.querySelector(".intel-box--stronghold") ?? null;
  const militaryEl = layerEl?.querySelector(".intel-box--military") ?? null;
  const civilEl = layerEl?.querySelector(".intel-box--civil") ?? null;
  const singleEl =
    layerEl?.querySelector(
      ".cell-intel-stack > .intel-box:not(.intel-box--stronghold):not(.intel-box--military):not(.intel-box--civil)"
    ) ?? null;

  const multiLayout = Boolean(strongholdEl || militaryEl || civilEl) && panelCountFromDom(strongholdEl, militaryEl, civilEl) > 1;

  logHoverIntelLayoutDebug({
    gridCell: { x: intelPinnedCell.value.x, y: intelPinnedCell.value.y },
    viewport: {
      width: window.innerWidth,
      height: window.innerHeight,
    },
    mouse: hoverCell.value
      ? {
          x: Math.round(hoverCell.value.screenX),
          y: Math.round(hoverCell.value.screenY),
        }
      : null,
    cellScreen: {
      left: Math.round(cellRect.left),
      top: Math.round(cellRect.top),
      right: Math.round(cellRect.left + cellRect.width),
      bottom: Math.round(cellRect.top + cellRect.height),
      width: Math.round(cellRect.width),
      height: Math.round(cellRect.height),
    },
    mapPanelScreen: (() => {
      const map = getMapPanelViewportRect();
      return {
        left: Math.round(map.left),
        top: Math.round(map.top),
        right: Math.round(map.left + map.width),
        bottom: Math.round(map.top + map.height),
        width: Math.round(map.width),
        height: Math.round(map.height),
      };
    })(),
    anchorSide: hoverIntelAnchorSide.value,
    verticalAlign: placement.verticalAlign,
    rawTop: placement.rawTop,
    containerScreen: rectToScreenDebug(layerEl),
    strongholdBoxScreen: rectToScreenDebug(strongholdEl),
    otherBoxScreen: rectToScreenDebug(militaryEl ?? civilEl),
    singleBoxScreen: multiLayout ? null : rectToScreenDebug(singleEl),
    placement,
    popupSize,
    dualLayout: multiLayout,
  });
}

watch([showHoverIntel, intelPinnedCell, () => popupMode.value], updateHoverIntelPosition, {
  deep: true,
});

function refreshMinimapViewport() {
  minimapViewport.value = mapCanvasRef.value?.getViewportWorldRect() ?? null;
}

function onViewportChange() {
  updateHoverIntelPosition();
  refreshMinimapViewport();
}

function onMinimapNavigate(payload: { worldX: number; worldY: number }) {
  mapCanvasRef.value?.panToWorldPoint(payload.worldX, payload.worldY);
  refreshMinimapViewport();
}

const intelRoad = computed(() => {
  if (!mapMaster.value || intelBarX.value === null || intelBarY.value === null) return null;
  if (state.value && !canShowTileMapIntel(state.value, intelBarX.value, intelBarY.value)) return null;
  return roadAtCell(mapMaster.value, intelBarX.value, intelBarY.value);
});

const intelTileInfo = computed(() => {
  if (!mapMaster.value || intelBarX.value === null || intelBarY.value === null) {
    return { terrainName: null, regionName: null };
  }
  if (state.value && !canShowTileMapIntel(state.value, intelBarX.value, intelBarY.value)) {
    return { terrainName: null, regionName: null };
  }
  return mapTileInfo(mapMaster.value, intelBarX.value, intelBarY.value);
});

const intelLandmark = computed(() => {
  if (!mapMaster.value || intelBarX.value === null || intelBarY.value === null) return null;
  if (state.value && !canShowTileMapIntel(state.value, intelBarX.value, intelBarY.value)) return null;
  return landmarkAtCell(mapMaster.value, intelBarX.value, intelBarY.value);
});

const intelBarStronghold = computed(() =>
  state.value && intelBarX.value !== null && intelBarY.value !== null
    ? strongholdsAtCellForIntel(state.value, intelBarX.value, intelBarY.value)[0] ?? null
    : null
);

const popupUsesCorner = computed(() => isCornerPopupMode(popupMode.value));

const cornerHintMode = computed(() => resolveCornerHintMode(popupMode.value));

const menuPopupMode = computed(() => resolveMenuPopupMode(popupMode.value));

const menuPopupBesieged = computed(() => {
  if (menuPopupMode.value === "characterCommand") {
    return isStrongholdBesieged(activeCharacterStronghold.value);
  }
  if (menuPopupMode.value === "strongholdCommand") {
    return isStrongholdBesieged(activeStrongholdForCommands.value);
  }
  return false;
});

const entityPickerVisible = computed(() => popupMode.value === "entityPicker");

const routeOverlays = computed((): MapRouteOverlay[] => {
  const overlays: MapRouteOverlay[] = [];
  const visibleUnits = filterUnitsForRouteDisplay(
    state.value?.units ?? [],
    routeVisibilityContext.value
  );
  const previewActive =
    popupMode.value === "moveSelect"
    && previewRoutePoints.value.length >= 2
    && (selectedUnitId.value !== null || selectedCharacterId.value !== null);

  for (const character of state.value?.mapCharacters ?? []) {
    if (!character.isPlayerControlled || !character.route?.length) continue;
    const points = normalizeRoute(character.route);
    if (points.length < 2) continue;
    if (
      previewActive
      && selectedCharacterId.value === character.id
    ) {
      continue;
    }
    overlays.push({
      unitId: -character.id,
      points,
      variant: selectedCharacterId.value === character.id ? "emphasized" : "committed",
    });
  }

  for (const unit of visibleUnits) {
    const points = normalizeRoute(unit.route);
    if (points.length < 2) continue;
    if (previewActive && unit.id === selectedUnitId.value) continue;

    overlays.push({
      unitId: unit.id,
      points,
      variant: unit.id === selectedUnitId.value ? "emphasized" : "committed",
    });
  }

  if (previewActive) {
    const actorId = selectedUnitId.value ?? -(selectedCharacterId.value ?? 0);
    overlays.push({
      unitId: actorId,
      points: previewRoutePoints.value,
      variant: "preview",
    });
  }

  return overlays;
});

const moveRelayMarkers = computed((): MapMoveRelayMarker[] => {
  if (popupMode.value !== "moveSelect") return [];

  const markers: MapMoveRelayMarker[] = moveCommittedWaypoints.value.map((point, index) => ({
    x: point.x,
    y: point.y,
    kind: "committed" as const,
    order: index + 1,
  }));

  if (movePendingRelay.value) {
    markers.push({
      x: movePendingRelay.value.x,
      y: movePendingRelay.value.y,
      kind: "pending",
      order: markers.length + 1,
    });
  }

  return markers;
});

watch(
  () => popupMode.value,
  (mode) => {
    if (mode !== "moveSelect") {
      previewRoutePoints.value = [];
    }
  }
);

function resetMovePath() {
  moveCommittedWaypoints.value = [];
  movePendingRelay.value = null;
  previewRoutePoints.value = [];
  clearMovePathDebug();
}

function isValidMovePathCell(x: number, y: number): boolean {
  if (selectedCharacterId.value !== null && state.value) {
    const mapChar = state.value.mapCharacters?.find((c) => c.id === selectedCharacterId.value);
    if (mapChar) return mapChar.x !== x || mapChar.y !== y;
  }
  const unit = selectedUnit.value;
  if (!unit) return false;
  return unit.x !== x || unit.y !== y;
}

async function refreshMovePathPreview(): Promise<boolean> {
  if (!movePendingRelay.value) return false;

  const destination = movePendingRelay.value;
  const via = [...moveCommittedWaypoints.value];
  const serial = ++previewRequestSerial;

  if (selectedCharacterId.value !== null) {
    const ws = state.value;
    const mapChar = ws?.mapCharacters?.find((c) => c.id === selectedCharacterId.value);
    if (!mapChar) return false;

    try {
      const preview = await previewCharacterPath(
        selectedCharacterId.value,
        destination.x,
        destination.y,
        { via },
      );
      if (serial !== previewRequestSerial) return false;
      const points = normalizeRoute(preview.points);
      if (points.length < 2) return false;
      previewRoutePoints.value = points;
      return true;
    } catch {
      return false;
    }
  }

  const unit = selectedUnit.value;
  if (!unit || selectedUnitId.value === null) {
    return false;
  }

  logMovePath("api.request.via", {
    serial,
    unitId: selectedUnitId.value,
    unitAt: { x: unit.x, y: unit.y },
    via,
    destination,
    apiMode: apiMode.value,
    usingMockFallback: usingMockFallback.value,
  });

  try {
    const preview = await previewUnitPath(
      selectedUnitId.value,
      destination.x,
      destination.y,
      { via }
    );
    if (serial !== previewRequestSerial) {
      logMovePath("api.stale", { serial, currentSerial: previewRequestSerial });
      return false;
    }

    const points = normalizeRoute(preview.points);
    logMovePath("api.response.via", {
      serial,
      points: formatPathPoints(points),
      first: points[0] ?? null,
      last: points.length ? points[points.length - 1] : null,
      viaOnPath: via.map((relay) => ({
        relay,
        index: findPointOnPath(points, relay),
      })),
    });

    if (points.length < 2) {
      logMovePath("preview.tooShort", { serial, points: formatPathPoints(points) });
      return false;
    }

    previewRoutePoints.value = points;
    return true;
  } catch (err) {
    logMovePath("api.error", {
      serial,
      message: err instanceof Error ? err.message : String(err),
    });
    return false;
  }
}

function handleBeginMove() {
  resetMovePath();
  onBeginMove();
}

function handleBeginDirective() {
  directiveDialogVisible.value = true;
}

async function handleDirectiveConfirm(payload: { directive: UnitDirectiveValue }) {
  if (selectedUnitId.value === null || !state.value) return;

  loading.value = true;
  error.value = "";
  try {
    const response = await setUnitDirective(selectedUnitId.value, payload.directive);

    state.value = response.state;
    const lord = response.state.lord;
    const unit = response.state.units.find((u) => u.id === selectedUnitId.value);
    const sameTile = unit && lord && unit.x === lord.x && unit.y === lord.y;
    info.value =
      response.outcome === "CarrierDispatched" ||
      response.outcome === "MessengerDispatched"
        ? `方针已从当主所在格 (${lord?.x}, ${lord?.y}) 派出信使，到达后生效`
        : sameTile
          ? "当主与本队同格，方针已即时生效"
          : "方针已即时生效";
    if (response.outcome === "AppliedImmediately") {
      appendEvents([{ category: "PolicyApplied", message: `✅ ${unit?.name ?? "部队"} 方针已即时生效` }]);
    } else {
      appendEvents([{ category: "PolicyDispatched", message: `📨 方针信使已从当主处出发，目标 ${unit?.name ?? "部队"}` }]);
    }
  } catch (e) {
    const message = e instanceof Error ? e.message : "方针设定失败";
    await handleStrategyApiError(message, message);
  } finally {
    loading.value = false;
  }
}

function formatMovePathLogDetail(detail: Record<string, unknown>): string {
  return JSON.stringify(detail, null, 2);
}

async function handleMovePathClick(payload: {
  x: number;
  y: number;
  screenX: number;
  screenY: number;
}) {
  const unit = selectedUnit.value;
  const mapChar =
    selectedCharacterId.value != null
      ? state.value?.mapCharacters?.find((c) => c.id === selectedCharacterId.value)
      : null;
  const actorAt = mapChar
    ? { x: mapChar.x, y: mapChar.y }
    : unit
      ? { x: unit.x, y: unit.y }
      : null;

  logMovePath("click", {
    cell: { x: payload.x, y: payload.y },
    unitAt: actorAt,
    pending: movePendingRelay.value,
    committed: [...moveCommittedWaypoints.value],
    previewBefore: formatPathPoints(previewRoutePoints.value),
    valid: isValidMovePathCell(payload.x, payload.y),
  });

  if (!isValidMovePathCell(payload.x, payload.y)) return;

  onSelectCell(payload);
  if (stateId.value !== "moveTargetSelect" && stateId.value !== "characterMoveTargetSelect") return;

  if (!actorAt) return;

  const clicked = { x: payload.x, y: payload.y };

  if (
    movePendingRelay.value &&
    movePendingRelay.value.x === clicked.x &&
    movePendingRelay.value.y === clicked.y
  ) {
    logMovePath("click.confirm", { destination: clicked, via: [...moveCommittedWaypoints.value] });
    await confirmMovePath(clicked, payload);
    return;
  }

  const onExistingPath =
    previewRoutePoints.value.length > 0 &&
    findPointOnPath(previewRoutePoints.value, clicked) >= 0;

  logMovePath("click.branch", {
    clicked,
    onExistingPath,
    onPathIdx: onExistingPath ? findPointOnPath(previewRoutePoints.value, clicked) : -1,
  });

  if (onExistingPath) {
    previewRequestSerial++;
    const truncated = truncateMovePathAtCell(
      moveCommittedWaypoints.value,
      movePendingRelay.value,
      previewRoutePoints.value,
      clicked
    );
    if (!truncated) return;

    moveCommittedWaypoints.value = truncated.committed;
    movePendingRelay.value = truncated.pending;
    previewRoutePoints.value = truncated.previewPath;
    selectedCell.value = clicked;
    return;
  }

  const previousRelay = movePendingRelay.value;

  if (previousRelay) {
    moveCommittedWaypoints.value.push({ ...previousRelay });
  }
  movePendingRelay.value = clicked;
  selectedCell.value = clicked;

  const ok = await refreshMovePathPreview();
  if (!ok) {
    logMovePath("click.previewRollback", { previousRelay, clicked });
    if (previousRelay) {
      moveCommittedWaypoints.value.pop();
    }
    movePendingRelay.value = previousRelay;
    selectedCell.value = previousRelay ?? { x: actorAt.x, y: actorAt.y };
  } else {
    logMovePath("click.stateAfter", {
      pending: movePendingRelay.value,
      committed: [...moveCommittedWaypoints.value],
      preview: formatPathPoints(previewRoutePoints.value),
    });
  }
}

async function confirmMovePath(
  destination: MapPoint,
  payload: { screenX: number; screenY: number }
) {
  const target: StrategyMoveTarget = {
    x: destination.x,
    y: destination.y,
    screenX: payload.screenX,
    screenY: payload.screenY,
  };
  const via = [...moveCommittedWaypoints.value];
  resetMovePath();
  enterExecutingCommand();
  await executeMove(target, via);
}

function normalizeRoute(
  route: Array<MapPoint | { x?: number; y?: number; X?: number; Y?: number }> | undefined
): MapPoint[] {
  if (!route?.length) return [];
  return route.map((p) => ({
    x: p.x ?? (p as { X: number }).X,
    y: p.y ?? (p as { Y: number }).Y,
  }));
}

/** 指令菜单：锚定在单位格旁；选点提示：地图区域右上角。 */
const popupStyle = computed(() => {
  if (popupMode.value === "none" || !mapPanelRef.value) {
    return { display: "none" };
  }

  if (popupUsesCorner.value) {
    return {};
  }

  if (!menuAnchor.value) return { display: "none" };

  const anchor = menuAnchor.value;
  const panel = {
    left: 0,
    top: 0,
    width: mapPanelRef.value.clientWidth,
    height: mapPanelRef.value.clientHeight,
  };

  if (anchor.panelAnchorRect) {
    const side = anchor.anchorSide ?? "left";
    const pos = resolveAnchoredPanelPlacementForSide(
      anchor.panelAnchorRect,
      panel,
      { width: MENU_POPUP_W, height: MENU_POPUP_H },
      side
    );
    return { left: `${pos.left}px`, top: `${pos.top}px` };
  }

  if (mapCanvasRef.value) {
    const cellRect = mapCanvasRef.value.getCellPanelRect(
      anchor.x,
      anchor.y,
      mapPanelRef.value
    );
    if (cellRect) {
      const panel = {
        left: 0,
        top: 0,
        width: mapPanelRef.value.clientWidth,
        height: mapPanelRef.value.clientHeight,
      };
      const pos = resolveAnchoredPanelPlacement(cellRect, panel, {
        width: MENU_POPUP_W,
        height: MENU_POPUP_H,
      });
      logStrategyMapCoords("menu-popup-position", {
        anchor,
        cellRect,
        panelSize: { width: panel.width, height: panel.height },
        position: pos,
      });
      return { left: `${pos.left}px`, top: `${pos.top}px` };
    }
  }

  const rect = mapPanelRef.value.getBoundingClientRect();
  const left = Math.min(
    Math.max(8, anchor.screenX - rect.left + 8),
    rect.width - MENU_POPUP_W
  );
  const top = Math.min(
    Math.max(8, anchor.screenY - rect.top + 8),
    rect.height - MENU_POPUP_H
  );
  logStrategyMapCoords("menu-popup-fallback", { anchor, rect: { width: rect.width, height: rect.height }, left, top });
  return { left: `${left}px`, top: `${top}px` };
});

/** 指令菜单 tooltip：菜单在左半屏时向右弹出，否则向左。 */
const commandTooltipSide = computed<"left" | "right">(() => {
  if (!menuAnchor.value || !mapPanelRef.value) return "right";
  const rect = mapPanelRef.value.getBoundingClientRect();
  const mid = rect.left + rect.width / 2;
  return menuAnchor.value.screenX < mid ? "right" : "left";
});

function clearMapHoverState() {
  onHoverCell(null);
  intelPinnedCell.value = null;
  intelLayerHovered.value = false;
}

function handleHoverCell(
  cell: { x: number; y: number; screenX: number; screenY: number } | null
) {
  if (intelDialogVisible.value) return;

  logStrategyMapCoords("hover-cell", {
    cell,
    stateId: stateId.value,
    popupMode: popupMode.value,
  });
  onHoverCell(cell);

  if (cell && state.value && canShowCellHoverIntel(state.value, cell.x, cell.y)) {
    intelPinnedCell.value = { x: cell.x, y: cell.y };
  } else if (!intelLayerHovered.value) {
    intelPinnedCell.value = null;
  } else if (
    intelPinnedCell.value &&
    state.value &&
    !canShowCellHoverIntel(state.value, intelPinnedCell.value.x, intelPinnedCell.value.y)
  ) {
    intelPinnedCell.value = null;
  }
}

function onIntelLayerPointerEnter() {
  intelLayerHovered.value = true;
}

function onIntelLayerPointerLeave() {
  intelLayerHovered.value = false;
  const cell = hoverCell.value;
  if (
    !cell ||
    !intelPinnedCell.value ||
    cell.x !== intelPinnedCell.value.x ||
    cell.y !== intelPinnedCell.value.y
  ) {
    intelPinnedCell.value = null;
  }
}

function showUnavailableActionTip(reason: string) {
  ElMessage({
    message: reason,
    type: "info",
    duration: 2800,
    showClose: true,
  });
}

async function notifyActionBlocked(title: string, reason: string) {
  info.value = reason;
  await ElMessageBox.alert(reason, title, {
    type: "warning",
    confirmButtonText: "知道了",
  });
}

async function handleStrategyApiError(
  message: string,
  fallbackMessage: string,
  options: Partial<ApiErrorResolveContext> = {},
): Promise<void> {
  const resolution = resolveStrategyApiError(message, {
    fallbackMessage,
    lordAtResidenceTip: LORD_AT_RESIDENCE_REQUIRED_TIP,
    lordCommandStrongholdTip: LORD_COMMAND_STRONGHOLD_TIP,
    characterGateApCost: CHARACTER_GATE_AP_COST,
    dataNotFoundHint:
      "服务端未加载剧本（可能 WebApi 已重启），请刷新页面或点击「重新加载」",
    dataNotFoundReloadHint: "服务端未加载剧本，已尝试自动重载；若仍失败请刷新页面",
    ...options,
  });
  await applyStrategyApiErrorResolution(
    resolution,
    (value) => {
      error.value = value;
    },
    notifyActionBlocked,
    fallbackMessage,
  );
}

function handleBeginAttack() {
  const reason = attackApBlockReason(selectedUnit.value);
  if (reason) {
    void notifyActionBlocked("无法攻击", reason);
    return;
  }
  onBeginAttack();
}

function handleBeginMerge() {
  onBeginMerge();
}

function handleBeginSplit() {
  if (!selectedUnit.value?.composition.length) {
    void notifyActionBlocked("无法分兵", "该部队没有可拆子编制");
    return;
  }
  splitDialogVisible.value = true;
}

function handleBeginExpedition() {
  if (!canExpedition.value) {
    void notifyActionBlocked("无法出征", expeditionTooltip.value);
    return;
  }
  expeditionDialogVisible.value = true;
}

const lordResidenceStrongholdId = computed(() => {
  if (!state.value) return null;
  const force = playerForce.value;
  if (force?.lordResidenceStrongholdId && force.lordResidenceStrongholdId > 0) {
    return force.lordResidenceStrongholdId;
  }
  const residence = state.value.strongholds.find(
    (s) => s.forceId === state.value!.playerForceId && s.isLordResidence
  );
  return residence?.id ?? null;
});

function openDiplomacyDialog(
  action: DiplomacyMissionAction,
  initialTargetForceId: number | null = null
) {
  diplomacyAction.value = action;
  diplomacyInitialTargetForceId.value = initialTargetForceId;
  diplomacyTargetForceId.value = initialTargetForceId;
  diplomacySuccessChance.value = null;
  diplomacyTravelDays.value = null;
  diplomacyPeaceRequiredWarScore.value = null;
  diplomacyPeaceCanForceAcceptance.value = false;
  diplomacyPeaceTerms.value = {
    cededStrongholdIds: [],
    reparationsMoney: 0,
    demandOuterVassalage: false,
  };
  diplomacyForcePickActive.value = false;
  closeForceCommandMenu();
  handlePopupCancel();
  diplomacyDialogVisible.value = true;
}

async function refreshDiplomacyPreview() {
  if (!diplomacyDialogVisible.value) return;
  const characterId = diplomacyCharacterId.value;
  const targetForceId = diplomacyTargetForceId.value;
  if (!characterId || !targetForceId) {
    diplomacySuccessChance.value = null;
    diplomacyTravelDays.value = null;
    return;
  }

  diplomacyPreviewLoading.value = true;
  try {
    const missionPreviewPromise = previewDiplomacyMission({
      characterId,
      targetForceId,
      action: diplomacyAction.value,
    });
    if (diplomacyAction.value === "Peace") {
      const [missionPreview, peacePreview] = await Promise.all([
        missionPreviewPromise,
        previewPeaceSettlement({
          characterId,
          targetForceId,
          ...diplomacyPeaceTerms.value,
        }),
      ]);
      diplomacySuccessChance.value = peacePreview.acceptanceChancePercent;
      diplomacyTravelDays.value = missionPreview.travelDays;
      diplomacyPeaceRequiredWarScore.value = peacePreview.requiredWarScore;
      diplomacyPeaceCanForceAcceptance.value = peacePreview.canForceAcceptance;
    } else {
      const preview = await missionPreviewPromise;
      diplomacySuccessChance.value = preview.successChancePercent;
      diplomacyTravelDays.value = preview.travelDays;
      diplomacyPeaceRequiredWarScore.value = null;
      diplomacyPeaceCanForceAcceptance.value = false;
    }
  } catch (err) {
    diplomacySuccessChance.value = null;
    diplomacyPeaceRequiredWarScore.value = null;
    diplomacyPeaceCanForceAcceptance.value = false;
    ElMessage.warning(err instanceof Error ? err.message : "无法预览外交成功率");
  } finally {
    diplomacyPreviewLoading.value = false;
  }
}

async function handleDiplomacyConfirm() {
  const characterId = diplomacyCharacterId.value;
  const targetForceId = diplomacyTargetForceId.value;
  if (!characterId || !targetForceId) return;

  try {
    const next = diplomacyAction.value === "Peace"
      ? await orderPeaceSettlement({
          characterId,
          targetForceId,
          ...diplomacyPeaceTerms.value,
        })
      : await orderDiplomacyMission({
          characterId,
          targetForceId,
          action: diplomacyAction.value,
        });
    state.value = next;
    diplomacyDialogVisible.value = false;
    info.value = `已派遣使节执行${
      diplomacyAction.value === "Ally" ? "同盟" : diplomacyAction.value === "War" ? "宣战" : "议和"
    }任务`;
  } catch (err) {
    const raw = err instanceof Error ? err.message : "未知错误";
    const peaceErrors: Record<string, string> = {
      InvalidPeaceStronghold: "所选据点已不属于对方，请重新选择条款",
      PeaceMustLeaveStronghold: "不能在和谈中割走对方全部据点",
      InsufficientPeaceReparations: "对方府库不足以支付这笔赔款",
      InvalidPeaceVassalage: "当前双方身份不满足外藩臣服条件",
      PeaceTermsExceedMaximumWarScore: "条款总成本超过 100 战争分数，请减少要求",
      NotEnemyForce: "双方已不处于战争状态",
    };
    const friendly = Object.entries(peaceErrors).find(([code]) => raw.includes(code))?.[1] ?? raw;
    void notifyActionBlocked("外交任务失败", friendly);
  }
}

function beginDiplomacyForcePick() {
  diplomacyForcePickActive.value = true;
  diplomacyDialogVisible.value = false;
  closeForceCommandMenu();
  battlePreview.value = null;
  battleConfirmVisible.value = false;
  resetMovePath();
  if (popupMode.value !== "none") {
    onCancel();
  }
  enterDiplomacyForceSelection();
}

/** 外交地图选点校验：通过后填入势力并恢复对话框。 */
function applyDiplomacyForceFromStronghold(strongholdId: number): boolean {
  const ws = state.value;
  if (!ws) return false;

  const sh = ws.strongholds.find((s) => s.id === strongholdId);
  if (!sh) {
    ElMessage.warning("无效据点");
    return false;
  }

  const forceId = sh.forceId;
  if (forceId === ws.playerForceId) {
    ElMessage.warning("请选择其他势力的据点");
    return false;
  }

  const force = ws.forces.find((f) => f.id === forceId);
  if (!force) {
    ElMessage.warning("无法识别势力");
    return false;
  }

  if (force.status === "InnerVassal") {
    ElMessage.warning("内藩请走外政，不可作为外交对象");
    return false;
  }

  if ((force.category ?? "Military") !== "Military") {
    ElMessage.warning("请选择武家势力据点");
    return false;
  }

  diplomacyTargetForceId.value = forceId;
  diplomacyInitialTargetForceId.value = forceId;
  diplomacyForcePickActive.value = false;
  diplomacyDialogVisible.value = true;
  void refreshDiplomacyPreview();
  const missionError = validateDiplomacyMissionTarget(ws, diplomacyAction.value, forceId);
  if (!missionError) {
    ElMessage.success(`已选择 ${force.name}`);
  }
  return true;
}

function cancelDiplomacyForcePick() {
  diplomacyForcePickActive.value = false;
  diplomacyDialogVisible.value = true;
}

onDiplomacyForceStrongholdPickedRef.value = applyDiplomacyForceFromStronghold;
onDiplomacyForcePickCancelledRef.value = cancelDiplomacyForcePick;

function handleBeginTaxRate() {
  const sh = activeStrongholdForCommands.value;
  if (!sh || !canAdjustTaxStronghold.value) {
    void notifyActionBlocked("无法调整税率", taxRateTooltip.value);
    return;
  }
  if (!canLordCommandActiveStronghold.value) {
    void notifyActionBlocked("无法调整税率", LORD_AT_RESIDENCE_REQUIRED_TIP);
    return;
  }
  taxRateDialogVisible.value = true;
}

function handleBeginGovernancePolicy() {
  const sh = activeStrongholdForCommands.value;
  if (!sh || !canSetGovernancePolicyStronghold.value) {
    void notifyActionBlocked("无法设置方针", governancePolicyTooltip.value);
    return;
  }
  if (!canLordCommandActiveStronghold.value) {
    void notifyActionBlocked("无法设置方针", LORD_AT_RESIDENCE_REQUIRED_TIP);
    return;
  }
  governancePolicyDialogVisible.value = true;
}

function handleBeginMercenaryRecruit() {
  if (!canLordCommandActiveStronghold.value) {
    void notifyActionBlocked("无法募兵", LORD_AT_RESIDENCE_REQUIRED_TIP);
    return;
  }
  mercenaryRecruitDialogMode.value = "assign";
  mercenaryRecruitDialogVisible.value = true;
}

function handleBeginPersonalMercenaryRecruit() {
  if (!canExecutePersonalCommands.value) {
    void notifyActionBlocked("无法募兵", "须在城内且为当主、领主或代官方可执行个人指令");
    return;
  }
  mercenaryRecruitDialogMode.value = "personal";
  mercenaryRecruitDialogVisible.value = true;
}

function handleBeginRecruit() {
  if (!canLordCommandActiveStronghold.value) {
    void notifyActionBlocked("无法征兵", LORD_AT_RESIDENCE_REQUIRED_TIP);
    return;
  }
  recruitDialogMode.value = "assign";
  recruitDialogVisible.value = true;
}

function handleBeginPersonalRecruit() {
  if (!canExecutePersonalCommands.value) {
    void notifyActionBlocked("无法征兵", "须在城内且为当主、领主或代官方可执行个人指令");
    return;
  }
  recruitDialogMode.value = "personal";
  recruitDialogVisible.value = true;
}

async function handleRecruitConfirm(payload: { characterId: number }) {
  const isPersonal = recruitDialogMode.value === "personal";
  const sh = isPersonal
    ? activeCharacterStrongholdForCommands.value
    : activeStrongholdForCommands.value;
  if (!sh || !state.value) return;

  loading.value = true;
  error.value = "";
  try {
    state.value = isPersonal
      ? await personalRecruit(payload.characterId)
      : await recruitAtStronghold(sh.id, payload.characterId);
    const general = state.value.characters?.find((c) => c.id === payload.characterId);
    if (general) {
      showRecruitSpeechBubble(
        general.name ?? "将领",
        recruitAssignmentBubbleMessage("conscript", sh.name ?? "据点", isPersonal ? "personal" : "assign"),
      );
    }
    info.value = general
      ? isPersonal
        ? `${general.name} 已在 ${sh.name} 执行个人征兵任务（60 日期限）`
        : `已向 ${general.name} 发布 ${sh.name} 征兵任务（将领抵达后执行）`
      : isPersonal
        ? `已在 ${sh.name} 派发个人征兵任务`
        : `已在 ${sh.name} 派发征兵任务`;
    onCancel();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "征兵失败";
  } finally {
    loading.value = false;
  }
}

async function handleMercenaryRecruitConfirm(payload: { characterId: number; budgetMoney: number }) {
  const isPersonal = mercenaryRecruitDialogMode.value === "personal";
  const sh = isPersonal
    ? activeCharacterStrongholdForCommands.value
    : activeStrongholdForCommands.value;
  if (!sh || !state.value) return;

  loading.value = true;
  error.value = "";
  try {
    state.value = isPersonal
      ? await personalMercenaryRecruit(payload.characterId, payload.budgetMoney)
      : await mercenaryRecruitAtStronghold(sh.id, payload.characterId, payload.budgetMoney);
    const general = state.value.characters?.find((c) => c.id === payload.characterId);
    if (general) {
      showRecruitSpeechBubble(
        general.name ?? "将领",
        recruitAssignmentBubbleMessage("mercenary", sh.name ?? "据点", isPersonal ? "personal" : "assign"),
      );
    }
    info.value = general
      ? isPersonal
        ? `${general.name} 已以 ${payload.budgetMoney.toLocaleString()} 文在 ${sh.name} 个人募兵（60 日期限）`
        : `已向 ${general.name} 发布 ${sh.name} 募兵任务（预算 ${payload.budgetMoney.toLocaleString()} 文，将领抵达后执行）`
      : isPersonal
        ? `已在 ${sh.name} 派发个人募兵任务`
        : `已在 ${sh.name} 发布募兵任务`;
    onCancel();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "募兵失败";
  } finally {
    loading.value = false;
  }
}

async function handleTaxRateConfirm(payload: {
  pollTaxRate?: number;
  agricultureTaxRate?: number;
  commerceTaxRate?: number;
  tariffTaxRate?: number;
}) {
  const sh = activeStrongholdForCommands.value ?? popupStronghold.value ?? selectedStronghold.value;
  if (!sh || !state.value) return;

  loading.value = true;
  error.value = "";
  try {
    const response = await setStrongholdTaxRates(sh.id, payload);
    state.value = response.state;
    info.value =
      response.outcome === "CarrierDispatched" ||
      response.outcome === "MessengerDispatched"
        ? `税令已从当主居城派出信使，抵达 ${sh.name} 后生效`
        : `${sh.name} 税率已即时生效`;
    if (response.outcome === "AppliedImmediately") {
      appendEvents([{ category: "TaxRateApplied", message: `✅ ${sh.name} 税率已即时生效` }]);
    } else {
      appendEvents([{ category: "TaxRateDispatched", message: `📨 税令信使已出发，目标 ${sh.name}` }]);
    }
    onCancel();
  } catch (e) {
    const message = e instanceof Error ? e.message : "税率调整失败";
    await handleStrategyApiError(message, message, {
      lordNotAtResidenceMessage: LORD_AT_RESIDENCE_REQUIRED_TIP,
    });
  } finally {
    loading.value = false;
  }
}

async function handleGovernancePolicyConfirm(priority: StrongholdGovernancePriorityValue) {
  const sh = activeStrongholdForCommands.value ?? popupStronghold.value ?? selectedStronghold.value;
  if (!sh || !state.value) return;

  loading.value = true;
  error.value = "";
  try {
    const response = await setStrongholdGovernancePriority(sh.id, priority);
    state.value = response.state;
    const priorityLabel =
      priority === "Military"
        ? "军事优先"
        : priority === "Domestic"
          ? "内政优先"
          : "自由决策";
    info.value =
      response.outcome === "CarrierDispatched" ||
      response.outcome === "MessengerDispatched"
        ? `方针令已从当主居城派出信使，抵达 ${sh.name} 后生效（${priorityLabel}）`
        : `${sh.name} 方针已即时生效（${priorityLabel}）`;
    if (response.outcome === "AppliedImmediately") {
      appendEvents([
        { category: "GovernancePriorityApplied", message: `✅ ${sh.name} 方针已即时生效：${priorityLabel}` },
      ]);
    } else {
      appendEvents([
        { category: "GovernancePriorityDispatched", message: `📨 方针信使已出发，目标 ${sh.name}（${priorityLabel}）` },
      ]);
    }
    onCancel();
  } catch (e) {
    const message = e instanceof Error ? e.message : "方针设置失败";
    await handleStrategyApiError(message, message, {
      lordNotAtResidenceMessage: LORD_AT_RESIDENCE_REQUIRED_TIP,
    });
  } finally {
    loading.value = false;
  }
}

async function handleBeginEspionage() {
  const sh = popupStronghold.value ?? selectedStronghold.value;
  if (!sh || !canEspionageStronghold.value) {
    void notifyActionBlocked("无法间谍", "仅可对情报未明的非本家势力据点展开间谍搜索");
    return;
  }

  loading.value = true;
  error.value = "";
  try {
    state.value = await recordEspionageIntel({
      targetKind: "Stronghold",
      targetId: sh.id,
      scope: "Both",
      precision: "Fuzzy",
    });
    info.value = `已对 ${sh.name} 完成间谍搜索`;
    onCancel();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "间谍搜索失败";
  } finally {
    loading.value = false;
  }
}

function handleBeginAppointLord() {
  if (!canLordCommandActiveStronghold.value) {
    showUnavailableActionTip(LORD_COMMAND_STRONGHOLD_TIP);
    return;
  }
  appointLordDialogVisible.value = true;
}

function handleBeginTransferCharacter() {
  if (!canLordCommandActiveStronghold.value) {
    showUnavailableActionTip(LORD_COMMAND_STRONGHOLD_TIP);
    return;
  }
  transferCharacterDialogVisible.value = true;
}

async function handleAppointLordConfirm(payload: {
  strongholdId: number;
  characterId: number;
  appointType: "Lord" | "Mayor";
  closeAfter: boolean;
}) {
  if (!state.value) return;
  loading.value = true;
  error.value = "";
  try {
    state.value = await appointStrongholdLord(
      payload.strongholdId,
      payload.characterId,
      payload.appointType,
    );
    const sh = state.value.strongholds.find((s) => s.id === payload.strongholdId);
    if (payload.appointType === "Mayor") {
      info.value = `已任命代官，将领正前往 ${sh?.name ?? "目标据点"}`;
    } else {
      const isDirect = payload.characterId === forceLordCharacterIdForState(state.value);
      info.value = isDirect
        ? `${sh?.name ?? "据点"} 已设为当主直辖`
        : `已任命领主，将领正前往 ${sh?.name ?? "目标据点"}`;
    }
    if (payload.closeAfter) {
      onCancel();
    }
  } catch (e) {
    const message = e instanceof Error ? e.message : "任命失败";
    await handleStrategyApiError(message, message, {
      lordNotAtResidenceMessage: LORD_COMMAND_STRONGHOLD_TIP,
    });
  } finally {
    loading.value = false;
  }
}

function handleBeginRecallCharacter() {
  if (!canLordCommandActiveStronghold.value) {
    showUnavailableActionTip(LORD_COMMAND_STRONGHOLD_TIP);
    return;
  }
  recallCharacterDialogVisible.value = true;
}

async function handleTransferCharacterConfirm(payload: {
  mode: "dispatch" | "summon";
  strongholdId: number;
  destinationStrongholdId?: number;
  characterId: number;
  closeAfter: boolean;
}) {
  if (!state.value) return;
  loading.value = true;
  error.value = "";
  try {
    state.value = await transferCharacterToStronghold(payload.strongholdId, {
      characterId: payload.characterId,
      mode: payload.mode,
      destinationStrongholdId: payload.destinationStrongholdId,
    });
    const general = state.value.characters?.find((c) => c.id === payload.characterId);
    if (payload.mode === "dispatch") {
      const dest = state.value.strongholds.find((s) => s.id === payload.destinationStrongholdId);
      info.value = general
        ? `已派遣 ${general.name} 前往 ${dest?.name ?? "目标据点"}`
        : `已派遣将领前往 ${dest?.name ?? "目标据点"}`;
    } else {
      const sh = state.value.strongholds.find((s) => s.id === payload.strongholdId);
      info.value = general
        ? `已下令 ${general.name} 前往 ${sh?.name ?? "目标据点"}`
        : `已下令将领前往 ${sh?.name ?? "目标据点"}`;
    }
    if (payload.closeAfter) {
      onCancel();
    }
  } catch (e) {
    const message = e instanceof Error ? e.message : "调动失败";
    await handleStrategyApiError(message, message, {
      lordNotAtResidenceMessage: LORD_COMMAND_STRONGHOLD_TIP,
    });
  } finally {
    loading.value = false;
  }
}

async function handleRecallCharacterConfirm(payload: {
  strongholdId: number;
  characterId: number;
  closeAfter: boolean;
}) {
  if (!state.value) return;
  loading.value = true;
  error.value = "";
  try {
    const response = await recallCharacter(payload.strongholdId, payload.characterId);
    const nextState = response.state;
    state.value = nextState;
    const general = nextState.characters?.find((c) => c.id === payload.characterId);
    if (response.outcome === "AppliedImmediately") {
      info.value = general
        ? `召回令已传达，${general.name} 正尽快回城`
        : "召回令已传达，将领正尽快回城";
    } else {
      info.value = general
        ? `已派出信使向 ${general.name} 传达召回令`
        : "已派出信使传达召回令";
    }
    if (payload.closeAfter) {
      onCancel();
    }
  } catch (e) {
    const message = e instanceof Error ? e.message : "召回失败";
    await handleStrategyApiError(message, message, {
      lordNotAtResidenceMessage: LORD_COMMAND_STRONGHOLD_TIP,
    });
  } finally {
    loading.value = false;
  }
}

function forceLordCharacterIdForState(ws: StrategyWorldState): number | null {
  const residence = resolveLordResidenceStronghold(ws);
  if (!residence) return null;
  const lordName = ws.lord.name?.trim();
  if (lordName) {
    const byName = (ws.characters ?? []).find(
      (c) => c.forceId === ws.playerForceId && c.name === lordName,
    );
    if (byName) return byName.id;
  }
  return (ws.characters ?? []).find(
    (c) => c.forceId === ws.playerForceId && c.strongholdId === residence.id,
  )?.id ?? null;
}

async function handleSplitDialogConfirm(payload: { subUnitIds: number[]; unitName?: string }) {
  pendingSplitSubUnitIds.value = payload.subUnitIds;
  pendingSplitUnitName.value = payload.unitName;
  splitDialogVisible.value = false;
  enterSplitSpawnSelection();
}

async function handleExpeditionConfirm(payload: {
  unitName?: string;
  commanderId: number;
  composition: import("@/api/strategyTypes").StrategyDeployCompositionEntry[];
  deployToMap: boolean;
}) {
  const sh = selectedStronghold.value;
  if (!sh) return;

  loading.value = true;
  error.value = "";
  try {
    state.value = await deployFromStronghold(sh.id, payload);
    info.value = payload.deployToMap
      ? `已从 ${sh.name} 组建并出城`
      : `已在 ${sh.name} 组建部队（在城中）`;
    onCancel();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "组建失败";
  } finally {
    loading.value = false;
  }
}

async function executeMerge(sourceUnitId: number, targetUnitId: number) {
  loading.value = true;
  error.value = "";
  try {
    state.value = await mergeUnits(sourceUnitId, targetUnitId);
    info.value = "部队已合并";
    pendingMergeTargetUnitId.value = null;
    onCancel();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "合并失败";
    pendingMergeTargetUnitId.value = null;
  } finally {
    loading.value = false;
  }
}

async function executeSplit(unitId: number, target: StrategyMoveTarget, subUnitIds: number[], unitName?: string) {
  loading.value = true;
  error.value = "";
  try {
    state.value = await splitUnit(unitId, subUnitIds, target.x, target.y, unitName);
    info.value = `已在 (${target.x}, ${target.y}) 分兵`;
    pendingSplitSubUnitIds.value = [];
    onCancel();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "分兵失败";
    pendingSplitSubUnitIds.value = [];
    onCancel();
  } finally {
    loading.value = false;
  }
}

async function handleSiegeOrder(mode: "Assault" | "Encircle") {
  const unitId = selectedUnitId.value;
  const sh = popupStronghold.value;
  if (unitId === null || !sh) return;

  const reason = siegeApBlockReason(selectedUnit.value);
  if (reason) {
    void notifyActionBlocked("无法攻城", reason);
    return;
  }

  loading.value = true;
  error.value = "";
  try {
    state.value = await orderUnitSiege(unitId, sh.id, mode);
    info.value = mode === "Assault" ? `已对 ${sh.name} 下达强攻` : `已对 ${sh.name} 下达包围`;
    onCancel();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "攻城指令失败";
  } finally {
    loading.value = false;
  }
}

function handleSelectUnit(payload: {
  unitId: number;
  screenX: number;
  screenY: number;
  panelAnchorRect?: { left: number; top: number; width: number; height: number };
  anchorSide?: AnchorSide;
}) {
  closeForceCommandMenu();
  logStrategyMapCoords("select-unit", payload);
  onSelectUnit(payload);

  if (stateId.value === "mergeTargetSelect") {
    const targetId = pendingMergeTargetUnitId.value;
    const sourceId = selectedUnitId.value;
    if (targetId && sourceId !== null && targetId !== sourceId) {
      void executeMerge(sourceId, targetId);
    }
  }
}

function handleSelectStronghold(payload: { strongholdId: number; screenX: number; screenY: number }) {
  closeForceCommandMenu();
  logStrategyMapCoords("select-stronghold", payload);
  onSelectStronghold(payload);
}

function handleSelectCharacter(payload: { characterId: number; screenX: number; screenY: number }) {
  closeForceCommandMenu();
  logStrategyMapCoords("select-character", payload);
  onSelectCharacter(payload);
}

function handleSelectCellEntities(payload: { x: number; y: number; screenX: number; screenY: number }) {
  if (!state.value) return;
  closeForceCommandMenu();
  logStrategyMapCoords("select-cell-entities", payload);
  const entities = collectMapCellEntityOptions(state.value, payload.x, payload.y, {
    includeUnits: mapUnitSelectionEnabled.value,
    includeCharacters: mapCharacterSelectionEnabled.value,
    includeStrongholds: mapStrongholdSelectionEnabled.value,
    includeConvoys: mapConvoySelectionEnabled.value,
  });
  if (entities.length <= 1) return;
  onSelectCellEntities(payload, entities);
}

function handlePickCellEntity(entity: import("@/utils/mapCellEntityPicker").MapCellEntityOption) {
  onPickCellEntity(entity);
}

function handleEntityPickerCancel() {
  handlePopupCancel();
}

async function handleBeginLeaveStronghold() {
  const characterId = selectedCharacterId.value ?? resolvePlayerLordCharacterId(state.value!);
  if (!characterId || !state.value) return;

  const sh = activeCharacterStronghold.value;
  const force = isStrongholdBesieged(sh);
  if (force) {
    try {
      await ElMessageBox.confirm(
        `${sh?.name ?? "据点"} 正被围攻。强行出城将消耗 ${CHARACTER_GATE_AP_COST} AP，并可能负伤或被俘。是否继续？`,
        "强行出城",
        { type: "warning", confirmButtonText: "强行出城", cancelButtonText: "取消" },
      );
    } catch {
      return;
    }
  }

  loading.value = true;
  error.value = "";
  try {
    state.value = await leaveStrongholdAsCharacter(characterId, force);
    info.value = force ? "当主已强行出城" : "当主已出城";
    selectedCharacterId.value = characterId;
  } catch (e) {
    const message = e instanceof Error ? e.message : "出城失败";
    await handleStrategyApiError(message, message);
  } finally {
    loading.value = false;
  }
}

async function handleBeginEnterStronghold() {
  const ws = state.value;
  const characterId = selectedCharacterId.value ?? (ws ? resolvePlayerLordCharacterId(ws) : null);
  const sh = ws
    ? resolveCharacterGateStronghold(
        ws,
        activeCharacterStronghold.value ?? selectedStronghold.value,
      )
    : null;
  if (!characterId || !sh || !ws) {
    void notifyActionBlocked("无法入城", "须与本家据点同格且当主在地图方可入城");
    return;
  }

  if (!canEnterStrongholdAtCell(ws, sh)) {
    void notifyActionBlocked("无法入城", "须与本家据点同格且当主在地图方可入城");
    return;
  }

  const force = isStrongholdBesieged(sh);
  if (force) {
    try {
      await ElMessageBox.confirm(
        `${sh.name} 正被围攻。强行入城将消耗 ${CHARACTER_GATE_AP_COST} AP，并可能负伤或被俘。是否继续？`,
        "强行入城",
        { type: "warning", confirmButtonText: "强行入城", cancelButtonText: "取消" },
      );
    } catch {
      return;
    }
  }

  loading.value = true;
  error.value = "";
  try {
    state.value = await enterStrongholdAsCharacter(characterId, sh.id, force);
    info.value = force ? `当主已强行进入 ${sh.name}` : `当主已进入 ${sh.name}`;
    selectedCharacterId.value = characterId;
  } catch (e) {
    const message = e instanceof Error ? e.message : "入城失败";
    await handleStrategyApiError(message, message);
  } finally {
    loading.value = false;
  }
}

async function handleBeginUnitEnterStronghold() {
  const unitId = selectedUnitId.value;
  const sh = popupStronghold.value;
  if (!unitId || !sh) {
    void notifyActionBlocked("无法入城", "须在据点格上且部队在地图方可入城");
    return;
  }

  loading.value = true;
  error.value = "";
  try {
    state.value = await enterUnitStronghold(unitId, sh.id);
    info.value = `${selectedUnit.value?.name ?? "部队"} 已进入 ${sh.name}`;
    onCancel();
  } catch (e) {
    const message = e instanceof Error ? e.message : "入城失败";
    await handleStrategyApiError(message, message);
  } finally {
    loading.value = false;
  }
}

async function handleBeginUnitExitStronghold() {
  const unitId = selectedUnitId.value;
  const sh = unitPopupStronghold.value;
  if (!unitId || !sh) {
    void notifyActionBlocked("无法出城", "部队须在城内方可出城");
    return;
  }

  loading.value = true;
  error.value = "";
  try {
    state.value = await exitUnitStronghold(unitId, sh.id);
    info.value = `${selectedUnit.value?.name ?? "部队"} 已离开 ${sh.name}`;
    onCancel();
  } catch (e) {
    const message = e instanceof Error ? e.message : "出城失败";
    await handleStrategyApiError(message, message);
  } finally {
    loading.value = false;
  }
}

async function handleBeginUnitDisband() {
  const unitId = selectedUnitId.value;
  const unit = selectedUnit.value;
  if (!unitId || !unit) return;

  try {
    await ElMessageBox.confirm(
      `确定在 ${unitPopupStronghold.value?.name ?? "据点"} 建制解散「${unit.name}」？兵力与物资将归还据点。`,
      "建制解散",
      { type: "warning", confirmButtonText: "解散", cancelButtonText: "取消" },
    );
  } catch {
    return;
  }

  loading.value = true;
  error.value = "";
  try {
    state.value = await disbandUnitOrganizationally(unitId);
    info.value = `${unit.name} 已建制解散`;
    selectedUnitId.value = null;
    onCancel();
  } catch (e) {
    const message = e instanceof Error ? e.message : "解散失败";
    await handleStrategyApiError(message, message);
  } finally {
    loading.value = false;
  }
}

function handleBeginVisit() {
  void notifyActionBlocked("拜访", "拜访、登庸与计谋将在 RPG 模式中扩展");
}

function handleSelectConvoy(payload: { convoyId: number; screenX: number; screenY: number }) {
  closeForceCommandMenu();
  logStrategyMapCoords("select-convoy", payload);
  onSelectConvoy(payload);
}

async function handleSelectCell(payload: { x: number; y: number; screenX: number; screenY: number }) {
  logStrategyMapCoords("select-cell", { ...payload, stateId: stateId.value });

  if (stateId.value === "moveTargetSelect" || stateId.value === "characterMoveTargetSelect") {
    await handleMovePathClick(payload);
    return;
  }

  const target = onSelectCell(payload);
  if (!target) return;

  if (stateId.value === "attackTargetSelect") {
    const apReason = attackApBlockReason(selectedUnit.value);
    if (apReason) {
      void notifyActionBlocked("无法攻击", apReason);
      onCancel();
      return;
    }
    await loadBattlePreview(target);
    if (battlePreview.value) {
      onBattlePreviewReady();
      battleConfirmVisible.value = true;
    }
    return;
  }

  if (stateId.value === "splitSpawnSelect") {
    const unitId = selectedUnitId.value;
    const subUnitIds = [...pendingSplitSubUnitIds.value];
    if (unitId !== null && subUnitIds.length > 0) {
      await executeSplit(unitId, target, subUnitIds, pendingSplitUnitName.value);
    }
    return;
  }

  await executeMove(target);
}

function handleMapContextMenu() {
  if (mapRightClickEnabled.value) {
    onMapRightClick();
  }
}

async function loadBattlePreview(target: StrategyMoveTarget) {
  if (selectedUnitId.value === null) return;

  loading.value = true;
  error.value = "";
  try {
    battlePreview.value = await previewBattle(selectedUnitId.value, target.x, target.y);
  } catch (e) {
    const message = e instanceof Error ? e.message : "战前预览失败";
    await handleStrategyApiError(message, message, {
      attackApBlockReason:
        attackApBlockReason(selectedUnit.value) ?? "AP 不足，无法发起攻击",
      dataNotFoundReloadHint: "服务端未加载剧本，已尝试自动重载；若仍失败请刷新页面",
    });
    battlePreview.value = null;
    onCancel();
  } finally {
    loading.value = false;
  }
}

function openMapIntel() {
  clearMapHoverState();
  const mainTab = resolveIntelMainTabForMenuPopup(menuPopupMode.value);

  if (mainTab === "person") {
    const id =
      selectedCharacterId.value ?? resolvePlayerLordCharacterId(state.value!);
    if (id && state.value) {
      const name =
        state.value.characters?.find((c) => c.id === id)?.name
        ?? state.value.lord.name
        ?? "当主";
      openIntelSystemFocused("person", id, `👤 ${name}`);
      return;
    }
  }

  if (mainTab === "stronghold") {
    const sh =
      selectedStronghold.value
      ?? popupStronghold.value
      ?? activeStrongholdForCommands.value;
    if (sh) {
      openIntelSystemFocused("stronghold", sh.id, `🏯 ${sh.name}`);
      return;
    }
  }

  openEntityIntelDialog();
}

function openEntityIntelDialog() {
  if (selectedUnit.value) {
    intelDialogTarget.value = { kind: "unit", unit: selectedUnit.value };
    intelDialogVisible.value = true;
    return;
  }
  if (selectedConvoy.value) {
    intelDialogTarget.value = { kind: "convoy", convoy: selectedConvoy.value };
    intelDialogVisible.value = true;
  }
}

function openIntelSystemFocused(
  tab: string,
  entityId: number | null = null,
  title?: string,
) {
  intelSystemInitialTab.value = tab;
  intelSystemInitialEntityId.value = entityId;
  intelSystemInitialRealmFilter.value = "all";
  intelSystemFocusMode.value = true;
  intelSystemFocusTitle.value = title ?? "情报";
  intelSystemVisible.value = true;
}

async function handleBattleConfirm(_payload: BattleConfirmPayload) {
  const target = lockedCommand.value;
  if (!target || selectedUnitId.value === null) return;
  onConfirmBattle();
  battleConfirmVisible.value = false;
  await executeBattle(target);
}

async function executeBattle(target: StrategyMoveTarget) {
  if (selectedUnitId.value === null) return;

  loading.value = true;
  error.value = "";
  try {
    state.value = await orderUnitAttack(selectedUnitId.value, target.x, target.y);
    battlePreview.value = null;
    onBattleSucceeded();
    info.value = "攻击命令已下达，推进日期后由系统结算战斗";
  } catch (e) {
    const message = e instanceof Error ? e.message : "攻击命令失败";
    await handleStrategyApiError(message, message, {
      attackApBlockReason:
        attackApBlockReason(selectedUnit.value) ?? "AP 不足，无法下达攻击命令",
    });
    onBattleFailed(target);
    if (battlePreview.value) battleConfirmVisible.value = true;
  } finally {
    loading.value = false;
  }
}

function showResolvedBattle(result: StrategyBattleResult) {
  battleResult.value = result;
  battleResultVisible.value = true;
}

function pushNotification(notification: StrategyPendingNotification) {
  if (shouldSkipPendingNotification(notification, pendingNotifications.value)) {
    return;
  }

  pendingNotifications.value = [...pendingNotifications.value, notification];
}

function openSettlementDialog(event: StrategyEvent) {
  settlementDetail.value = parseEconomySettlementFromEvent(event);
  settlementDialogVisible.value = true;
}

function openEventDetailDialog(event: StrategyEvent) {
  const category =
    event.category === "StrategicReportArrived" && event.detailCategory
      ? event.detailCategory === "SiegeEncircle"
        ? "围城开始"
        : event.detailCategory === "SiegeAssault"
          ? "强攻开始"
          : messageCategoryLabel(event.detailCategory)
      : event.category === "RecruitTaskCompleted"
        ? event.title?.trim() || event.brief?.trim() || "募兵/征兵汇报"
        : messageCategoryLabel(event.category);
  eventDetailTitle.value = category;
  eventDetailText.value =
    event.category === "StrategicReportArrived"
      ? strategicReportDetailText(event)
      : event.detailMessage?.trim() || event.message;
  eventDetailVisible.value = true;
}

function handleNotificationOpen(notification: StrategyPendingNotification) {
  pendingNotifications.value = pendingNotifications.value.filter(
    (item) => item.id !== notification.id
  );

  handlePendingNotificationOpen(notification, {
    showResolvedBattle,
    openEventDetailDialog,
    openSettlementDialog,
  });
}

function handlePopupCancel() {
  battlePreview.value = null;
  battleConfirmVisible.value = false;
  resetMovePath();
  onCancel();
}

function toggleForceCommandMenu() {
  forceCommandVisible.value = !forceCommandVisible.value;
}

function closeForceCommandMenu() {
  forceCommandVisible.value = false;
}

function openForceIntelFromMenu() {
  closeForceCommandMenu();
  const ws = state.value;
  if (!ws) return;
  const forceId = ws.playerForceId;
  const forceName = ws.forces.find((f) => f.id === forceId)?.name ?? "势力";
  openIntelSystemFocused("force", forceId, `${forceName} · 势力情报`);
}

function openIntelSystemDialog() {
  intelSystemInitialTab.value = "force";
  intelSystemInitialEntityId.value = null;
  intelSystemInitialRealmFilter.value = "all";
  intelSystemFocusMode.value = false;
  intelSystemFocusTitle.value = "";
  intelSystemVisible.value = true;
}

function onRosterUnitSelect(unitId: number, event: MouseEvent) {
  if (selectedUnitId.value === unitId && popupMode.value !== "none") {
    onCancel();
    return;
  }

  const panel = mapPanelRef.value;
  if (!panel) return;

  const item = event.currentTarget;
  if (!(item instanceof HTMLElement)) return;

  const panelRect = panel.getBoundingClientRect();
  const itemRect = item.getBoundingClientRect();

  handleSelectUnit({
    unitId,
    screenX: event.clientX,
    screenY: event.clientY,
    panelAnchorRect: {
      left: itemRect.left - panelRect.left,
      top: itemRect.top - panelRect.top,
      width: itemRect.width,
      height: itemRect.height,
    },
    anchorSide: "left",
  });
}

function isInsideForcePopup(target: EventTarget | null): boolean {
  if (!(target instanceof Node)) return false;
  const forceEl = forcePopupRef.value?.$el as HTMLElement | undefined;
  const statusEl = forceStatusRef.value;
  return Boolean(
    (forceEl && forceEl.contains(target)) || (statusEl && statusEl.contains(target))
  );
}

function isInsideMapPopup(target: EventTarget | null): boolean {
  if (!(target instanceof Node)) return false;
  const menuEl = menuPopupRef.value?.$el as HTMLElement | undefined;
  const pickerEl = cellEntityPickerRef.value?.$el as HTMLElement | undefined;
  const cornerEl = cornerPopupRef.value?.$el as HTMLElement | undefined;
  return Boolean(
    (menuEl && menuEl.contains(target))
    || (pickerEl && pickerEl.contains(target))
    || (cornerEl && cornerEl.contains(target))
  );
}

function isBlockingOverlayTarget(target: EventTarget | null): boolean {
  if (!(target instanceof Element)) return false;
  return Boolean(target.closest(".el-overlay, .el-message-box"));
}

/** 侧栏调试区、顶部提示条等 UI 不应触发「点空白取消地图 Popup」。 */
function isInsideProtectedChrome(target: EventTarget | null): boolean {
  if (!(target instanceof Element)) return false;
  return Boolean(target.closest(".side-panel, .map-unit-roster-float, .unit-roster-panel, .error-bar"));
}

function handleGlobalPointerDown(event: PointerEvent) {
  if (forceCommandVisible.value && !isInsideForcePopup(event.target)) {
    closeForceCommandMenu();
  }

  if (popupMode.value === "none") return;
  if (isInsideMapPopup(event.target)) return;
  if (isBlockingOverlayTarget(event.target)) return;
  if (isInsideProtectedChrome(event.target)) return;
  if (popupUsesCorner.value && mapCanvasRef.value?.containsPointerTarget(event.target)) return;

  event.preventDefault();
  event.stopPropagation();
  handlePopupCancel();
}

function onBattleConfirmVisibleChange(visible: boolean) {
  battleConfirmVisible.value = visible;
  if (!visible && stateId.value === "battleConfirm") {
    battlePreview.value = null;
    onCancel();
  }
}

function onIntelDialogVisibleChange(visible: boolean) {
  intelDialogVisible.value = visible;
  if (visible) {
    clearMapHoverState();
  } else {
    intelDialogTarget.value = null;
  }
}

async function executeMove(target: StrategyMoveTarget, via: MapPoint[] = []) {
  if (selectedCharacterId.value !== null) {
    loading.value = true;
    error.value = "";
    try {
      state.value = await moveCharacter(
        selectedCharacterId.value,
        target.x,
        target.y,
        via,
      );
      onMoveSucceeded();
      info.value = "当主开始移动";
    } catch (e) {
      error.value = e instanceof Error ? e.message : "移动指令失败";
      onMoveFailed(target);
    } finally {
      loading.value = false;
      await refreshMovementTrace();
    }
    return;
  }

  if (selectedUnitId.value === null) return;

  loading.value = true;
  error.value = "";
  try {
    state.value = await moveUnit(selectedUnitId.value, target.x, target.y, via);
    onMoveSucceeded();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "移动指令失败";
    onMoveFailed(target);
  } finally {
    loading.value = false;
    await refreshMovementTrace();
  }
}

async function refreshMovementTrace() {
  if (apiMode.value === "mock" || multiplayerSession.value) return;
  try {
    movementTrace.value = await getMovementTrace();
  } catch {
    /* Live 未启动或无 trace 端点时忽略 */
  }
}

async function ensureMapMaster(world?: StrategyWorldState | null) {
  const scenarioId = world?.scenarioId ?? state.value?.scenarioId ?? "mini_kanto";
  const width = world?.map.width ?? state.value?.map.width ?? 20;
  const height = world?.map.height ?? state.value?.map.height ?? 20;

  if (mapMasterMatchesScenario(mapMaster.value, scenarioId, width, height)) {
    return;
  }

  mapMaster.value = await getStrategyMapMaster();
}

function syncLastGameStartSettingsFromWorldState(world: StrategyWorldState) {
  if (!world.startOptions) return;
  const { intelDebugMode, ...customStartOptions } = world.startOptions;
  lastGameStartSettings.value = {
    scenarioId: world.scenarioId,
    difficulty: resolveDifficultyFromOptions(customStartOptions),
    customStartOptions,
    intelDebugMode: intelDebugMode ?? true,
    allForcesAiControlled: world.allForcesAiControlled ?? false,
  };
}

function resetMapSessionUi() {
  selectedUnitId.value = null;
  selectedStrongholdId.value = null;
  selectedConvoyId.value = null;
  selectedCell.value = null;
  battleConfirmVisible.value = false;
  battleResultVisible.value = false;
  intelDialogVisible.value = false;
  eventFeed.value = [];
  pendingNotifications.value = [];
  dismissRecruitReportBubble();
  settlementDialogVisible.value = false;
  settlementDetail.value = null;
  mapInteraction.reset();
  resetMovePath();
}

async function startGameWithSettings(settings: GameStartSettings) {
  lastGameStartSettings.value = settings;
  writeGameStartSettings(settings);
  initialLoading.value = true;
  initialLoadError.value = "";
  initialLoadPhase.value = "map";
  error.value = "";
  info.value = "";

  try {
    mapMaster.value = await getStrategyMapMaster();
    initialLoadPhase.value = "state";
    state.value = await loadScenario({
      scenarioId: settings.scenarioId,
      difficulty: settings.difficulty,
      customStartOptions: buildLoadStartOptions(settings),
      allForcesAiControlled: settings.allForcesAiControlled,
    });
    resetMapSessionUi();
    maybeShowTutorial();
    applyApiSourceInfoMessage();
    initialLoading.value = false;
    await refreshMovementTrace();
  } catch (e) {
    initialLoadPhase.value = "error";
    initialLoadError.value = e instanceof Error ? e.message : "加载失败";
    error.value = initialLoadError.value;
  }
}

/** 刷新页面时恢复后端当前局（不重新 loadScenario、不弹开局设置）。 */
async function resumeExistingGame() {
  initialLoading.value = true;
  initialLoadError.value = "";
  initialLoadPhase.value = "map";
  error.value = "";
  info.value = "";

  try {
    mapMaster.value = await getStrategyMapMaster();
    initialLoadPhase.value = "state";
    const next = await getStrategyState();
    await ensureMapMaster(next);
    state.value = next;
    syncLastGameStartSettingsFromWorldState(next);
    resetMapSessionUi();
    maybeShowTutorial();
    applyApiSourceInfoMessage();
    initialLoading.value = false;
    await refreshMovementTrace();
  } catch (e) {
    initialLoadPhase.value = "error";
    initialLoadError.value = e instanceof Error ? e.message : "读取世界状态失败";
    error.value = initialLoadError.value;
    initialLoading.value = false;
  }
}

function startMultiplayerPolling() {
  if (multiplayerPollTimer) clearInterval(multiplayerPollTimer);
  const generation = ++multiplayerPollGeneration;
  let lastWorldVersion = -1;
  let lastPresentedSequence = 0;
  if (!multiplayerSession.value) return;

  multiplayerPollTimer = setInterval(async () => {
    if (multiplayerPollPending || loading.value || initialLoading.value || !multiplayerSession.value) return;
    const originalState = state.value;
    const session = multiplayerSession.value;
    multiplayerPollPending = true;
    try {
      const room = await heartbeatMultiplayerRoom(session);
      if (generation !== multiplayerPollGeneration || multiplayerSession.value !== session) return;
      const mailbox = await getMultiplayerEvents(session);
      if (generation !== multiplayerPollGeneration || multiplayerSession.value !== session) return;
      const unread = mailbox.entries.filter(entry => entry.sequence > lastPresentedSequence);
      if (unread.length > 0) {
        if (mailbox.historyTruncated && lastPresentedSequence < unread[0]!.sequence - 1)
          info.value = "离线消息超过保留上限，部分旧消息已过期。";
        appendEvents(unread.map(entry => entry.event));
        lastPresentedSequence = unread[unread.length - 1]!.sequence;
      }
      // A failed acknowledgement retries the same cursor without displaying duplicates in this session.
      if (mailbox.entries.length > 0)
        await acknowledgeMultiplayerEvents(session, mailbox.entries[mailbox.entries.length - 1]!.sequence);
      if (generation !== multiplayerPollGeneration || multiplayerSession.value !== session) return;
      if (room.worldVersion === lastWorldVersion) return;
      const next = await getStrategyState();
      await ensureMapMaster(next);
      // A command, a session change or unmount may have occurred during fetch.
      if (generation !== multiplayerPollGeneration || multiplayerSession.value !== session
          || loading.value || state.value !== originalState) return;
      state.value = next;
      lastWorldVersion = room.worldVersion;
    } catch {
      // 短暂掉线由下一次轮询恢复；主动操作仍会显示明确错误。
    } finally {
      multiplayerPollPending = false;
    }
  }, 2500);
}

async function resumeMultiplayerGame() {
  multiplayerSession.value = readMultiplayerSession();
  if (!multiplayerSession.value) {
    error.value = "没有有效的多人房间会话";
    return;
  }

  await resumeExistingGame();
  if (!initialLoadError.value) {
    info.value = `联机房间 ${multiplayerSession.value.roomName}（${multiplayerSession.value.roomId}），势力 ${multiplayerSession.value.forceId}`;
    startMultiplayerPolling();
  }
}

function applyApiSourceInfoMessage() {
  const message = resolveStrategyApiSourceInfo(
    usingMockFallback.value,
    lastRequest.value?.source,
  );
  if (message) info.value = message;
}

function goToGameStartSettings() {
  emit("request-game-start");
}

async function exitMultiplayerRoom() {
  if (!multiplayerSession.value) return;
  try {
    await ElMessageBox.confirm("退出后该势力会暂时由 AI 接管。确定退出房间吗？", "退出联机", {
      confirmButtonText: "退出",
      cancelButtonText: "取消",
      type: "warning",
    });
    await leaveMultiplayerRoom();
    multiplayerPollGeneration++;
    multiplayerSession.value = null;
    if (multiplayerPollTimer) clearInterval(multiplayerPollTimer);
    multiplayerPollTimer = null;
    emit("exit-multiplayer");
  } catch (e) {
    if (e instanceof Error) ElMessage.error(e.message);
  }
}

async function bootstrapGame() {
  if (lastGameStartSettings.value) {
    await startGameWithSettings(lastGameStartSettings.value);
    return;
  }
  await resumeExistingGame();
}

async function fetchGameState() {
  loading.value = true;
  error.value = "";
  info.value = "";
  try {
    const next = await getStrategyState();
    await ensureMapMaster(next);
    state.value = next;
    selectedUnitId.value = null;
    selectedStrongholdId.value = null;
    selectedConvoyId.value = null;
    selectedCell.value = null;
    battleConfirmVisible.value = false;
    battleResultVisible.value = false;
    intelDialogVisible.value = false;
    eventFeed.value = [];
    pendingNotifications.value = [];
    dismissRecruitReportBubble();
    settlementDialogVisible.value = false;
    settlementDetail.value = null;
    mapInteraction.reset();
    resetMovePath();
    applyApiSourceInfoMessage();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "读取世界状态失败";
  } finally {
    loading.value = false;
    await refreshMovementTrace();
  }
}

/** 开发用：从剧本 JSON 重新初始化后端内存仿真。 */
async function reloadScenario() {
  if (multiplayerSession.value) {
    await fetchGameState();
    info.value = "已刷新多人房间状态";
    return;
  }

  if (lastGameStartSettings.value) {
    loading.value = true;
    try {
      await startGameWithSettings(lastGameStartSettings.value);
      info.value = "已按当前开局设置重新加载剧本";
    } catch (e) {
      error.value = e instanceof Error ? e.message : "重新加载失败";
    } finally {
      loading.value = false;
    }
    return;
  }
  goToGameStartSettings();
}

function switchApiMode(mode: StrategyApiMode) {
  setApiMode(mode);
  void fetchGameState();
}

function appendEvents(events: StrategyEvent[]) {
  if (!events.length) return;
  eventFeed.value = [...eventFeed.value, ...events].slice(-80);
  const playerForceId = state.value?.playerForceId;
  for (const evt of events) {
    if (evt.category === "RecruitTaskCompleted") {
      showRecruitReportBubble(evt);
    }
    const trayItem = notificationFromEvent(evt, playerForceId, state.value ?? undefined);
    if (trayItem) pushNotification(trayItem);
  }
}

function recruitAssignmentBubbleMessage(
  kind: "conscript" | "mercenary",
  strongholdName: string,
  mode: "assign" | "personal",
): string {
  const kindLabel = kind === "mercenary" ? "募兵" : "征兵";
  if (mode === "personal") {
    return `主公，我这就开始在${strongholdName}${kindLabel}！`;
  }
  return `主公，遵命！我这就前往${strongholdName}执行${kindLabel}任务。`;
}

function showRecruitSpeechBubble(
  characterName: string,
  message: string,
  event?: StrategyEvent,
) {
  if (recruitReportBubbleTimer) {
    clearTimeout(recruitReportBubbleTimer);
    recruitReportBubbleTimer = null;
  }
  recruitReportBubble.value = {
    characterName: characterName.trim() || "将领",
    message: message.trim() || "遵命！",
    event,
  };
  recruitReportBubbleTimer = setTimeout(() => {
    recruitReportBubble.value = null;
    recruitReportBubbleTimer = null;
  }, 12000);
}

function showRecruitReportBubble(evt: StrategyEvent) {
  showRecruitSpeechBubble(
    evt.characterName?.trim() || "将领",
    recruitCompletionBubbleMessage(evt),
    evt,
  );
}

function dismissRecruitReportBubble() {
  if (recruitReportBubbleTimer) {
    clearTimeout(recruitReportBubbleTimer);
    recruitReportBubbleTimer = null;
  }
  recruitReportBubble.value = null;
}

function dismissRecruitReportNotification(event: StrategyEvent) {
  pendingNotifications.value = pendingNotifications.value.filter(
    (n) =>
      !(
        n.event?.category === "RecruitTaskCompleted"
        && n.event?.characterId === event.characterId
      ),
  );
}

function openRecruitReportFromBubble(event: StrategyEvent) {
  openEventDetailDialog(event);
  dismissRecruitReportBubble();
  dismissRecruitReportNotification(event);
}

async function onAdvanceDay() {
  if (loading.value) return;
  loading.value = true;
  error.value = "";
  try {
    const response = await advanceDay();
    state.value = response.state;
    appendEvents(response.events ?? []);
    if (multiplayerSession.value && response.daysAdvanced === 0) {
      info.value = "已准备，等待房间内其他在线玩家准备完成";
    } else if (multiplayerSession.value) {
      info.value = "全员已准备，服务器已统一推进一天";
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : "推进日期失败";
    // 业务：自动推进失败时切回战略，避免空转重试
    if (!gamePaused.value) gamePaused.value = true;
  } finally {
    loading.value = false;
    await refreshMovementTrace();
  }
}

async function refreshSaveSlots() {
  saveSlotsLoading.value = true;
  try {
    saveSlots.value = await listStrategySaveSlots();
  } finally {
    saveSlotsLoading.value = false;
  }
}

async function openSaveSlotDialog() {
  if (multiplayerSession.value) {
    info.value = "多人房间由服务器统一保存，不能使用单机存档槽";
    return;
  }

  error.value = "";
  saveSlotDialogVisible.value = true;
  try {
    await refreshSaveSlots();
  } catch (e) {
    error.value = e instanceof Error ? e.message : "读取存档位失败";
    saveSlotDialogVisible.value = false;
  }
}

function resetMapSelectionAfterLoad() {
  selectedUnitId.value = null;
  selectedStrongholdId.value = null;
  selectedConvoyId.value = null;
  selectedCell.value = null;
  mapInteraction.reset();
}

async function onSaveToSlot(slot: number) {
  loading.value = true;
  error.value = "";
  try {
    await saveStrategyToSlot(slot);
    await refreshSaveSlots();
    saveSlotDialogVisible.value = false;
    info.value = `已存档至档位 ${slot}`;
  } catch (e) {
    error.value = e instanceof Error ? e.message : "存档失败";
  } finally {
    loading.value = false;
  }
}

async function onLoadFromSlot(slot: number) {
  const slotInfo = saveSlots.value.find((row) => row.slot === slot);
  if (slotInfo && !slotInfo.occupied) {
    error.value = `档位 ${slot} 为空，无法读档`;
    return;
  }

  loading.value = true;
  error.value = "";
  try {
    state.value = await loadStrategyFromSlot(slot);
    resetMapSelectionAfterLoad();
    saveSlotDialogVisible.value = false;
    info.value = `已从档位 ${slot} 读档`;
  } catch (e) {
    error.value = e instanceof Error ? e.message : "读档失败";
  } finally {
    loading.value = false;
    await refreshMovementTrace();
  }
}

async function onAdvanceDays(days: number) {
  if (loading.value) return;
  loading.value = true;
  error.value = "";
  try {
    const response = await advanceDays(days);
    state.value = response.state;
    appendEvents(response.events ?? []);
    if (multiplayerSession.value && response.daysAdvanced === 0) {
      info.value = "已准备，等待房间内其他在线玩家准备完成";
    }
  } catch (e) {
    error.value = e instanceof Error ? e.message : "批量推进日期失败";
    if (!gamePaused.value) gamePaused.value = true;
  } finally {
    loading.value = false;
    await refreshMovementTrace();
  }
}

async function handleCharacterInteraction(
  targetCharacterId: number,
  interaction: "Talk" | "Gift" | "Marry" | "DeclineMarriage",
) {
  if (!state.value) return;
  const lordId = resolvePlayerLordCharacterId(state.value);
  if (lordId == null) {
    error.value = "未找到玩家当主，无法互动";
    return;
  }

  loading.value = true;
  error.value = "";
  try {
    state.value = await interactWithCharacter(lordId, targetCharacterId, interaction);
    info.value = interaction === "Gift" ? "赠礼完成，双方关系已更新"
      : interaction === "Talk" ? "交谈完成，双方关系已更新" : "婚约操作完成，请查看人物记忆与关系";
  } catch (e) {
    error.value = characterSocialError(e instanceof Error ? e.message : "人物互动失败");
  } finally {
    loading.value = false;
  }
}

function isTypingTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) return false;
  const tag = target.tagName;
  return tag === "INPUT" || tag === "TEXTAREA" || target.isContentEditable;
}

function handleStrategyKeydown(event: KeyboardEvent) {
  if (event.key === "Escape" && isDiplomacyForcePicking.value) {
    event.preventDefault();
    onCancel();
    return;
  }

  if (event.code !== "Space" && event.key !== " ") return;
  if (isTypingTarget(event.target)) return;
  if (initialLoading.value || !state.value) return;

  event.preventDefault();
  gamePaused.value = !gamePaused.value;
}

onMounted(() => {
  window.addEventListener("keydown", handleStrategyKeydown);
  window.addEventListener("pointerdown", handleGlobalPointerDown, true);
  window.addEventListener("resize", updateHoverIntelPosition);
  window.addEventListener("resize", updateMapTopOverlayHeight);

  if (props.autoResume) {
    void resumeExistingGame();
  }
});

defineExpose({
  startGameWithSettings,
  resumeMultiplayerGame,
});

onBeforeUnmount(() => {
  multiplayerPollGeneration++;
  window.removeEventListener("keydown", handleStrategyKeydown);
  window.removeEventListener("pointerdown", handleGlobalPointerDown, true);
  window.removeEventListener("resize", updateHoverIntelPosition);
  window.removeEventListener("resize", updateMapTopOverlayHeight);
  mapTopOverlayResizeObserver?.disconnect();
  mapTopOverlayResizeObserver = null;
  if (multiplayerPollTimer) clearInterval(multiplayerPollTimer);
  multiplayerPollTimer = null;
  clearAutoAdvanceTimer();
});

watch([gamePaused, gameSpeed], () => syncAutoAdvanceLoop());

watch(
  () => [state.value, initialLoading.value] as const,
  () => {
    void nextTick(() => bindMapTopOverlayObserver());
    if (!gamePaused.value && !initialLoading.value && state.value) {
      syncAutoAdvanceLoop();
    }
  },
);
</script>

<template>
  <div class="strategy-page">
    <el-alert v-if="info" type="info" :title="info" show-icon :closable="false" class="error-bar" />
    <el-alert v-if="error" type="error" :title="error" show-icon :closable="false" class="error-bar" />

    <div class="strategy-body">
      <aside class="side-panel" :class="{ 'side-panel--pick-mode': isDiplomacyForcePicking }">
        <template v-if="multiplayerSession">
          <h3>多人房间</h3>
          <p class="hint">
            {{ multiplayerSession.roomName }} · {{ multiplayerSession.roomId }}<br>
            {{ multiplayerSession.playerName }} · 势力 {{ multiplayerSession.forceId }}
          </p>
          <el-button size="small" type="danger" plain @click="exitMultiplayerRoom">退出房间</el-button>
        </template>
        <h3>调试</h3>
        <el-button size="small" :loading="loading" @click="reloadScenario">重新加载剧本</el-button>
        <el-button size="small" @click="goToGameStartSettings">开局设置</el-button>
        <el-button
          type="primary"
          size="small"
          :loading="loading"
          class="debug-advance-btn"
          @click="onAdvanceDay"
        >
          ▶ 推进 1 日
        </el-button>

        <h3>API 诊断</h3>
        <ul class="diag-list">
          <li>模式：<code>{{ apiMode }}</code></li>
          <li v-if="lastRequest">
            请求：<code>{{ lastRequest.method }} {{ lastRequest.path }}</code>
          </li>
          <li v-if="lastRequest">
            完整 URL：<code class="url">{{ lastRequest.fullUrl }}</code>
          </li>
          <li v-if="lastRequest">页面 Origin：<code>{{ lastRequest.pageOrigin }}</code></li>
          <li v-if="lastRequest">
            来源 / 结果：
            <code>{{ lastRequest.source }}</code>
            /
            <code>{{ lastRequest.ok ? "OK" : "失败" }}</code>
            <span v-if="lastRequest.status"> ({{ lastRequest.status }})</span>
          </li>
          <li v-if="lastRequest?.error" class="diag-error">错误：{{ lastRequest.error }}</li>
          <li v-if="lastRequest">时间：{{ lastRequest.at }}</li>
        </ul>
        <div class="mode-buttons">
          <el-button size="small" :type="apiMode === 'auto' ? 'primary' : 'default'" @click="switchApiMode('auto')">
            Auto
          </el-button>
          <el-button size="small" :type="apiMode === 'live' ? 'primary' : 'default'" @click="switchApiMode('live')">
            Live
          </el-button>
          <el-button size="small" :type="apiMode === 'mock' ? 'primary' : 'default'" @click="switchApiMode('mock')">
            Mock
          </el-button>
        </div>

        <h3>移动路径诊断</h3>
        <p class="hint">开发模式：关注 <code>api.response.via</code> 中 <code>viaOnPath</code> 是否包含全部中继。</p>
        <ul class="move-path-log">
          <li v-for="(entry, i) in movePathDebugEntries" :key="i">
            <span class="log-time">{{ entry.at }}</span>
            <strong>{{ entry.event }}</strong>
            <pre class="log-detail">{{ formatMovePathLogDetail(entry.detail) }}</pre>
          </li>
          <li v-if="!movePathDebugEntries.length" class="hint">暂无日志（进入移动选格后点击地图）</li>
        </ul>

        <h3>选中单位</h3>
        <template v-if="selectedOperableUnit">
          <p
            class="unit-name"
            :style="{ color: getForceColorCss(selectedOperableUnit.unit.forceId) }"
          >
            {{ selectedOperableUnitDisplayName }}
            <span v-if="!isMapOperableUnit(selectedOperableUnit)" class="offmap-tag">视野外</span>
          </p>
          <ul class="unit-stats">
            <li v-if="isMapOperableUnit(selectedOperableUnit)">
              位置：({{ selectedOperableUnit.unit.x }}, {{ selectedOperableUnit.unit.y }})
            </li>
            <li v-else>位置：视野外（仅侧栏可操作）</li>
            <li>兵数：{{ formatSoldiers(selectedOperableUnit.unit.soldiers) }}</li>
            <li v-if="isMapOperableUnit(selectedOperableUnit)">
              移动力：{{ selectedOperableUnit.unit.movement }}
            </li>
            <li>AP：{{ selectedOperableUnit.unit.ap }}</li>
            <li>状态：{{ selectedOperableUnit.unit.status }}</li>
            <li
              v-if="isMapOperableUnit(selectedOperableUnit) && selectedOperableUnit.unit.status === 'Moving'"
              class="ap-hint"
            >
              移动中：若 AP 不足，需再推进数日沿路径继续（见下方移动诊断）。
            </li>
          </ul>
          <p class="hint">
            {{
              isMapOperableUnit(selectedOperableUnit)
                ? "点击地图上的己方单位打开指令菜单。"
                : "该部队当前不在视野内，可在右侧列表选中后下达指令（若已实装）。"
            }}
          </p>
        </template>
        <p v-else class="empty">点击地图或右侧列表中的单位进行选择</p>

        <h3 v-if="movementTrace.length">移动诊断（Live）</h3>
        <ol v-if="movementTrace.length" class="trace-list">
          <li v-for="entry in movementTrace.slice(-12).reverse()" :key="entry.sequence">
            <code>{{ entry.phase }}</code>
            {{ entry.message }}
            <span v-if="entry.unitId"> #{{ entry.unitId }}</span>
            <span v-if="entry.fromX != null"> ({{ entry.fromX }},{{ entry.fromY }})</span>
            <span v-if="entry.toX != null">→({{ entry.toX }},{{ entry.toY }})</span>
            <span v-if="entry.detail" class="trace-detail"> {{ entry.detail }}</span>
          </li>
        </ol>

        <h3>后勤</h3>
        <ul class="stronghold-list">
          <li>运输队：{{ state?.supplyConvoys.length ?? 0 }}</li>
          <li>在途文书：{{ state?.messageCarriers.length ?? 0 }}</li>
        </ul>
      </aside>

      <div class="map-column">
        <main ref="mapPanelRef" class="map-panel" @contextmenu.prevent="handleMapContextMenu">
          <StrategyMapLoadingScene
            v-if="initialLoading"
            :phase="initialLoadPhase"
            :map-name="mapMaster?.name ?? '迷你关东试玩'"
            :error="initialLoadError"
            @retry="bootstrapGame"
          />
          <StrategyMapCanvas
            v-if="state && mapMaster && !initialLoading"
            ref="mapCanvasRef"
            :world-state="state"
            :map-master="mapMaster"
            :selected-unit-id="selectedUnitId"
            :selected-character-id="selectedCharacterId"
            :selected-convoy-id="selectedConvoyId"
            :hover-unit-id="hoverUnitId"
            :hover-stronghold-id="hoverStrongholdId"
            :hover-convoy-id="hoverConvoyId"
            :selected-cell="selectedCell"
            :route-overlays="routeOverlays"
            :move-relay-markers="moveRelayMarkers"
            :map-unit-selection-enabled="mapUnitSelectionEnabled"
            :map-character-selection-enabled="mapCharacterSelectionEnabled"
            :map-convoy-selection-enabled="mapConvoySelectionEnabled"
            :map-cell-selection-enabled="mapCellSelectionEnabled"
            :map-stronghold-selection-enabled="mapStrongholdSelectionEnabled"
            :map-hover-suppressed="intelDialogVisible || diplomacyDialogVisible"
            :map-color-mode="mapColorMode"
            @select-unit="handleSelectUnit"
            @select-character="handleSelectCharacter"
            @select-stronghold="handleSelectStronghold"
            @select-convoy="handleSelectConvoy"
            @select-cell="handleSelectCell"
            @select-cell-entities="handleSelectCellEntities"
            @hover-cell="handleHoverCell"
            @viewport-change="onViewportChange"
          />

          <div
            v-if="state && !isDiplomacyForcePicking"
            ref="mapTopOverlayRef"
            class="map-overlay map-overlay--top"
            @pointerdown.stop
            @click.stop
            @wheel.stop
          >
            <div class="map-top-left">
              <div class="map-message-column">
                <div class="map-time-control map-float-panel">
                  <span class="date">{{ dateText }}</span>
                  <el-radio-group v-model="gameSpeed" size="small" class="speed-radios">
                    <el-radio-button :label="1" title="1 倍速">▶</el-radio-button>
                    <el-radio-button :label="2" title="2 倍速">▶▶</el-radio-button>
                    <el-radio-button :label="4" title="4 倍速">▶▶▶</el-radio-button>
                    <el-radio-button
                      v-if="state.allForcesAiControlled"
                      :label="8"
                      title="观战快进：每批推进 7 日"
                    >
                      观战×8
                    </el-radio-button>
                  </el-radio-group>
                  <el-tag
                    v-if="campaignStatusText"
                    :type="campaignStatusType"
                    size="small"
                    effect="dark"
                    :title="state.campaignStatus?.objective"
                  >
                    {{ campaignStatusText }}
                  </el-tag>
                </div>
                <div class="map-message-zone">
                  <StrategyMessageFeedToolbar
                    v-model:show-player="showPlayerMessages"
                    v-model:show-world="showWorldMessages"
                    @open-dialog="messageDialogVisible = true"
                  />
                  <StrategyEventFeed :events="scopedEventFeed" />
                  <div v-if="recruitReportBubble" class="recruit-report-slot">
                    <StrategyRecruitReportBubble
                      :visible="!!recruitReportBubble"
                      :character-name="recruitReportBubble.characterName"
                      :message="recruitReportBubble.message"
                      :event="recruitReportBubble.event"
                      @open-detail="openRecruitReportFromBubble"
                      @dismiss="dismissRecruitReportBubble"
                    />
                  </div>
                </div>
              </div>
              <div
                v-if="playerForce && playerForceStats"
                class="map-top-status map-float-panel"
              >
                <div
                  ref="forceStatusRef"
                  class="map-top-status__force map-top-status__force--clickable"
                  title="点击打开势力指令"
                  @click.stop="toggleForceCommandMenu"
                >
                  <div class="force-identity">
                    <span class="force-name">{{ playerForce.name }}</span>
                    <span v-if="playerLordName" class="force-lord" title="当主">
                      👑 {{ playerLordName }}
                    </span>
                    <span v-if="lordResidenceName" class="force-residence" title="居城">
                      🏠 {{ lordResidenceName }}
                    </span>
                    <span class="force-stat" title="威望">
                      ⭐ {{ playerForceStats.prestige }}
                    </span>
                    <span class="force-stat" title="正统">
                      📜 {{ playerForceStats.orthodoxy }}
                    </span>
                  </div>
                  <div class="force-resources">
                    <span class="force-stat" title="金钱">
                      💰 {{ formatMoneyKan(playerForce.money) }}
                    </span>
                    <span class="force-stat" title="粮食">
                      🌾 {{ formatFoodKoku(playerForce.food) }}
                    </span>
                    <span class="force-stat" title="据点数（封地合计 · 本势力）">
                      🏯 {{ playerForceStats.strongholdCount }}
                      <span class="force-stat-sub">({{ playerForceStats.ownStrongholdCount }})</span>
                    </span>
                    <span class="force-stat" title="将领数（封地合计 · 本势力）">
                      ⚔ {{ playerForceStats.characterCount }}
                      <span class="force-stat-sub">({{ playerForceStats.ownCharacterCount }})</span>
                    </span>
                  </div>
                </div>
                <StrategyForceCommandPopup
                  v-if="forceCommandVisible && playerForce"
                  ref="forcePopupRef"
                  class="map-force-command-layer"
                  :force-name="playerForce.name"
                  tooltip-side="right"
                  @show-intel="openForceIntelFromMenu"
                  @open-diplomacy="openDiplomacyDialog($event)"
                  @cancel="closeForceCommandMenu"
                />
              </div>
            </div>
            <div class="map-top-actions map-float-panel">
              <el-button size="small" @click="openTutorial">帮助</el-button>
              <el-button size="small" @click="openIntelSystemDialog">情报</el-button>
              <el-button size="small" @click="systemMenuVisible = true">系统</el-button>
            </div>
          </div>

          <div
            v-if="state"
            class="map-overlay map-overlay--bottom"
            :class="{ 'map-overlay--pick-mode': isDiplomacyForcePicking }"
            @pointerdown.stop
            @click.stop
            @wheel.stop
          >
            <div class="map-bottom-left">
              <div class="map-bottom-left-panel">
                <div v-if="pendingNotifications.length" class="map-bottom-notify">
                  <StrategyNotificationTray
                    :notifications="pendingNotifications"
                    @open="handleNotificationOpen"
                  />
                </div>
                <StrategyIntelBar
                  class="map-bottom-intel"
                  :world-state="state"
                  :x="intelBarX"
                  :y="intelBarY"
                  :terrain-name="intelTileInfo.terrainName"
                  :region-name="intelTileInfo.regionName"
                  :road-name="intelRoad?.typeName ?? null"
                  :road-level="intelRoad?.level ?? null"
                  :stronghold="intelBarStronghold"
                  :landmark-name="intelLandmark?.name ?? null"
                />
              </div>
            </div>
            <div class="map-bottom-right">
              <div class="map-game-pace map-float-panel">
                <el-button
                  v-if="gamePaused"
                  type="primary"
                  size="large"
                  class="game-pace-btn"
                  :disabled="initialLoading || !state"
                  title="进入进行：按左上角倍速自动推进日期"
                  @click="gamePaused = false"
                >
                  进行
                </el-button>
                <el-button
                  v-else
                  type="primary"
                  size="large"
                  class="game-pace-btn"
                  title="进入战略：暂停自动推进，可手动下达指令"
                  @click="gamePaused = true"
                >
                  战略
                </el-button>
              </div>
              <StrategyMapViewControls
                v-model="mapColorMode"
                class="map-bottom-view"
                :world-state="state"
                :map-master="mapMaster"
                :viewport="minimapViewport"
                @navigate="onMinimapNavigate"
              />
            </div>
          </div>

          <div
            v-if="state && !isDiplomacyForcePicking"
            class="map-unit-roster-float"
            :style="unitRosterFloatStyle"
          >
            <StrategyOperableUnitList
              :world-state="state"
              :selected-unit-id="selectedUnitId"
              @select="onRosterUnitSelect"
            />
          </div>

          <StrategyMapCellEntityPicker
            v-if="entityPickerVisible && menuAnchor"
            ref="cellEntityPickerRef"
            class="map-popup-layer map-popup-layer--anchor"
            :style="popupStyle"
            :entities="cellEntityPickerOptions"
            @pick="handlePickCellEntity"
            @cancel="handleEntityPickerCancel"
          />
          <StrategyMapPopup
            v-if="menuPopupMode && menuAnchor"
            ref="menuPopupRef"
            class="map-popup-layer map-popup-layer--anchor"
            :style="popupStyle"
            :mode="menuPopupMode"
            :entity-name="primaryPopupEntityName"
            :x="menuAnchor.x"
            :y="menuAnchor.y"
            :unit="selectedUnit"
            :tooltip-side="commandTooltipSide"
            :can-siege="canSiegePopupStronghold"
            :siege-stronghold-id="popupStronghold?.id ?? null"
            :can-expedition="canExpedition"
            :expedition-tooltip="expeditionTooltip"
            :lord-at-residence="canLordCommandActiveStronghold"
            :stronghold-commands-tooltip="LORD_COMMAND_STRONGHOLD_TIP"
            :can-adjust-tax="canAdjustTaxStronghold"
            :tax-rate-tooltip="taxRateTooltip"
            :can-set-governance-policy="canSetGovernancePolicyStronghold"
            :governance-policy-tooltip="governancePolicyTooltip"
            :can-espionage="canEspionageStronghold"
            :show-stronghold-directive="showStrongholdDirectiveButton"
            :stronghold-directive-only="strongholdDirectiveOnlyMenu"
            :can-unit-move="menuPopupMode === 'command' && selectedUnitDirectlyControlled && !selectedUnit?.inStronghold"
            :can-unit-siege="menuPopupMode === 'command' && selectedUnitDirectlyControlled && !selectedUnit?.inStronghold"
            :can-unit-enter-stronghold="menuPopupMode === 'command' && canUnitEnterStronghold"
            :can-unit-exit-stronghold="menuPopupMode === 'command' && canUnitExitStronghold"
            :can-unit-disband="menuPopupMode === 'command' && canUnitDisband"
            :can-unit-open-market="menuPopupMode === 'command' && canUnitOpenMarket"
            :can-stronghold-trade="canStrongholdTrade"
            :stronghold-trade-tooltip="strongholdTradeTooltip"
            :can-view-personal-market="menuPopupMode === 'characterCommand' && canViewPersonalMarket"
            :personal-market-tooltip="personalMarketTooltip"
            :merchant-shops="popupMerchantShops"
            :can-leave-stronghold="menuPopupMode === 'characterCommand' ? characterPopupProps.canLeaveStronghold : false"
            :can-character-move="menuPopupMode === 'characterCommand' ? characterPopupProps.canCharacterMove : false"
            :can-enter-stronghold="menuPopupMode === 'characterCommand' ? characterPopupProps.canEnterStronghold : false"
            :can-visit-others="menuPopupMode === 'characterCommand' ? characterPopupProps.canVisitOthers : false"
            :can-character-espionage="menuPopupMode === 'characterCommand' ? characterPopupProps.canCharacterEspionage : false"
            :gate-ap-cost="CHARACTER_GATE_AP_COST"
            :lord-ap="lordAp"
            :is-stronghold-besieged="menuPopupBesieged"
            :can-execute-personal-commands="canExecutePersonalCommands"
            @begin-move="handleBeginMove"
            @begin-attack="handleBeginAttack"
            @begin-directive="handleBeginDirective"
            @begin-merge="handleBeginMerge"
            @begin-split="handleBeginSplit"
            @begin-expedition="handleBeginExpedition"
            @begin-tax-rate="handleBeginTaxRate"
            @begin-governance-policy="handleBeginGovernancePolicy"
            @begin-mercenary-recruit="handleBeginMercenaryRecruit"
            @begin-recruit="handleBeginRecruit"
            @begin-personal-mercenary-recruit="handleBeginPersonalMercenaryRecruit"
            @begin-personal-recruit="handleBeginPersonalRecruit"
            @begin-espionage="handleBeginEspionage"
            @begin-appoint-lord="handleBeginAppointLord"
            @begin-transfer-character="handleBeginTransferCharacter"
            @begin-recall-character="handleBeginRecallCharacter"
            @begin-leave-stronghold="handleBeginLeaveStronghold"
            @begin-enter-stronghold="handleBeginEnterStronghold"
            @begin-unit-enter-stronghold="handleBeginUnitEnterStronghold"
            @begin-unit-exit-stronghold="handleBeginUnitExitStronghold"
            @begin-unit-disband="handleBeginUnitDisband"
            @open-unit-market="handleOpenUnitMarket"
            @open-stronghold-market="handleOpenStrongholdMarket"
            @open-personal-market="handleOpenPersonalMarket"
            @open-merchant-shop="handleOpenMerchantShop"
            @open-diplomacy="openDiplomacyDialog($event, popupStronghold?.forceId ?? null)"
            @begin-visit="handleBeginVisit"
            @siege-assault="handleSiegeOrder('Assault')"
            @siege-encircle="handleSiegeOrder('Encircle')"
            @show-intel="openMapIntel()"
            @cancel="handlePopupCancel"
          />
          <StrategyMapPopup
            v-if="popupUsesCorner"
            ref="cornerPopupRef"
            class="map-popup-layer map-popup-layer--corner"
            :mode="cornerHintMode"
            :x="selectedCell?.x ?? 0"
            :y="selectedCell?.y ?? 0"
            @cancel="handlePopupCancel"
          />
          <div v-if="!state && !initialLoading" class="map-placeholder">
            <el-empty description="未能加载剧本数据" />
          </div>
        </main>
      </div>
    </div>

    <StrategyEntityIntelDialog
      v-if="state"
      :visible="intelDialogVisible"
      :world-state="state"
      :target="intelDialogTarget"
      @update:visible="onIntelDialogVisibleChange"
    />
    <StrategyBattleConfirmDialog
      v-if="state"
      :visible="battleConfirmVisible"
      :world-state="state"
      :attacker="selectedUnit"
      :preview="battlePreview"
      @update:visible="onBattleConfirmVisibleChange"
      @confirm="handleBattleConfirm"
    />
    <StrategyBattleResultDialog
      :visible="battleResultVisible"
      :result="battleResult"
      :player-force-id="state?.playerForceId ?? 1"
      :world-state="state"
      @update:visible="battleResultVisible = $event"
    />
    <StrategyEconomySettlementDialog
      :visible="settlementDialogVisible"
      :detail="settlementDetail"
      @update:visible="settlementDialogVisible = $event"
    />
    <el-dialog
      :model-value="eventDetailVisible"
      :title="eventDetailTitle"
      width="560px"
      align-center
      destroy-on-close
      class="strategy-event-detail-dialog strategy-dialog-centered-footer"
      @update:model-value="eventDetailVisible = $event"
    >
      <textarea
        class="event-detail-textarea"
        readonly
        tabindex="-1"
        :value="eventDetailText"
        aria-label="消息详情"
      />
      <template #footer>
        <el-button type="primary" @click="eventDetailVisible = false">关闭</el-button>
      </template>
    </el-dialog>
    <StrategyMessageDialog
      :visible="messageDialogVisible"
      :events="scopedEventFeed"
      :player-force-id="state?.playerForceId"
      @update:visible="messageDialogVisible = $event"
      @open-detail="handleNotificationOpen"
    />
    <StrategyDirectiveDialog
      v-if="state"
      :visible="directiveDialogVisible"
      :unit="selectedUnit"
      :lord="state.lord"
      :strongholds="state.strongholds"
      @update:visible="directiveDialogVisible = $event"
      @confirm="handleDirectiveConfirm"
    />
    <StrategySplitDialog
      :visible="splitDialogVisible"
      :unit="selectedUnit"
      @update:visible="splitDialogVisible = $event"
      @confirm="handleSplitDialogConfirm"
    />
    <StrategyExpeditionDialog
      v-if="state"
      :visible="expeditionDialogVisible"
      :stronghold="selectedStronghold"
      :world-state="state"
      @update:visible="expeditionDialogVisible = $event"
      @confirm="handleExpeditionConfirm"
    />
    <StrategyDiplomacyDialog
      v-if="state"
      :visible="diplomacyDialogVisible"
      :action="diplomacyAction"
      :world-state="state"
      :lord-residence-stronghold-id="lordResidenceStrongholdId"
      :initial-target-force-id="diplomacyInitialTargetForceId"
      :success-chance-percent="diplomacySuccessChance"
      :travel-days="diplomacyTravelDays"
      :preview-loading="diplomacyPreviewLoading"
      :peace-required-war-score="diplomacyPeaceRequiredWarScore"
      :peace-can-force-acceptance="diplomacyPeaceCanForceAcceptance"
      @update:visible="diplomacyDialogVisible = $event"
      @update:character-id="diplomacyCharacterId = $event"
      @update:target-force-id="diplomacyTargetForceId = $event"
      @update:peace-terms="diplomacyPeaceTerms = $event"
      @request-preview="refreshDiplomacyPreview"
      @pick-force-from-map="beginDiplomacyForcePick"
      @confirm="handleDiplomacyConfirm"
    />
    <StrategyTaxRateDialog
      v-if="state"
      :visible="taxRateDialogVisible"
      :stronghold="activeStrongholdForCommands"
      :world-state="state"
      @update:visible="taxRateDialogVisible = $event"
      @confirm="handleTaxRateConfirm"
    />
    <StrategyMarketDialog
      v-if="state"
      :visible="marketDialogVisible"
      :world-state="state"
      :stronghold-id="marketDialogStrongholdId"
      :stronghold-name="marketDialogStrongholdName"
      :trade-mode="marketDialogTradeMode"
      :trade-unit="marketDialogTradeUnitResolved"
      :lord-money="marketDialogLordTreasury.money"
      :lord-food="marketDialogLordTreasury.food"
      :lord-horse="marketDialogLordTreasury.horse"
      @update:visible="marketDialogVisible = $event"
      @traded="handleMarketTraded($event)"
    />
    <StrategyStrongholdGovernanceDialog
      v-if="state"
      :visible="governancePolicyDialogVisible"
      :stronghold="activeStrongholdForCommands"
      :world-state="state"
      @update:visible="governancePolicyDialogVisible = $event"
      @confirm="handleGovernancePolicyConfirm"
    />
    <StrategyMercenaryRecruitDialog
      v-if="state"
      :visible="mercenaryRecruitDialogVisible"
      :mode="mercenaryRecruitDialogMode"
      :stronghold="
        mercenaryRecruitDialogMode === 'personal'
          ? activeCharacterStrongholdForCommands
          : activeStrongholdForCommands
      "
      :acting-character-id="activeCharacterForCommands?.id ?? null"
      :world-state="state"
      @update:visible="mercenaryRecruitDialogVisible = $event"
      @confirm="handleMercenaryRecruitConfirm"
    />
    <StrategyRecruitDialog
      v-if="state"
      :visible="recruitDialogVisible"
      :mode="recruitDialogMode"
      :stronghold="
        recruitDialogMode === 'personal'
          ? activeCharacterStrongholdForCommands
          : activeStrongholdForCommands
      "
      :acting-character-id="activeCharacterForCommands?.id ?? null"
      :world-state="state"
      @update:visible="recruitDialogVisible = $event"
      @confirm="handleRecruitConfirm"
    />
    <StrategyAppointLordDialog
      v-if="state"
      :visible="appointLordDialogVisible"
      :initial-stronghold="activeStrongholdForCommands"
      :world-state="state"
      @update:visible="appointLordDialogVisible = $event"
      @confirm="handleAppointLordConfirm"
    />
    <StrategyTransferCharacterDialog
      v-if="state"
      :visible="transferCharacterDialogVisible"
      :initial-stronghold="activeStrongholdForCommands"
      :world-state="state"
      @update:visible="transferCharacterDialogVisible = $event"
      @confirm="handleTransferCharacterConfirm"
    />
    <StrategyRecallCharacterDialog
      v-if="state"
      :visible="recallCharacterDialogVisible"
      :initial-stronghold="activeStrongholdForCommands"
      :world-state="state"
      @update:visible="recallCharacterDialogVisible = $event"
      @confirm="handleRecallCharacterConfirm"
    />
    <StrategyIntelSystemDialog
      :visible="intelSystemVisible"
      :world-state="state"
      :initial-tab="intelSystemInitialTab"
      :initial-realm-filter="intelSystemInitialRealmFilter"
      :initial-selected-entity-id="intelSystemInitialEntityId"
      :focus-mode="intelSystemFocusMode"
      :focus-title="intelSystemFocusTitle"
      @update:visible="intelSystemVisible = $event"
      @interact="handleCharacterInteraction"
    />
    <StrategySystemMenuDialog
      :visible="systemMenuVisible"
      @update:visible="systemMenuVisible = $event"
      @open-save-slots="openSaveSlotDialog"
      @open-load-slots="openSaveSlotDialog"
    />
    <StrategySaveSlotDialog
      :visible="saveSlotDialogVisible"
      :slots="saveSlots"
      :loading="loading || saveSlotsLoading"
      @update:visible="saveSlotDialogVisible = $event"
      @save="onSaveToSlot"
      @load="onLoadFromSlot"
    />
    <StrategyTutorialDialog
      :visible="tutorialVisible"
      :spectator="state?.allForcesAiControlled"
      @update:visible="tutorialVisible = $event"
      @finish="completeTutorial"
    />

    <div
      v-if="showHoverIntel && state && intelPinnedCell"
      ref="hoverIntelLayerRef"
      class="hover-intel-layer"
      :style="hoverIntelStyle"
      @pointerdown.stop
      @click.stop
      @pointerenter="onIntelLayerPointerEnter"
      @pointerleave="onIntelLayerPointerLeave"
    >
      <StrategyCellIntelHover
        :world-state="state"
        :x="intelPinnedCell.x"
        :y="intelPinnedCell.y"
              :anchor-side="hoverIntelAnchorSide"
              :vertical-align="hoverIntelVerticalAlign"
            />
    </div>
  </div>
</template>

<style scoped>
.strategy-page {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
  min-width: 0;
  gap: 8px;
  overflow: hidden;
  box-sizing: border-box;
  padding: 8px;
}

.map-float-panel {
  flex-shrink: 0;
  padding: 8px 14px;
  font-size: 0.88rem;
  color: #e2e8f0;
  background: rgba(15, 23, 42, 0.92);
  border: 1px solid rgba(148, 163, 184, 0.35);
  border-radius: 10px;
  backdrop-filter: blur(6px);
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.35);
  pointer-events: auto;
}

.map-top-left {
  display: flex;
  flex-direction: row;
  align-items: flex-start;
  gap: 8px;
  flex: 0 1 auto;
  min-width: 0;
  max-width: min(calc(100% - 120px), 720px);
}

.map-message-column {
  display: flex;
  flex-direction: column;
  align-items: stretch;
  gap: 4px;
  width: 24em;
  flex-shrink: 0;
  min-width: 0;
}

.map-time-control {
  display: inline-flex;
  align-items: center;
  align-self: flex-start;
  gap: 6px;
  flex-shrink: 0;
  width: fit-content;
  max-width: 100%;
  padding: 6px 10px;
  box-sizing: border-box;
}

.map-time-control .date {
  flex-shrink: 0;
  font-variant-numeric: tabular-nums;
  white-space: pre;
}

.map-time-control .speed-radios {
  flex-shrink: 0;
}

.map-time-control .speed-radios :deep(.el-radio-button) {
  flex: 1 1 0;
}

.map-time-control .speed-radios :deep(.el-radio-button__inner) {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 34px;
  min-width: 34px;
  padding: 4px 0;
  font-size: 0.68rem;
  line-height: 1.2;
  box-sizing: border-box;
}

.map-top-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
  margin-left: auto;
}

.map-top-status {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  flex-shrink: 0;
}

.map-top-status__force {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 4px 12px;
}

.map-top-status__force .force-identity,
.map-top-status__force .force-resources {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px 12px;
}

.map-top-status__force .force-name {
  font-weight: 600;
}

.map-top-status__force .force-lord {
  color: #e2e8f0;
}

.map-top-status__force .force-residence {
  color: #94a3b8;
}

.map-top-status__force .force-stat {
  color: #cbd5e1;
  font-size: 0.84rem;
  white-space: nowrap;
}

.map-top-status__force .force-stat-sub {
  color: #94a3b8;
  font-size: 0.8rem;
}

@media (min-width: 1366px) {
  .map-top-status__force {
    flex-direction: row;
    flex-wrap: nowrap;
    align-items: center;
    gap: 12px;
  }
}

.map-top-status__force--clickable {
  cursor: pointer;
  border-radius: 6px;
  padding: 2px 4px;
  margin: -2px -4px;
  transition: background 0.15s ease;
}

.map-top-status__force--clickable:hover {
  background: rgba(148, 163, 184, 0.15);
}

.map-force-command-layer {
  position: absolute;
  top: calc(100% + 6px);
  right: 0;
  z-index: 20;
}

.map-top-actions :deep(.el-button) {
  margin: 0;
}

.date {
  color: #e2e8f0;
  font-size: 0.9rem;
  white-space: nowrap;
}

.map-column {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  min-height: 0;
  background: #0f172a;
  border-radius: 8px;
  overflow: hidden;
}

.map-column .map-panel {
  flex: 1;
  min-height: 0;
  position: relative;
  overflow: hidden;
  border-radius: 8px;
}

.map-overlay {
  position: absolute;
  left: 0;
  right: 0;
  z-index: 14;
  pointer-events: none;
  padding: 10px 12px;
  box-sizing: border-box;
}

.map-overlay--top {
  top: 0;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.map-message-zone {
  display: flex;
  flex-direction: column;
  gap: 4px;
  width: 100%;
  flex-shrink: 0;
  min-width: 0;
  font-size: 0.82rem;
  pointer-events: none;
}

.map-message-zone :deep(.message-feed-toolbar) {
  pointer-events: auto;
}

.recruit-report-slot {
  display: block;
  pointer-events: auto;
}

.map-overlay--bottom {
  bottom: 0;
  display: flex;
  flex-direction: row;
  align-items: flex-end;
  gap: 10px;
}

.map-bottom-left {
  flex: 1;
  display: flex;
  justify-content: center;
  min-width: 0;
}

.map-bottom-left-panel {
  width: 100%;
  display: flex;
  flex-direction: column;
  align-items: stretch;
  gap: 8px;
  min-width: 0;
}

@media (min-width: 768px) {
  .map-bottom-left-panel {
    width: 80%;
  }
}

@media (min-width: 1200px) {
  .map-bottom-left-panel {
    width: 60%;
  }
}

.map-bottom-notify {
  display: flex;
  flex-direction: row;
  justify-content: flex-end;
  align-items: center;
  /* 与 StrategyIntelBar 水平 padding 对齐，使图标行宽 = 情报栏内容区宽 */
  padding: 0 14px;
  box-sizing: border-box;
  pointer-events: auto;
}

.map-bottom-intel {
  width: 100%;
  flex: 0 1 auto;
  min-width: 0;
  pointer-events: auto;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.35);
}

.map-bottom-right {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 8px;
  flex-shrink: 0;
}

.map-game-pace {
  pointer-events: auto;
  display: flex;
  justify-content: flex-end;
}

.game-pace-btn {
  min-width: 5.5em;
  padding: 10px 22px;
  font-size: 0.95rem;
  font-weight: 600;
}

.map-bottom-view {
  flex-shrink: 0;
}

.map-bottom-view :deep(.map-view-controls) {
  pointer-events: auto;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.35);
}

.map-unit-roster-float {
  position: absolute;
  right: 12px;
  z-index: 13;
  width: min(240px, 28vw);
  display: flex;
  flex-direction: column;
  min-height: 0;
  pointer-events: auto;
  box-sizing: border-box;
}

.map-unit-roster-float :deep(.unit-roster-panel) {
  flex: 1 1 auto;
  min-height: 0;
  max-height: 100%;
  overflow: hidden;
}

.map-unit-roster-float :deep(.unit-roster-list) {
  overflow-y: auto;
  overscroll-behavior: contain;
}

.strategy-body {
  display: flex;
  flex: 1;
  gap: 12px;
  min-height: 0;
}

.side-panel {
  width: 280px;
  flex-shrink: 0;
  padding: 12px;
  background: #1e293b;
  color: #e2e8f0;
  border-radius: 8px;
  overflow-y: auto;
  font-size: 0.9rem;
}

.side-panel h3 {
  margin: 0 0 8px;
  font-size: 0.85rem;
  color: #94a3b8;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.side-panel h3:not(:first-child) {
  margin-top: 16px;
}

.debug-advance-btn {
  display: block;
  width: 100%;
  margin-top: 8px;
}

.unit-name {
  margin: 0 0 8px;
  font-weight: 600;
  font-size: 1rem;
  display: flex;
  align-items: center;
  gap: 6px;
}

.offmap-tag {
  font-size: 0.68rem;
  font-weight: 500;
  color: #94a3b8;
  border: 1px solid rgba(148, 163, 184, 0.45);
  border-radius: 999px;
  padding: 0 6px;
}

.unit-stats {
  margin: 0 0 12px;
  padding-left: 18px;
}

.hint {
  margin: 0 0 16px;
  color: #64748b;
  font-size: 0.8rem;
  line-height: 1.4;
}

.trace-list {
  margin: 0 0 12px;
  padding-left: 1.2rem;
  font-size: 0.72rem;
  color: #cbd5e1;
  max-height: 220px;
  overflow-y: auto;
}

.trace-list li {
  margin-bottom: 4px;
}

.trace-detail {
  color: #94a3b8;
}

.empty {
  color: #64748b;
  margin: 0 0 16px;
}

.stronghold-list {
  margin: 0;
  padding-left: 18px;
  line-height: 1.6;
}

.diag-list {
  margin: 0 0 10px;
  padding-left: 18px;
  line-height: 1.5;
  font-size: 0.8rem;
  word-break: break-all;
}

.diag-list code {
  font-size: 0.75rem;
  color: #93c5fd;
}

.diag-list code.url {
  display: inline-block;
  margin-top: 2px;
}

.diag-error {
  color: #fca5a5;
}

.mode-buttons {
  display: flex;
  gap: 6px;
  margin-bottom: 16px;
}

.move-path-log {
  margin: 0 0 16px;
  padding-left: 0;
  list-style: none;
  max-height: 320px;
  overflow-y: auto;
  font-size: 0.72rem;
}

.move-path-log li {
  margin-bottom: 8px;
  padding-bottom: 8px;
  border-bottom: 1px solid #334155;
}

.move-path-log .log-time {
  color: #64748b;
  margin-right: 6px;
}

.move-path-log .log-detail {
  margin: 4px 0 0;
  padding: 6px;
  background: #0f172a;
  border-radius: 4px;
  white-space: pre-wrap;
  word-break: break-all;
  color: #cbd5e1;
  font-family: ui-monospace, monospace;
  font-size: 0.68rem;
}

.map-popup-layer {
  position: absolute;
  z-index: 20;
  pointer-events: auto;
}

.map-popup-layer--anchor {
  /* left/top 由 popupStyle 动态设置 */
}

.map-popup-layer--secondary {
  z-index: 21;
}

.map-popup-layer--corner {
  top: 12px;
  right: 12px;
  left: auto;
}

.hover-intel-layer {
  position: fixed;
  z-index: 200;
  pointer-events: auto;
  width: max-content;
  max-width: none;
  padding: 0;
  box-sizing: border-box;
  background: transparent;
  border: none;
  border-radius: 0;
  box-shadow: none;
  overflow: visible;
  isolation: isolate;
}

/* 兜底：确保子组件面板背景/边框在地图上方可见 */
.hover-intel-layer :deep(.intel-box) {
  background: #1e293b;
  border: 1px solid #38bdf8;
  border-radius: 8px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.5);
}

.hover-intel-layer :deep(.intel-box--civil) {
  border-color: #4ade80;
}

.map-placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
}

.error-bar {
  margin: 0;
  flex-shrink: 0;
}

.event-detail-textarea {
  display: block;
  width: 100%;
  min-height: min(360px, 50vh);
  max-height: min(360px, 50vh);
  margin: 0;
  padding: 10px 12px;
  box-sizing: border-box;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  resize: none;
  outline: none;
  background: #f8fafc;
  color: #1e293b;
  font-family: "Yu Mincho", "MS Mincho", "SimSun", serif;
  font-size: 0.88rem;
  line-height: 1.55;
  white-space: pre-wrap;
  overflow-y: auto;
}
</style>
