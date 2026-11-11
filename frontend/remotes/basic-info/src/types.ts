/** Describes one Basic Information list for the shared editor (CatalogueList.vue). */
export interface ListSpec {
  /** URL name of the list in reference-svc: /api/reference/{list} */
  list: string
  title: [string, string]
  singular: [string, string]
  hasCode?: boolean
  parent?: { list: string; label: [string, string] }
  /** The parent's parent (the country on the city screen): chosen first, it narrows the parent list. */
  grandparent?: { list: string; label: [string, string] }
}
