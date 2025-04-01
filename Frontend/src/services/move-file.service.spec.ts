import { TestBed } from '@angular/core/testing';

import { MoveFileService } from './move-file.service';

describe('MoveFileService', () => {
  let service: MoveFileService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MoveFileService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
