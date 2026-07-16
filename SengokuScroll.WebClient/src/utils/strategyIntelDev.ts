/** 情报系统中仅开发阶段显示的字段。 */
export function isIntelDevFieldsVisible(): boolean {
  return import.meta.env.DEV;
}
