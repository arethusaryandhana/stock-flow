<script setup lang="ts">
import { ref, watch } from 'vue'

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

function groupThousands(value: string) {
  return value.replace(/\B(?=(\d{3})+(?!\d))/g, '.')
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
    ? `,${fraction.replace(/\D/g, '').slice(0, props.decimalScale)}`
    : ''

  return `${isNegative ? '-' : ''}${groupThousands(digits)}${decimal}`
}

const displayValue = ref(formatModelValue(props.modelValue))

watch(() => props.modelValue, (value) => {
  const formatted = formatModelValue(value)
  if (formatted !== displayValue.value) displayValue.value = formatted
})

function handleInput(event: Event) {
  const input = event.target as HTMLInputElement
  const typed = input.value
  const isNegative = props.allowNegative && typed.trimStart().startsWith('-')
  const unsigned = typed.replace(/-/g, '')
  const commaIndex = props.decimalScale > 0 ? unsigned.indexOf(',') : -1
  const integerPart = (commaIndex >= 0 ? unsigned.slice(0, commaIndex) : unsigned).replace(/\D/g, '')
  const fractionPart = commaIndex >= 0
    ? unsigned.slice(commaIndex + 1).replace(/\D/g, '').slice(0, props.decimalScale)
    : ''

  if (!integerPart) {
    const emptyValue = isNegative ? '-' : ''
    displayValue.value = emptyValue
    input.value = emptyValue
    emit('update:modelValue', emptyValue)
    return
  }

  const normalizedInteger = integerPart.replace(/^0+(?=\d)/, '')
  const decimalDisplay = commaIndex >= 0 ? `,${fractionPart}` : ''
  const rawValue = `${isNegative ? '-' : ''}${normalizedInteger}${commaIndex >= 0 ? `.${fractionPart}` : ''}`
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
