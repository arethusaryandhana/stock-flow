import { createRouter, createWebHistory } from 'vue-router'
import Login from '../presentation/Login.vue'
import Dashboard from '../presentation/Dashboard.vue'
import Products from '../presentation/Products.vue'
import StockMovements from '../presentation/StockMovements.vue'
import StockAdjustments from '../presentation/StockAdjustments.vue'
import MasterData from '../presentation/MasterData.vue'
import PurchaseOrders from '../presentation/PurchaseOrders.vue'
import SalesOrders from '../presentation/SalesOrders.vue'
import Reports from '../presentation/Reports.vue'
import Receiving from '../presentation/Receiving.vue'
import OperationalSuppliers from '../presentation/OperationalSuppliers.vue'
import { api } from '../infrastructure/api'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', component: Login },
    { path: '/', component: Dashboard, meta: { auth: true } },
    { path: '/products', component: Products, meta: { auth: true } },
    { path: '/inventory/movements', component: StockMovements, meta: { auth: true } },
    { path: '/inventory/adjustments', component: StockAdjustments, meta: { auth: true } },
    { path: '/operations/purchase-orders', component: PurchaseOrders, meta: { auth: true } },
    { path: '/operations/sales-orders', component: SalesOrders, meta: { auth: true } },
    { path: '/reports', component: Reports, meta: { auth: true } },
    { path: '/operations/receiving', component: Receiving, meta: { auth: true } },
    { path: '/operations/suppliers', component: OperationalSuppliers, meta: { auth: true } },
    { path: '/master-data', redirect: '/master-data/categories', meta: { auth: true, admin: true } },
    { path: '/master-data/categories', component: MasterData, props: { entity: 'categories' }, meta: { auth: true, admin: true } },
    { path: '/master-data/products', component: MasterData, props: { entity: 'products' }, meta: { auth: true, admin: true } },
    { path: '/master-data/suppliers', component: MasterData, props: { entity: 'suppliers' }, meta: { auth: true, admin: true } },
    { path: '/master-data/customers', component: MasterData, props: { entity: 'customers' }, meta: { auth: true, admin: true } },
  ],
})

async function restoreSession() {
  if (sessionStorage.getItem('stockflow_authenticated') === 'true') return true

  try {
    const { data } = await api.get<{ fullName: string; role: string }>('/auth/session')
    sessionStorage.setItem('stockflow_authenticated', 'true')
    sessionStorage.setItem('stockflow_name', data.fullName)
    sessionStorage.setItem('stockflow_role', data.role)
    return true
  } catch {
    return false
  }
}

router.beforeEach(async (to) => {
  const hasSession = to.meta.auth ? await restoreSession() :
    sessionStorage.getItem('stockflow_authenticated') === 'true'

  if (to.meta.auth && !hasSession) return '/login'
  if (to.meta.admin && sessionStorage.getItem('stockflow_role')?.trim().toLowerCase() !== 'admin') return hasSession ? '/' : '/login'
  return true
})

export default router
