import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RenameFileModalComponent } from './rename-file-modal.component';
import {
  TranslateFakeLoader,
  TranslateLoader,
  TranslateModule,
} from '@ngx-translate/core';
import { FormsModule } from '@angular/forms';

describe('RenameFileModalComponent', () => {
  let component: RenameFileModalComponent;
  let fixture: ComponentFixture<RenameFileModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        RenameFileModalComponent,
        FormsModule,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RenameFileModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should set original and new file names when show is called', () => {
    component.show('file.txt');

    expect(component.originalFileName).toBe('file.txt');
    expect(component.newFileName).toBe('file.txt');
  });

  it('should emit onRename event with file names when saveChanges is called with valid input', () => {
    let oldFileName = '';
    let renamedFileName = '';
    component.rename.subscribe(([oldName, newName]) => {
      oldFileName = oldName;
      renamedFileName = newName;
    });

    component.show('file.txt');
    component.newFileName = 'renamed.txt';

    component.saveChanges();

    expect(oldFileName).toBe('file.txt');
    expect(renamedFileName).toBe('renamed.txt');
  });

  it('should not emit onRename event when saveChanges is called with empty new file name', () => {
    // Arrange
    let emitted = false;
    component.rename.subscribe(() => {
      emitted = true;
    });

    component.show('file.txt');
    component.newFileName = '';

    // Act
    component.saveChanges();

    // Assert
    expect(emitted).toBeFalse();
  });

  it('should not emit onRename event when saveChanges is called with same file name', () => {
    // Arrange
    let emitted = false;
    component.rename.subscribe(() => {
      emitted = true;
    });

    component.show('file.txt');
    component.newFileName = 'file.txt';

    // Act
    component.saveChanges();

    // Assert
    expect(emitted).toBeFalse();
  });

  it('should hide modal when saveChanges is called with valid input', () => {
    // Arrange
    spyOn(component, 'hide');
    component.show('file.txt');
    component.newFileName = 'renamed.txt';

    // Act
    component.saveChanges();

    // Assert
    expect(component.hide).toHaveBeenCalled();
  });
});
