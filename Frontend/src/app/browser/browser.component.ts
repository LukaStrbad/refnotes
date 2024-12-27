import {Component, ViewChild} from '@angular/core';
import {BrowserService} from '../../services/browser.service';
import {FormsModule} from "@angular/forms";
import {Directory} from "../../model/directory";
import {NgClass} from "@angular/common";
import {CreateNewModalComponent} from "../components/create-new-modal/create-new-modal.component";
import {HttpEventType} from "@angular/common/http";
import {LoggerService} from "../../services/logger.service";
import {forkJoin, lastValueFrom, tap} from "rxjs";

@Component({
    selector: 'app-browser',
    imports: [
        FormsModule,
        NgClass,
        CreateNewModalComponent
    ],
    templateUrl: './browser.component.html',
    styleUrl: './browser.component.scss'
})
export class BrowserComponent {
  currentFolder: Directory | null = null;
  @ViewChild("fileModal")
  fileModal!: CreateNewModalComponent;
  @ViewChild("folderModal")
  folderModal!: CreateNewModalComponent;
  pathStack: string[] = [];
  uploadProgress: { [key: string]: number | null } = {};

  get currentPath(): string {
    if (this.pathStack.length === 0) {
      return '/';
    }
    return '/' + this.pathStack.join('/');
  }

  get breadcrumbs(): BreadcrumbItem[] {
    const breadcrumbs: BreadcrumbItem[] = [{
      name: 'Home',
      path: '/'
    }];
    let path = '';
    for (let i = 0; i < this.pathStack.length; i++) {
      path += '/' + this.pathStack[i];
      breadcrumbs.push({
        name: this.pathStack[i],
        path: path
      });
    }
    return breadcrumbs;
  }

  constructor(
    private browser: BrowserService,
    private logger: LoggerService
  ) {
    browser.list(this.currentPath).then((currentFolder) => {
      this.currentFolder = currentFolder;
    });
  }

  async createNewFile(filename: string) {
    if (this.currentFolder === null) {
      return;
    }
    await this.browser.addTextFile(this.currentPath, filename, '');
    this.currentFolder.files.push(filename);
    this.fileModal.close();
  }

  async createNewFolder(folderName: string) {
    if (this.currentFolder === null) {
      return;
    }

    const path = this.currentPath == '/' ? `/${folderName}` : `${this.currentPath}/${folderName}`;
    await this.browser.addDirectory(path);
    this.currentFolder.directories.push(folderName);
    this.folderModal.close();
  }

  async deleteFile(name: string) {
    await this.browser.deleteFile(this.currentPath, name);
    this.currentFolder = await this.browser.list();
  }

  async openFolder(name: string) {
    this.pathStack.push(name);
    this.currentFolder = await this.browser.list(this.currentPath);
  }

  async deleteFolder(name: string) {
    const path = this.currentPath == '/' ? `/${name}` : `${this.currentPath}/${name}`;
    await this.browser.deleteDirectory(path);
    this.currentFolder = await this.browser.list();
  }

  async navigateToFolder(path: string) {
    if (path === '/') {
      this.pathStack = [];
    } else {
      const stack = path.split('/');
      stack.shift();
      this.pathStack = stack;
    }

    this.currentFolder = await this.browser.list(this.currentPath);
  }

  isTextFile(name: string) {
    return name.endsWith('.txt');
  }

  isMarkdownFile(name: string) {
    return name.endsWith('.md') || name.endsWith('.markdown');
  }

  async onFilesUpload(files: FileList) {
    this.uploadProgress = {};
    const observables = [];

    for (let i = 0; i < files.length; i++) {
      this.logger.info(`Uploading file ${files[i].name}`);
      const uploadObservable = this.browser.addFile(this.currentPath, files[i])
        .pipe(tap(event => {
          if (event.type === HttpEventType.UploadProgress) {
            this.uploadProgress[files[i].name] = event.total ? Math.round(100 * event.loaded / event.total) : null;
          } else if (event.type === HttpEventType.Response) {
            if (event.status === 200) {
              this.logger.info('File uploaded successfully', event);
              this.currentFolder?.files.push(files[i].name);
            } else {
              console.error('Error uploading file', event);
            }
          }
        }))

      observables.push(uploadObservable);
    }

    try {
      await lastValueFrom(forkJoin(observables));
    } catch (error) {
      console.error('Error uploading files', error);
    }

    this.fileModal.close();
  }
}

interface BreadcrumbItem {
  name: string;
  path: string;
}
