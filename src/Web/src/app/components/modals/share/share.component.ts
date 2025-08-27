import { Component, ElementRef, input, output, ViewChild } from '@angular/core';
import { TranslateDirective, TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LoggerService } from '../../../../services/logger.service';
import { NotificationService } from '../../../../services/notification.service';
import { getTranslation } from '../../../../utils/translation-utils';
import { TestTagDirective } from '../../../../directives/test-tag.directive';
import { ClipboardService } from '../../../../services/utils/clipboard.service';

@Component({
  selector: 'app-share-modal',
  imports: [TranslatePipe, TranslateDirective, TestTagDirective],
  templateUrl: './share.component.html',
  styleUrl: './share.component.css'
})
export class ShareModalComponent {
  readonly isPublic = input.required<boolean>();
  readonly publicLink = input.required<string | null>();
  readonly fileName = input.required<string>();

  readonly userShareLink = input.required<string | null>();

  shareType = ShareType.PublicFile;
  readonly ShareType = ShareType;

  readonly changePublicState = output<boolean>();
  readonly generateUserShareLink = output();

  @ViewChild('modal')
  modal!: ElementRef<HTMLDialogElement>;

  readonly canCopyToClipboard;

  constructor(
    private log: LoggerService,
    private notificationService: NotificationService,
    private translate: TranslateService,
    private clipboard: ClipboardService,
  ) {
    this.canCopyToClipboard = navigator.clipboard && window.isSecureContext;
  }

  show() {
    this.modal.nativeElement.showModal();
  }

  hide() {
    this.modal.nativeElement.close();
  }

  togglePublicState() {
    this.changePublicState.emit(!this.isPublic());
  }

  async copyToClipboard() {
    const link = this.publicLink();
    if (!link) {
      this.log.warn('No public link available to copy');
      return;
    }

    try {
      await this.clipboard.copyText(link);
      this.notificationService.info(await getTranslation(this.translate, 'share.public-link.copied-to-clipboard'))
    } catch (err) {
      this.log.error('Failed to copy link to clipboard:', err);
      this.notificationService.error(await getTranslation(this.translate, 'share.public-link.copy-failed'));
    }
  }

  onGenerateUserShareLink() {
    this.generateUserShareLink.emit();
  }

  async copyShareUrlToClipboard() {
    const link = this.userShareLink();
    if (!link) {
      this.log.warn('No user share link available to copy');
      return;
    }

    try {
      await this.clipboard.copyText(link);
      this.notificationService.info(await getTranslation(this.translate, 'share.user-link.copied-to-clipboard'))
    } catch (err) {
      this.log.error('Failed to copy link to clipboard:', err);
      this.notificationService.error(await getTranslation(this.translate, 'share.user-link.copy-failed'));
    }
  }
}

enum ShareType {
  PublicFile,
  OtherUser,
}
