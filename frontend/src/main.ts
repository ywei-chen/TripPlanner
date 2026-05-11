import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { useAuthStore } from './stores/auth.store'
import './assets/styles/main.css'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)

// 應用啟動時嘗試還原登入狀態
const auth = useAuthStore()
auth.fetchMe().finally(() => app.mount('#app'))
