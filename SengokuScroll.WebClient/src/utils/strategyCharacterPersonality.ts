/** CharacterDefinition.PersonalityData 字段与喜好五分类。 */

export const PERSONALITY_FIELD_KEYS = [
  "temper",
  "courage",
  "principle",
  "action",
  "friendship",
  "ambition",
  "desire",
  "drinking",
  "fortune",
] as const;

export type PersonalityFieldKey = (typeof PERSONALITY_FIELD_KEYS)[number];

export const PERSONALITY_FIELD_LABELS: Record<PersonalityFieldKey, string> = {
  temper: "性情",
  courage: "勇气",
  principle: "主义",
  action: "行动",
  friendship: "情义",
  ambition: "野心",
  desire: "物欲",
  drinking: "饮酒",
  fortune: "运势",
};

export const HOBBY_CATEGORY_KEYS = [
  "hobbyWeapon",
  "hobbyBook",
  "hobbyArt",
  "hobbyImport",
  "hobbyTreasure",
] as const;

export type HobbyCategoryKey = (typeof HOBBY_CATEGORY_KEYS)[number];

export const HOBBY_CATEGORY_LABELS: Record<HobbyCategoryKey, string> = {
  hobbyWeapon: "武具",
  hobbyBook: "书籍",
  hobbyArt: "艺术品",
  hobbyImport: "舶来品",
  hobbyTreasure: "财宝",
};

/** 将单一喜好值拆分为五类展示（待独立字段实装前）。 */
export function hobbyCategoryValues(
  hobby: number | undefined | null,
  anchorId: number
): Record<HobbyCategoryKey, number> {
  const base = Number.isFinite(Number(hobby)) ? Math.min(100, Math.max(0, Math.trunc(Number(hobby)))) : 0;
  const primary = Math.abs(anchorId) % HOBBY_CATEGORY_KEYS.length;
  const result = {} as Record<HobbyCategoryKey, number>;

  HOBBY_CATEGORY_KEYS.forEach((key, index) => {
    if (index === primary) {
      result[key] = base;
      return;
    }
    const spread = (Math.abs(anchorId) * (index + 7) + base) % 45;
    result[key] = Math.min(100, Math.max(0, Math.round(base * 0.25 + spread * 0.5)));
  });

  return result;
}
