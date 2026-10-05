import { nextTick, onBeforeUnmount, watch, type Ref } from 'vue'

const FOCUSABLE_SELECTOR = [
  'a[href]',
  'button:not(:disabled)',
  'input:not(:disabled)',
  'select:not(:disabled)',
  'textarea:not(:disabled)',
  '[tabindex]:not([tabindex="-1"])',
].join(', ')

function getFocusableElements(container: HTMLElement) {
  return Array.from(container.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR))
    .filter((element) => element.getClientRects().length > 0 && element.getAttribute('aria-hidden') !== 'true')
}

export function useModalFocus(
  isOpen: Readonly<Ref<boolean>>,
  dialog: Ref<HTMLElement | null>,
  onEscape: () => void,
) {
  let returnFocusTarget: HTMLElement | null = null
  let previousBodyOverflow = ''
  let bodyScrollLocked = false

  function focusReturnTarget(target: HTMLElement | null) {
    if (target?.isConnected) target.focus()
  }

  function unlockBodyScroll() {
    if (!bodyScrollLocked) return
    document.body.style.overflow = previousBodyOverflow
    bodyScrollLocked = false
  }

  function handleKeydown(event: KeyboardEvent) {
    const dialogElement = dialog.value
    if (!isOpen.value || !dialogElement) return

    if (event.key === 'Escape') {
      event.preventDefault()
      onEscape()
      return
    }

    if (event.key !== 'Tab') return

    const focusableElements = getFocusableElements(dialogElement)
    if (!focusableElements.length) {
      event.preventDefault()
      dialogElement.focus()
      return
    }

    const first = focusableElements[0]
    const last = focusableElements[focusableElements.length - 1]
    const activeElement = document.activeElement
    if (event.shiftKey && (activeElement === first || !dialogElement.contains(activeElement))) {
      event.preventDefault()
      last.focus()
    } else if (!event.shiftKey && (activeElement === last || !dialogElement.contains(activeElement))) {
      event.preventDefault()
      first.focus()
    }
  }

  async function focusDialog() {
    await nextTick()
    if (!isOpen.value || !dialog.value) return

    const preferredTarget = dialog.value.querySelector<HTMLElement>('[autofocus]')
    const firstFocusable = getFocusableElements(dialog.value)[0]
    ;(preferredTarget ?? firstFocusable ?? dialog.value).focus()
  }

  const stopWatching = watch(isOpen, (open) => {
    if (open) {
      returnFocusTarget = document.activeElement instanceof HTMLElement ? document.activeElement : null
      previousBodyOverflow = document.body.style.overflow
      document.body.style.overflow = 'hidden'
      bodyScrollLocked = true
      document.addEventListener('keydown', handleKeydown)
      void focusDialog()
      return
    }

    document.removeEventListener('keydown', handleKeydown)
    unlockBodyScroll()
    const target = returnFocusTarget
    returnFocusTarget = null
    void nextTick().then(() => focusReturnTarget(target))
  }, { flush: 'sync', immediate: true })

  onBeforeUnmount(() => {
    stopWatching()
    document.removeEventListener('keydown', handleKeydown)
    unlockBodyScroll()
    const target = returnFocusTarget
    returnFocusTarget = null
    void nextTick().then(() => focusReturnTarget(target))
  })
}
