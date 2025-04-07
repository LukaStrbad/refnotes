import { TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';
import {
  TranslateFakeLoader,
  TranslateLoader,
  TranslateModule,
  TranslateService,
} from '@ngx-translate/core';
import { NotificationService } from '../services/notification.service';
import { AuthService } from '../services/auth.service';
import { ActivatedRoute } from '@angular/router';

describe('AppComponent', () => {
  beforeEach(async () => {
    const authService = jasmine.createSpyObj<AuthService>('AuthService', ['isUserLoggedIn']);
    authService.isUserLoggedIn.and.returnValue(true);

    await TestBed.configureTestingModule({
      imports: [
        AppComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: [
        TranslateService,
        NotificationService,
        { provide: AuthService, useValue: authService },
        { provide: ActivatedRoute, useValue: { snapshot: {} } }
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should show notification when notification is added', () => {
    const notificationService = TestBed.inject(NotificationService);
    const fixture = TestBed.createComponent(AppComponent);

    const message = 'Test notification message';
    const title = 'Test notification title';
    notificationService.info(message, title);

    fixture.detectChanges();

    const nativeElement = fixture.nativeElement as HTMLElement;
    const notificationTitle = nativeElement.querySelector('[data-test="notification.title"]');
    const notificationMessage = nativeElement.querySelector('[data-test="notification.message"]');

    expect(notificationTitle?.innerHTML).toContain(title);
    expect(notificationMessage?.textContent).toContain(message);
  });
});
