import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MdEditorComponent } from '../components/md-editor/md-editor.component';
import { TranslateDirective, TranslatePipe, TranslateService } from '@ngx-translate/core';
import { TestTagDirective } from '../../directives/test-tag.directive';
import { FileService } from '../../services/file.service';
import { TagService } from '../../services/tag.service';
import { EditTagsModalComponent } from '../components/modals/edit-tags-modal/edit-tags-modal.component';
import { RenameFileModalComponent } from "../components/modals/rename-file-modal/rename-file-modal.component";
import { joinPaths, splitDirAndName } from '../../utils/path-utils';
import { NotificationService } from '../../services/notification.service';
import { getTranslation } from '../../utils/translation-utils';
import { Location } from '@angular/common';

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
  readonly groupId?: number;
  readonly linkBasePath: string = '';

  fileName: string;
  content = '';
  loading = true;
  tags: string[] = [];

  constructor(
    route: ActivatedRoute,
    private fileService: FileService,
    private tagService: TagService,
    private translate: TranslateService,
    private notificationService: NotificationService,
    private router: Router,
    private location: Location,
  ) {
    const path = route.snapshot.paramMap.get('path') as string;
    [this.directoryPath, this.fileName] = splitDirAndName(path);
    const groupId = route.snapshot.paramMap.get('groupId');
    if (groupId) {
      this.groupId = Number(groupId);
      this.linkBasePath = `/groups/${this.groupId}`;
    }

    fileService.getFile(this.directoryPath, this.fileName, this.groupId)
      .then((content) => {
        this.content = new TextDecoder().decode(content);
        this.loading = false;
      }, async () => {
        this.notificationService.error(await getTranslation(this.translate, 'error.load-file'));
      });

    tagService.listFileTags(this.directoryPath, this.fileName, this.groupId)
      .then((tags) => {
        this.tags = tags;
      }, async () => {
        this.notificationService.error(await getTranslation(this.translate, 'error.load-file-tags'));
      });
  }

  async saveContent() {
    await this.notificationService.awaitAndNotifyError(this.fileService.saveTextFile(
      this.directoryPath,
      this.fileName,
      this.content,
      this.groupId,
    ), {
      default: await getTranslation(this.translate, 'error.save-file'),
    });
  }

  async addTag([fileName, tag]: [string, string]) {
    if (this.tags.includes(tag)) {
      return;
    }

    await this.notificationService.awaitAndNotifyError(this.tagService.addFileTag(this.directoryPath, fileName, tag, this.groupId), {
      default: await getTranslation(this.translate, 'error.add-file-tag'),
    });
    this.tags.push(tag);
  }

  async removeTag([fileName, tag]: [string, string]) {
    await this.notificationService.awaitAndNotifyError(this.tagService.removeFileTag(this.directoryPath, fileName, tag, this.groupId), {
      default: await getTranslation(this.translate, 'error.remove-file-tag'),
    });
    this.tags = this.tags.filter((t) => t !== tag);
  }

  async renameFile([, newFileName]: [string, string]) {
    const oldFilePath = joinPaths(this.directoryPath, this.fileName);
    const newFilePath = joinPaths(this.directoryPath, newFileName);
    await this.notificationService.awaitAndNotifyError(this.fileService.moveFile(oldFilePath, newFilePath, this.groupId), {
      default: await getTranslation(this.translate, 'error.rename-file'),
    });

    const filePath = joinPaths(this.directoryPath, newFileName)

    const newUrl = this.router.createUrlTree([this.linkBasePath, 'file', filePath, 'edit']);
    this.location.replaceState(newUrl.toString());

    const oldName = this.fileName;
    this.fileName = newFileName;
    this.notificationService.info(await getTranslation(this.translate, 'editor.file-renamed-to', { oldName, newName: newFileName }));
  }
}
