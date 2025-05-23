import { Injectable, ViewContainerRef } from '@angular/core';
import { AskModalComponent } from '../app/components/modals/ask-modal/ask-modal.component';

@Injectable({
  providedIn: 'root'
})
export class AskModalService {
  viewContainer?: ViewContainerRef;

  /**
   * Displays the ask modal with the given title and message.
   * @param title The title of the modal.
   * @param message The message to be displayed in the modal.
   * @param translate Whether to translate the title and message using ngx-translate.
   * @returns A promise that resolves to true if the confirm button is clicked, false otherwise.
   */
  async prompt(title: string, message: string, { translate = false, body = undefined }: { translate: boolean, body?: string } = { translate: false }): Promise<boolean> {
    return await this.show(title, message, translate, body, false);
  }

  async confirm(title: string, message: string, { translate = false, body = undefined }: { translate: boolean, body?: string } = { translate: false }): Promise<boolean> {
    return await this.show(title, message, translate, body, true);
  }

  private async show(title: string, message: string, translate: boolean, body: string | undefined, useYesNo: boolean) {
    if (!this.viewContainer) {
      throw new Error('ViewContainerRef is not set. Please set it before using the AskModalService.');
    }

    const askModal = this.viewContainer.createComponent(AskModalComponent);

    askModal.instance.setText(title, message, translate);
    askModal.instance.setBody(body);
    askModal.instance.useYesNo = useYesNo;
    askModal.changeDetectorRef.detectChanges();
    askModal.instance.show();

    const result = await new Promise<boolean>((resolve) => {
      askModal.instance.onConfirm = () => {
        resolve(true);
      };

      askModal.instance.onCancel = () => {
        resolve(false);
      };
    });

    askModal.instance.close();
    askModal.destroy();
    return result;
  }
}
