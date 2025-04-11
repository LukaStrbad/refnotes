export interface File {
  name: string;
  tags: string[];
  size: number;
  created: Date;
  modified: Date;
}

export function createFromJsFile(file: globalThis.File): File {
  return {
    name: file.name,
    tags: [],
    size: file.size,
    created: new Date(),
    modified: new Date(),
  };
}
