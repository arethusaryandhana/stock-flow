<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const email = ref('admin@stockflow.local')
const password = ref('StockFlow123!')
const remember = ref(true)
const error = ref('')
const busy = ref(false)
const auth = useAuthStore()
const router = useRouter()

async function submit() {
  busy.value = true; error.value = ''
  try { await auth.login(email.value, password.value); router.push('/') } catch (requestError) { error.value = (requestError as Error).message } finally { busy.value = false }
}
</script>

<template>
  <main class="login">
    <section class="login-visual">
      <div class="login-brand"><div class="brand-mark"><span /> <span /> <span /></div><div><strong>StockFlow</strong><small>Inventory OS</small></div></div>
      <div class="login-copy"><p class="eyebrow">OPERATIONS, SIMPLIFIED</p><h2>Stok terkendali.<br>Bisnis melaju.</h2><p>Ruang kerja untuk melihat kondisi inventori, mengelola pergerakan, dan membuat keputusan restock dengan lebih percaya diri.</p><div class="login-points"><span>Real-time visibility</span><span>Audit-ready</span><span>Multi-workspace</span></div></div>
      <p class="login-footer">© 2026 StockFlow · Inventory operations platform</p>
    </section>
    <section class="login-form-side">
      <div class="login-card">
        <p class="eyebrow">WELCOME BACK</p><h1>Masuk ke workspace</h1><p class="subtitle">Lanjutkan mengelola operasional StockFlow Anda.</p>
        <form class="login-form" @submit.prevent="submit">
          <label class="login-label">Email kerja<input v-model="email" type="email" autocomplete="username" required></label>
          <label class="login-label">Password<input v-model="password" type="password" autocomplete="current-password" required></label>
          <div class="login-options"><label class="checkbox"><input v-model="remember" type="checkbox"> Ingat saya</label><span>Butuh bantuan?</span></div>
          <p v-if="error" class="alert">{{ error }}</p>
          <button class="primary login-submit" :disabled="busy">{{ busy ? 'Memeriksa akses…' : 'Masuk ke StockFlow →' }}</button>
        </form>
        <div class="demo-note"><strong>Demo workspace</strong> Kredensial demo sudah diisi. Silakan langsung masuk untuk melihat dashboard.</div>
      </div>
    </section>
  </main>
</template>
