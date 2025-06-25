import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateFakeLoader, TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { CreateGroupModalComponent } from './create-group-modal.component';
import { By } from '@angular/platform-browser';

describe('CreateGroupModalComponent', () => {
  let component: CreateGroupModalComponent;
  let fixture: ComponentFixture<CreateGroupModalComponent>;
  let modal: HTMLDialogElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        CreateGroupModalComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CreateGroupModalComponent);
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
    component.show();
    fixture.detectChanges();
    expect(modal.hasAttribute('open')).toBeTruthy();

    // Hide modal
    component.hide();
    fixture.detectChanges();
    expect(modal.hasAttribute('open')).toBeFalsy();
  });

  it('should clear group name when showing modal', () => {
    // Set a group name
    component.groupName = 'Test Group';

    // Show modal should clear the name
    component.show();
    expect(component.groupName).toBe('');
  });

  it('should emit group name when creating', () => {
    spyOn(component.create, 'emit');
    const groupName = 'Test Group';

    // Set group name
    component.groupName = groupName;

    // Create group
    component.createGroup();

    expect(component.create.emit).toHaveBeenCalledWith(groupName);
    expect(modal.hasAttribute('open')).toBeFalsy(); // Modal should close
  });

  it('should not emit empty group name', () => {
    spyOn(component.create, 'emit');

    // Try to create with empty name
    component.groupName = '';
    component.createGroup();

    // Try to create with whitespace name
    component.groupName = '   ';
    component.createGroup();

    expect(component.create.emit).not.toHaveBeenCalled();
  });

  it('should create button be disabled when group name is empty', () => {
    const createButton = fixture.debugElement.query(By.css('[data-test="button-create-group"]')).nativeElement as HTMLButtonElement;

    // Empty name
    component.groupName = '';
    fixture.detectChanges();
    expect(createButton.disabled).toBeTruthy();

    // Whitespace name
    component.groupName = '   ';
    fixture.detectChanges();
    expect(createButton.disabled).toBeTruthy();

    // Valid name
    component.groupName = 'Test Group';
    fixture.detectChanges();
    expect(createButton.disabled).toBeFalsy();
  });

  it('should close modal when cancel button is clicked', () => {
    // First show the modal
    component.show();
    fixture.detectChanges();
    expect(modal.hasAttribute('open')).toBeTruthy();

    // Click cancel button
    const cancelButton = fixture.debugElement.query(By.css('[data-test="button-cancel-create"]')).nativeElement as HTMLButtonElement;
    cancelButton.click();
    fixture.detectChanges();

    expect(modal.hasAttribute('open')).toBeFalsy();
  });

  it('should create group when enter key is pressed in input', () => {
    spyOn(component.create, 'emit');
    const groupName = 'Test Group';

    // Set group name
    component.groupName = groupName;
    fixture.detectChanges();

    // Simulate enter key in the input
    const input = fixture.debugElement.query(By.css('[data-test="input-group-name"]')).nativeElement as HTMLInputElement;
    const event = new KeyboardEvent('keyup', { key: 'Enter' });
    input.dispatchEvent(event);

    expect(component.create.emit).toHaveBeenCalledWith(groupName);
    expect(modal.hasAttribute('open')).toBeFalsy();
  });
});
