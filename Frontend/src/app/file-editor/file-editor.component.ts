import { Component, effect, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BrowserService } from '../../services/browser.service';
import { MdEditorComponent } from "../components/md-editor/md-editor.component";
import { TranslateDirective, TranslatePipe } from '@ngx-translate/core';
import {TestTagDirective} from "../../directives/test-tag.directive";
import {FileService} from "../../services/file.service";

@Component({
  selector: 'app-file-editor',
  imports: [
    MdEditorComponent,
    TranslatePipe,
    TranslateDirective,
    TestTagDirective,
  ],
  templateUrl: './file-editor.component.html',
  styleUrl: './file-editor.component.css',
})
export class FileEditorComponent {
  readonly directoryPath: string;
  readonly fileName: string;
  content = '';
  loading = true;

  constructor(
    route: ActivatedRoute,
    private fileService: FileService,
  ) {
    this.directoryPath = route.snapshot.queryParamMap.get(
      'directory',
    ) as string;
    this.fileName = route.snapshot.queryParamMap.get('file') as string;

    fileService.getFile(this.directoryPath, this.fileName).then((content) => {
      this.content = new TextDecoder().decode(content);
      this.loading = false;
    });
  }

  async saveContent() {
    await this.fileService.saveTextFile(
      this.directoryPath,
      this.fileName,
      this.content,
    );
  }
}
