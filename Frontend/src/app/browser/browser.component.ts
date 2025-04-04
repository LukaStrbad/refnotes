import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
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
import { NavigationEnd, Router, RouterLink } from '@angular/router';
import { TranslateDirective, TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../services/auth.service';
import { TestTagDirective } from '../../directives/test-tag.directive';
import { FileService } from '../../services/file.service';
import { File } from '../../model/file';
import { EditTagsModalComponent } from '../components/modals/edit-tags-modal/edit-tags-modal.component';
import { TagService } from '../../services/tag.service';
import * as fileUtils from '../../utils/file-utils';
import { RenameFileModalComponent } from "../components/modals/rename-file-modal/rename-file-modal.component";
import { joinPaths } from '../../utils/path-utils';
import { MoveFileService } from '../../services/move-file.service';
import { NotificationService } from '../../services/notification.service';
import { getTranslation } from '../../utils/translation-utils';

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
    RenameFileModalComponent
  ],
  templateUrl: './browser.component.html',
  styleUrl: './browser.component.css',
})
export class BrowserComponent implements OnInit, OnDestroy {
  protected readonly tagLimit = 3;

  currentFolder: Directory | null = null;
  @ViewChild('fileModal')
  fileModal!: CreateNewModalComponent;
  @ViewChild('folderModal')
  folderModal!: CreateNewModalComponent;
  pathStack: string[] = [];
  currentPath = '/';
  uploadProgress: Record<string, number | null> = {};
  readonly filesToMove: ReadonlySet<string>;

  private navSubscription?: Subscription;

  /**
   * For testing purposes, this property is used to store the promise returned by the refreshRoute method.
   */
  loadingPromise?: Promise<void>;
  /**
   * For testing purposes, this property is used to store the promise from inside the refreshRoute method.
   */
  refreshRouteInnerPromise?: Promise<Directory>;

  fileUtils = fileUtils;

  get breadcrumbs(): BreadcrumbItem[] {
    const breadcrumbs: BreadcrumbItem[] = [];
    let path = '';
    for (const stackPart of this.pathStack) {
      path += '/' + stackPart;
      breadcrumbs.push({
        name: stackPart,
        path: path,
        icon: 'folder',
      });
    }
    return breadcrumbs;
  }

  constructor(
    private browser: BrowserService,
    private fileService: FileService,
    private tagService: TagService,
    private logger: LoggerService,
    private router: Router,
    private auth: AuthService,
    private moveFileService: MoveFileService,
    private notificationService: NotificationService,
    private translateService: TranslateService,
  ) {
    this.filesToMove = this.moveFileService.filesToMove;
  }

  ngOnInit(): void {
    if (this.auth.user === null) {
      this.router.navigate(['/login']).then();
    }

    this.navSubscription = this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe(() => {
        this.loadingPromise = this.refreshRoute();
      });
    this.loadingPromise = this.refreshRoute();
  }

  ngOnDestroy(): void {
    this.navSubscription?.unsubscribe();
  }

  async refreshRoute() {
    this.currentFolder = null;
    // Get the current path from the URL (removes the leading /browser)
    this.pathStack = this.router.url.split('/').slice(2);
    if (this.pathStack.length === 0) {
      this.currentPath = '/';
    } else {
      this.currentPath = '/' + this.pathStack.join('/');
    }

    const observable = this.browser.listCached(this.currentPath);
    // Cached
    this.refreshRouteInnerPromise = firstValueFrom(observable);
    this.currentFolder = await this.refreshRouteInnerPromise;
    // From server
    this.refreshRouteInnerPromise = lastValueFrom(observable);
    this.currentFolder = await this.refreshRouteInnerPromise;
    console.log(this.currentFolder.files);
  }

  async createNewFile(filename: string) {
    if (this.currentFolder === null) {
      return;
    }

    await this.notificationService.awaitAndNotifyError(
      this.fileService.addTextFile(this.currentPath, filename, ''),
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
      this.browser.addDirectory(path),
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
      this.fileService.deleteFile(this.currentPath, file.name),
      {
        404: await getTranslation(this.translateService, 'error.file-not-found'),
      }
    );

    this.notificationService.success(await getTranslation(this.translateService, 'browser.file-deleted'));
    await this.refreshRoute();
  }

  async openFolder(name: string) {
    const newPath =
      this.currentPath === '/' ? `/${name}` : `${this.currentPath}/${name}`;
    await this.navigateToFolder(newPath);
  }

  async deleteFolder(name: string) {
    const path =
      this.currentPath == '/' ? `/${name}` : `${this.currentPath}/${name}`;

    await this.notificationService.awaitAndNotifyError(this.browser.deleteDirectory(path),
      {
        404: await getTranslation(this.translateService, 'error.folder-not-found'),
      }
    );

    this.notificationService.success(await getTranslation(this.translateService, 'browser.folder-deleted'));
    await this.refreshRoute();
  }

  async navigateToFolder(path: string) {
    if (path === '/') {
      await this.router.navigateByUrl('/browser');
      return;
    }

    await this.router.navigateByUrl(`/browser${path}`);
  }

  async onFilesUpload(files: FileList) {
    this.uploadProgress = {};
    const observables = [];

    for (const file of Array.from(files)) {
      const uploadObservable = this.fileService
        .addFile(this.currentPath, file)
        .pipe(
          tap((event) => {
            if (event.type === HttpEventType.UploadProgress) {
              this.uploadProgress[file.name] = event.total
                ? Math.round((100 * event.loaded) / event.total)
                : null;
            } else if (event.type === HttpEventType.Response) {
              if (event.status === 200) {
                this.currentFolder?.files.push({
                  name: file.name,
                  tags: [],
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

  async openEdit(file: File) {
    await this.router.navigate(['/editor'], {
      queryParams: {
        directory: this.currentPath,
        file: file.name,
      },
    });
  }

  async openPreview(file: File) {
    await this.router.navigate(['/preview'], {
      queryParams: {
        directory: this.currentPath,
        file: file.name,
      },
    });
  }

  limitTags(tags: string[]): string[] {
    return tags.slice(0, this.tagLimit);
  }

  getRemainingTags(tags: string[]): string[] {
    return tags.slice(this.tagLimit);
  }

  async addTag([fileName, tag]: [string, string]) {
    await this.notificationService.awaitAndNotifyError(this.tagService.addFileTag(this.currentPath, fileName, tag), {
      default: await getTranslation(this.translateService, 'error.add-file-tag'),
    });

    const file = this.currentFolder?.files.find((f) => f.name === fileName);
    if (file && !file.tags.includes(tag)) {
      file.tags.push(tag);
    }
  }

  async removeTag([fileName, tag]: [string, string]) {
    await this.notificationService.awaitAndNotifyError(this.tagService.removeFileTag(this.currentPath, fileName, tag), {
      default: await getTranslation(this.translateService, 'error.remove-file-tag'),
    });

    const file = this.currentFolder?.files.find((f) => f.name === fileName);
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
    await this.fileService.moveFile(oldFilePath, newFilePath);
    const file = this.currentFolder?.files.find((f) => f.name === oldFileName);
    if (file) {
      file.name = newFileName;
    }
  }

  toggleFileToMove(filename: string, event: Event) {
    const target = event.target as HTMLInputElement;
    const filePath = joinPaths(this.currentPath, filename);
    if (target.checked) {
      this.moveFileService.addFile(filePath);
    }
    else {
      this.moveFileService.removeFile(filePath);
    }
  }

  isFileToMove(filename: string): boolean {
    const filePath = joinPaths(this.currentPath, filename);
    return this.filesToMove.has(filePath);
  }

  cancelMove() {
    this.moveFileService.clearFilesToMove();
  }

  async moveFiles() {
    const filesFromCurrentFolder = new Set(this.currentFolder?.files.map(f => joinPaths(this.currentPath, f.name)));

    // Find only the files that are not in the current folder
    const filesToMove = this.filesToMove.difference(filesFromCurrentFolder);
    if (filesToMove.size === 0) {
      return;
    }

    try {
      await this.notificationService.awaitAndNotifyError(this.moveFileService.moveFiles(this.currentPath), {
        default: await getTranslation(this.translateService, 'error.move-files'),
      });
    } finally {
      await this.refreshRoute();
    }
  }
}

interface BreadcrumbItem {
  name: string;
  path: string;
  icon: string;
}
