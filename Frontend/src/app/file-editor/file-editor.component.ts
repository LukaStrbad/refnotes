import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MdEditorComponent } from '../components/md-editor/md-editor.component';
import { TranslateDirective, TranslatePipe } from '@ngx-translate/core';
import { TestTagDirective } from '../../directives/test-tag.directive';
import { FileService } from '../../services/file.service';
import { TagService } from '../../services/tag.service';
import { EditTagsModalComponent } from '../components/modals/edit-tags-modal/edit-tags-modal.component';
import { RenameFileModalComponent } from "../components/modals/rename-file-modal/rename-file-modal.component";
import { joinPaths } from '../../utils/path-utils';

@Component({
  selector: 'app-file-editor',
  imports: [
    MdEditorComponent,
    TranslatePipe,
    TranslateDirective,
    TestTagDirective,
    EditTagsModalComponent,
    RenameFileModalComponent
],
  templateUrl: './file-editor.component.html',
  styleUrl: './file-editor.component.css',
})
export class FileEditorComponent {
  readonly directoryPath: string;
  fileName: string;
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

  async saveContent() {
    await this.fileService.saveTextFile(
      this.directoryPath,
      this.fileName,
      this.content,
    );
  }

  async addTag([fileName, tag]: [string, string]) {
    await this.tagService.addFileTag(this.directoryPath, fileName, tag);
    if (!this.tags.includes(tag)) {
      this.tags.push(tag);
    }
  }

  async removeTag([fileName, tag]: [string, string]) {
    await this.tagService.removeFileTag(this.directoryPath, fileName, tag);
    this.tags = this.tags.filter((t) => t !== tag);
  }

  async renameFile([_, newFileName]: [string, string]) {
    const oldFilePath = joinPaths(this.directoryPath, this.fileName);
    const newFilePath = joinPaths(this.directoryPath, newFileName);
    await this.fileService.moveFile(oldFilePath, newFilePath);
    this.fileName = newFileName;
  }
}
