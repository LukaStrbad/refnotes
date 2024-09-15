import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MdEditorComponent } from './md-editor.component';

describe('MdEditorComponent', () => {
  let component: MdEditorComponent;
  let fixture: ComponentFixture<MdEditorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MdEditorComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MdEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
