<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { api } from '../infrastructure/api'
import { useI18n } from '../i18n'
import { useToastStore } from '../stores/toast'

const emit = defineEmits<{ close: [] }>()
const { t } = useI18n()
const toast = useToastStore()
const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const showCurrentPassword = ref(false)
const showNewPassword = ref(false)
const showConfirmPassword = ref(false)
const error = ref('')
const saving = ref(false)

function close() {
  if (!saving.value) emit('close')
}

function closeOnEscape(event: KeyboardEvent) {
  if (event.key === 'Escape') close()
}

async function submit() {
  error.value = ''

  if (newPassword.value.length < 8) {
    error.value = t('changePassword.passwordMinLength')
    return
  }

  if (newPassword.value !== confirmPassword.value) {
    error.value = t('changePassword.passwordMismatch')
    return
  }

  saving.value = true
  try {
    const { data } = await api.post<{ message: string }>('/auth/change-password', {
      currentPassword: currentPassword.value,
      newPassword: newPassword.value,
    })
    toast.success(data.message)
    emit('close')
  } catch (requestError) {
    error.value = (requestError as Error).message
  } finally {
    saving.value = false
  }
}

onMounted(() => document.addEventListener('keydown', closeOnEscape))
onBeforeUnmount(() => document.removeEventListener('keydown', closeOnEscape))
</script>

<template>
  <Teleport to="body">
    <div class="modal-backdrop" @click.self="close">
      <form
        class="modal change-password-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="change-password-title"
        :aria-busy="saving"
        @submit.prevent="submit"
      >
        <div class="modal-head">
          <div>
            <p class="eyebrow">{{ t('changePassword.eyebrow') }}</p>
            <h2 id="change-password-title">{{ t('changePassword.title') }}</h2>
            <p>{{ t('changePassword.description') }}</p>
          </div>
          <button class="close-button" type="button" :disabled="saving" :aria-label="t('common.close')" @click="close">×</button>
        </div>

        <div class="modal-body change-password-form">
          <label class="login-label">
            {{ t('changePassword.currentPassword') }}
            <div class="password-field">
              <input v-model="currentPassword" :type="showCurrentPassword ? 'text' : 'password'" autocomplete="current-password" required autofocus :disabled="saving">
              <button class="password-toggle" type="button" :disabled="saving" :aria-label="showCurrentPassword ? t('changePassword.hideCurrentPassword') : t('changePassword.showCurrentPassword')" :aria-pressed="showCurrentPassword" @click="showCurrentPassword = !showCurrentPassword">
                <svg class="eye-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.75-6 10-6 10 6 10 6-3.75 6-10 6-10-6-10-6Z" /><circle cx="12" cy="12" r="2.75" /><path v-if="!showCurrentPassword" d="m3 3 18 18" /></svg>
              </button>
            </div>
          </label>

          <label class="login-label">
            {{ t('changePassword.newPassword') }}
            <div class="password-field">
              <input v-model="newPassword" :type="showNewPassword ? 'text' : 'password'" autocomplete="new-password" minlength="8" required :disabled="saving">
              <button class="password-toggle" type="button" :disabled="saving" :aria-label="showNewPassword ? t('changePassword.hideNewPassword') : t('changePassword.showNewPassword')" :aria-pressed="showNewPassword" @click="showNewPassword = !showNewPassword">
                <svg class="eye-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.75-6 10-6 10 6 10 6-3.75 6-10 6-10-6-10-6Z" /><circle cx="12" cy="12" r="2.75" /><path v-if="!showNewPassword" d="m3 3 18 18" /></svg>
              </button>
            </div>
          </label>

          <label class="login-label">
            {{ t('changePassword.confirmPassword') }}
            <div class="password-field">
              <input v-model="confirmPassword" :type="showConfirmPassword ? 'text' : 'password'" autocomplete="new-password" minlength="8" required :disabled="saving">
              <button class="password-toggle" type="button" :disabled="saving" :aria-label="showConfirmPassword ? t('changePassword.hideConfirmPassword') : t('changePassword.showConfirmPassword')" :aria-pressed="showConfirmPassword" @click="showConfirmPassword = !showConfirmPassword">
                <svg class="eye-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M2 12s3.75-6 10-6 10 6 10 6-3.75 6-10 6-10-6-10-6Z" /><circle cx="12" cy="12" r="2.75" /><path v-if="!showConfirmPassword" d="m3 3 18 18" /></svg>
              </button>
            </div>
          </label>

          <p v-if="error" class="alert">{{ error }}</p>
          <div class="modal-actions">
            <button class="secondary" type="button" :disabled="saving" @click="close">{{ t('common.cancel') }}</button>
            <button class="primary" :disabled="saving"><span v-if="saving" class="button-spinner" aria-hidden="true" />{{ saving ? t('changePassword.saving') : t('changePassword.save') }}</button>
          </div>
        </div>
      </form>
    </div>
  </Teleport>
</template>

<style scoped>
.change-password-modal { width: min(100%, 470px); }
.change-password-form { display: grid; gap: 16px; }
.change-password-form .alert { margin: 0; }
.change-password-form .modal-actions { margin-top: 2px; }
</style>
