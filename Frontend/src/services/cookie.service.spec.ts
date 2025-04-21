import { TestBed } from '@angular/core/testing';

import { CookieService } from './cookie.service';

describe('CookieService', () => {
  let service: CookieService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CookieService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should set and get a cookie', () => {
    const name = 'testCookie';
    const value = 'testValue';
    const expires = new Date();
    expires.setDate(expires.getDate() + 1);

    service.setCookie(name, value, expires);
    const retrievedValue = service.getCookie(name);

    expect(retrievedValue).toBe(value);
  });

  it('should return null for a non-existent cookie', () => {
    const retrievedValue = service.getCookie('nonExistentCookie');
    expect(retrievedValue).toBeNull();
  });
});
