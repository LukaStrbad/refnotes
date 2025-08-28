export interface SharedFile {
  id: number;
  sharedEncryptedFile: {
    id: number;
    name: string;
    size: number;
    created: Date;
    modified: Date;
    encryptedDirectoryId: number;
  };
  sharedToDirectory: {
    id: number;
    name: string;
    path: string;
  };
  created: Date;
}