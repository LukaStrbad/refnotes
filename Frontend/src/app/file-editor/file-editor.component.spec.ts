import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FileEditorComponent } from './file-editor.component';
import {
  TranslateFakeLoader,
  TranslateLoader,
  TranslateModule,
  TranslateService,
} from '@ngx-translate/core';
import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import {BrowserService} from "../../services/browser.service";

@Component({
  selector: 'app-md-editor',
  template: '',
})
class MdEditorStub {}

describe('FileEditorComponent', () => {
  let component: FileEditorComponent;
  let fixture: ComponentFixture<FileEditorComponent>;
  let browserService: jasmine.SpyObj<BrowserService>;

  beforeEach(async () => {
    browserService = jasmine.createSpyObj('BrowserService', ['getFile', 'saveTextfile']);

    await TestBed.configureTestingModule({
      imports: [
        FileEditorComponent,
        MdEditorStub,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: [
        TranslateService,
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: () => 'test' } } } },
        { provide: BrowserService, useValue: browserService },
      ],
    }).compileComponents();
  });

  it('should create', () => {
    browserService.getFile.and.resolveTo(new ArrayBuffer(0));

    fixture = TestBed.createComponent(FileEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component).toBeTruthy();
  });

  it('should load file content', async () => {
    browserService.getFile.and.resolveTo(new TextEncoder().encode('test'));

    fixture = TestBed.createComponent(FileEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    await fixture.whenStable();

    expect(component.content).toBe('test');
    expect(component.loading).toBeFalse();
  });

  it('should show loading skeleton', async () => {
    let resolve: ((value?: unknown) => void) | null = null;
    const waitPromise = new Promise(r => {resolve = r;});
    browserService.getFile.and.callFake(async () => {
      await waitPromise;
      return new TextEncoder().encode('test');
    });

    fixture = TestBed.createComponent(FileEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    await fixture.whenStable();

    let skeleton = fixture.nativeElement.querySelector('.skeleton');
    expect(skeleton).toBeTruthy();
    expect(component.content).toBe('');
    expect(component.loading).toBeTrue();

    resolve!();
    await waitPromise;
    await fixture.whenStable();
    fixture.detectChanges();

    skeleton = fixture.nativeElement.querySelector('.skeleton');
    expect(skeleton).toBeFalsy();
    expect(component.content).toBe('test');
    expect(component.loading).toBeFalse();
  });

  it('should save file content', async () => {
    browserService.getFile.and.resolveTo(new ArrayBuffer(0));

    fixture = TestBed.createComponent(FileEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    await fixture.whenStable();

    component.content = 'test';
    const saveButton = fixture.nativeElement.querySelector('[data-test="save-button"]');
    saveButton.click();

    expect(browserService.saveTextfile).toHaveBeenCalledWith('test', 'test', 'test');
  });
});
