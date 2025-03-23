import { Component, effect, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FileService } from '../../services/file.service';
import { TagService } from '../../services/tag.service';
import { TranslateDirective, TranslatePipe } from '@ngx-translate/core';
import * as fileUtils from '../../utils/file-utils';
import { MarkdownHighlighter } from '../../utils/markdown-highlighter';
import { SettingsService } from '../../services/settings.service';
import { isMarkdownFile, isTextFile } from '../../utils/file-utils';

@Component({
  selector: 'app-file-preview',
  imports: [TranslatePipe, TranslateDirective],
  templateUrl: './file-preview.component.html',
  styleUrl: './file-preview.component.css',
  encapsulation: ViewEncapsulation.None,
})
export class FilePreviewComponent {
  private readonly markdownHighlighter: MarkdownHighlighter;
  readonly directoryPath: string;
  readonly fileName: string;
  content = '';
  loading = true;
  tags: string[] = [];
  fileType: fileUtils.FileType = 'unknown';

  fileUtils = fileUtils;

  constructor(
    route: ActivatedRoute,
    private fileService: FileService,
    private tagService: TagService,
    public settings: SettingsService,
  ) {
    this.directoryPath = route.snapshot.queryParamMap.get(
      'directory',
    ) as string;
    this.fileName = route.snapshot.queryParamMap.get('file') as string;
    this.fileType = fileUtils.getFileType(this.fileName);

    fileService.getFile(this.directoryPath, this.fileName)
      .then((content) => {
        const text = new TextDecoder().decode(content);
        if (this.fileType === 'markdown') {
          const markdown = this.markdownHighlighter.parse(text)
          this.content = markdown as string;
        } else if (this.fileType === 'text') {
          console.log(text);
          this.content = text;
        }
        this.loading = false;
      });

    tagService.listFileTags(this.directoryPath, this.fileName).then((tags) => {
      this.tags = tags;
    });

    this.markdownHighlighter = new MarkdownHighlighter(
      this.settings.mdEditor().showLineNumbers,
      this.directoryPath,
    );

    effect(() => {
      const showLineNumbers = this.settings.mdEditor().showLineNumbers;
      this.markdownHighlighter.showLineNumbers = showLineNumbers;
    });
  }
}
