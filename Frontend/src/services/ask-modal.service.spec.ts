import { TestBed } from '@angular/core/testing';

import { AskModalService } from './ask-modal.service';

describe('AskModalService', () => {
  let service: AskModalService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AskModalService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
