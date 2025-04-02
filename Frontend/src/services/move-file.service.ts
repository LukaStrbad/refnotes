import { Injectable } from '@angular/core';
import { FileService } from './file.service';
import { joinPaths } from '../utils/path-utils';

@Injectable({
  providedIn: 'root'
})
export class MoveFileService {
  private readonly _filesToMove = new Set<string>();

  get filesToMove(): ReadonlySet<string> {
    return this._filesToMove;
  }

  constructor(
    private fileService: FileService
  ) { }

  addFile(filePath: string) {
    this._filesToMove.add(filePath);
  }

  removeFile(filePath: string) {
    return this._filesToMove.delete(filePath);
  }

  clearFilesToMove() {
    this._filesToMove.clear();
  }

  async moveFiles(destination: string) {
    const movePromises = Array.from(this._filesToMove).map((file) => {
      const fileName = file.split('/').pop() || '';
      const newFilePath = joinPaths(destination, fileName);
      return this.fileService.moveFile(file, newFilePath);
    });
    await Promise.all(movePromises);
    this.clearFilesToMove();
  }
}
