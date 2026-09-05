/** CharacterDefinition.PersonalityData 字段与喜好五分类。 */

import { t } from "@/i18n/textLocalizer";

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

export function personalityFieldLabel(key: PersonalityFieldKey): string {
  if (key === "action") return "慎重（越高越谨慎）";
  return t(`enum.personality.${key}`);
}

export const HOBBY_CATEGORY_KEYS = [
  "hobbyWeapon",
  "hobbyBook",
  "hobbyArt",
  "hobbyImport",
  "hobbyTreasure",
] as const;

export type HobbyCategoryKey = (typeof HOBBY_CATEGORY_KEYS)[number];

export function hobbyCategoryLabel(key: HobbyCategoryKey): string {
  return `${t(`enum.hobby.${key}`)}（展示推导）`;
}

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
