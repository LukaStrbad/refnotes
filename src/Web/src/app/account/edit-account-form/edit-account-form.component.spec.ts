import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditAccountFormComponent } from './edit-account-form.component';
import { TranslateFakeLoader, TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { UserResponse } from '../../../model/user-response';
import { click } from '../../../tests/click-utils';

describe('EditAccountFormComponent', () => {
  let component: EditAccountFormComponent;
  let fixture: ComponentFixture<EditAccountFormComponent>;
  let nativeElement: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        EditAccountFormComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ]
    })
      .compileComponents();

    fixture = TestBed.createComponent(EditAccountFormComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('accountInfo', undefined);
    fixture.detectChanges();

    nativeElement = fixture.nativeElement as HTMLElement;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize form with empty values', () => {
    const nameInput = nativeElement.querySelector('input[data-test="edit-account.name"]') as HTMLInputElement;
    const usernameInput = nativeElement.querySelector('input[data-test="edit-account.username"]') as HTMLInputElement;
    const emailInput = nativeElement.querySelector('input[data-test="edit-account.email"]') as HTMLInputElement;

    expect(nameInput.value).toBe('');
    expect(usernameInput.value).toBe('');
    expect(emailInput.value).toBe('');
  });

  it('should change form values when accountInfo changes', () => {
    const mockUser: UserResponse = {
      id: 123,
      username: 'testuser',
      name: 'Test User',
      email: 'testuser@example.com',
      roles: [],
      emailConfirmed: true
    };

    fixture.componentRef.setInput('accountInfo', mockUser);
    fixture.detectChanges();

    const nameInput = nativeElement.querySelector('input[data-test="edit-account.name"]') as HTMLInputElement;
    const usernameInput = nativeElement.querySelector('input[data-test="edit-account.username"]') as HTMLInputElement;
    const emailInput = nativeElement.querySelector('input[data-test="edit-account.email"]') as HTMLInputElement;

    expect(nameInput.value).toBe(mockUser.name);
    expect(usernameInput.value).toBe(mockUser.username);
    expect(emailInput.value).toBe(mockUser.email);
  });

  it('should emit saveChanges with updated user data on form submission', () => {
    spyOn(component.saveChanges, 'emit');

    const nameInput = nativeElement.querySelector('input[data-test="edit-account.name"]') as HTMLInputElement;
    const usernameInput = nativeElement.querySelector('input[data-test="edit-account.username"]') as HTMLInputElement;
    const emailInput = nativeElement.querySelector('input[data-test="edit-account.email"]') as HTMLInputElement;
    const submitButton = nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;

    nameInput.value = 'New Name';
    nameInput.dispatchEvent(new Event('input'));

    usernameInput.value = 'newusername';
    usernameInput.dispatchEvent(new Event('input'));

    emailInput.value = 'newemail@example.com';
    emailInput.dispatchEvent(new Event('input'));

    fixture.detectChanges();

    click(submitButton);

    expect(component.saveChanges.emit).toHaveBeenCalledWith({
      newName: 'New Name',
      newUsername: 'newusername',
      newEmail: 'newemail@example.com'
    });
  });
});
