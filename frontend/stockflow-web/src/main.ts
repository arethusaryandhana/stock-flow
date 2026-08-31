import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { initializeTheme } from './theme'
import './style.css'

initializeTheme()
createApp(App).use(createPinia()).use(router).mount('#app')
