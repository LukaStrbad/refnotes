import { TestBed } from '@angular/core/testing';

import { BrowserFavoriteService } from './browser-favorite.service';

describe('BrowserFavoriteService', () => {
  let service: BrowserFavoriteService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(BrowserFavoriteService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
