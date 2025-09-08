import { Injectable, signal } from '@angular/core';
import { PublicFileService } from '../../public-file.service';
import { splitDirAndName } from '../../../utils/path-utils';
import { FileService } from '../../file.service';

@Injectable({
  providedIn: 'root'
})
export class ShareService {
  private readonly _directoryPath = signal('');
  private readonly _fileName = signal('');
  private readonly _filePath = signal('');
  private readonly _isPublic = signal(false);
  private readonly _publicLink = signal<string | null>(null);
  private readonly _userShareLink = signal<string | null>(null);

  readonly directoryPath = this._directoryPath.asReadonly();
  readonly fileName = this._fileName.asReadonly();
  readonly filePath = this._filePath.asReadonly();
  readonly isPublic = this._isPublic.asReadonly();
  readonly publicLink = this._publicLink.asReadonly();
  readonly userShareLink = this._userShareLink.asReadonly();

  constructor(
    private publicFileService: PublicFileService,
    private fileService: FileService,
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

  async loadUserShareLink() {
    const userShareUrl = await this.fileService.shareFile(this.filePath());
    this._userShareLink.set(userShareUrl);
  }
}
