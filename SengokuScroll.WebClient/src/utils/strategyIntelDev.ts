/** 情报系统中由对话框 checkbox 控制的隐藏字段。 */
export function isIntelDevFieldsVisible(intelDebugMode = false): boolean {
  return intelDebugMode === true;
}
