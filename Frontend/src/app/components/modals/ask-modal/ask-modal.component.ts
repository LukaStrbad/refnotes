import { AsyncPipe } from '@angular/common';
import { Component, computed, ElementRef, Signal, signal, ViewChild } from '@angular/core';
import { TranslateDirective, TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-ask-modal',
  imports: [AsyncPipe, TranslateDirective],
  templateUrl: './ask-modal.component.html',
  styleUrl: './ask-modal.component.css'
})
export class AskModalComponent {
  private _title = signal('');
  private _message = signal('');
  private _translate = signal(false);
  private _body = signal<string | null>(null);

  title: Signal<Promise<string>>;
  message: Signal<Promise<string>>;
  body: Signal<string | null> = this._body;
  useYesNo = false;

  onConfirm?: () => void;
  onCancel?: () => void;

  @ViewChild('modal')
  modal!: ElementRef<HTMLDialogElement>;

  constructor(private translate: TranslateService) {
    this.title = computed(() =>
      this._translate()
        ? firstValueFrom(this.translate.get(this._title()))
        : Promise.resolve(this._title())
    );

    this.message = computed(() =>
      this._translate()
        ? firstValueFrom(this.translate.get(this._message()))
        : Promise.resolve(this._message())
    );
  }

  setText(title: string, message: string, translate = false) {
    this._title.set(title);
    this._message.set(message);
    this._translate.set(translate);
  }

  setBody(body: string | null | undefined) {
    this._body.set(body ?? null);
  }

  show() {
    this.modal.nativeElement.showModal();
  }

  close() {
    this.modal.nativeElement.close();
  }
}
