import { TestBed } from '@angular/core/testing';

import { ImageBlobResolverService } from './image-blob-resolver.service';

describe('ImageBlobResolverService', () => {
  let service: ImageBlobResolverService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ImageBlobResolverService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
