import { createRouter, createWebHistory } from 'vue-router'
import Login from '../presentation/Login.vue'
import Dashboard from '../presentation/Dashboard.vue'
import Products from '../presentation/Products.vue'
import StockMovements from '../presentation/StockMovements.vue'
import StockAdjustments from '../presentation/StockAdjustments.vue'
import MasterData from '../presentation/MasterData.vue'
import PurchaseOrders from '../presentation/PurchaseOrders.vue'
import Receiving from '../presentation/Receiving.vue'
import OperationalSuppliers from '../presentation/OperationalSuppliers.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', component: Login },
    { path: '/', component: Dashboard, meta: { auth: true } },
    { path: '/products', component: Products, meta: { auth: true } },
    { path: '/inventory/movements', component: StockMovements, meta: { auth: true } },
    { path: '/inventory/adjustments', component: StockAdjustments, meta: { auth: true } },
    { path: '/operations/purchase-orders', component: PurchaseOrders, meta: { auth: true } },
    { path: '/operations/receiving', component: Receiving, meta: { auth: true } },
    { path: '/operations/suppliers', component: OperationalSuppliers, meta: { auth: true } },
    { path: '/master-data', redirect: '/master-data/categories', meta: { auth: true, admin: true } },
    { path: '/master-data/categories', component: MasterData, props: { entity: 'categories' }, meta: { auth: true, admin: true } },
    { path: '/master-data/products', component: MasterData, props: { entity: 'products' }, meta: { auth: true, admin: true } },
    { path: '/master-data/suppliers', component: MasterData, props: { entity: 'suppliers' }, meta: { auth: true, admin: true } },
    { path: '/master-data/customers', component: MasterData, props: { entity: 'customers' }, meta: { auth: true, admin: true } },
  ],
})

function isTokenExpired(token: string) {
  try {
    const payload = token.split('.')[1]
    if (!payload) return true

    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/')
    const padded = base64.padEnd(base64.length + (4 - (base64.length % 4)) % 4, '=')
    const { exp } = JSON.parse(atob(padded)) as { exp?: number }

    return typeof exp !== 'number' || exp * 1000 <= Date.now()
  } catch {
    return true
  }
}

function clearExpiredSession() {
  for (const key of ['stockflow_token', 'stockflow_name', 'stockflow_role']) {
    localStorage.removeItem(key)
  }
  sessionStorage.clear()
}

router.beforeEach((to) => {
  const token = localStorage.getItem('stockflow_token')
  const hasValidToken = Boolean(token && !isTokenExpired(token))

  if (token && !hasValidToken) clearExpiredSession()
  if (to.meta.auth && !hasValidToken) return '/login'
  if (to.meta.admin && localStorage.getItem('stockflow_role')?.trim().toLowerCase() !== 'admin') return hasValidToken ? '/' : '/login'
  return true
})

export default router
