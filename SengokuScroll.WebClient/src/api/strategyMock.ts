import type {
  StrategyAdvanceDayResponse,
  StrategyBattlePreview,
  StrategyCharacterSummaryState,
  StrategyEvent,
  StrategyInstantBattleResponse,
  StrategyPathPreview,
  StrategyStrongholdCityActorState,
  StrategyStrongholdState,
  StrategyWorldState,
  MapPoint,
} from "./strategyTypes";
import { buildManhattanPath, concatPathSegments } from "@/utils/strategyPathUtils";
import { GameStartOptionsProfile } from "@/gameStartOptions/GameStartOptionsProfile";
import { enrichStrongholdDerivedFields } from "@/utils/strategyStrongholdDerivedFields";
import {
  buildDefaultMockCharacterIntel,
  enrichMockCharacterIntel,
  generateMockPaperDollName,
} from "@/api/mockIntelCharacterExtras";

const MOCK_ORG_FORCE_IDS = {
  mitsui: 10_001,
  imai: 10_002,
  nanban: 10_003,
  shoganji: 10_004,
} as const;

const MOCK_MERCHANT_HOUSES = ["三井屋", "今井屋", "津田屋", "住友屋", "鸿池屋"] as const;

function resolveMockMerchantHouseName(strongholdId: number): (typeof MOCK_MERCHANT_HOUSES)[number] {
  return MOCK_MERCHANT_HOUSES[Math.max(0, strongholdId - 1) % MOCK_MERCHANT_HOUSES.length];
}

function resolveMockMerchantOrgId(houseName: string): number {
  switch (houseName) {
    case "三井屋":
      return MOCK_ORG_FORCE_IDS.mitsui;
    case "今井屋":
      return MOCK_ORG_FORCE_IDS.imai;
    case "南蛮商会":
      return MOCK_ORG_FORCE_IDS.nanban;
    case "证愿寺":
      return MOCK_ORG_FORCE_IDS.shoganji;
    default:
      return 10_100 + (Math.abs(houseName.charCodeAt(0) * 31 + houseName.length) % 8_900);
  }
}

function resolveMockMerchantLeader(houseName: string): string {
  switch (houseName) {
    case "三井屋":
      return "三井高利";
    case "今井屋":
      return "今井宗久";
    case "津田屋":
      return "津田算长";
    case "住友屋":
      return "住友吉次";
    case "鸿池屋":
      return "鸿池新七";
    case "南蛮商会":
      return "南蛮商人";
    default:
      return `${houseName}当主`;
  }
}

function resolveMockShopMoney(strongholdId: number, houseName: string): number {
  if (houseName === "南蛮商会") return 35_000;
  return 18_000 + Math.max(0, strongholdId) * 2_500;
}

function buildMockOrganizationForces(
  strongholds: StrategyStrongholdState[],
): StrategyWorldState["forces"] {
  const shopCounts = new Map<number, number>();
  const characterCounts = new Map<number, number>();
  const treasury = new Map<number, { money: number; food: number }>();
  const residenceByOrg = new Map<number, number>();

  for (const stronghold of strongholds) {
    for (const actor of stronghold.cityActors ?? []) {
      const orgId = actor.forceId;
      if (!orgId || orgId <= 0 || (actor.kind !== "Merchant" && actor.kind !== "Religion")) {
        continue;
      }
      shopCounts.set(orgId, (shopCounts.get(orgId) ?? 0) + 1);
      characterCounts.set(
        orgId,
        (characterCounts.get(orgId) ?? 0) + Math.max(actor.characterCount, actor.characterIds?.length ?? 0)
      );
      const current = treasury.get(orgId) ?? { money: 0, food: 0 };
      treasury.set(orgId, {
        money: current.money + actor.money,
        food: current.food + actor.food,
      });
      if (!residenceByOrg.has(orgId) && (actor.branchLabel === "本店" || actor.branchLabel === "本院")) {
        residenceByOrg.set(orgId, stronghold.id);
      }
    }
  }

  const orgNames = new Map<number, string>();
  for (const stronghold of strongholds) {
    for (const actor of stronghold.cityActors ?? []) {
      const orgId = actor.forceId;
      if (!orgId || orgId <= 0) continue;
      if (actor.kind !== "Merchant" && actor.kind !== "Religion") continue;
      if (!orgNames.has(orgId)) orgNames.set(orgId, actor.name);
    }
  }

  return [...orgNames.entries()]
    .map(([id, name]) => {
      const totals = treasury.get(id) ?? { money: 0, food: 0 };
      return {
        id,
        name,
        food: totals.food,
        money: totals.money,
        status: "Independence",
        strongholdCount: shopCounts.get(id) ?? 0,
        characterCount: characterCounts.get(id) ?? 0,
        prestige: 0,
        orthodoxy: 0,
        lordResidenceStrongholdId: residenceByOrg.get(id) ?? strongholds[0]?.id ?? 1,
        category: actorKindFromOrgName(name),
      };
    })
    .filter((force) => (shopCounts.get(force.id) ?? 0) > 0);
}

function actorKindFromOrgName(name: string): "Merchant" | "Religion" {
  if (name.includes("寺") || name.includes("社") || name.includes("宫") || name.includes("神社")) {
    return "Religion";
  }
  return "Merchant";
}

function resolveMockTempleName(name: string): string {
  switch (name) {
    case "清洲":
      return "热田神宫";
    case "小田原":
      return "早云寺";
    case "冈崎":
      return "八幡宫";
    case "骏府":
      return "久能山浅间神社";
    default:
      return `${name}寺`;
  }
}

function buildMockCityActors(
  id: number,
  name: string,
  population: number,
  hostForceId: number,
): StrategyStrongholdCityActorState[] {
  const commerceValue = Math.max(1000, population * 2);
  const wildIds = id === 1 ? [90_001, 90_002, 90_003] : [];
  const actors: StrategyStrongholdCityActorState[] = [
    {
      id: id * 1000 + 1,
      name: `${name}官府`,
      kind: "Government",
      forceId: hostForceId,
      money: 0,
      food: 0,
      horse: 0,
      commerceProduction: 0,
      agricultureProduction: 0,
      characterCount: 0,
      characterIds: [],
    },
    {
      id: id * 1000 + 2,
      name: "民间",
      kind: "Civilian",
      money: 0,
      food: 0,
      horse: 0,
      commerceProduction: 0,
      agricultureProduction: 0,
      characterCount: wildIds.length,
      characterIds: wildIds,
    },
  ];

  if (commerceValue >= 20) {
    const houseName = id === 1 ? "三井屋" : resolveMockMerchantHouseName(id);
    const orgId = resolveMockMerchantOrgId(houseName);
    const leaderId = 90_000 + id * 100 + 7;
    const characterIds =
      id === 1 && houseName === "三井屋" ? [90_011, 90_014] : [leaderId];
    actors.push({
      id: id * 1000 + 7,
      name: houseName,
      kind: "Merchant",
      forceId: orgId,
      money: resolveMockShopMoney(id, houseName),
      food: 2_400_000,
      horse: 100,
      commerceProduction: Math.max(500, Math.floor(commerceValue / 40)),
      agricultureProduction: 0,
      characterCount: characterIds.length,
      characterIds,
      leaderName: resolveMockMerchantLeader(houseName),
      branchLabel: "本店",
    });
  }

  if (population >= 40_000) {
    const templeName = resolveMockTempleName(name);
    const templeOrgId = resolveMockMerchantOrgId(templeName);
    const isDemoTemple = id === 1;
    const priestId = 90_000 + id * 100 + 8;
    const priestName = isDemoTemple ? "大祝官" : generateMockPaperDollName(priestId, templeName);
    actors.push({
      id: id * 1000 + 8,
      name: templeName,
      kind: "Religion",
      forceId: templeOrgId,
      money: 12_000,
      food: 1_200_000,
      horse: 20,
      commerceProduction: 0,
      agricultureProduction: isDemoTemple ? 240_000 : Math.max(120_000, population * 3),
      characterCount: isDemoTemple ? 2 : 1,
      characterIds: isDemoTemple ? [90_020, 90_021] : [priestId],
      leaderName: priestName,
      branchLabel: "本院",
    });
  }

  if (commerceValue >= 80_000) {
    const branchLeaderId = id === 1 ? 90_030 : 90_000 + id * 100 + 9;
    actors.push({
      id: id * 1000 + 9,
      name: "南蛮商会",
      kind: "Merchant",
      forceId: MOCK_ORG_FORCE_IDS.nanban,
      money: resolveMockShopMoney(id, "南蛮商会"),
      food: 1_200_000,
      horse: 300,
      commerceProduction: Math.max(800, Math.floor(commerceValue / 40)),
      agricultureProduction: 0,
      characterCount: 1,
      characterIds: [branchLeaderId],
      leaderName: id === 1 ? "柏来图" : resolveMockMerchantLeader("南蛮商会"),
      branchLabel: "分店",
    });
  }

  if (id === 1) {
    actors.push({
      id: id * 1000 + 71,
      name: "今井屋",
      kind: "Merchant",
      forceId: MOCK_ORG_FORCE_IDS.imai,
      money: 28_000,
      food: 1_200_000,
      horse: 180,
      commerceProduction: 640,
      agricultureProduction: 0,
      characterCount: 2,
      characterIds: [90_012, 90_013],
      leaderName: "今井宗久",
      branchLabel: "分店",
    });
    actors.push({
      id: id * 1000 + 88,
      name: "证愿寺",
      kind: "Religion",
      forceId: MOCK_ORG_FORCE_IDS.shoganji,
      money: 8_000,
      food: 600_000,
      horse: 12,
      commerceProduction: 0,
      agricultureProduction: 120_000,
      characterCount: 1,
      characterIds: [90_022],
      leaderName: "证愿寺住持",
      branchLabel: "分院",
    });
  }

  return actors.map((actor) => {
    const suffix = actor.id % 1000;
    const leaderByCharacterId: Record<number, string> =
      id === 1
        ? {
            90_011: "三井高利",
            90_014: "三井与一",
            90_012: "今井宗久",
            90_013: "津田作左卫门",
            90_020: "大祝官",
            90_021: "神官",
            90_022: "证愿寺住持",
            90_030: "柏来图",
          }
        : {};
    const leaderFromStaff = actor.characterIds
      ?.map((characterId) => leaderByCharacterId[characterId])
      .find((value) => value);
    const leaderName =
      actor.leaderName ?? leaderFromStaff ?? (actor.kind === "Government" ? "—" : "—");
    const branchLabel =
      actor.branchLabel ??
      (actor.kind === "Merchant"
        ? suffix === 7
          ? "本店"
          : suffix === 9 || suffix === 71
            ? "分店"
            : "—"
        : actor.kind === "Religion"
          ? suffix === 8
            ? "本院"
            : suffix === 88
              ? "分院"
              : "—"
          : "—");
    return { ...actor, leaderName, branchLabel };
  });
}

function buildMockCityActorCharacters(): StrategyCharacterSummaryState[] {
  return [
    enrichMockCharacterIntel({ id: 90_001, forceId: 0, name: "佐藤源平", strongholdId: 1, locationType: "Stronghold", forceStatus: "Idle" }),
    enrichMockCharacterIntel({ id: 90_002, forceId: 0, name: "和田义盛", strongholdId: 1, locationType: "Stronghold", forceStatus: "Idle" }),
    enrichMockCharacterIntel({ id: 90_003, forceId: 0, name: "山本勘助", strongholdId: 1, locationType: "Stronghold", forceStatus: "Idle" }),
    enrichMockCharacterIntel({ id: 90_011, forceId: MOCK_ORG_FORCE_IDS.mitsui, name: "三井高利", strongholdId: 1, locationType: "Stronghold", forceStatus: "Idle" }),
    enrichMockCharacterIntel({ id: 90_014, forceId: MOCK_ORG_FORCE_IDS.mitsui, name: "三井与一", strongholdId: 1, locationType: "Stronghold", forceStatus: "Idle", leaderId: 90_011 }),
    enrichMockCharacterIntel({ id: 90_012, forceId: MOCK_ORG_FORCE_IDS.imai, name: "今井宗久", strongholdId: 1, locationType: "Stronghold", forceStatus: "Idle" }),
    enrichMockCharacterIntel({ id: 90_013, forceId: MOCK_ORG_FORCE_IDS.imai, name: "津田作左卫门", strongholdId: 1, locationType: "Stronghold", forceStatus: "Idle" }),
    enrichMockCharacterIntel({ id: 90_020, forceId: resolveMockMerchantOrgId("热田神宫"), name: "大祝官", strongholdId: 1, locationType: "Stronghold", forceStatus: "Idle", religionName: "神道教" }),
    enrichMockCharacterIntel({ id: 90_021, forceId: resolveMockMerchantOrgId("热田神宫"), name: "神官", strongholdId: 1, locationType: "Stronghold", forceStatus: "Idle", religionName: "神道教" }),
    enrichMockCharacterIntel({ id: 90_022, forceId: MOCK_ORG_FORCE_IDS.shoganji, name: "证愿寺住持", strongholdId: 1, locationType: "Stronghold", forceStatus: "Idle", religionName: "佛教" }),
    enrichMockCharacterIntel({ id: 90_030, forceId: MOCK_ORG_FORCE_IDS.nanban, name: "柏来图", strongholdId: 1, locationType: "Stronghold", forceStatus: "Idle", religionName: "基督教" }),
  ];
}

function buildMockMilitaryCharacters(): StrategyCharacterSummaryState[] {
  return [
    enrichMockCharacterIntel({ id: 1, forceId: 1, name: "织田信长", strongholdId: 1, locationType: "Stronghold", forceStatus: "Idle", age: 42 }),
    enrichMockCharacterIntel({ id: 2, forceId: 1, name: "柴田胜家", strongholdId: 1, locationType: "Stronghold", forceStatus: "Idle", age: 38 }),
    enrichMockCharacterIntel({ id: 4, forceId: 1, name: "林秀贞", strongholdId: 1, locationType: "Stronghold", forceStatus: "Idle", age: 45 }),
    enrichMockCharacterIntel({ id: 6, forceId: 3, name: "酒井忠次", strongholdId: 2, locationType: "Stronghold", forceStatus: "Idle", age: 40 }),
  ];
}

function buildMockCharactersFromWorld(
  strongholds: StrategyStrongholdState[],
): StrategyCharacterSummaryState[] {
  const rows = [...buildMockCityActorCharacters(), ...buildMockMilitaryCharacters()];
  const existing = new Set(rows.map((row) => row.id));

  for (const stronghold of strongholds) {
    for (const actor of stronghold.cityActors ?? []) {
      const characterIds = actor.characterIds ?? [];
      if (characterIds.length === 0) continue;
      for (const characterId of characterIds) {
        if (characterId <= 0 || existing.has(characterId)) continue;
        const seedLabel = actor.name ?? `人物#${characterId}`;
        const paperName = generateMockPaperDollName(characterId, seedLabel);
        const extras = buildDefaultMockCharacterIntel(characterId, seedLabel);
        rows.push(
          enrichMockCharacterIntel({
            id: characterId,
            forceId: actor.forceId ?? 0,
            name:
              actor.leaderName && actor.leaderName !== "—" && actor.characterIds?.[0] === characterId
                ? actor.leaderName
                : paperName,
            strongholdId: stronghold.id,
            locationType: "Stronghold",
            forceStatus: "Idle",
            religionName: actor.kind === "Religion" ? "神道教" : undefined,
            ...extras,
          })
        );
        existing.add(characterId);
      }
    }
  }

  return rows;
}

function enrichMockStrongholds(
  items: (Partial<StrategyStrongholdState> &
    Pick<StrategyStrongholdState, "id" | "name" | "forceId" | "x" | "y">)[],
  residenceName: string | null
): StrategyStrongholdState[] {
  return items.map((s) => {
    const lordId = s.lordId ?? 0;
    const population = s.population ?? 0;
    const merged = {
      typeId: 1,
      typeName: "平城",
      pollTaxRate: 10,
      agricultureTaxRate: 25,
      commerceTaxRate: 12,
      tariffTaxRate: 8,
      isHistorical: true,
      defense: 25,
      defenseFacilities: [],
      stability: 50,
      popularFeelings: 50,
      mayorName: null,
      morale: 80,
      training: 65,
      cultureName: "日本",
      religionName: "神道教",
      governancePriority: "Autonomous",
      money: 0,
      food: 0,
      population,
      garrisonSoldiers: 800,
      lordName: "当主",
      ...s,
      cityActors: (() => {
        const actors = s.cityActors ?? buildMockCityActors(s.id, s.name, population, s.forceId);
        const lordName = s.lordName ?? "当主";
        return actors.map((actor) =>
          actor.kind === "Government" ? { ...actor, leaderName: lordName } : actor
        );
      })(),
      lordId,
      isLordResidence: s.isLordResidence ?? s.name === residenceName,
      isDirectRule: s.isDirectRule ?? lordId === 0,
    } satisfies StrategyStrongholdState;

    return enrichStrongholdDerivedFields(merged);
  });
}

/** mini_kanto 初始状态（与后端 JSON 对齐，供 Mock 使用）。 */
export function createMiniKantoState(): StrategyWorldState {
  const residenceName = "清洲";
  const militaryForces: StrategyWorldState["forces"] = [
      {
        id: 1,
        name: "织田家",
        food: 80000000,
        money: 8000000,
        status: "Independence",
        strongholdCount: 5,
        characterCount: 4,
        prestige: 72,
        orthodoxy: 65,
        lordResidenceStrongholdId: 1,
        category: "Military",
      },
      {
        id: 3,
        name: "酒井家",
        food: 20000000,
        money: 2000000,
        status: "InnerVassal",
        suzerainForceId: 1,
        strongholdCount: 1,
        characterCount: 1,
        prestige: 58,
        orthodoxy: 62,
        lordResidenceStrongholdId: 2,
        category: "Military",
      },
      {
        id: 2,
        name: "今川家",
        food: 70000000,
        money: 6000000,
        status: "Independence",
        strongholdCount: 1,
        characterCount: 2,
        prestige: 58,
        orthodoxy: 70,
        lordResidenceStrongholdId: 6,
        category: "Military",
      },
      {
        id: 5,
        name: "里见家",
        food: 12000000,
        money: 1200000,
        status: "Independence",
        strongholdCount: 1,
        characterCount: 1,
        prestige: 42,
        orthodoxy: 55,
        lordResidenceStrongholdId: 11,
        category: "Military",
      },
      {
        id: 6,
        name: "德川家",
        food: 18000000,
        money: 1800000,
        status: "Independence",
        strongholdCount: 1,
        characterCount: 1,
        prestige: 64,
        orthodoxy: 68,
        lordResidenceStrongholdId: 12,
        category: "Military",
      },
  ];
  const strongholds = enrichMockStrongholds(
      [
      {
        id: 1,
        name: "清洲",
        forceId: 1,
        x: 2,
        y: 8,
        food: 90000000,
        population: 48000,
        pollTaxRate: 12,
        agricultureTaxRate: 28,
        commerceTaxRate: 15,
        tariffTaxRate: 10,
        lordId: 0,
        isDirectRule: true,
        isLordResidence: true,
        lordName: "织田信长",
        mayorName: "林秀贞",
        stability: 72,
        popularFeelings: 68,
        morale: 85,
        training: 68,
        cultureName: "日本",
        religionName: "神道教",
        money: 18000000,
        garrisonSoldiers: 1500,
      },
      {
        id: 2,
        name: "犬山",
        forceId: 3,
        x: 4,
        y: 4,
        lordId: 6,
        food: 72000000,
        population: 36000,
        garrisonSoldiers: 1200,
        lordName: "酒井忠次",
        mayorName: "酒井忠次",
        morale: 78,
        training: 62,
        cultureName: "日本",
        religionName: "神道教",
        money: 9000000,
      },
      {
        id: 3,
        name: "冈崎",
        forceId: 1,
        x: 2,
        y: 14,
        food: 60000000,
        population: 42000,
        morale: 80,
        training: 65,
        cultureName: "日本",
        religionName: "神道教",
        money: 7200000,
        garrisonSoldiers: 1000,
      },
      {
        id: 4,
        name: "小田原",
        forceId: 2,
        x: 16,
        y: 10,
        food: 108000000,
        population: 72000,
        garrisonSoldiers: 1800,
        pollTaxRate: 11,
        agricultureTaxRate: 26,
        commerceTaxRate: 14,
        tariffTaxRate: 9,
        lordId: 5,
        lordName: "北条氏康",
        morale: 82,
        training: 70,
        cultureName: "日本",
        religionName: "神道教",
        money: 24000000,
      },
      {
        id: 5,
        name: "骏府",
        forceId: 4,
        x: 14,
        y: 6,
        lordId: 7,
        lordName: "北条纲成",
        food: 84000000,
        population: 54000,
        morale: 79,
        training: 64,
        cultureName: "日本",
        religionName: "神道教",
        money: 15000000,
        garrisonSoldiers: 1200,
      },
      {
        id: 6,
        name: "挂川",
        forceId: 2,
        x: 12,
        y: 14,
        food: 66000000,
        population: 39000,
        garrisonSoldiers: 1400,
        morale: 76,
        training: 60,
        cultureName: "日本",
        religionName: "神道教",
        money: 10800000,
      },
      {
        id: 7,
        name: "三河凑",
        forceId: 1,
        x: 6,
        y: 12,
        food: 48000000,
        population: 24000,
        morale: 74,
        training: 58,
        cultureName: "日本",
        religionName: "神道教",
        money: 4800000,
        garrisonSoldiers: 800,
      },
      {
        id: 8,
        name: "伊豆港",
        forceId: 2,
        x: 16,
        y: 16,
        food: 60000000,
        population: 30000,
        garrisonSoldiers: 900,
        morale: 75,
        training: 55,
        cultureName: "日本",
        religionName: "神道教",
        money: 5400000,
      },
      {
        id: 9,
        name: "足助",
        forceId: 1,
        x: 8,
        y: 2,
        food: 42000000,
        population: 21000,
        morale: 72,
        training: 56,
        cultureName: "日本",
        religionName: "神道教",
        money: 3600000,
        garrisonSoldiers: 700,
      },
      {
        id: 10,
        name: "沼津",
        forceId: 4,
        x: 10,
        y: 16,
        food: 36000000,
        population: 18000,
        garrisonSoldiers: 800,
        morale: 70,
        training: 54,
        cultureName: "日本",
        religionName: "神道教",
        money: 3000000,
      },
      {
        id: 11,
        name: "馆山",
        forceId: 5,
        x: 18,
        y: 0,
        lordId: 8,
        lordName: "里见义弘",
        food: 48000000,
        population: 27000,
        morale: 73,
        training: 58,
        cultureName: "日本",
        religionName: "神道教",
        money: 4200000,
        garrisonSoldiers: 900,
      },
      {
        id: 12,
        name: "滨松",
        forceId: 6,
        x: 12,
        y: 2,
        lordId: 9,
        lordName: "德川家康",
        food: 54000000,
        population: 33000,
        garrisonSoldiers: 1100,
        morale: 77,
        training: 63,
        cultureName: "日本",
        religionName: "神道教",
        money: 6000000,
      },
    ],
      residenceName
    );
  const organizationForces = buildMockOrganizationForces(strongholds);
  return {
    scenarioId: "mini_kanto",
    playerForceId: 1,
    lord: { name: "织田信长", unitId: null, x: 2, y: 8, residenceStrongholdName: residenceName },
    map: {
      name: "迷你关东试玩",
      width: 20,
      height: 20,
    },
    date: { year: 1560, month: 1, day: 1 },
    forces: [...militaryForces, ...organizationForces],
    diplomacies: [
      { targetForceId: 2, relation: "Enemy" },
      { targetForceId: 6, relation: "Allied" },
    ],
    wars: [
      {
        id: 1,
        aggressorForceId: 1,
        defenderForceId: 2,
        aggressorForceIds: [1],
        defenderForceIds: [2],
        playerWarScore: 0,
        startYear: 1560,
        startMonth: 1,
        startDay: 1,
        recentScoreEvents: [],
      },
    ],
    strongholds,
    characters: buildMockCharactersFromWorld(strongholds),
    units: [
      {
        id: 1,
        name: "织田先锋",
        forceId: 1,
        x: 8,
        y: 8,
        soldiers: 3000,
        food: 6000000,
        ap: 10,
        movement: 10,
        status: "Waiting",
        directive: "Move",
        stance: "Normal",
        siegeMode: "None",
        directiveTargetId: 0,
        targetUnitId: 0,
        route: [],
        commanderName: "柴田胜家",
        commanderId: 2,
        morale: 82,
        training: 72,
        cultureName: "日本",
        religionName: "神道教",
        money: 3000000,
        composition: [
          { id: 1, typeId: 1, typeName: "足轻", soldiers: 1890, ratioPercent: 63 },
          { id: 2, typeId: 2, typeName: "弓兵", soldiers: 480, ratioPercent: 16 },
          { id: 3, typeId: 3, typeName: "骑兵", soldiers: 360, ratioPercent: 12 },
          { id: 4, typeId: 4, typeName: "铁炮", soldiers: 270, ratioPercent: 9 },
        ],
        supplyStatus: "Sufficient",
        foodDaysRemaining: 10,
        inTransitSupplies: [],
      },
      {
        id: 2,
        name: "今川先锋",
        forceId: 2,
        x: 16,
        y: 12,
        soldiers: 2400,
        food: 4800000,
        ap: 10,
        movement: 10,
        status: "Waiting",
        directive: "Move",
        stance: "Normal",
        siegeMode: "None",
        directiveTargetId: 0,
        targetUnitId: 0,
        route: [],
        commanderName: "今川氏真",
        commanderId: 3,
        morale: 78,
        training: 68,
        cultureName: "日本",
        religionName: "神道教",
        money: 1800000,
        composition: [
          { id: 5, typeId: 1, typeName: "足轻", soldiers: 1500, ratioPercent: 63 },
          { id: 6, typeId: 2, typeName: "弓兵", soldiers: 360, ratioPercent: 15 },
          { id: 7, typeId: 3, typeName: "骑兵", soldiers: 300, ratioPercent: 13 },
          { id: 8, typeId: 4, typeName: "铁炮", soldiers: 240, ratioPercent: 10 },
        ],
        supplyStatus: "Sufficient",
        foodDaysRemaining: 8,
        inTransitSupplies: [],
      },
    ],
    ownUnitRoster: [],
    supplyConvoys: [],
    messageCarriers: [],
  };
}

const moveTargets = new Map<number, { x: number; y: number }>();
const pendingAttacks = new Map<number, { x: number; y: number }>();

let mockState = createMiniKantoState();

export function resetMockState(): StrategyWorldState {
  mockState = createMiniKantoState();
  moveTargets.clear();
  pendingAttacks.clear();
  return cloneState(mockState);
}

export function mockLoadScenario(_scenarioId: string): StrategyWorldState {
  return resetMockState();
}

export function mockGetState(): StrategyWorldState {
  return cloneState(mockState);
}

export function mockPreviewUnitPath(
  unitId: number,
  x: number,
  y: number,
  options?: { from?: MapPoint; via?: MapPoint[] }
): StrategyPathPreview {
  const unit = mockState.units.find((u) => u.id === unitId);
  if (!unit) throw new Error(`UnitNotFound:${unitId}`);

  const start = options?.from ?? { x: unit.x, y: unit.y };
  const stops = [...(options?.via ?? []), { x, y }];
  if (stops.length > 0 && stops[0]!.x === start.x && stops[0]!.y === start.y) {
    stops.shift();
  }
  let from = start;
  const segments: MapPoint[][] = [];
  for (const stop of stops) {
    segments.push(buildManhattanPath(from.x, from.y, stop.x, stop.y));
    from = stop;
  }
  return { points: concatPathSegments(segments) };
}

export function mockMoveUnit(
  unitId: number,
  x: number,
  y: number,
  via?: MapPoint[]
): StrategyWorldState {
  const unit = mockState.units.find((u) => u.id === unitId);
  if (!unit) throw new Error(`UnitNotFound:${unitId}`);

  const preview = mockPreviewUnitPath(unitId, x, y, { via });
  moveTargets.set(unitId, { x, y });
  unit.status = "Moving";
  unit.route = preview.points;
  return cloneState(mockState);
}

function manhattanAdjacent(a: { x: number; y: number }, b: { x: number; y: number }) {
  return Math.abs(a.x - b.x) + Math.abs(a.y - b.y) === 1;
}

export function mockPreviewBattle(unitId: number, x: number, y: number): StrategyBattlePreview {
  const attacker = mockState.units.find((u) => u.id === unitId);
  if (!attacker) throw new Error(`UnitNotFound:${unitId}`);
  const defender = mockState.units.find((u) => u.x === x && u.y === y);
  if (!defender || defender.forceId === attacker.forceId) throw new Error("AttackTargetNotFound");
  if (!manhattanAdjacent(attacker, { x, y })) throw new Error("TargetLocationNotAdjacent");

  const atkPower = attacker.soldiers;
  const defPower = defender.soldiers;
  const winRate = Math.min(95, Math.max(5, Math.round((atkPower / (atkPower + defPower)) * 100)));

  return {
    attackerUnitId: attacker.id,
    defenderUnitId: defender.id,
    targetX: x,
    targetY: y,
    attackerWinRatePercent: winRate,
    attackerSoldiers: attacker.soldiers,
    defenderSoldiers: defender.soldiers,
    defenderName: defender.name,
    estimatedAttackerLossMin: Math.floor(attacker.soldiers * 0.1),
    estimatedAttackerLossMax: Math.floor(attacker.soldiers * 0.25),
    estimatedDefenderLossMin: Math.floor(defender.soldiers * 0.3),
    estimatedDefenderLossMax: Math.floor(defender.soldiers * 0.6),
    resolutionSeed: 1,
  };
}

export function mockExecuteInstantBattle(
  unitId: number,
  x: number,
  y: number
): StrategyInstantBattleResponse {
  const preview = mockPreviewBattle(unitId, x, y);
  const attacker = mockState.units.find((u) => u.id === unitId)!;
  const defender = mockState.units.find((u) => u.id === preview.defenderUnitId)!;

  const attackerWon = preview.attackerWinRatePercent >= 50;
  const attLoss = Math.floor(attacker.soldiers * (attackerWon ? 0.15 : 0.4));
  const defLoss = Math.floor(defender.soldiers * (attackerWon ? 0.45 : 0.15));

  attacker.soldiers = Math.max(0, attacker.soldiers - attLoss);
  defender.soldiers = Math.max(0, defender.soldiers - defLoss);
  attacker.ap = Math.max(0, attacker.ap - 5);
  attacker.route = [];

  const attBefore = preview.attackerSoldiers;
  const defBefore = preview.defenderSoldiers;

  return {
    state: cloneState(mockState),
    result: {
      attackerWon,
      attackerUnitId: unitId,
      defenderUnitId: preview.defenderUnitId,
      attackerForceId: attacker.forceId,
      defenderForceId: defender.forceId,
      attackerName: attacker.name,
      defenderName: defender.name,
      attackerSoldiersBefore: attBefore,
      defenderSoldiersBefore: defBefore,
      attackerCasualties: attLoss,
      defenderCasualties: defLoss,
      attackerSoldiersAfter: attacker.soldiers,
      defenderSoldiersAfter: defender.soldiers,
      attackerWinRatePercent: preview.attackerWinRatePercent,
      resolutionSeed: preview.resolutionSeed,
      resolutionRoll: preview.attackerWinRatePercent >= 50 ? 10 : 90,
      logEntries: [
        { order: 1, side: "system", phase: "接触", message: `${attacker.name} 与 ${defender.name} 在野外遭遇。` },
        { order: 2, side: "attacker", phase: "接敌", message: `${attacker.name} 发起进攻（${attBefore} 名）。` },
        { order: 3, side: "defender", phase: "接敌", message: `${defender.name} 列阵应战（${defBefore} 名）。` },
        {
          order: 4,
          side: "system",
          phase: "交锋",
          message: `战前评估：攻方胜率 ${preview.attackerWinRatePercent}%。`,
        },
        attackerWon
          ? { order: 5, side: "attacker", phase: "突破", message: `突破成功，己方 −${attLoss} → 剩余 ${attacker.soldiers}。` }
          : { order: 5, side: "attacker", phase: "受挫", message: `攻势受挫，己方 −${attLoss} → 剩余 ${attacker.soldiers}。` },
        attackerWon
          ? { order: 6, side: "defender", phase: "溃退", message: `敌军 −${defLoss} → 剩余 ${defender.soldiers}。` }
          : { order: 6, side: "defender", phase: "维持", message: `守军 −${defLoss} → 剩余 ${defender.soldiers}。` },
        {
          order: 7,
          side: "system",
          phase: "结束",
          message: attackerWon ? "攻方获胜，当日野战结束。" : "守方获胜，当日野战结束。",
        },
      ],
    },
  };
}

export function mockOrderUnitAttack(unitId: number, x: number, y: number): StrategyWorldState {
  mockPreviewBattle(unitId, x, y);
  pendingAttacks.set(unitId, { x, y });
  return cloneState(mockState);
}

export function mockAdvanceDay(): StrategyAdvanceDayResponse {
  const events: StrategyEvent[] = [];
  const instantEnabled = GameStartOptionsProfile.fromWorldState(mockState).shouldShowInstantEventSummary();

  for (const [unitId, target] of [...pendingAttacks.entries()]) {
    pendingAttacks.delete(unitId);
    const battle = mockExecuteInstantBattle(unitId, target.x, target.y);
    mockState = battle.state;
    const brief = `${battle.result.attackerName} vs ${battle.result.defenderName}`;
    if (instantEnabled) {
      events.push({
        category: "InstantEventSummary",
        message: `⚡ 前线急报（待证实）：${brief}`,
        brief,
      });
    }
    events.push({
      category: "BattleReportArrived",
      message: `📨 战报信使抵达：${brief}`,
      brief,
      battleResult: battle.result,
    });
  }

  const d = mockState.date;
  mockState = {
    ...mockState,
    date: advanceDate(d.year, d.month, d.day),
    units: mockState.units.map((u) => {
      const target = moveTargets.get(u.id);
      if (!target || u.status !== "Moving") return { ...u };

      let nx = u.x;
      let ny = u.y;
      if (u.x !== target.x) nx += Math.sign(target.x - u.x);
      else if (u.y !== target.y) ny += Math.sign(target.y - u.y);

      const arrived = nx === target.x && ny === target.y;
      if (arrived) moveTargets.delete(u.id);

      const route =
        u.route.length > 1
          ? u.route.slice(1)
          : arrived
            ? []
            : buildManhattanPath(nx, ny, target.x, target.y);

      return {
        ...u,
        x: nx,
        y: ny,
        ap: Math.max(0, u.ap - 2),
        status: arrived ? "Waiting" : "Moving",
        route: arrived ? [] : route.length > 0 ? route : buildManhattanPath(nx, ny, target.x, target.y),
      };
    }),
  };
  return { state: cloneState(mockState), resolvedBattles: [], events };
}

function advanceDate(year: number, month: number, day: number) {
  day += 1;
  if (day > 30) {
    day = 1;
    month += 1;
  }
  if (month > 12) {
    month = 1;
    year += 1;
  }
  return { year, month, day };
}

export function mockSetUnitDirective(
  unitId: number,
  directive: string
): import("./strategyTypes").StrategyPolicyChangeResponse {
  const unit = mockState.units.find((u) => u.id === unitId);
  if (!unit) throw new Error(`UnitNotFound:${unitId}`);

  const lord = mockState.lord;
  const issuerX = unit.forceId === mockState.playerForceId ? lord.x : unit.x;
  const issuerY = unit.forceId === mockState.playerForceId ? lord.y : unit.y;

  if (issuerX === unit.x && issuerY === unit.y) {
    unit.directive = directive;
    return { state: cloneState(mockState), outcome: "AppliedImmediately" };
  }

  const nextId = mockState.messageCarriers.reduce((max, m) => Math.max(max, m.id), 0) + 1;
  mockState.messageCarriers.push({
    id: nextId,
    name: `文书 #${nextId}`,
    forceId: unit.forceId,
    x: issuerX,
    y: issuerY,
    isMilitary: false,
    soldiers: 10,
    courierCount: 2,
    escortSoldierCount: 8,
    ap: 6,
    movement: 6,
    status: "Moving",
    payloadType: "PolicyChange",
    directive: "PolicyChange",
    route: buildManhattanPath(issuerX, issuerY, unit.x, unit.y),
    morale: 80,
    training: 70,
    cultureName: "日本",
    religionName: "神道教",
    money: 0,
    targetUnitId: unitId,
    targetUnitName: unit.name,
    originStrongholdId: 0,
    originStrongholdName: null,
    pendingDirective: directive,
  });

  return { state: cloneState(mockState), outcome: "CarrierDispatched" };
}

function cloneState(state: StrategyWorldState): StrategyWorldState {
  return structuredClone(state);
}
