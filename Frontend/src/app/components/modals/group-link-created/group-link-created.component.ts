import { Component, ElementRef, Signal, signal, ViewChild } from '@angular/core';
import { TranslateDirective, TranslatePipe, TranslateService } from '@ngx-translate/core';
import { NotificationService } from '../../../../services/notification.service';
import { getTranslation } from '../../../../utils/translation-utils';
import { TestTagDirective } from '../../../../directives/test-tag.directive';

@Component({
  selector: 'app-group-link-created',
  imports: [TranslateDirective, TranslatePipe, TestTagDirective],
  templateUrl: './group-link-created.component.html',
  styleUrl: './group-link-created.component.css'
})
export class GroupLinkCreatedComponent {
  private _link = signal('');
  link: Signal<string> = this._link;

  @ViewChild('modal')
  modal!: ElementRef<HTMLDialogElement>;

  constructor(
    private notificationService: NotificationService,
    private translate: TranslateService,
  ) { }

  show(link: string) {
    this._link.set(link);
    this.modal.nativeElement.showModal();
  }

  close() {
    this.modal.nativeElement.close();
  }

  async copyLink() {
    try {
      await navigator.clipboard.writeText(this.link());
      this.notificationService.info(await getTranslation(this.translate, 'groups.link-created.copied-to-clipboard'))
    } catch {
      this.notificationService.error(await getTranslation(this.translate, 'groups.link-created.copy-failed'))
    }
  }
}
