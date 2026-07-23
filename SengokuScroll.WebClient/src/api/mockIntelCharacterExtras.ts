import type {
  StrategyCharacterRelationState,
  StrategyCharacterSummaryState,
  StrategyCharacterTaskState,
} from "@/api/strategyTypes";

const FAMILY_NAMES = [
  "藤原", "源", "平", "安倍", "橘", "大江", "菅原", "纪", "中原", "秦",
  "佐藤", "铃木", "高桥", "田中", "渡边", "伊藤", "山本", "中村", "小林", "加藤",
];

const GIVEN_NAMES = [
  "信平", "义朝", "忠信", "正信", "盛政", "长政", "康政", "信政", "重政", "定政",
  "宗久", "算长", "吉次", "新七", "作左卫门", "与一", "半藏", "勘助", "久秀", "信玄",
];

export function generateMockPaperDollName(characterId: number, seedLabel: string): string {
  let seed = characterId;
  for (let i = 0; i < seedLabel.length; i += 1) {
    seed = Math.imul(seed, 31) + seedLabel.charCodeAt(i);
  }
  seed = Math.abs(seed);
  const family = FAMILY_NAMES[seed % FAMILY_NAMES.length];
  const given = GIVEN_NAMES[Math.floor(seed / FAMILY_NAMES.length) % GIVEN_NAMES.length];
  return `${family}${given}`;
}

const MOCK_RELATIONS: Record<number, StrategyCharacterRelationState[]> = {
  1: [
    { relationType: "仇敌", relationTone: "仇视", characterId: 3, characterName: "今川氏真" },
    { relationType: "仇敌", relationTone: "险恶", characterId: 5, characterName: "北条氏康" },
  ],
  2: [
    { relationType: "师父", relationTone: "友好", characterId: 1, characterName: "织田信长" },
  ],
  3: [
    { relationType: "仇敌", relationTone: "仇视", characterId: 1, characterName: "织田信长" },
    { relationType: "仇敌", relationTone: "险恶", characterId: 5, characterName: "北条氏康" },
  ],
  4: [
    { relationType: "师父", relationTone: "普通", characterId: 1, characterName: "织田信长" },
  ],
  5: [
    { relationType: "仇敌", relationTone: "险恶", characterId: 3, characterName: "今川氏真" },
  ],
  6: [
    { relationType: "师父", relationTone: "友好", characterId: 9, characterName: "德川家康" },
  ],
  9: [
    { relationType: "师父", relationTone: "友好", characterId: 3, characterName: "今川氏真" },
    { relationType: "仇敌", relationTone: "仇视", characterId: 1, characterName: "织田信长" },
  ],
  10: [
    { relationType: "师父", relationTone: "亲密", characterId: 1, characterName: "织田信长" },
  ],
  11: [
    { relationType: "师父", relationTone: "友好", characterId: 1, characterName: "织田信长" },
  ],
  90_011: [
    { relationType: "子女", relationTone: "亲密", characterId: 90_014, characterName: "三井与一" },
  ],
  90_014: [
    { relationType: "父亲", relationTone: "亲密", characterId: 90_011, characterName: "三井高利" },
  ],
};
const MOCK_TASKS: Record<number, StrategyCharacterTaskState[]> = {
  1: [
    { taskCategory: "Life", name: "统一领国", target: "织田", status: "长期", remaining: "—" },
    { taskCategory: "Personal", name: "缔结婚姻", target: "清洲", status: "筹划中", remaining: "—" },
    { taskCategory: "Force", name: "整顿军备", target: "清洲", status: "进行中", remaining: "—" },
  ],
  2: [
    { taskCategory: "Life", name: "扬名立万", target: "织田", status: "长期", remaining: "—" },
    { taskCategory: "Personal", name: "稳固仕官", target: "织田", status: "进行中", remaining: "—" },
  ],
  4: [
    { taskCategory: "Personal", name: "稳固仕官", target: "织田", status: "进行中", remaining: "—" },
    { taskCategory: "PartTime", name: "巡查代官", target: "清洲", status: "例行", remaining: "—" },
  ],
  6: [
    { taskCategory: "Life", name: "守成领内", target: "酒井", status: "长期", remaining: "—" },
    { taskCategory: "Personal", name: "稳固仕官", target: "酒井", status: "进行中", remaining: "—" },
  ],
  90_001: [
    { taskCategory: "Personal", name: "奔走仕官", target: "清洲", status: "进行中", remaining: "—" },
    { taskCategory: "Life", name: "扬名立万", target: "清洲", status: "长期", remaining: "—" },
  ],
  90_011: [
    { taskCategory: "Life", name: "扩大商路", target: "三井屋", status: "长期", remaining: "—" },
    { taskCategory: "PartTime", name: "整理账簿", target: "三井屋", status: "例行", remaining: "—" },
  ],
  90_020: [
    { taskCategory: "Life", name: "弘传本教", target: "热田神宫", status: "长期", remaining: "—" },
    { taskCategory: "Personal", name: "缔结婚姻", target: "清洲", status: "筹划中", remaining: "—" },
  ],
  90_022: [
    { taskCategory: "Life", name: "弘传本教", target: "证愿寺", status: "长期", remaining: "—" },
    { taskCategory: "PartTime", name: "整理账簿", target: "证愿寺", status: "例行", remaining: "—" },
  ],
};

export function enrichMockCharacterIntel(
  character: StrategyCharacterSummaryState,
): StrategyCharacterSummaryState {
  const relations = MOCK_RELATIONS[character.id];
  const activeTasks = MOCK_TASKS[character.id];
  if (!relations && !activeTasks) return character;
  return {
    ...character,
    ...(relations ? { relations } : {}),
    ...(activeTasks ? { activeTasks } : {}),
  };
}

export function buildDefaultMockCharacterIntel(
  characterId: number,
  seedLabel: string,
): Pick<StrategyCharacterSummaryState, "relations" | "activeTasks"> {
  const seed = Math.abs(Math.imul(characterId, 17) + seedLabel.length);
  return {
    relations: [],
    activeTasks: [
      {
        taskCategory: "Personal",
        name: seed % 2 === 0 ? "稳固仕官" : "奔走仕官",
        target: seedLabel,
        status: "进行中",
        remaining: "—",
      },
      {
        taskCategory: "Life",
        name: seed % 3 === 0 ? "扬名立万" : "守成领内",
        target: seedLabel,
        status: "长期",
        remaining: "—",
      },
    ],
  };
}
