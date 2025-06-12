import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShareModalComponent } from './share.component';
import { LoggerService } from '../../../../services/logger.service';
import { createMockLoggerService } from '../../../../services/logger.service.spec';
import { NotificationService } from '../../../../services/notification.service';
import { createMockNotificationService } from '../../../../services/notification.service.spec';
import { TranslateFakeLoader, TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { click } from '../../../../tests/click-utils';
import { ClipboardService } from '../../../../services/utils/clipboard.service';
import { createMockClipboardService } from '../../../../services/utils/clipboard.service.spec';

describe('ShareComponent', () => {
  let component: ShareModalComponent;
  let fixture: ComponentFixture<ShareModalComponent>;
  let notificationService: jasmine.SpyObj<NotificationService>;
  let clipboardService: jasmine.SpyObj<ClipboardService>;
  let nativeElement: HTMLElement;

  beforeEach(async () => {
    notificationService = createMockNotificationService();
    clipboardService = createMockClipboardService();

    await TestBed.configureTestingModule({
      imports: [
        ShareModalComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: [
        { provide: LoggerService, useValue: createMockLoggerService() },
        { provide: NotificationService, useValue: notificationService },
        { provide: ClipboardService, useValue: clipboardService },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ShareModalComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('isPublic', true);
    fixture.componentRef.setInput('publicLink', 'https://example.com/public/test-hash');
    fixture.componentRef.setInput('fileName', 'test-file.txt');
    fixture.detectChanges();

    nativeElement = fixture.nativeElement as HTMLElement;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should toggle public state on button click', () => {
    const changePublicStateSpy = spyOn(component.changePublicState, 'emit');
    fixture.componentRef.setInput('isPublic', false);
    fixture.detectChanges();

    const publicToggle = nativeElement.querySelector('[data-test="share.toggle.public"]') as HTMLInputElement;
    expect(publicToggle).toBeTruthy();

    click(publicToggle);
    fixture.detectChanges();

    expect(changePublicStateSpy).toHaveBeenCalledWith(true);
  });

  it('should show info notification when copying link', async () => {
    fixture.detectChanges();

    const copyButton = nativeElement.querySelector('[data-test="share.button.copy"]') as HTMLButtonElement;
    expect(copyButton).toBeTruthy();

    click(copyButton);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(clipboardService.copyText).toHaveBeenCalledWith('https://example.com/public/test-hash');
    expect(notificationService.info).toHaveBeenCalled();
  });
});
