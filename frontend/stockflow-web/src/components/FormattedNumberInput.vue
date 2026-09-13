<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useDisplayPreferences } from '../preferences'

defineOptions({ inheritAttrs: false })

const props = withDefaults(defineProps<{
  modelValue: string | number
  decimalScale?: number
  allowNegative?: boolean
}>(), {
  decimalScale: 0,
  allowNegative: false,
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

const { resolvedNumberLocale } = useDisplayPreferences()
const separators = computed(() => {
  const parts = new Intl.NumberFormat(resolvedNumberLocale.value).formatToParts(12345.6)
  return {
    decimal: parts.find((part) => part.type === 'decimal')?.value ?? '.',
    group: parts.find((part) => part.type === 'group')?.value ?? ',',
  }
})

function groupThousands(value: string) {
  return value.replace(/\B(?=(\d{3})+(?!\d))/g, separators.value.group)
}

function formatModelValue(value: string | number) {
  const raw = String(value ?? '').trim()
  if (!raw) return ''
  if (props.allowNegative && raw === '-') return '-'

  const isNegative = props.allowNegative && raw.startsWith('-')
  const unsigned = raw.replace(/^[+-]/, '')
  const [integer = '', fraction] = unsigned.split('.')
  const digits = integer.replace(/\D/g, '') || '0'
  const decimal = props.decimalScale > 0 && fraction !== undefined
    ? `${separators.value.decimal}${fraction.replace(/\D/g, '').slice(0, props.decimalScale)}`
    : ''

  return `${isNegative ? '-' : ''}${groupThousands(digits)}${decimal}`
}

const displayValue = ref(formatModelValue(props.modelValue))

watch([() => props.modelValue, resolvedNumberLocale], ([value]) => {
  const formatted = formatModelValue(value)
  if (formatted !== displayValue.value) displayValue.value = formatted
})

function handleInput(event: Event) {
  const input = event.target as HTMLInputElement
  const typed = input.value
  const isNegative = props.allowNegative && typed.trimStart().startsWith('-')
  const unsigned = typed.replace(/-/g, '')
  const decimalIndex = props.decimalScale > 0 ? unsigned.indexOf(separators.value.decimal) : -1
  const integerPart = (decimalIndex >= 0 ? unsigned.slice(0, decimalIndex) : unsigned).replace(/\D/g, '')
  const fractionPart = decimalIndex >= 0
    ? unsigned.slice(decimalIndex + separators.value.decimal.length).replace(/\D/g, '').slice(0, props.decimalScale)
    : ''

  if (!integerPart) {
    const emptyValue = isNegative ? '-' : ''
    displayValue.value = emptyValue
    input.value = emptyValue
    emit('update:modelValue', emptyValue)
    return
  }

  const normalizedInteger = integerPart.replace(/^0+(?=\d)/, '')
  const decimalDisplay = decimalIndex >= 0 ? `${separators.value.decimal}${fractionPart}` : ''
  const rawValue = `${isNegative ? '-' : ''}${normalizedInteger}${decimalIndex >= 0 ? `.${fractionPart}` : ''}`
  const formatted = `${isNegative ? '-' : ''}${groupThousands(normalizedInteger)}${decimalDisplay}`

  displayValue.value = formatted
  input.value = formatted
  emit('update:modelValue', rawValue)
}
</script>

<template>
  <input
    v-bind="$attrs"
    :value="displayValue"
    type="text"
    :inputmode="decimalScale > 0 ? 'decimal' : 'numeric'"
    autocomplete="off"
    @input="handleInput"
  >
</template>
