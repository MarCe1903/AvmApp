const DEFAULT_PRIORITY_KEYS = ['data', 'Data', 'result', 'value', '$values', 'items', 'Items'];

function findArray<T>(value: unknown, priorityKeys: readonly string[]): T[] | null {
  if (Array.isArray(value)) {
    return value as T[];
  }

  if (!value || typeof value !== 'object') {
    return null;
  }

  const record = value as Record<string, unknown>;

  for (const key of priorityKeys) {
    if (!(key in record)) {
      continue;
    }

    const found = findArray<T>(record[key], priorityKeys);
    if (found) {
      return found;
    }
  }

  for (const nested of Object.values(record)) {
    const found = findArray<T>(nested, priorityKeys);
    if (found) {
      return found;
    }
  }

  return null;
}

export function extractArray<T>(value: unknown, priorityKeys: readonly string[] = DEFAULT_PRIORITY_KEYS): T[] {
  return findArray<T>(value, priorityKeys) ?? [];
}
