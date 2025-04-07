import { Injectable } from '@angular/core';
import { FileService } from './file.service';
import { joinPaths } from '../utils/path-utils';

@Injectable({
  providedIn: 'root'
})
export class SelectFileService {
  private readonly _selectedFiles = new Set<string>();

  get selectedFiles(): ReadonlySet<string> {
    return this._selectedFiles;
  }

  constructor(
    private fileService: FileService
  ) { }

  addFile(filePath: string) {
    this._selectedFiles.add(filePath);
  }

  removeFile(filePath: string) {
    return this._selectedFiles.delete(filePath);
  }

  clearSelectedFiles() {
    this._selectedFiles.clear();
  }

  async moveFiles(destination: string) {
    const movePromises = Array.from(this._selectedFiles).map((file) => {
      const fileName = file.split('/').pop() || '';
      const newFilePath = joinPaths(destination, fileName);
      return this.fileService.moveFile(file, newFilePath);
    });
    await Promise.all(movePromises);
    this.clearSelectedFiles();
  }
}
