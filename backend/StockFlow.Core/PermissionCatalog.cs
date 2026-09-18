namespace StockFlow.Core;

public sealed record PermissionDefinition(
    string Code,
    string Group,
    string Name,
    PermissionKind Kind,
    bool ManagerDefault = false,
    bool StaffDefault = false);

public static class PermissionCatalog
{
    public static IReadOnlyDictionary<string, string> RequiredMenuForAction { get; } =
        new Dictionary<string, string>
        {
            ["action.categories.manage"] = "menu.master.categories",
            ["action.products.manage"] = "menu.master.products",
            ["action.suppliers.manage"] = "menu.master.suppliers",
            ["action.customers.manage"] = "menu.master.customers",
            ["action.audit.view"] = "menu.audit",
            ["action.users.manage"] = "menu.users",
            ["action.roles.manage"] = "menu.roles",
            ["action.access.manage"] = "menu.access",
            ["action.inventory.adjust"] = "menu.adjustments",
            ["action.inventory.settings"] = "menu.settings",
            ["action.purchasing.manage"] = "menu.purchase-orders",
            ["action.receiving.manage"] = "menu.receiving",
            ["action.sales.manage"] = "menu.sales-orders",
            ["action.reports.export"] = "menu.reports",
            ["action.company.manage"] = "menu.settings"
        };

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new("menu.dashboard", "workspace", "Dashboard", PermissionKind.Menu, true, true),
        new("menu.master.categories", "administration", "Kategori", PermissionKind.Menu),
        new("menu.master.products", "administration", "Master produk", PermissionKind.Menu),
        new("menu.master.suppliers", "administration", "Master supplier", PermissionKind.Menu),
        new("menu.master.customers", "administration", "Master customer", PermissionKind.Menu),
        new("menu.audit", "administration", "Riwayat audit", PermissionKind.Menu),
        new("menu.users", "access", "Pengguna", PermissionKind.Menu),
        new("menu.roles", "access", "Role", PermissionKind.Menu),
        new("menu.access", "access", "Akses menu", PermissionKind.Menu),
        new("menu.products", "inventory", "Produk dan stok", PermissionKind.Menu, true, true),
        new("menu.movements", "inventory", "Pergerakan stok", PermissionKind.Menu, true, true),
        new("menu.adjustments", "inventory", "Penyesuaian stok", PermissionKind.Menu, true, true),
        new("menu.purchase-orders", "operations", "Purchase order", PermissionKind.Menu, true, true),
        new("menu.sales-orders", "operations", "Sales order", PermissionKind.Menu, true, true),
        new("menu.receiving", "operations", "Penerimaan barang", PermissionKind.Menu, true, true),
        new("menu.suppliers", "operations", "Supplier", PermissionKind.Menu, true, true),
        new("menu.reports", "insight", "Laporan", PermissionKind.Menu, true, true),
        new("menu.settings", "insight", "Pengaturan", PermissionKind.Menu, true, true),
        new("action.categories.manage", "administration", "Kelola kategori", PermissionKind.Action),
        new("action.products.manage", "administration", "Kelola master produk", PermissionKind.Action),
        new("action.suppliers.manage", "administration", "Kelola supplier", PermissionKind.Action),
        new("action.customers.manage", "administration", "Kelola customer", PermissionKind.Action),
        new("action.audit.view", "administration", "Lihat riwayat audit", PermissionKind.Action),
        new("action.users.manage", "access", "Kelola pengguna", PermissionKind.Action),
        new("action.roles.manage", "access", "Kelola role", PermissionKind.Action),
        new("action.access.manage", "access", "Kelola izin role", PermissionKind.Action),
        new("action.inventory.adjust", "inventory", "Buat penyesuaian stok", PermissionKind.Action, true),
        new("action.inventory.settings", "inventory", "Kelola pengaturan inventori", PermissionKind.Action),
        new("action.purchasing.manage", "operations", "Kelola purchase order", PermissionKind.Action, true),
        new("action.receiving.manage", "operations", "Catat penerimaan barang", PermissionKind.Action, true),
        new("action.sales.manage", "operations", "Kelola sales order", PermissionKind.Action, true),
        new("action.reports.export", "insight", "Ekspor laporan", PermissionKind.Action, true, true),
        new("action.company.manage", "insight", "Kelola profil perusahaan", PermissionKind.Action)
    ];

    public static IReadOnlyList<string> DefaultsFor(string roleName) => roleName switch
    {
        "Admin" => All.Select(permission => permission.Code).ToArray(),
        "Manager" => All.Where(permission => permission.ManagerDefault)
            .Select(permission => permission.Code).ToArray(),
        "Staff" => All.Where(permission => permission.StaffDefault)
            .Select(permission => permission.Code).ToArray(),
        _ => []
    };
}
