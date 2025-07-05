export interface File {
  name: string;
  path: string;
  tags: string[];
  size: number;
  created: Date;
  modified: Date;
}

export type FileInfo = File;

export interface FileWithTime extends File {
  createdLong?: string;
  createdShort?: string;
  modifiedLong?: string;
  modifiedShort?: string;
}

export function createFromJsFile(file: globalThis.File, directoryPath: string): File {
  return {
    name: file.name,
    path: `${directoryPath}/${file.name}`,
    tags: [],
    size: file.size,
    created: new Date(),
    modified: new Date(),
  };
}
