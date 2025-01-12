import { TestTagDirective } from './test-tag.directive';
import { ElementRef } from '@angular/core';
import { environment } from '../environments/environment';

describe('TestTagDirective', () => {
  it('should set data-test tag in development', () => {
    const element = document.createElement('div');
    const elementRef = new ElementRef(element);
    const directive = new TestTagDirective(elementRef);
    directive.testTag = 'test';
    directive.ngOnInit();

    expect(element.getAttribute('data-test')).toBe('test');
    expect(element.getAttribute('testtag')).toBeNull();
  });

  it('should not set data-test tag in production', () => {
    const originalProduction = environment.production;
    environment.production = true;
    try {
      const element = document.createElement('div');
      const elementRef = new ElementRef(element);
      const directive = new TestTagDirective(elementRef);
      directive.testTag = 'test';
      directive.ngOnInit();

      expect(element.getAttribute('data-test')).toBeNull();
      expect(element.getAttribute('testtag')).toBeNull();
    } finally {
      environment.production = originalProduction;
    }
  });
});
