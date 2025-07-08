import { AfterViewInit, ChangeDetectorRef, Component, effect, ElementRef, OnDestroy, ViewChild, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FileService } from '../../services/file.service';
import { TagService } from '../../services/tag.service';
import { TranslateDirective, TranslatePipe, TranslateService } from '@ngx-translate/core';
import * as fileUtils from '../../utils/file-utils';
import { MarkdownHighlighter } from '../../utils/markdown-highlighter';
import { SettingsService } from '../../services/settings.service';
import { NgClass } from '@angular/common';
import { getImageBlobUrl } from '../../utils/image-utils';
import { TestTagDirective } from '../../directives/test-tag.directive';
import { NotificationService } from '../../services/notification.service';
import { getTranslation } from '../../utils/translation-utils';
import { FileWithTime } from '../../model/file';
import { joinPaths, splitDirAndName } from '../../utils/path-utils';
import { ByteSizePipe } from '../../pipes/byte-size.pipe';
import { updateFileTime } from '../../utils/date-utils';
import { FileProvider } from './file-provider';
import { LoggerService } from '../../services/logger.service';
import { getStatusCode } from '../../utils/errorHandler';
import { HomeButtonComponent } from "../components/home-button/home-button.component";
import { FileLoadingState } from '../../model/loading-state';

@Component({
  selector: 'app-file-preview',
  imports: [TranslatePipe, TranslateDirective, NgClass, TestTagDirective, ByteSizePipe, HomeButtonComponent],
  templateUrl: './file-preview.component.html',
  styleUrl: './file-preview.component.css',
  encapsulation: ViewEncapsulation.None,
})
export class FilePreviewComponent implements OnDestroy, AfterViewInit {
  private markdownHighlighter?: MarkdownHighlighter;
  fileName?: string;
  readonly groupId?: number;
  readonly fileProvider: FileProvider;
  readonly isPublicFile: boolean;

  loadingState: FileLoadingState = FileLoadingState.Loading;
  tags: string[] = [];
  fileType: fileUtils.FileType = 'unknown';
  imageSrc: string | null = null;
  fileInfo: FileWithTime | null = null;

  fileUtils = fileUtils;

  @ViewChild('previewRef') previewContentElement!: ElementRef<HTMLElement>;

  LoadingState = FileLoadingState;

  constructor(
    route: ActivatedRoute,
    tagService: TagService,
    private log: LoggerService,
    private fileService: FileService,
    public settings: SettingsService,
    private changeDetector: ChangeDetectorRef,
    private translate: TranslateService,
    private notificationService: NotificationService,
  ) {
    const publicFileHash = route.snapshot.paramMap.get('publicHash');
    if (publicFileHash) {
      this.fileProvider = FileProvider.createPublicFileProvider(
        fileService,
        publicFileHash,
      );
      this.isPublicFile = true;
    } else {
      const path = route.snapshot.paramMap.get('path') as string;
      const groupId = route.snapshot.paramMap.get('groupId');
      if (groupId) {
        this.groupId = Number(groupId);
      }

      this.fileProvider = FileProvider.createRegularFileProvider(
        fileService,
        tagService,
        path,
        this.groupId,
      );
      this.isPublicFile = false;
    }

    effect(() => {
      const showLineNumbers = this.settings.mdEditor().showLineNumbers;
      if (!this.markdownHighlighter) {
        return;
      }
      this.markdownHighlighter.showLineNumbers = showLineNumbers;
    });
  }

  ngAfterViewInit(): void {
    this.init().catch(error => {
      const statusCode = getStatusCode(error);
      if (statusCode === 404) {
        this.loadingState = FileLoadingState.NotFound;
      }
    });
  }

  private async init() {
    const path = await this.fileProvider.filePath;
    const [, fileName] = splitDirAndName(path);
    this.fileName = fileName;
    this.fileType = fileUtils.getFileType(this.fileName);

    this.markdownHighlighter = await this.fileProvider.createMarkdownHighlighter(
      this.settings.mdEditor().showLineNumbers,
      (dirPath: string, fileName: string) => this.fileProvider.getImage(joinPaths(dirPath, fileName)),
    );

    const promises: Promise<void>[] = [];

    if (this.fileType === 'image') {
      promises.push(this.loadImage());
    } else if (this.fileType === 'markdown' || this.fileType === 'text') {
      promises.push(this.loadFile());
    }

    const listTagsPromise = this.fileProvider.listTags().then(tags => {
      this.tags = tags;
    }).catch(async () => {
      this.notificationService.error(await getTranslation(this.translate, 'error.load-file-tags'));
    });
    promises.push(listTagsPromise);

    const fileInfoPromise = this.fileProvider.getFileInfo().then(async fileInfo => {
      this.fileInfo = await updateFileTime(fileInfo, this.translate, this.translate.currentLang);
    });
    promises.push(fileInfoPromise);

    await Promise.all(promises);
  }

  ngOnDestroy(): void {
    if (this.imageSrc) {
      URL.revokeObjectURL(this.imageSrc);
    }
  }

  async loadFile() {
    if (!this.markdownHighlighter) {
      throw new Error('MarkdownHighlighter is not initialized');
    }

    try {
      const content = await this.fileProvider.getFile();
      const text = new TextDecoder().decode(content);
      if (this.fileType === 'markdown') {
        const markdown = this.markdownHighlighter.parse(text) as string;
        this.loadingState = FileLoadingState.Loaded;
        this.changeDetector.detectChanges();
        this.previewContentElement.nativeElement.innerHTML = markdown;
        this.updateImages();
      } else if (this.fileType === 'text') {
        this.loadingState = FileLoadingState.Loaded;
        this.changeDetector.detectChanges();
        this.previewContentElement.nativeElement.innerHTML = text;
      }
    } catch (error) {
      const statusCode = getStatusCode(error);
      if (statusCode === 404) {
        this.loadingState = FileLoadingState.NotFound;
      } else {
        this.loadingState = FileLoadingState.Error;
        this.log.error('Error loading file:', error);
        this.notificationService.error(await getTranslation(this.translate, 'error.load-file'));
      }
    }
  }

  async loadImage() {
    const filePath = await this.fileProvider.filePath;
    const [directoryPath, fileName] = splitDirAndName(filePath);

    try {
      const data = await this.fileService.getImage(directoryPath, fileName, this.groupId);
      if (!data) {
        return;
      }

      this.imageSrc = getImageBlobUrl(fileName, data);
      this.loadingState = FileLoadingState.Loaded;
    } catch {
      this.notificationService.error(await getTranslation(this.translate, 'error.load-image'));
    };
  }

  /**
   * Updates the image elements with the correct image source
   */
  private updateImages() {
    this.markdownHighlighter?.updateImages(this.previewContentElement);
  }
}
