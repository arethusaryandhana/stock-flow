import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import test from 'node:test'
import ts from 'typescript'

const policySource = readFileSync(new URL('../src/accessPolicy.ts', import.meta.url), 'utf8')
const policyJavaScript = ts.transpileModule(policySource, {
  compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 },
}).outputText
const { hasPermission, visibleMenuGroups } = await import(
  `data:text/javascript,${encodeURIComponent(policyJavaScript)}`
)

test('sidebar menus and page access follow each role permission set', () => {
  const groups = [
    { label: 'Access', items: [
      { path: '/admin/users', permission: 'menu.users' },
      { path: '/admin/roles', permission: 'menu.roles' },
      { path: '/admin/access-history', permission: 'menu.access-history' },
    ] },
    { label: 'Operations', items: [
      { path: '/operations/purchase-orders', permission: 'menu.purchase-orders' },
    ] },
  ]
  const scenarios = [
    { role: 'Admin', permissions: ['menu.users', 'menu.roles', 'menu.access-history', 'menu.purchase-orders', 'action.users.manage'], visible: 4, canManageUser: true },
    { role: 'Manager', permissions: ['menu.purchase-orders'], visible: 1, canManageUser: false },
    { role: 'Staff', permissions: ['menu.purchase-orders', 'menu.access-history'], visible: 2, canManageUser: false },
    { role: 'Custom read only', permissions: ['menu.users'], visible: 1, canManageUser: false },
  ]

  for (const scenario of scenarios) {
    const visible = visibleMenuGroups(groups, scenario.permissions).flatMap((group) => group.items)
    assert.equal(visible.length, scenario.visible, scenario.role)
    for (const item of groups.flatMap((group) => group.items)) {
      assert.equal(visible.some((entry) => entry.path === item.path),
        hasPermission(scenario.permissions, item.permission), `${scenario.role}: ${item.path}`)
    }
    assert.equal(hasPermission(scenario.permissions, 'action.users.manage'),
      scenario.canManageUser, scenario.role)
  }
})

test('every sidebar path has the same page permission', () => {
  const app = readFileSync(new URL('../src/App.vue', import.meta.url), 'utf8')
  const router = readFileSync(new URL('../src/router/index.ts', import.meta.url), 'utf8')
  const menuPairs = [...app.matchAll(/path: '([^']+)'[^\n]*permission: '(menu\.[^']+)'/g)]
    .map((match) => [match[1], match[2]])
  const routePairs = [...router.matchAll(/path: '([^']+)'[^\n]*permission: '(menu\.[^']+)'/g)]
    .map((match) => [match[1], match[2]])

  assert.equal(menuPairs.length, 19)
  assert.equal(new Set(menuPairs.map(([path]) => path)).size, menuPairs.length)
  assert.deepEqual(menuPairs.sort(), routePairs.sort())

  const catalog = readFileSync(new URL('../../../backend/StockFlow.Core/PermissionCatalog.cs', import.meta.url), 'utf8')
  const catalogMenus = [...catalog.matchAll(/new\("(menu\.[^"]+)",/g)].map((match) => match[1])
  assert.deepEqual(menuPairs.map(([, permission]) => permission).sort(), catalogMenus.sort())
})
