import { Directive, ElementRef, Input, OnInit } from '@angular/core';
import { environment } from '../environments/environment';

@Directive({
  // eslint-disable-next-line @angular-eslint/directive-selector
  selector: '[testTag]',
})
export class TestTagDirective implements OnInit {
  @Input() testTag = '';

  constructor(private el: ElementRef<HTMLElement>) {
  }

  ngOnInit(): void {
    if (!environment.production) {
      this.el.nativeElement.setAttribute('data-test', this.testTag);
    }
    // Angular also adds 'testtag' to the element's attributes, so we need to remove it
    this.el.nativeElement.removeAttribute('testtag');
  }
}
