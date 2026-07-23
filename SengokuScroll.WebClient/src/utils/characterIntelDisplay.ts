/** 任务类别 → 情报展示标签 */
export function taskCategoryLabel(category: string | undefined): string {
  switch (category) {
    case "Personal":
      return "个人";
    case "Life":
      return "人生";
    case "Force":
      return "势力";
    case "PartTime":
      return "兼职";
    default:
      return category?.trim() || "—";
  }
}
