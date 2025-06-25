import { File } from './file';

export interface Directory {
  name: string;
  files: File[];
  directories: string[];
}
