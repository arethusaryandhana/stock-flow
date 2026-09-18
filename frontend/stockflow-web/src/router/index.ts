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
import AuditHistory from '../presentation/AuditHistory.vue'
import Settings from '../presentation/Settings.vue'
import Users from '../presentation/Users.vue'
import RoleAccess from '../presentation/RoleAccess.vue'
import { useAuthStore } from '../stores/auth'
import { api, getAccessToken } from '../infrastructure/api'

const sessionKeys = ['stockflow_authenticated', 'stockflow_name', 'stockflow_email', 'stockflow_role']

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', component: Login },
    { path: '/', component: Dashboard, meta: { auth: true, permission: 'menu.dashboard' } },
    { path: '/products', component: Products, meta: { auth: true, permission: 'menu.products' } },
    { path: '/inventory/movements', component: StockMovements, meta: { auth: true, permission: 'menu.movements' } },
    { path: '/inventory/adjustments', component: StockAdjustments, meta: { auth: true, permission: 'menu.adjustments' } },
    { path: '/operations/purchase-orders', component: PurchaseOrders, meta: { auth: true, permission: 'menu.purchase-orders' } },
    { path: '/operations/sales-orders', component: SalesOrders, meta: { auth: true, permission: 'menu.sales-orders' } },
    { path: '/reports', component: Reports, meta: { auth: true, permission: 'menu.reports' } },
    { path: '/operations/receiving', component: Receiving, meta: { auth: true, permission: 'menu.receiving' } },
    { path: '/operations/suppliers', component: OperationalSuppliers, meta: { auth: true, permission: 'menu.suppliers' } },
    { path: '/master-data', redirect: '/master-data/categories', meta: { auth: true } },
    { path: '/master-data/categories', component: MasterData, props: { entity: 'categories' }, meta: { auth: true, permission: 'menu.master.categories' } },
    { path: '/master-data/products', component: MasterData, props: { entity: 'products' }, meta: { auth: true, permission: 'menu.master.products' } },
    { path: '/master-data/suppliers', component: MasterData, props: { entity: 'suppliers' }, meta: { auth: true, permission: 'menu.master.suppliers' } },
    { path: '/master-data/customers', component: MasterData, props: { entity: 'customers' }, meta: { auth: true, permission: 'menu.master.customers' } },
    { path: '/admin/audit-history', component: AuditHistory, meta: { auth: true, permission: 'menu.audit' } },
    { path: '/admin/users', component: Users, meta: { auth: true, permission: 'menu.users' } },
    { path: '/admin/roles', component: RoleAccess, props: { mode: 'roles' }, meta: { auth: true, permission: 'menu.roles' } },
    { path: '/admin/access', component: RoleAccess, props: { mode: 'access' }, meta: { auth: true, permission: 'menu.access' } },
    { path: '/settings', component: Settings, meta: { auth: true, permission: 'menu.settings' } },
    { path: '/forbidden', component: { template: '<div class="page"><div class="surface-card" style="padding:2rem"><h1>Akses ditolak</h1><p>Role Anda tidak memiliki akses ke halaman ini.</p></div></div>' }, meta: { auth: true } },
  ],
})

async function restoreSession() {
  if (!getAccessToken()) {
    sessionKeys.forEach((key) => sessionStorage.removeItem(key))
    return false
  }
  if (sessionStorage.getItem('stockflow_authenticated') === 'true') return true

  try {
    const { data } = await api.get<{ fullName: string; email: string; role: string }>('/auth/session')
    sessionStorage.setItem('stockflow_authenticated', 'true')
    sessionStorage.setItem('stockflow_name', data.fullName)
    sessionStorage.setItem('stockflow_email', data.email)
    sessionStorage.setItem('stockflow_role', data.role)
    return true
  } catch {
    return false
  }
}

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  const hasSession = to.meta.auth ? await restoreSession() :
    Boolean(getAccessToken()) && sessionStorage.getItem('stockflow_authenticated') === 'true'

  if (to.meta.auth && !hasSession) return '/login'
  if (to.meta.auth) {
    try {
      await auth.refreshAccess()
    } catch {
      return '/login'
    }
    if (to.meta.permission && !auth.can(to.meta.permission as string)) return '/forbidden'
  }
  return true
})

export default router
