import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EditTagsModalComponent } from './edit-tags-modal.component';
import {
  TranslateFakeLoader,
  TranslateLoader,
  TranslateModule,
} from '@ngx-translate/core';
import { ElementRef } from '@angular/core';
import { By } from '@angular/platform-browser';

describe('EditTagsModalComponent', () => {
  let component: EditTagsModalComponent;
  let fixture: ComponentFixture<EditTagsModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        EditTagsModalComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(EditTagsModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render tags when show is called', () => {
    component.show('file.txt', ['Tag 1', 'Tag 2']);

    fixture.detectChanges();

    const tags = fixture.nativeElement.querySelectorAll(
      '[data-test="list-item-tag"]',
    ) as NodeList;
    expect(tags.length).toBe(2);
    expect(tags[0].textContent).toBe('Tag 1');
    expect(tags[1].textContent).toBe('Tag 2');
  });

  it('should emit onAdd event with tag name when addTag is called', () => {
    let newTag = '';
    component.onAdd.subscribe(([fileName, tag]) => (newTag = tag));

    const input = fixture.nativeElement.querySelector(
      '[data-test="input-add-tag"]',
    ) as HTMLInputElement;
    const button = fixture.nativeElement.querySelector(
      '[data-test="button-add-tag"]',
    ) as HTMLButtonElement;

    input.value = 'New Tag';
    input.dispatchEvent(new Event('input'));
    button.click();

    expect(newTag).toBe('New Tag');
  });

  it('should emit onRemove event with tag name when removeTag is called', () => {
    let removedTag = '';
    component.onRemove.subscribe(([fileName, tag]) => (removedTag = tag));

    component.show('file.txt', ['Tag 1', 'Tag 2']);

    fixture.detectChanges();

    const tags = fixture.nativeElement.querySelectorAll(
      '[data-test="list-item-tag"]',
    );
    const removeButton = tags[0].querySelector(
      '[data-test="button-remove-tag"]',
    ) as HTMLButtonElement;

    removeButton.click();

    expect(removedTag).toBe('Tag 1');
  });
});
