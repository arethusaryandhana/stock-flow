import { createRouter, createWebHistory } from 'vue-router'
import Login from '../presentation/Login.vue'
import Dashboard from '../presentation/Dashboard.vue'
import Products from '../presentation/Products.vue'
import StockMovements from '../presentation/StockMovements.vue'
import StockAdjustments from '../presentation/StockAdjustments.vue'
import MasterData from '../presentation/MasterData.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', component: Login },
    { path: '/', component: Dashboard, meta: { auth: true } },
    { path: '/products', component: Products, meta: { auth: true } },
    { path: '/inventory/movements', component: StockMovements, meta: { auth: true } },
    { path: '/inventory/adjustments', component: StockAdjustments, meta: { auth: true } },
    { path: '/master-data', redirect: '/master-data/categories', meta: { auth: true, admin: true } },
    { path: '/master-data/categories', component: MasterData, props: { entity: 'categories' }, meta: { auth: true, admin: true } },
    { path: '/master-data/products', component: MasterData, props: { entity: 'products' }, meta: { auth: true, admin: true } },
    { path: '/master-data/suppliers', component: MasterData, props: { entity: 'suppliers' }, meta: { auth: true, admin: true } },
    { path: '/master-data/customers', component: MasterData, props: { entity: 'customers' }, meta: { auth: true, admin: true } },
  ],
})

router.beforeEach((to) => {
  const token = localStorage.getItem('stockflow_token')
  if (to.meta.auth && !token) return '/login'
  if (to.meta.admin && localStorage.getItem('stockflow_role')?.trim().toLowerCase() !== 'admin') return token ? '/' : '/login'
  return true
})

export default router
