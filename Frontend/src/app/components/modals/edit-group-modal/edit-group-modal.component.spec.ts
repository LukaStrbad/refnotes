import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateFakeLoader, TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { EditGroupModalComponent } from './edit-group-modal.component';
import { By } from '@angular/platform-browser';
import { UserGroupRole } from '../../../../model/user-group';

describe('EditGroupModalComponent', () => {
  let component: EditGroupModalComponent;
  let fixture: ComponentFixture<EditGroupModalComponent>;
  let modal: HTMLDialogElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        EditGroupModalComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: []
    }).compileComponents();

    fixture = TestBed.createComponent(EditGroupModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    modal = fixture.debugElement.query(By.css('.modal')).nativeElement;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show and hide modal', () => {
    // Initially modal should not be open
    expect(modal.hasAttribute('open')).toBeFalsy();

    // Show modal
    component.show({
      id: 1,
      name: 'Test Group',
      role: UserGroupRole.Owner,
    });
    fixture.detectChanges();
    expect(modal.hasAttribute('open')).toBeTruthy();

    // Hide modal
    component.hide();
    fixture.detectChanges();
    expect(modal.hasAttribute('open')).toBeFalsy();
  });

  it('should set group name on open', () => {
    // Set a group name
    component.groupName = 'Test Group';

    // Show modal should set the group name
    component.show({
      id: 1,
      name: 'Test Group',
      role: UserGroupRole.Owner,
    });
    expect(component.groupName).toBe('Test Group');
  });

  it('should emit group name when creating', () => {
    spyOn(component.edit, 'emit');
    const groupName = 'Test Group';

    component.show({
      id: 1,
      name: 'Test Group',
      role: UserGroupRole.Owner,
    });

    // Edit group
    component.editGroup();

    expect(component.edit.emit).toHaveBeenCalledWith([1, { name: groupName }]);
    expect(modal.hasAttribute('open')).toBeFalsy(); // Modal should close
  });

  it('should not emit empty group name', () => {
    spyOn(component.edit, 'emit');

    // Try to edit with empty name
    component.show({
      id: 1,
      name: '',
      role: UserGroupRole.Owner,
    });
    component.editGroup();
    component.hide();

    // Try to edit with whitespace name
    component.show({
      id: 1,
      name: '   ',
      role: UserGroupRole.Owner,
    });
    component.editGroup();
    component.hide();

    expect(component.edit.emit).not.toHaveBeenCalled();
  });

  it('edit button should be disabled when group name is empty', () => {
    const editButton = fixture.debugElement.query(By.css('[data-test="button-update-group"]')).nativeElement as HTMLButtonElement;

    // Empty name
    component.show({
      id: 1,
      name: '',
      role: UserGroupRole.Owner,
    });
    fixture.detectChanges();
    expect(editButton.disabled).toBeTruthy();

    // Whitespace name
    component.show({
      id: 1,
      name: '   ',
      role: UserGroupRole.Owner,
    });
    fixture.detectChanges();
    expect(editButton.disabled).toBeTruthy();

    // Valid name
    component.show({
      id: 1,
      name: 'Test Group',
      role: UserGroupRole.Owner,
    });
    fixture.detectChanges();
    expect(editButton.disabled).toBeFalsy();
  });

  it('should close modal when cancel button is clicked', () => {
    // First show the modal
    component.show({
      id: 1,
      name: 'Test Group',
      role: UserGroupRole.Owner,
    });
    fixture.detectChanges();
    expect(modal.hasAttribute('open')).toBeTruthy();

    // Click cancel button
    const cancelButton = fixture.debugElement.query(By.css('[data-test="button-cancel-edit"]')).nativeElement as HTMLButtonElement;
    cancelButton.click();
    fixture.detectChanges();

    expect(modal.hasAttribute('open')).toBeFalsy();
  });

  it('should edit group when enter key is pressed in input', () => {
    spyOn(component.edit, 'emit');
    const groupName = 'Test Group';

    // Set group name
    component.show({
      id: 1,
      name: groupName,
      role: UserGroupRole.Owner,
    });
    fixture.detectChanges();

    // Simulate enter key in the input
    const input = fixture.debugElement.query(By.css('[data-test="input-group-name"]')).nativeElement as HTMLInputElement;
    const event = new KeyboardEvent('keyup', { key: 'Enter' });
    input.dispatchEvent(event);

    expect(component.edit.emit).toHaveBeenCalledWith([1, { name: groupName }]);
    expect(modal.hasAttribute('open')).toBeFalsy();
  });
});
