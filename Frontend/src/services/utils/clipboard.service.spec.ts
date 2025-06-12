import { TestBed } from '@angular/core/testing';

import { ClipboardService } from './clipboard.service';
import { LoggerService } from '../logger.service';
import { createMockLoggerService } from '../logger.service.spec';

export function createMockClipboardService(): jasmine.SpyObj<ClipboardService> {
  return jasmine.createSpyObj('ClipboardService', ['copyText']);
}

describe('ClipboardService', () => {
  let service: ClipboardService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [{ provide: LoggerService, useValue: createMockLoggerService() }]
    });
    service = TestBed.inject(ClipboardService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should copy text to clipboard', async () => {
    const text = 'Test clipboard text';
    spyOn(navigator.clipboard, 'writeText').and.resolveTo();

    await service.copyText(text);

    expect(navigator.clipboard.writeText).toHaveBeenCalledWith(text);
  });
});
