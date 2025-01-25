import { Component, effect, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BrowserService } from '../../services/browser.service';
import { MdEditorComponent } from "../components/md-editor/md-editor.component";
import { TranslateDirective, TranslatePipe } from '@ngx-translate/core';
import {TestTagDirective} from "../../directives/test-tag.directive";

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
    private browser: BrowserService,
  ) {
    this.directoryPath = route.snapshot.queryParamMap.get(
      'directory',
    ) as string;
    this.fileName = route.snapshot.queryParamMap.get('file') as string;

    browser.getFile(this.directoryPath, this.fileName).then((content) => {
      this.content = new TextDecoder().decode(content);
      this.loading = false;
    });
  }

  async saveContent() {
    await this.browser.saveTextfile(
      this.directoryPath,
      this.fileName,
      this.content,
    );
  }
}
