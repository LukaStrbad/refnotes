import { Injectable, signal } from '@angular/core';
import { PublicFileService } from '../../public-file.service';
import { splitDirAndName } from '../../../utils/path-utils';

@Injectable({
  providedIn: 'root'
})
export class ShareService {
  private _directoryPath = signal('');
  private _fileName = signal('');
  private _filePath = signal('');
  private _isPublic = signal(false);
  private _publicLink = signal<string | null>(null);

  directoryPath = this._directoryPath.asReadonly();
  fileName = this._fileName.asReadonly();
  filePath = this._filePath.asReadonly();
  isPublic = this._isPublic.asReadonly();
  publicLink = this._publicLink.asReadonly();

  constructor(
    private publicFileService: PublicFileService,
  ) { }

  async setPublicState(isPublic: boolean) {
    this._isPublic.set(isPublic);
    if (isPublic) {
      this._publicLink.set(await this.publicFileService.createPublicFile(this.filePath()));
    } else {
      await this.publicFileService.deletePublicFile(this.filePath());
      this._publicLink.set(null); // Clear public link when not public
    }
  }

  setFilePath(filePath: string) {
    this._filePath.set(filePath);
    const [directoryPath, fileName] = splitDirAndName(filePath);
    this._directoryPath.set(directoryPath);
    this._fileName.set(fileName);
    this._publicLink.set(null); // Reset public link when file path changes
  }

  async loadPublicLink() {
    const publicUrl = await this.publicFileService.getUrl(this.filePath());
    this._publicLink.set(publicUrl);
    this._isPublic.set(publicUrl !== null); // If publicUrl is null, not public
  }
}
