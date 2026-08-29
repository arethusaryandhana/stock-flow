import { defineStore } from 'pinia'

export type ToastType = 'success' | 'error' | 'info'

type ToastItem = {
  id: number
  message: string
  type: ToastType
}

let nextToastId = 0
const dismissTimers = new Map<number, ReturnType<typeof window.setTimeout>>()

export const useToastStore = defineStore('toast', {
  state: () => ({
    items: [] as ToastItem[],
  }),
  actions: {
    show(message: string, type: ToastType = 'success', duration = 3200) {
      const id = ++nextToastId
      this.items.push({ id, message, type })

      if (this.items.length > 4) this.dismiss(this.items[0].id)
      if (duration > 0) {
        dismissTimers.set(id, window.setTimeout(() => this.dismiss(id), duration))
      }

      return id
    },
    success(message: string) {
      return this.show(message, 'success')
    },
    error(message: string) {
      return this.show(message, 'error', 4500)
    },
    info(message: string) {
      return this.show(message, 'info')
    },
    dismiss(id: number) {
      this.items = this.items.filter((item) => item.id !== id)
      const timer = dismissTimers.get(id)
      if (timer) window.clearTimeout(timer)
      dismissTimers.delete(id)
    },
  },
})
