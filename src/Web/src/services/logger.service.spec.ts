import { TestBed } from '@angular/core/testing';

import { LoggerService } from './logger.service';
import { environment } from '../environments/environment';

export function createMockLoggerService(): jasmine.SpyObj<LoggerService> {
  return jasmine.createSpyObj('LoggerService', ['currentTimestamp', 'info', 'warn', 'error']);
}

describe('LoggerService', () => {
  let service: LoggerService;
  let originalProduction: boolean;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(LoggerService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  beforeEach(() => {
    originalProduction = environment.production;
    environment.production = false;
  });

  afterEach(() => {
    environment.production = originalProduction;
  });

  it('should return a valid date-time string from currentTimestamp()', () => {
    const timestamp = service.currentTimestamp();
    expect(timestamp).toMatch(/^\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}/);
  });

  it('should log info when not in production', () => {
    const consoleSpy = spyOn(console, 'log');
    service.info('test message');
    expect(consoleSpy).toHaveBeenCalled();
    expect(consoleSpy.calls.mostRecent().args[0]).toContain('[INFO] test message');
  });

  it('should not log info when in production', () => {
    environment.production = true;
    const consoleSpy = spyOn(console, 'log');
    service.info('test message');
    expect(consoleSpy).not.toHaveBeenCalled();
  });

  it('should log warning when not in production', () => {
    const consoleSpy = spyOn(console, 'warn');
    service.warn('test warning');
    expect(consoleSpy).toHaveBeenCalled();
    expect(consoleSpy.calls.mostRecent().args[0]).toContain('[WARN] test warning');
  });

  it('should also log warning when in production', () => {
    environment.production = true;
    const consoleSpy = spyOn(console, 'warn');
    service.warn('test warning');
    expect(consoleSpy).toHaveBeenCalled();
    expect(consoleSpy.calls.mostRecent().args[0]).toContain('[WARN] test warning');
  });

  it('should log error when not in production', () => {
    const consoleSpy = spyOn(console, 'error');
    service.error('test error');
    expect(consoleSpy).toHaveBeenCalled();
    expect(consoleSpy.calls.mostRecent().args[0]).toContain('[ERROR] test error');
  });

  it('should also log error when in production', () => {
    environment.production = true;
    const consoleSpy = spyOn(console, 'error');
    service.error('test error');
    expect(consoleSpy).toHaveBeenCalled();
    expect(consoleSpy.calls.mostRecent().args[0]).toContain('[ERROR] test error');
  });
});
