import { useMemo, useState } from "react";

export type SortDirection = "asc" | "desc";

export interface SortState<K extends string> {
  key: K;
  direction: SortDirection;
}

export interface UseSortableTableResult<T, K extends string> {
  sorted: T[];
  sortState: SortState<K>;
  toggleSort: (key: K) => void;
}

/**
 * Generic client-side sort hook.
 *
 * @param rows   The array to sort (typically already filtered).
 * @param defaultKey  The column key to sort by on first render.
 * @param defaultDir  Initial sort direction (defaults to "asc").
 * @param getValue    Optional accessor override: (row, key) => comparable value.
 *                    Defaults to `row[key]`.
 */
export function useSortableTable<T extends Record<string, unknown>, K extends keyof T & string>(
  rows: T[] | undefined,
  defaultKey: K,
  defaultDir: SortDirection = "asc",
  getValue?: (row: T, key: K) => unknown
): UseSortableTableResult<T, K> {
  const [sortState, setSortState] = useState<SortState<K>>({
    key: defaultKey,
    direction: defaultDir,
  });

  const toggleSort = (key: K) => {
    setSortState((prev) =>
      prev.key === key
        ? { key, direction: prev.direction === "asc" ? "desc" : "asc" }
        : { key, direction: "asc" }
    );
  };

  const sorted = useMemo(() => {
    if (!rows) return [];
    const { key, direction } = sortState;
    const multiplier = direction === "asc" ? 1 : -1;

    return [...rows].sort((a, b) => {
      const aVal = getValue ? getValue(a, key) : a[key];
      const bVal = getValue ? getValue(b, key) : b[key];

      if (aVal == null && bVal == null) return 0;
      if (aVal == null) return 1 * multiplier;
      if (bVal == null) return -1 * multiplier;

      if (typeof aVal === "string" && typeof bVal === "string") {
        return aVal.localeCompare(bVal) * multiplier;
      }

      return (aVal < bVal ? -1 : aVal > bVal ? 1 : 0) * multiplier;
    });
  }, [rows, sortState, getValue]);

  return { sorted, sortState, toggleSort };
}
