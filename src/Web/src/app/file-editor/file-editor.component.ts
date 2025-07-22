import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild } from '@angular/core';
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
import { ShareModalComponent } from "../components/modals/share/share.component";
import { ShareService } from '../../services/components/modals/share.service';
import { ClientIdMessage, FileSyncMessage, FileSyncMessageType, FileUpdatedMessage } from '../../model/file-sync-message';
import { LoggerService } from '../../services/logger.service';
import { getFileType } from '../../utils/file-utils';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-file-editor',
  imports: [
    MdEditorComponent,
    TranslatePipe,
    TranslateDirective,
    TestTagDirective,
    EditTagsModalComponent,
    RenameFileModalComponent,
    ShareModalComponent,
    FormsModule,
  ],
  templateUrl: './file-editor.component.html',
  styleUrl: './file-editor.component.css',
})
export class FileEditorComponent implements AfterViewInit, OnDestroy {
  readonly directoryPath: string;
  readonly groupId?: number;
  readonly linkBasePath: string = '';
  // It's fine if this changes in for each file editor instance,
  // as the clientId is used to identify the client for file sync.
  // It does not need to be persistent across sessions.
  private readonly clientId = this.createClientId();

  private socket?: WebSocket;

  fileName: string;
  content = '';
  loading = true;
  tags: string[] = [];
  readonly fileType;

  @ViewChild('shareModal')
  shareModal!: ShareModalComponent;

  @ViewChild('textarea')
  textarea!: ElementRef<HTMLTextAreaElement>;

  constructor(
    route: ActivatedRoute,
    private fileService: FileService,
    private tagService: TagService,
    private translate: TranslateService,
    private notificationService: NotificationService,
    private router: Router,
    private location: Location,
    private log: LoggerService,

    public share: ShareService,
  ) {
    const path = route.snapshot.paramMap.get('path') as string;
    [this.directoryPath, this.fileName] = splitDirAndName(path);
    this.fileType = getFileType(this.fileName);
    const groupId = route.snapshot.paramMap.get('groupId');
    if (groupId) {
      this.groupId = Number(groupId);
      this.linkBasePath = `/groups/${this.groupId}`;
    }

    this.loadFile();

    tagService.listFileTags(this.directoryPath, this.fileName, this.groupId)
      .then((tags) => {
        this.tags = tags;
      }, async () => {
        this.notificationService.error(await getTranslation(this.translate, 'error.load-file-tags'));
      });
  }

  ngAfterViewInit(): void {
    const filePath = joinPaths(this.directoryPath, this.fileName);
    const socket = this.fileService.createFileSyncSocket(filePath, this.groupId);
    this.socket = socket;

    socket.addEventListener('open', () => {
      this.log.info('File sync socket opened for file:', filePath);
      const message: ClientIdMessage = {
        messageType: FileSyncMessageType.ClientId,
        clientId: this.clientId,
      };
      // Send the client ID message to the server
      socket.send(JSON.stringify(message));
    });

    socket.addEventListener('message', async (event) => {
      const data = JSON.parse(event.data) as FileSyncMessage;

      if (data.messageType == FileSyncMessageType.UpdateTime) {
        const updateMessage = data as FileUpdatedMessage;
        // Update the file if the update is not from this client
        if (updateMessage.senderClientId !== this.clientId) {
          await this.loadFile();
          this.notificationService.info(await getTranslation(this.translate, 'editor.file-updated'));
        }
      }
    });

    socket.addEventListener('error', async (error) => {
      console.error('File sync socket error:', error);
      this.notificationService.error(await getTranslation(this.translate, 'error.file-sync-socket'));
    });
  }

  ngOnDestroy(): void {
    this.socket?.close();
  }

  async loadFile() {
    try {
      const content = await this.fileService.getFile(this.directoryPath, this.fileName, this.groupId)
      this.content = new TextDecoder().decode(content);
      this.loading = false;

      if (this.fileType === 'text') {
        // Adjust the height of the textarea to fit the content
        setTimeout(() => {
          if (this.textarea && this.textarea.nativeElement) {
            this.adjustTextareaHeight(this.textarea.nativeElement);
          }
        }, 0);
      }
    } catch (e) {
      this.notificationService.error(await getTranslation(this.translate, 'error.load-file'));
      this.log.error('Error loading file:', e);
    }
  }

  async saveContent() {
    await this.notificationService.awaitAndNotifyError(this.fileService.saveTextFile(
      this.directoryPath,
      this.fileName,
      this.content,
      this.groupId,
      this.clientId,
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

  adjustTextareaHeight(textarea: HTMLTextAreaElement) {
    // Reset height to auto to get the correct scrollHeight
    textarea.style.height = 'auto';
    // Set height to scrollHeight to accommodate all content
    textarea.style.height = textarea.scrollHeight + 'px';
  }

  async openShareModal() {
    this.share.setFilePath(joinPaths(this.directoryPath, this.fileName));
    await this.share.loadPublicLink();
    this.shareModal.show();
  }

  private createClientId(): string {
    // Use crypto API for generating a unique client ID
    if (typeof window !== 'undefined' && window.crypto && window.crypto.randomUUID) {
      return window.crypto.randomUUID();
    }

    // Fallback for environments where crypto.randomUUID is not available
    return 'client-' + Math.random().toString(36).substring(2, 15) + '-' + Date.now();
  }
}
