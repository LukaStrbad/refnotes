import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AskModalComponent } from './ask-modal.component';
import { TranslateFakeLoader, TranslateLoader, TranslateModule } from '@ngx-translate/core';

describe('AskModalComponent', () => {
  let component: AskModalComponent;
  let fixture: ComponentFixture<AskModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        AskModalComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ]
    })
      .compileComponents();

    fixture = TestBed.createComponent(AskModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should set title and message when setText is called', async () => {
    const title = 'Test Title';
    const message = 'Test Message';

    component.setText(title, message);
    fixture.detectChanges();

    expect(await component.title()).toBe(title);
    expect(await component.message()).toBe(message);
  });

  it('should set body when setBody is called', () => {
    const body = 'Test Body';

    component.setBody(body);
    fixture.detectChanges();

    expect(component.body()).toBe(body);
  });
});
