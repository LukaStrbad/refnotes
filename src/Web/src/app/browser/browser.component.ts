import { Component, computed, OnDestroy, OnInit, signal, ViewChild } from '@angular/core';
import { BrowserService } from '../../services/browser.service';
import { FormsModule } from '@angular/forms';
import { Directory } from '../../model/directory';
import { NgClass } from '@angular/common';
import { CreateNewModalComponent } from '../components/create-new-modal/create-new-modal.component';
import { HttpEventType } from '@angular/common/http';
import { LoggerService } from '../../services/logger.service';
import {
  filter,
  firstValueFrom,
  forkJoin,
  lastValueFrom,
  Subscription,
  tap,
} from 'rxjs';
import { ActivatedRoute, NavigationEnd, Router, RouterLink } from '@angular/router';
import { TranslateDirective, TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../services/auth.service';
import { TestTagDirective } from '../../directives/test-tag.directive';
import { FileService } from '../../services/file.service';
import { createFromJsFile, File, FileWithTime } from '../../model/file';
import { EditTagsModalComponent } from '../components/modals/edit-tags-modal/edit-tags-modal.component';
import { TagService } from '../../services/tag.service';
import * as fileUtils from '../../utils/file-utils';
import { RenameFileModalComponent } from "../components/modals/rename-file-modal/rename-file-modal.component";
import { joinPaths, splitDirAndName } from '../../utils/path-utils';
import { SelectFileService } from '../../services/select-file.service';
import { NotificationService } from '../../services/notification.service';
import { getTranslation } from '../../utils/translation-utils';
import { AskModalService } from '../../services/ask-modal.service';
import { ByteSizePipe } from '../../pipes/byte-size.pipe';
import { convertDateLocale, updateFileTime } from '../../utils/date-utils';
import { ShareService } from '../../services/components/modals/share.service';
import { ShareModalComponent } from '../components/modals/share/share.component';
import { BrowserFavoriteService } from '../../services/components/browser-favorite.service';
import { FileFavoriteDetails } from '../../model/file-favorite-details';
import { DirectoryFavoriteDetails } from '../../model/directory-favorite-details';

@Component({
  selector: 'app-browser',
  imports: [
    FormsModule,
    NgClass,
    CreateNewModalComponent,
    TranslatePipe,
    TranslateDirective,
    RouterLink,
    TestTagDirective,
    EditTagsModalComponent,
    RenameFileModalComponent,
    ByteSizePipe,
    ShareModalComponent
  ],
  templateUrl: './browser.component.html',
  styleUrl: './browser.component.css',
})
export class BrowserComponent implements OnInit, OnDestroy {
  // Readonly
  protected readonly tagLimit = 3;
  readonly selectedFiles: ReadonlySet<string>;
  readonly groupId?: number;
  readonly linkBasePath: string = '';
  readonly breadcrumbs: BreadcrumbItem[] = [];
  readonly currentFolder = signal<Directory | null>(null);
  readonly files = computed(() => {
    const folder = this.currentFolder();
    const fileFavorites = this.favorite.fileFavorites();
    return folder === null ? [] : this.mapDirectoryFiles(folder, fileFavorites);
  });
  readonly directories = computed(() => {
    const folder = this.currentFolder();
    const folderFavorites = this.favorite.directoryFavorites();
    return folder === null ? [] : this.mapDirectorySubdirectories(folder, folderFavorites);
  });

  // Public
  pathStack: string[] = [];
  currentPath = '/';
  uploadProgress: Record<string, number | null> = {};
  lastCheckedFile: File | null = null;
  areAllFilesSelected = false;
  updateFileTimesInterval?: number;
  fileUtils = fileUtils;

  // Private
  private _dateLang = 'en-UK';
  private navSubscription?: Subscription;

  // Views
  @ViewChild('fileModal')
  fileModal!: CreateNewModalComponent;
  @ViewChild('folderModal')
  folderModal!: CreateNewModalComponent;
  @ViewChild('shareModal')
  shareModal!: ShareModalComponent;

  /**
   * For testing purposes, this property is used to store the promise returned by the refreshRoute method.
   */
  loadingPromise?: Promise<void>;
  /**
   * For testing purposes, this property is used to store the promise from inside the refreshRoute method.
   */
  refreshRouteInnerPromise?: Promise<Directory>;

  private get dateLang(): string {
    return this._dateLang;
  }

  private set dateLang(value: string) {
    this._dateLang = convertDateLocale(value);
  }

  constructor(
    private browser: BrowserService,
    private fileService: FileService,
    private tagService: TagService,
    private logger: LoggerService,
    private router: Router,
    private route: ActivatedRoute,
    private auth: AuthService,
    private selectFileService: SelectFileService,
    private notificationService: NotificationService,
    private translateService: TranslateService,
    private askModal: AskModalService,
    public share: ShareService,
    public favorite: BrowserFavoriteService,
  ) {
    this.selectedFiles = this.selectFileService.selectedFiles;

    this.dateLang = this.translateService.currentLang;
    this.translateService.onLangChange.subscribe((event) => {
      this.dateLang = event.lang;
    });

    const groupId = this.route.snapshot.params['groupId'];
    if (groupId) {
      this.groupId = Number(groupId);
      this.linkBasePath = `/groups/${this.groupId}`;
      this.favorite.setGroupId(this.groupId);
    }
  }

  ngOnInit(): void {
    this.navSubscription = this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe(() => {
        this.loadingPromise = this.refreshRoute().then(this.resetBreadcrumbs.bind(this));
      });

    this.loadingPromise = this.refreshRoute().then(this.resetBreadcrumbs.bind(this));
  }

  ngOnDestroy(): void {
    this.navSubscription?.unsubscribe();
  }

  async refreshRoute() {
    this.currentFolder.set(null);
    // Get the current path from the URL (removes the leading /browser)
    const urlSplit = this.router.url.split('/');
    const browserIndex = urlSplit.indexOf('browser');
    this.pathStack = urlSplit.slice(browserIndex + 1);

    this.currentPath = joinPaths('/', ...this.pathStack);

    const observable = this.browser.listCached(this.currentPath, this.groupId);
    // Cached
    this.refreshRouteInnerPromise = firstValueFrom(observable);
    this.currentFolder.set(await this.refreshRouteInnerPromise);
    // From server
    this.refreshRouteInnerPromise = lastValueFrom(observable);
    this.currentFolder.set(await this.refreshRouteInnerPromise);

    this.areAllFilesSelected = this.checkIfAllFilesAreSelected();

    await this.updateFileTimes();
    clearInterval(this.updateFileTimesInterval);
    // Update file times every minute
    this.updateFileTimesInterval = setInterval(() => {
      this.updateFileTimes();
    }, 1000 * 60);
  }

  private resetBreadcrumbs() {
    this.breadcrumbs.length = 0;
    this.breadcrumbs.push(...this.getBreadcrumbs());
  }

  private getBreadcrumbs(): BreadcrumbItem[] {
    const breadcrumbs: BreadcrumbItem[] = [];

    const path: string[] = [this.linkBasePath + '/browser'];

    for (const stackPart of this.pathStack) {
      path.push(stackPart);
      breadcrumbs.push({
        name: stackPart,
        path: [...path],
        icon: 'folder',
      });
    }
    return breadcrumbs;
  }

  async createNewFile(filename: string) {
    if (this.currentFolder === null) {
      return;
    }

    await this.notificationService.awaitAndNotifyError(
      this.fileService.addTextFile(this.currentPath, filename, '', this.groupId),
      {
        409: await getTranslation(this.translateService, 'error.file-already-exists')
      }
    );

    this.notificationService.success(await getTranslation(this.translateService, 'browser.file-created'));
    this.fileModal.close();
    await this.refreshRoute();
  }

  async createNewFolder(folderName: string) {
    if (this.currentFolder === null) {
      return;
    }

    const path =
      this.currentPath == '/'
        ? `/${folderName}`
        : `${this.currentPath}/${folderName}`;

    await this.notificationService.awaitAndNotifyError(
      this.browser.addDirectory(path, this.groupId),
      {
        409: await getTranslation(this.translateService, 'error.folder-already-exists'),
      }
    );

    this.notificationService.success(await getTranslation(this.translateService, 'browser.folder-created'));
    this.folderModal.close();
    await this.refreshRoute();
  }

  async deleteFile(file: File) {
    await this.notificationService.awaitAndNotifyError(
      this.fileService.deleteFile(this.currentPath, file.name, this.groupId),
      {
        404: await getTranslation(this.translateService, 'error.file-not-found'),
      }
    );

    this.notificationService.success(await getTranslation(this.translateService, 'browser.file-deleted'));
    await this.refreshRoute();
    this.favorite.removeLocalFileFavorite(file);
  }

  async deleteSelectedFiles() {
    const files = [...this.selectedFiles].join(', ');
    const accepted = await this.askModal.confirm('browser.title.modal.delete-files', 'browser.message.modal.delete-files', { translate: true, body: files });

    if (!accepted) {
      return;
    }

    const promises = [...this.selectedFiles].map((file) => {
      const [dir, name] = splitDirAndName(file);
      return this.fileService.deleteFile(dir, name, this.groupId);
    })

    try {
      await this.notificationService.awaitAndNotifyError(Promise.all(promises), {
        default: await getTranslation(this.translateService, 'error.deleting-files'),
      });

      this.notificationService.success(await getTranslation(this.translateService, 'browser.files-deleted'));
    } finally {
      this.selectFileService.clearSelectedFiles();
      // Refresh the route to update the file list as some file might have been deleted
      await this.refreshRoute();
    }
  }

  async openFolder(name: string) {
    const newPath =
      this.currentPath === '/' ? `/${name}` : `${this.currentPath}/${name}`;
    await this.navigateToFolder(newPath);
  }

  async deleteFolder(name: string) {
    const path = joinPaths(this.currentPath, name);

    await this.notificationService.awaitAndNotifyError(this.browser.deleteDirectory(path, this.groupId),
      {
        404: await getTranslation(this.translateService, 'error.folder-not-found'),
      }
    );

    this.notificationService.success(await getTranslation(this.translateService, 'browser.folder-deleted'));
    await this.refreshRoute();
    this.favorite.removeLocalDirectoryFavorite(path);
  }

  async navigateToFolder(path: string) {
    const basePath = this.groupId ? `/groups/${this.groupId}/browser` : '/browser';
    const navigatePath = joinPaths(basePath, path);

    await this.router.navigateByUrl(navigatePath);
  }

  async onFilesUpload(files: FileList) {
    this.uploadProgress = {};
    const observables = [];

    for (const file of Array.from(files)) {
      const uploadObservable = this.fileService
        .addFile(this.currentPath, file, this.groupId)
        .pipe(
          tap((event) => {
            if (event.type === HttpEventType.UploadProgress) {
              this.uploadProgress[file.name] = event.total
                ? Math.round((100 * event.loaded) / event.total)
                : null;
            } else if (event.type === HttpEventType.Response) {
              if (event.status === 200) {
                this.currentFolder.update((folder) => {
                  if (!folder) {
                    return null;
                  }
                  folder?.files.push(createFromJsFile(file, this.currentPath));
                  return { ...folder };
                });
              } else {
                getTranslation(this.translateService, 'error.uploading-file', { name: file.name })
                  .then((translation) => {
                    this.notificationService.error(translation);
                  });
              }
            }
          }),
        );

      observables.push(uploadObservable);
    }

    try {
      await lastValueFrom(forkJoin(observables));
      this.notificationService.success(await getTranslation(this.translateService, 'browser.files-uploaded-successfully'));
    } catch (error) {
      this.logger.error('Error uploading files', error);
    }

    this.fileModal.close();
  }

  isEditable(file: File): boolean {
    return fileUtils.isEditable(file.path);
  }

  getFilePath(file: File): string {
    return joinPaths(this.currentPath, file.path);
  }

  async openPreview(file: File) {
    const path = joinPaths(this.currentPath, file.path);
    const navigationRoute = [];
    navigationRoute.push(this.linkBasePath, 'file', path, 'preview');
    await this.router.navigate(navigationRoute);
  }

  limitTags(tags: string[]): string[] {
    return tags.slice(0, this.tagLimit);
  }

  getRemainingTags(tags: string[]): string[] {
    return tags.slice(this.tagLimit);
  }

  async addTag([fileName, tag]: [string, string]) {
    await this.notificationService.awaitAndNotifyError(this.tagService.addFileTag(this.currentPath, fileName, tag, this.groupId), {
      default: await getTranslation(this.translateService, 'error.add-file-tag'),
    });

    const file = this.files().find((f) => f.path === fileName);
    if (file && !file.tags.includes(tag)) {
      file.tags.push(tag);
    }
  }

  async removeTag([fileName, tag]: [string, string]) {
    await this.notificationService.awaitAndNotifyError(this.tagService.removeFileTag(this.currentPath, fileName, tag, this.groupId), {
      default: await getTranslation(this.translateService, 'error.remove-file-tag'),
    });

    const file = this.files().find((f) => f.path === fileName);
    if (file) {
      const index = file.tags.indexOf(tag);
      if (index !== -1) {
        file.tags.splice(index, 1);
      }
    }
  }

  async renameFile([oldFileName, newFileName]: [string, string]) {
    const oldFilePath = joinPaths(this.currentPath, oldFileName);
    const newFilePath = joinPaths(this.currentPath, newFileName);
    await this.fileService.moveFile(oldFilePath, newFilePath, this.groupId);
    const file = this.files().find((f) => f.path === oldFileName);
    if (file) {
      file.path = newFileName;
      file.modified = new Date();
    }
  }

  toggleFileSelect(file: File, event: MouseEvent) {
    // Prevent text selection when shift is pressed and the checkbox is clicked
    document.getSelection()?.removeAllRanges();

    const target = event.target as HTMLInputElement;
    const shiftPressed = event.shiftKey;

    let files = [file];
    // Add all files between the last checked file and the current one
    if (shiftPressed) {
      if (this.lastCheckedFile === null) {
        this.lastCheckedFile = file;
      }
      const currentFiles = this.files();
      const lastCheckedIndex = currentFiles.findIndex(f => f === this.lastCheckedFile) ?? -1;
      const currentIndex = currentFiles.findIndex(f => f === file) ?? -1;
      if (lastCheckedIndex !== -1 && currentIndex !== -1) {
        const start = Math.min(lastCheckedIndex, currentIndex);
        const end = Math.max(lastCheckedIndex, currentIndex);
        files = currentFiles.slice(start, end + 1) ?? [file];
      }
    }

    files.forEach((file) => {
      const filePath = joinPaths(this.currentPath, file.path);
      if (target.checked) {
        this.selectFileService.addFile(filePath);
      } else {
        this.selectFileService.removeFile(filePath);
      }
    });

    this.lastCheckedFile = file;
    this.areAllFilesSelected = this.checkIfAllFilesAreSelected();
  }

  toggleSelectAllFiles() {
    if (this.currentFolder === null) {
      return;
    }

    // If all files are selected, deselect them
    if (this.areAllFilesSelected) {
      this.files().forEach((file) => {
        const filePath = joinPaths(this.currentPath, file.path);
        this.selectFileService.removeFile(filePath);
      });
      this.areAllFilesSelected = false;
      return;
    }

    // If not all files are selected, select them
    this.files().forEach((file) => {
      const filePath = joinPaths(this.currentPath, file.path);
      this.selectFileService.addFile(filePath);
    });
    this.areAllFilesSelected = true;
  }

  isFileSelected(filename: string): boolean {
    const filePath = joinPaths(this.currentPath, filename);
    return this.selectedFiles.has(filePath);
  }

  private checkIfAllFilesAreSelected(): boolean {
    if (this.currentFolder === null) {
      return false;
    }
    const allFiles = this.files().map(f => joinPaths(this.currentPath, f.path));
    return allFiles.every(file => this.selectedFiles.has(file));
  }

  cancelSelect() {
    this.selectFileService.clearSelectedFiles();
  }

  async moveFiles() {
    const filesFromCurrentFolder = new Set(this.files().map(f => joinPaths(this.currentPath, f.path)));

    // Find only the files that are not in the current folder
    const filesToMove = this.selectedFiles.difference(filesFromCurrentFolder);
    if (filesToMove.size === 0) {
      return;
    }

    try {
      await this.notificationService.awaitAndNotifyError(this.selectFileService.moveFiles(this.currentPath, this.groupId), {
        default: await getTranslation(this.translateService, 'error.move-files'),
      });
    } finally {
      await this.refreshRoute();
    }
  }

  private async updateFileTimes() {
    for (const file of this.files()) {
      await updateFileTime(file, this.translateService, this.dateLang)
    }
  }

  downloadFile(file: File) {
    const filePath = joinPaths(this.currentPath, file.path);
    this.fileService.downloadFile(filePath, this.groupId);
  }

  async openShareModal(file: File) {
    this.share.setFilePath(file.path);
    await this.share.loadPublicLink();
    this.shareModal.show();
  }

  private mapDirectoryFiles(directory: Directory, fileFavorites: FileFavoriteDetails[]): BrowserComponentFile[] {
    return directory.files.map((file: BrowserComponentFile) => {
      const isFavorite = fileFavorites.some(fav => fav.fileInfo.path === file.path && fav.group?.id === this.groupId);
      file.isFavorite = isFavorite;
      return file;
    });
  }

  private mapDirectorySubdirectories(directory: Directory, folderFavorites: DirectoryFavoriteDetails[]): BrowserComponentDirectory[] {
    return directory.directories.map((name) => {
      const path = joinPaths(this.currentPath, name);
      const isFavorite = folderFavorites.some(fav => fav.path === path && fav.group?.id === this.groupId);
      return {
        name,
        path,
        isFavorite,
      };
    });
  }
}

interface BreadcrumbItem {
  name: string;
  path: string[];
  icon: string;
}

interface BrowserComponentFile extends FileWithTime {
  isFavorite?: boolean;
}

interface BrowserComponentDirectory {
  name: string;
  path: string;
  isFavorite: boolean;
}
