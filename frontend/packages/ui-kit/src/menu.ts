/**
 * The sub-menu of an area (owner, 20 Nov 2026: the design he approved replaces the row of tabs of «دریافت‌ها» and the plain list of
 * «اطلاعات پایه» by one white column beside the main menu, its items under small group titles). The order of the items is the order
 * the owner gave; the group titles only sit between them.
 */
export interface MenuItem<Id extends string = string> {
  id: Id
  /** [Farsi, English] */
  title: readonly [string, string]
  /** Drawn in a coloured box (owner's report no. 6, item 12: «فهرست جامع دریافت‌ها»). */
  boxed?: boolean
}
export interface MenuGroup<Id extends string = string> {
  /** [Farsi, English]; a group without a title stands alone (the boxed item). */
  title?: readonly [string, string]
  items: readonly MenuItem<Id>[]
}

/** Every item of the menu, in the order shown. */
export function menuItems<Id extends string>(groups: readonly MenuGroup<Id>[]): MenuItem<Id>[] {
  return groups.flatMap(g => [...g.items])
}

/** The path to an item for the line shown when the sub-menu is closed: area ‹ group ‹ item. */
export function menuPath<Id extends string>(area: string, groups: readonly MenuGroup<Id>[], id: Id, fa: boolean): string[] {
  const pick = (x: readonly [string, string]) => (fa ? x[0] : x[1])
  for (const g of groups) {
    const item = g.items.find(i => i.id === id)
    if (item) return g.title && pick(g.title) !== pick(item.title) ? [area, pick(g.title), pick(item.title)] : [area, pick(item.title)]
  }
  return [area]
}
