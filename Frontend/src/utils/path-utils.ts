export function resolveRelativeFolderPath(root: string, relativePath: string): string {
  // If the relative path is an absolute path, return it as is
  if (relativePath.startsWith('/')) {
    return relativePath;
  }

  const rootParts = root.split('/').filter(part => part.length > 0);
  const relativeParts = relativePath.split('/').filter(part => part.length > 0);

  for (const part of relativeParts) {
    if (part === '..') {
      rootParts.pop();
    } else if (part !== '.') {
      rootParts.push(part);
    }
  }

  return `/${rootParts.join('/')}`;
}

/**
 * Splits a path into directory and file name
 * @param path The path to split
 */
export function splitDirAndName(path: string): [string, string] {
  const parts = path.split('/').filter(p => p.length > 0);
  if (parts.length === 0) {
    return ['/', ''];
  }

  const name = parts.pop()!;
  const dir = parts.length === 0 ? '/' : `/${parts.join('/')}`;
  return [dir, name];
}
