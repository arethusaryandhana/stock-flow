export function hasPermission(permissions: readonly string[], required: string): boolean {
  return permissions.includes(required)
}

export function visibleMenuGroups<T extends { items: readonly { permission: string }[] }>(
  groups: readonly T[], permissions: readonly string[],
): Array<T & { items: T['items'][number][] }> {
  return groups.map((group) => ({
    ...group,
    items: group.items.filter((item) => hasPermission(permissions, item.permission)),
  })).filter((group) => group.items.length > 0)
}
