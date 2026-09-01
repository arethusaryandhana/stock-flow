import { computed, ref } from 'vue'

const activeRequestCount = ref(0)

export const isRequestPending = computed(() => activeRequestCount.value > 0)

export function beginRequest() {
  activeRequestCount.value += 1
}

export function endRequest() {
  activeRequestCount.value = Math.max(0, activeRequestCount.value - 1)
}
