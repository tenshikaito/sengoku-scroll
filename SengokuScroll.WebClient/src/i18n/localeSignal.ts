import { ref } from "vue";

/** 语言切换时递增，供组合式函数与 label helper 建立响应式依赖。 */
export const localeTick = ref(0);

export function bumpLocaleTick(): void {
  localeTick.value += 1;
}
