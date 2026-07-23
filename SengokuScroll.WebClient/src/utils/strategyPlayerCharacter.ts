import type { StrategyStrongholdState, StrategyUnitState, StrategyWorldState } from "@/api/strategy";
import { GameStartOptionsProfile } from "@/gameStartOptions/GameStartOptionsProfile";
import { isPlayerRealmForce } from "@/utils/mapEntityColors";
import { isLordAtResidence } from "@/utils/strategyLordCommands";

/** 解析玩家当主角色 Id。 */
export function resolvePlayerLordCharacterId(worldState: StrategyWorldState): number | null {
  const fromLord = worldState.lord.characterId;
  if (fromLord != null && fromLord > 0) return fromLord;

  const lordName = worldState.lord.name?.trim();
  if (!lordName) return null;
  return (
    worldState.characters?.find(
      (c) => !c.isDead && c.forceId === worldState.playerForceId && c.name === lordName,
    )?.id ?? null
  );
}

/** 当主是否正在领兵（LocationType=Unit）。 */
export function isLordLeadingUnit(worldState: StrategyWorldState): boolean {
  return worldState.lord.locationType === "Unit";
}

/** 当主当前率领的部队 Id（领兵时按 commanderId 解析，否则取 meta 绑定）。 */
export function resolveLordLedUnitId(worldState: StrategyWorldState): number | null {
  const lordCharacterId = resolvePlayerLordCharacterId(worldState);
  if (lordCharacterId != null) {
    const ledUnit = worldState.units.find((u) => u.commanderId === lordCharacterId);
    if (ledUnit) return ledUnit.id;
  }

  const metaUnitId = worldState.lord.unitId;
  if (metaUnitId != null && metaUnitId > 0) return metaUnitId;

  return null;
}

export function buildLordUnitControlContext(worldState: StrategyWorldState) {
  const lordCharacterId = resolvePlayerLordCharacterId(worldState);
  const character = lordCharacterId
    ? worldState.characters?.find((c) => c.id === lordCharacterId)
    : null;

  return {
    lordUnitId: resolveLordLedUnitId(worldState) ?? worldState.lord.unitId,
    lordCharacterId,
    lordX: worldState.lord.x,
    lordY: worldState.lord.y,
    lordCharacterLocationType: character?.locationType ?? worldState.lord.locationType,
  };
}

/** 当主是否在城内（Stronghold）。 */
export function isLordInStronghold(worldState: StrategyWorldState): boolean {
  return worldState.lord.locationType === "Stronghold";
}

/** 当主是否在地图上独立行动。 */
export function isLordOnMap(worldState: StrategyWorldState): boolean {
  return worldState.lord.locationType === "Map";
}

/** 当主是否位于指定据点内。 */
export function isLordInStrongholdId(worldState: StrategyWorldState, strongholdId: number): boolean {
  if (!isLordInStronghold(worldState)) return false;
  const characterId = resolvePlayerLordCharacterId(worldState);
  if (characterId == null) return false;
  const character = worldState.characters?.find((c) => c.id === characterId);
  return character?.locationType === "Stronghold" && character.strongholdId === strongholdId;
}

/** 当主是否与格点重合。 */
export function isLordAtCell(worldState: StrategyWorldState, x: number, y: number): boolean {
  return worldState.lord.x === x && worldState.lord.y === y;
}

/** 据点是否正被围攻（强攻或包围）。 */
export function isStrongholdBesieged(
  stronghold: StrategyStrongholdState | null | undefined,
): boolean {
  if (!stronghold?.siegeThreat) return false;
  return stronghold.siegeThreat === "Encircle" || stronghold.siegeThreat === "Assault";
}

/** 默认出入城 AP 消耗（与后端 EnterStrongholdAp 对齐）。 */
export const CHARACTER_GATE_AP_COST = 1;

/** 格点上的玩家当主地图角色（出城后）。 */
export function playerLordMapCharacterAtCell(
  worldState: StrategyWorldState,
  x: number,
  y: number,
) {
  const characterId = resolvePlayerLordCharacterId(worldState);
  if (characterId == null || !isLordOnMap(worldState)) return null;
  return (
    worldState.mapCharacters?.find(
      (c) => c.isPlayerControlled && c.id === characterId && c.x === x && c.y === y,
    ) ?? null
  );
}

/** 当主所在格上的据点。 */
export function strongholdAtLordCell(
  worldState: StrategyWorldState,
): StrategyStrongholdState | null {
  return (
    worldState.strongholds.find(
      (s) => s.x === worldState.lord.x && s.y === worldState.lord.y,
    ) ?? null
  );
}

/** 本家势力据点（非内藩势力自身据点）。 */
export function isPlayerRealmStronghold(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState,
): boolean {
  if (stronghold.forceId === worldState.playerForceId) {
    const force = worldState.forces.find((f) => f.id === worldState.playerForceId);
    return force?.status !== "InnerVassal";
  }

  const owner = worldState.forces.find((f) => f.id === stronghold.forceId);
  return (
    owner?.status === "InnerVassal"
    && owner.suzerainForceId === worldState.playerForceId
  );
}

/** 当主是否位于指定据点（在城 / 地图同格 / 领兵同格）。 */
export function isLordPresentAtStronghold(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState,
): boolean {
  if (isLordInStrongholdId(worldState, stronghold.id)) return true;
  return isLordAtCell(worldState, stronghold.x, stronghold.y);
}

/** 内藩当主居城（宗主为本家）。 */
export function isInnerVassalLordResidenceStronghold(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState,
): boolean {
  const owner = worldState.forces.find((f) => f.id === stronghold.forceId);
  if (!owner || owner.status !== "InnerVassal") return false;
  if (owner.suzerainForceId !== worldState.playerForceId) return false;
  return owner.lordResidenceStrongholdId === stronghold.id;
}

/** 旗下内藩据点（非本家 forceId，宗主为本家）。 */
export function isInnerVassalRealmStronghold(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState | null | undefined,
): boolean {
  if (!stronghold || stronghold.forceId === worldState.playerForceId) return false;
  return isPlayerRealmStronghold(worldState, stronghold);
}

/** 本家可下达指令的兵队（本家 + 旗下内藩，不含敌方）。 */
export function isPlayerRealmUnit(
  worldState: StrategyWorldState,
  unitId: number,
): boolean {
  const mapUnit = worldState.units.find((u) => u.id === unitId);
  if (mapUnit) {
    return isPlayerRealmForce(mapUnit.forceId, worldState.playerForceId, worldState.forces);
  }
  const roster = worldState.ownUnitRoster?.find((u) => u.id === unitId);
  if (roster) {
    return isPlayerRealmForce(roster.forceId, worldState.playerForceId, worldState.forces);
  }
  return false;
}

/** 当主可直接移动/攻城的兵队（本家直属，不含内藩等间接统属）。 */
export function resolvePlayerControlMode(worldState: StrategyWorldState): string {
  return GameStartOptionsProfile.fromWorldState(worldState).control.mode;
}

/** 当主是否领兵或与部队同格（仅角色控制模式下可直控）。 */
export function isLordLeadingOrWithUnit(
  worldState: StrategyWorldState,
  unit: StrategyUnitState,
): boolean {
  const ledUnitId = resolveLordLedUnitId(worldState);
  if (ledUnitId != null && ledUnitId === unit.id) return true;

  const lordCharacterId = resolvePlayerLordCharacterId(worldState);
  if (lordCharacterId != null && unit.commanderId === lordCharacterId) return true;

  return worldState.lord.x === unit.x && worldState.lord.y === unit.y;
}

export function isLordDirectlyControlledUnit(
  worldState: StrategyWorldState,
  unit: StrategyUnitState | null | undefined,
): boolean {
  if (!unit) return false;
  if (unit.forceId !== worldState.playerForceId) return false;

  return GameStartOptionsProfile.fromWorldState(worldState).allowsDirectUnitControl(
    unit,
    worldState.playerForceId,
    buildLordUnitControlContext(worldState),
  );
}

/** 外政据点是否显示「方针」按钮：已任命领主的外城（不含内藩）。 */
export function canShowStrongholdDirectiveButton(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState | null | undefined,
): boolean {
  if (!stronghold || !isPlayerRealmStronghold(worldState, stronghold)) return false;
  if (isInnerVassalRealmStronghold(worldState, stronghold)) return false;
  return !stronghold.isDirectRule && stronghold.lordId > 0;
}

/** 当主可否对指定据点下达据点级指令（驻居城可遥控本家全境，否则须亲赴该据点格）。 */
export function canLordCommandStronghold(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState | null | undefined,
): boolean {
  if (!stronghold || !isPlayerRealmStronghold(worldState, stronghold)) return false;
  if (isLordAtResidence(worldState)) return true;
  return isLordPresentAtStronghold(worldState, stronghold);
}

/** 角色是否驻留于指定据点城内。 */
export function resolveCharacterStronghold(
  worldState: StrategyWorldState,
  characterId: number,
): StrategyStrongholdState | null {
  const character = worldState.characters?.find((c) => c.id === characterId);
  if (!character || character.locationType !== "Stronghold" || !character.strongholdId) return null;
  return worldState.strongholds.find((s) => s.id === character.strongholdId) ?? null;
}

/** 角色是否为该城领主或代官。 */
export function isCharacterStrongholdOfficial(
  characterId: number,
  stronghold: StrategyStrongholdState,
): boolean {
  return characterId === stronghold.lordId || characterId === (stronghold.mayorId ?? 0);
}

/** 领主/代官/当主在城内时可亲自执行个人军事/内政指令。 */
export function canExecutePersonalStrongholdCommands(
  worldState: StrategyWorldState,
  characterId: number | null | undefined,
): boolean {
  if (!worldState || characterId == null || characterId <= 0) return false;
  const stronghold = resolveCharacterStronghold(worldState, characterId);
  if (!stronghold || !isPlayerRealmStronghold(worldState, stronghold)) return false;

  const playerLordId = resolvePlayerLordCharacterId(worldState);
  if (characterId === playerLordId) return true;
  return isCharacterStrongholdOfficial(characterId, stronghold);
}

/** 据点任命领主或代官是否驻留于该城城内。 */
export function isStrongholdOfficialPresentAtStronghold(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState,
): boolean {
  const officialIds = [stronghold.lordId, stronghold.mayorId ?? 0].filter((id) => id > 0);
  for (const id of officialIds) {
    const character = worldState.characters?.find((c) => c.id === id);
    if (
      character
      && !character.isDead
      && character.locationType === "Stronghold"
      && character.strongholdId === stronghold.id
    ) {
      return true;
    }
  }
  return false;
}

/** 同格据点是否可入城（本家或空城等由后端校验；前端仅判存在且当主在地图）。 */
export function canEnterStrongholdAtCell(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState | null | undefined,
): boolean {
  if (!isLordOnMap(worldState)) return false;
  const target = stronghold ?? strongholdAtLordCell(worldState);
  if (!target) return false;
  return isLordAtCell(worldState, target.x, target.y);
}

/** 角色出入城指令所针对的据点。 */
export function resolveCharacterGateStronghold(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState | null | undefined,
): StrategyStrongholdState | null {
  return stronghold ?? strongholdAtLordCell(worldState);
}

/** 城内除当主外的其它角色数量（拜访占位用）。 */
export function countOtherCharactersInStronghold(
  worldState: StrategyWorldState,
  strongholdId: number,
): number {
  const playerId = resolvePlayerLordCharacterId(worldState);
  return (worldState.characters ?? []).filter(
    (c) =>
      !c.isDead
      && c.locationType === "Stronghold"
      && c.strongholdId === strongholdId
      && c.id !== playerId,
  ).length;
}

/** 当主在敌方据点上是否可用角色谍报（不在据点菜单显示）。 */
export function canCharacterEspionageAtCell(
  worldState: StrategyWorldState,
  stronghold: StrategyStrongholdState | null | undefined,
): boolean {
  if (!stronghold || !isLordOnMap(worldState)) return false;
  if (stronghold.forceId === worldState.playerForceId) return false;
  if (!isLordAtCell(worldState, stronghold.x, stronghold.y)) return false;
  return true;
}
