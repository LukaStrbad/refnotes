import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ChangePasswordFormComponent } from './change-password-form.component';
import { TranslateFakeLoader, TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { AskModalService } from '../../../services/ask-modal.service';
import { click } from '../../../tests/click-utils';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

describe('ChangePasswordFormComponent', () => {
  let component: ChangePasswordFormComponent;
  let fixture: ComponentFixture<ChangePasswordFormComponent>;
  let askModal: jasmine.SpyObj<AskModalService>;
  let nativeElement: HTMLElement;

  beforeEach(async () => {
    askModal = jasmine.createSpyObj('AskModalService', ['confirm', 'prompt']);

    await TestBed.configureTestingModule({
      imports: [
        ChangePasswordFormComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: [
        provideAnimationsAsync(),
        { provide: AskModalService, useValue: askModal }
      ],
    })
      .compileComponents();

    fixture = TestBed.createComponent(ChangePasswordFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    nativeElement = fixture.nativeElement as HTMLElement;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should keep the button disabled when passwords do not match', () => {
    const currentPasswordInput = nativeElement.querySelector('input[data-test="change-password.current"]') as HTMLInputElement;
    const newPasswordInput = nativeElement.querySelector('input[data-test="change-password.new"]') as HTMLInputElement;
    const confirmPasswordInput = nativeElement.querySelector('input[data-test="change-password.confirm"]') as HTMLInputElement;
    const submitButton = nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;

    currentPasswordInput.value = 'currentPass123';
    newPasswordInput.value = 'newPass123';
    confirmPasswordInput.value = 'differentPass123';

    currentPasswordInput.dispatchEvent(new Event('input'));
    newPasswordInput.dispatchEvent(new Event('input'));
    confirmPasswordInput.dispatchEvent(new Event('input'));

    fixture.detectChanges();

    expect(submitButton.disabled).toBeTrue();
  });

  it('should emit updatePassword on form submission', async () => {
    spyOn(component.updatePassword, 'emit');
    askModal.confirm.and.resolveTo(true);

    const currentPasswordInput = nativeElement.querySelector('input[data-test="change-password.current"]') as HTMLInputElement;
    const newPasswordInput = nativeElement.querySelector('input[data-test="change-password.new"]') as HTMLInputElement;
    const confirmPasswordInput = nativeElement.querySelector('input[data-test="change-password.confirm"]') as HTMLInputElement;
    const submitButton = nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;

    currentPasswordInput.value = 'currentPass123';
    newPasswordInput.value = 'newPass123';
    confirmPasswordInput.value = 'newPass123';

    currentPasswordInput.dispatchEvent(new Event('input'));
    newPasswordInput.dispatchEvent(new Event('input'));
    confirmPasswordInput.dispatchEvent(new Event('input'));

    fixture.detectChanges();

    click(submitButton);
    await fixture.whenStable();

    expect(component.updatePassword.emit).toHaveBeenCalledWith({
      oldPassword: 'currentPass123',
      newPassword: 'newPass123',
    });
  });
});
