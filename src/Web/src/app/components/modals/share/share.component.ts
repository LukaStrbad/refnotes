import { Component, ElementRef, EventEmitter, input, model, Output, ViewChild } from '@angular/core';
import { TranslateDirective, TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LoggerService } from '../../../../services/logger.service';
import { NotificationService } from '../../../../services/notification.service';
import { getTranslation } from '../../../../utils/translation-utils';
import { TestTagDirective } from '../../../../directives/test-tag.directive';

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

  @Output()
  changePublicState = new EventEmitter<boolean>();

  @ViewChild('modal')
  modal!: ElementRef<HTMLDialogElement>;

  constructor(
    private log: LoggerService,
    private notificationService: NotificationService,
    private translate: TranslateService,
  ) { }

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
      await navigator.clipboard.writeText(link);
      this.notificationService.info(await getTranslation(this.translate, 'share.public-link.copied-to-clipboard'))
    } catch (err) {
      this.log.error('Failed to copy link to clipboard:', err);
      this.notificationService.error(await getTranslation(this.translate, 'share.public-link.copy-failed'));
    }
  }
}
