import { computed, ref } from 'vue'
import { useI18n } from './i18n'

export type TimeZonePreference = 'system' | 'Asia/Jakarta' | 'Asia/Makassar' | 'Asia/Jayapura' | 'UTC'
export type DateFormatPreference = 'regional' | 'dmy' | 'mdy' | 'ymd'
export type NumberFormatPreference = 'regional' | 'id-ID' | 'en-US'
export type PageSizePreference = 5 | 10 | 25 | 50 | 100

type StoredPreferences = {
  timeZone: TimeZonePreference
  dateFormat: DateFormatPreference
  numberFormat: NumberFormatPreference
  defaultPageSize: PageSizePreference
}

type DateDisplayOptions = {
  includeYear?: boolean
  includeTime?: boolean
  includeSeconds?: boolean
}

export const timeZoneOptions: readonly TimeZonePreference[] = ['system', 'Asia/Jakarta', 'Asia/Makassar', 'Asia/Jayapura', 'UTC']
export const dateFormatOptions: readonly DateFormatPreference[] = ['regional', 'dmy', 'mdy', 'ymd']
export const numberFormatOptions: readonly NumberFormatPreference[] = ['regional', 'id-ID', 'en-US']
export const pageSizeOptions: readonly PageSizePreference[] = [5, 10, 25, 50, 100]

const storageKey = 'stockflow_display_preferences_v1'
const defaults: StoredPreferences = {
  timeZone: 'system',
  dateFormat: 'regional',
  numberFormat: 'regional',
  defaultPageSize: 10,
}

function readStoredPreferences(): StoredPreferences {
  try {
    const stored = JSON.parse(localStorage.getItem(storageKey) ?? '{}') as Partial<StoredPreferences>
    return {
      timeZone: timeZoneOptions.includes(stored.timeZone as TimeZonePreference) ? stored.timeZone as TimeZonePreference : defaults.timeZone,
      dateFormat: dateFormatOptions.includes(stored.dateFormat as DateFormatPreference) ? stored.dateFormat as DateFormatPreference : defaults.dateFormat,
      numberFormat: numberFormatOptions.includes(stored.numberFormat as NumberFormatPreference) ? stored.numberFormat as NumberFormatPreference : defaults.numberFormat,
      defaultPageSize: pageSizeOptions.includes(stored.defaultPageSize as PageSizePreference) ? stored.defaultPageSize as PageSizePreference : defaults.defaultPageSize,
    }
  } catch {
    return { ...defaults }
  }
}

const stored = readStoredPreferences()
const timeZone = ref<TimeZonePreference>(stored.timeZone)
const dateFormat = ref<DateFormatPreference>(stored.dateFormat)
const numberFormat = ref<NumberFormatPreference>(stored.numberFormat)
const defaultPageSize = ref<PageSizePreference>(stored.defaultPageSize)

function persist() {
  localStorage.setItem(storageKey, JSON.stringify({
    timeZone: timeZone.value,
    dateFormat: dateFormat.value,
    numberFormat: numberFormat.value,
    defaultPageSize: defaultPageSize.value,
  } satisfies StoredPreferences))
}

function dateParts(date: Date, locale: string, zone: string, includeYear: boolean) {
  const formatter = new Intl.DateTimeFormat(locale, {
    day: '2-digit',
    month: 'short',
    ...(includeYear ? { year: 'numeric' as const } : {}),
    timeZone: zone,
  })
  const parts = formatter.formatToParts(date)
  return {
    day: parts.find((part) => part.type === 'day')?.value ?? '',
    month: parts.find((part) => part.type === 'month')?.value ?? '',
    year: parts.find((part) => part.type === 'year')?.value ?? '',
  }
}

export function useDisplayPreferences() {
  const { locale } = useI18n()
  const systemTimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC'
  const resolvedTimeZone = computed(() => timeZone.value === 'system' ? systemTimeZone : timeZone.value)
  const resolvedNumberLocale = computed(() => numberFormat.value === 'regional' ? locale.value : numberFormat.value)

  function setTimeZone(value: TimeZonePreference) {
    if (!timeZoneOptions.includes(value)) return
    timeZone.value = value
    persist()
  }

  function setDateFormat(value: DateFormatPreference) {
    if (!dateFormatOptions.includes(value)) return
    dateFormat.value = value
    persist()
  }

  function setNumberFormat(value: NumberFormatPreference) {
    if (!numberFormatOptions.includes(value)) return
    numberFormat.value = value
    persist()
  }

  function setDefaultPageSize(value: number) {
    if (!pageSizeOptions.includes(value as PageSizePreference)) return
    defaultPageSize.value = value as PageSizePreference
    persist()
  }

  function formatNumber(value: number, options: Intl.NumberFormatOptions = {}) {
    return new Intl.NumberFormat(resolvedNumberLocale.value, options).format(value)
  }

  function formatDate(value: string | Date, options: DateDisplayOptions = {}) {
    const parsed = value instanceof Date ? value : new Date(value)
    if (Number.isNaN(parsed.getTime())) return '—'

    const includeYear = options.includeYear ?? true
    const includeTime = options.includeTime ?? false
    const includeSeconds = options.includeSeconds ?? false
    const zone = resolvedTimeZone.value

    let result: string
    if (dateFormat.value === 'regional') {
      result = new Intl.DateTimeFormat(locale.value, {
        day: '2-digit',
        month: 'short',
        ...(includeYear ? { year: 'numeric' as const } : {}),
        timeZone: zone,
      }).format(parsed)
    } else {
      const parts = dateParts(parsed, locale.value, zone, includeYear)
      if (dateFormat.value === 'ymd') {
        const numericParts = new Intl.DateTimeFormat('en-CA', {
          day: '2-digit', month: '2-digit', ...(includeYear ? { year: 'numeric' as const } : {}), timeZone: zone,
        }).formatToParts(parsed)
        const day = numericParts.find((part) => part.type === 'day')?.value ?? ''
        const month = numericParts.find((part) => part.type === 'month')?.value ?? ''
        const year = numericParts.find((part) => part.type === 'year')?.value ?? ''
        result = includeYear ? `${year}-${month}-${day}` : `${month}-${day}`
      } else if (dateFormat.value === 'mdy') {
        result = includeYear ? `${parts.month} ${parts.day}, ${parts.year}` : `${parts.month} ${parts.day}`
      } else {
        result = includeYear ? `${parts.day} ${parts.month} ${parts.year}` : `${parts.day} ${parts.month}`
      }
    }

    if (!includeTime) return result
    const time = new Intl.DateTimeFormat(locale.value, {
      hour: '2-digit',
      minute: '2-digit',
      ...(includeSeconds ? { second: '2-digit' as const } : {}),
      timeZone: zone,
    }).format(parsed)
    return `${result}, ${time}`
  }

  return {
    timeZone,
    dateFormat,
    numberFormat,
    defaultPageSize,
    systemTimeZone,
    resolvedTimeZone,
    resolvedNumberLocale,
    setTimeZone,
    setDateFormat,
    setNumberFormat,
    setDefaultPageSize,
    formatNumber,
    formatDate,
  }
}
