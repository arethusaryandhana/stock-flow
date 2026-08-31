<script setup lang="ts">
import { useI18n } from '../i18n'
import { type ThemePreference, useTheme } from '../theme'

const { t } = useI18n()
const { preference, setTheme } = useTheme()

const options: Array<{ value: ThemePreference; icon: string; labelKey: string }> = [
  { value: 'system', icon: '◐', labelKey: 'theme.system' },
  { value: 'light', icon: '☀', labelKey: 'theme.light' },
  { value: 'dark', icon: '☾', labelKey: 'theme.dark' },
]

function cycleTheme() {
  const currentIndex = options.findIndex((option) => option.value === preference.value)
  setTheme(options[(currentIndex + 1) % options.length].value)
}
</script>

<template>
  <div class="theme-switcher" role="group" :aria-label="t('theme.label')">
    <div class="theme-options">
      <button
        v-for="option in options"
        :key="option.value"
        class="theme-option"
        :class="{ active: preference === option.value }"
        type="button"
        :aria-label="t(option.labelKey)"
        :aria-pressed="preference === option.value"
        :title="t(option.labelKey)"
        @click="setTheme(option.value)"
      >
        <span aria-hidden="true">{{ option.icon }}</span>
      </button>
    </div>
    <button class="theme-cycle" type="button" :aria-label="t('theme.cycle')" :title="t('theme.cycle')" @click="cycleTheme">
      <span aria-hidden="true">{{ options.find((option) => option.value === preference)?.icon }}</span>
    </button>
  </div>
</template>
