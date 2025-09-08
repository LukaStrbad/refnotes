import { File } from './file';
import { SharedFile } from './shared-file';

export interface Directory {
  name: string;
  files: File[];
  sharedFiles: SharedFile[];
  directories: string[];
}
