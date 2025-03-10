import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FileService } from '../../services/file.service';
import { TagService } from '../../services/tag.service';
import { TranslateDirective, TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-file-preview',
  imports: [TranslatePipe, TranslateDirective],
  templateUrl: './file-preview.component.html',
  styleUrl: './file-preview.component.css'
})
export class FilePreviewComponent {
  readonly directoryPath: string;
  readonly fileName: string;
  content = '';
  loading = true;
  tags: string[] = [];

  constructor(
    route: ActivatedRoute,
    private fileService: FileService,
    private tagService: TagService,
  ) {
    this.directoryPath = route.snapshot.queryParamMap.get(
      'directory',
    ) as string;
    this.fileName = route.snapshot.queryParamMap.get('file') as string;

    fileService.getFile(this.directoryPath, this.fileName).then((content) => {
      this.content = new TextDecoder().decode(content);
      this.loading = false;
    });

    tagService.listFileTags(this.directoryPath, this.fileName).then((tags) => {
      this.tags = tags;
    });
  }
}
