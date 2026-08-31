import { computed, ref } from 'vue'

export type ThemePreference = 'system' | 'light' | 'dark'
export type ResolvedTheme = Exclude<ThemePreference, 'system'>

const storageKey = 'stockflow_theme'
const systemThemeQuery = window.matchMedia('(prefers-color-scheme: dark)')

function readStoredPreference(): ThemePreference {
  const stored = localStorage.getItem(storageKey)
  return stored === 'light' || stored === 'dark' ? stored : 'system'
}

const preference = ref<ThemePreference>(readStoredPreference())
const systemTheme = ref<ResolvedTheme>(systemThemeQuery.matches ? 'dark' : 'light')
const resolvedTheme = computed<ResolvedTheme>(() =>
  preference.value === 'system' ? systemTheme.value : preference.value,
)

function applyTheme() {
  const root = document.documentElement
  root.dataset.theme = resolvedTheme.value
  root.dataset.themePreference = preference.value
  root.style.colorScheme = resolvedTheme.value

  const themeColor = document.querySelector<HTMLMetaElement>('meta[name="theme-color"]')
  themeColor?.setAttribute('content', resolvedTheme.value === 'dark' ? '#111a28' : '#3567d6')
}

function handleSystemThemeChange(event: MediaQueryListEvent) {
  systemTheme.value = event.matches ? 'dark' : 'light'
  if (preference.value === 'system') applyTheme()
}

export function initializeTheme() {
  applyTheme()
  systemThemeQuery.addEventListener('change', handleSystemThemeChange)
}

export function useTheme() {
  function setTheme(nextPreference: ThemePreference) {
    preference.value = nextPreference
    localStorage.setItem(storageKey, nextPreference)
    applyTheme()
  }

  return { preference, resolvedTheme, setTheme }
}
