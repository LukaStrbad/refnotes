import { TestBed } from '@angular/core/testing';

import { UserService } from './user.service';
import { provideHttpClient } from '@angular/common/http';
import { AuthService } from './auth.service';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../environments/environment';
import { TranslateFakeLoader, TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';

const apiUrl = environment.apiUrl + '/user';

describe('UserService', () => {
  let service: UserService;
  let httpMock: HttpTestingController;
  let auth: jasmine.SpyObj<AuthService>;

  beforeEach(() => {
    auth = jasmine.createSpyObj('AuthService', ['setUserAndToken']);

    TestBed.configureTestingModule({
      imports: [
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateFakeLoader,
          },
        }),
      ],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
      ],
    });
    service = TestBed.inject(UserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should call setUserAndToken after confirming email', async () => {
    const token = 'test-token';

    const promise = service.confirmEmail(token);
    const req = httpMock.expectOne(`${apiUrl}/confirmEmail/${token}`);
    req.flush({});
    await promise;
    expect(auth.setUserAndToken).toHaveBeenCalled();
  });


  it('should resend email confirmation', async () => {
    const translate = TestBed.inject(TranslateService);
    spyOnProperty(translate, 'currentLang').and.returnValue('en');

    const promise = service.resendEmailConfirmation();
    const req = httpMock.expectOne(`${apiUrl}/resendEmailConfirmation?lang=en`);
    req.flush({});
    await promise;
  });
});
