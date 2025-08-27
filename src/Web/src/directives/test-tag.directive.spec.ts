import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TestTagDirective } from './test-tag.directive';
import { environment } from '../environments/environment';

describe('TestTagDirective', () => {
  @Component({
    template: `<div id="target" testTag="test.tag"></div>`,
    imports: [TestTagDirective]
  })
  class HostComponent { }

  let fixture: ComponentFixture<HostComponent>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HostComponent],
    });
  });

  it('should set data-test tag in development', () => {
    const original = environment.production;
    environment.production = false;
    try {
      fixture = TestBed.createComponent(HostComponent);
      fixture.detectChanges();

      const el: HTMLElement = fixture.nativeElement.querySelector('#target');
      expect(el.getAttribute('data-test')).toBe('test.tag');
      // Delete the testtag attribute that Angular adds
      expect(el.getAttribute('testtag')).toBeNull();
    } finally {
      environment.production = original;
    }
  });

  it('should not set data-test tag in production', () => {
    const original = environment.production;
    environment.production = true;
    try {
      fixture = TestBed.createComponent(HostComponent);
      fixture.detectChanges();

      const el: HTMLElement = fixture.nativeElement.querySelector('#target');
      expect(el.getAttribute('data-test')).toBeNull();
      expect(el.getAttribute('testtag')).toBeNull();
    } finally {
      environment.production = original;
    }
  });
});
