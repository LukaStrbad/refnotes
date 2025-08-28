import { Component, OnInit, signal } from '@angular/core';
import { FileService } from '../../services/file.service';
import { SharedFile } from '../../model/shared-file';
import { LoadingState } from '../../model/loading-state';
import { TranslatePipe } from '@ngx-translate/core';
import { NgClass, DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { ByteSizePipe } from '../../pipes/byte-size.pipe';

@Component({
  selector: 'app-shared-files',
  imports: [TranslatePipe, NgClass, DatePipe, ByteSizePipe],
  templateUrl: './shared-files.component.html',
  styleUrl: './shared-files.component.css'
})
export class SharedFilesComponent implements OnInit {
  readonly sharedFiles = signal<SharedFile[]>([]);
  readonly loadingState = signal<LoadingState>(LoadingState.Loading);
  readonly LoadingState = LoadingState;

  constructor(
    private fileService: FileService,
    private router: Router
  ) {}

  async ngOnInit() {
    try {
      this.loadingState.set(LoadingState.Loading);
      const files = await this.fileService.getSharedFiles();
      this.sharedFiles.set(files);
      this.loadingState.set(LoadingState.Loaded);
    } catch (error) {
      console.error('Error loading shared files:', error);
      this.loadingState.set(LoadingState.Error);
    }
  }

  async openFile(sharedFile: SharedFile) {
    // Create the path to the shared file within the directory it was shared to
    const filePath = `${sharedFile.sharedToDirectory.path}/${sharedFile.sharedEncryptedFile.name}`;
    await this.router.navigate(['/file', filePath, 'preview']);
  }

  getFileIcon(fileName: string): string {
    if (fileName.endsWith('.md')) {
      return 'bi-filetype-md';
    } else if (fileName.match(/\.(txt|log)$/i)) {
      return 'bi-file-text';
    } else if (fileName.match(/\.(jpg|jpeg|png|gif|bmp|svg)$/i)) {
      return 'bi-file-image';
    } else {
      return 'bi-file-earmark';
    }
  }
}