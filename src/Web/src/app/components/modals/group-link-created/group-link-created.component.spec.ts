import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateFakeLoader, TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { GroupLinkCreatedComponent } from './group-link-created.component';
import { NotificationService } from '../../../../services/notification.service';
import { By } from '@angular/platform-browser';
import { ClipboardService } from '../../../../services/utils/clipboard.service';
import { createMockClipboardService } from '../../../../services/utils/clipboard.service.spec';

describe('GroupLinkCreatedComponent', () => {
  let component: GroupLinkCreatedComponent;
  let fixture: ComponentFixture<GroupLinkCreatedComponent>;
  let modal: HTMLDialogElement;
  let notificationService: jasmine.SpyObj<NotificationService>;
  let clipboardService: jasmine.SpyObj<ClipboardService>;

  beforeEach(async () => {
    notificationService = jasmine.createSpyObj('NotificationService', ['info', 'error']);
    clipboardService = createMockClipboardService();

    await TestBed.configureTestingModule({
      imports: [
        GroupLinkCreatedComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: [
        { provide: NotificationService, useValue: notificationService },
        { provide: ClipboardService, useValue: clipboardService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(GroupLinkCreatedComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    modal = fixture.debugElement.query(By.css('.modal')).nativeElement;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show and hide modal with link', () => {
    const testLink = 'http://test.com/join-group/123/test-code';

    // Initially modal should not be open
    expect(modal.hasAttribute('open')).toBeFalsy();
    expect(component.link()).toBe('');

    // Show modal with link
    component.show(testLink);
    fixture.detectChanges();

    expect(modal.hasAttribute('open')).toBeTruthy();
    expect(component.link()).toBe(testLink);

    // Close modal
    component.close();
    fixture.detectChanges();
    expect(modal.hasAttribute('open')).toBeFalsy();
  });

  it('should display the link in the modal', () => {
    const testLink = 'http://test.com/join-group/123/test-code';
    component.show(testLink);
    fixture.detectChanges();

    const linkElement = fixture.debugElement.query(By.css('a')).nativeElement as HTMLAnchorElement;
    expect(linkElement.href).toContain(testLink);
    expect(linkElement.textContent).toContain(testLink);
  });

  it('should copy link to clipboard when copy button is clicked', async () => {
    const testLink = 'http://test.com/join-group/123/test-code';
    component.show(testLink);
    fixture.detectChanges();

    // Click copy button
    const copyButton = fixture.debugElement.query(By.css('button[data-test="groups.link-created.copy"]')).nativeElement as HTMLButtonElement;
    copyButton.click();

    await fixture.whenStable();

    expect(clipboardService.copyText).toHaveBeenCalledWith(testLink);
    expect(notificationService.info).toHaveBeenCalled();
  });

  it('should close when clicking backdrop', () => {
    component.show('http://test.com/join-group/123/test-code');
    fixture.detectChanges();
    expect(modal.hasAttribute('open')).toBeTruthy();

    const backdropButton = fixture.debugElement.query(By.css('.modal-backdrop button')).nativeElement as HTMLButtonElement;
    backdropButton.click();
    fixture.detectChanges();

    expect(modal.hasAttribute('open')).toBeFalsy();
  });
});
