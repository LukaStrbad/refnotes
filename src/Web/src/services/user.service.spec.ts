import { TestBed } from '@angular/core/testing';

import { UserService } from './user.service';
import { provideHttpClient } from '@angular/common/http';
import { AuthService } from './auth.service';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../environments/environment';
import { TranslateFakeLoader, TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { UserResponse } from '../model/user-response';

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

  it('should get account info', async () => {
    const mockResponse: UserResponse = {
      id: 1,
      username: 'testuser',
      name: 'Test User',
      email: 'test@test.com',
      roles: ['user'],
      emailConfirmed: true,
    };

    const promise = service.getAccountInfo();
    const req = httpMock.expectOne(`${apiUrl}/accountInfo`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
    const response = await promise;
    expect(response).toEqual(mockResponse);
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

  it('should edit account and call setUserAndToken', async () => {
    const editRequest = {
      newName: 'New Name',
      newUsername: 'newusername',
      newEmail: 'newemail@test.com'
    };
    const mockResponse: UserResponse = {
      id: 1,
      username: 'newusername',
      name: 'New Name',
      email: 'newemail@test.com',
      roles: ['user'],
      emailConfirmed: false,
    };

    const promise = service.editAccount(editRequest, 'en');
    const req = httpMock.expectOne(`${apiUrl}/edit?lang=en`);
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
    const response = await promise;
    expect(response).toEqual(mockResponse);
  });

  it('should update password', async () => {
    const oldPassword = 'oldPassword';
    const newPassword = 'newPassword';

    const promise = service.updatePassword({ oldPassword, newPassword });
    const req = httpMock.expectOne(`${apiUrl}/updatePassword`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ oldPassword, newPassword });
    req.flush({});
    await promise;
  });
});
