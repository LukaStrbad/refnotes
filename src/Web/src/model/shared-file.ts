export interface SharedFile {
  sharedFileId: number;
  name: string;
  path: string;
  tags: string[];
  size: number;
  created: Date;
  modified: Date;
}

export interface SharedFileWithTime extends SharedFile {
  createdLong?: string;
  createdShort?: string;
  modifiedLong?: string;
  modifiedShort?: string;
}
