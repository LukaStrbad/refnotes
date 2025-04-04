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

@Component({
  selector: 'app-file-preview',
  imports: [TranslatePipe, TranslateDirective, NgClass, TestTagDirective],
  templateUrl: './file-preview.component.html',
  styleUrl: './file-preview.component.css',
  encapsulation: ViewEncapsulation.None,
})
export class FilePreviewComponent implements OnDestroy, AfterViewInit {
  private readonly markdownHighlighter: MarkdownHighlighter;
  readonly directoryPath: string;
  readonly fileName: string;

  loading = true;
  tags: string[] = [];
  fileType: fileUtils.FileType = 'unknown';
  imageSrc: string | null = null;

  fileUtils = fileUtils;

  @ViewChild('previewRef') previewContentElement!: ElementRef<HTMLElement>;

  constructor(
    route: ActivatedRoute,
    private fileService: FileService,
    private tagService: TagService,
    public settings: SettingsService,
    private changeDetector: ChangeDetectorRef,
    private translate: TranslateService,
    private notificationService: NotificationService,
  ) {
    this.directoryPath = route.snapshot.queryParamMap.get(
      'directory',
    ) as string;
    this.fileName = route.snapshot.queryParamMap.get('file') as string;
    this.fileType = fileUtils.getFileType(this.fileName);

    this.markdownHighlighter = new MarkdownHighlighter(
      this.settings.mdEditor().showLineNumbers,
      this.directoryPath,
      fileService,
    );

    effect(() => {
      const showLineNumbers = this.settings.mdEditor().showLineNumbers;
      this.markdownHighlighter.showLineNumbers = showLineNumbers;
    });
  }

  ngAfterViewInit(): void {
    if (this.fileType === 'image') {
      this.loadImage();
    } else if (this.fileType === 'markdown' || this.fileType === 'text') {
      this.loadFile();
    }
  }

  ngOnDestroy(): void {
    if (this.imageSrc) {
      URL.revokeObjectURL(this.imageSrc);
    }
  }

  loadFile() {
    this.fileService.getFile(this.directoryPath, this.fileName)
      .then((content) => {
        const text = new TextDecoder().decode(content);
        if (this.fileType === 'markdown') {
          const markdown = this.markdownHighlighter.parse(text) as string;
          this.loading = false;
          this.changeDetector.detectChanges();
          this.previewContentElement.nativeElement.innerHTML = markdown;
          this.updateImages();
        } else if (this.fileType === 'text') {
          this.loading = false;
          this.previewContentElement.nativeElement.innerHTML = text;
        }
      }, async () => {
        this.notificationService.error(await getTranslation(this.translate, 'error.load-file'));
      });

    this.tagService.listFileTags(this.directoryPath, this.fileName)
      .then((tags) => {
        this.tags = tags;
      }, async () => {
        this.notificationService.error(await getTranslation(this.translate, 'error.load-file-tags'));
      });
  }

  loadImage() {
    this.fileService.getImage(this.directoryPath, this.fileName)
      .then((data) => {
        if (!data) {
          return;
        }

        this.imageSrc = getImageBlobUrl(this.fileName, data);
        this.loading = false;
      }, async () => {
        this.notificationService.error(await getTranslation(this.translate, 'error.load-image'));
      });
  }

  /**
   * Updates the image elements with the correct image source
   */
  private updateImages() {
    this.markdownHighlighter.updateImages(this.previewContentElement);
  }
}
