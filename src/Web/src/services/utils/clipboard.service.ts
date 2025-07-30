import { Injectable } from '@angular/core';
import { LoggerService } from '../logger.service';

@Injectable({
  providedIn: 'root'
})
export class ClipboardService {

  constructor(private log: LoggerService) { }

  async copyText(text: string): Promise<void> {
    if (!navigator.clipboard) {
      throw new Error('Clipboard API not supported');
    }

    if (!window.isSecureContext) {
      this.log.warn('Clipboard API requires a secure context (HTTPS)');
      throw new Error('Clipboard API requires a secure context (HTTPS)');
    }

    try {
      await navigator.clipboard.writeText(text);
    } catch (err) {
      this.log.error('Failed to copy text to clipboard:', err);
      throw err;
    }
  }
}
