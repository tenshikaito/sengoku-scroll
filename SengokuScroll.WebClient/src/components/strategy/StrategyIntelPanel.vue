<script setup lang="ts">
defineProps<{
  /** 面板类型：决定边框色，单实体与多实体同套样式。 */
  variant: "stronghold" | "military" | "civil" | "battlefield";
  ariaLabel: string;
}>();
</script>

<template>
  <div class="intel-box" :class="`intel-box--${variant}`" :aria-label="ariaLabel">
    <slot />
  </div>
</template>
