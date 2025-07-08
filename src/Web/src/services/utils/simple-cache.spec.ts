import { fakeAsync, TestBed, tick } from '@angular/core/testing';

import { SimpleCache } from './simple-cache';

describe('SimpleCache', () => {
  let service: SimpleCache<string>;

  beforeEach(() => {
    service = new SimpleCache<string>(1000); // 1 second cache duration
    TestBed.configureTestingModule({});
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should return set value', fakeAsync(() => {
    const initialValue = 'test';
    service.set(initialValue);
    expect(service.get()).toBe(initialValue);
  }));

  it('should return undefined for expired cache', fakeAsync(() => {
    service.set('test');
    expect(service.get()).toBe('test');
    // Simulate cache expiration
    tick(1001)
    expect(service.get()).toBeUndefined();
  }));

  it('should return value from provider if cache is expired', fakeAsync(async () => {
    const value = await service.getOrProvide(async () => 'newValue');
    expect(value).toBe('newValue');
  }));

  it('should return cached value if not expired', fakeAsync(async () => {
    service.set('cachedValue');
    const value = await service.getOrProvide(async () => 'newValue');
    expect(value).toBe('cachedValue');
  }));

  it('should clear cache', () => {
    service.set('test');
    service.clear();
    expect(service.get()).toBeUndefined();
  });
});
