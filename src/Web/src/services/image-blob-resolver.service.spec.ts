import { fakeAsync, TestBed, tick } from '@angular/core/testing';

import { BlobStatus, ImageBlobResolverService } from './image-blob-resolver.service';
import { FileService } from './file.service';

describe('ImageBlobResolverService', () => {
  let service: ImageBlobResolverService;
  let fileService: jasmine.SpyObj<FileService>;

  beforeEach(() => {
    fileService = jasmine.createSpyObj<FileService>('FileService', ['getImage']);

    TestBed.configureTestingModule({
      providers: [
        { provide: FileService, useValue: fileService },
      ]
    });
    service = TestBed.inject(ImageBlobResolverService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should load an image blob', async () => {
    const src = 'path/to/image.jpg';
    const groupId = 1;

    fileService.getImage.and.resolveTo(new ArrayBuffer(8));

    const imageBlob = service.loadImage(src, groupId);

    expect(imageBlob.src).toBe(src);
    expect(imageBlob.groupId).toBe(groupId);
    expect(imageBlob.blobStatus).toBe(BlobStatus.Pending);
    expect(imageBlob.blobPromise).toBeDefined();

    await imageBlob.blobPromise;

    expect(imageBlob.blobStatus).toBe(BlobStatus.Resolved);
    expect(imageBlob.blob).toContain('blob:http')
  });

  it('should revoke image blobs', fakeAsync(async () => {
    const src = 'path/to/image.jpg';
    const groupId = 1;

    fileService.getImage.and.resolveTo(new ArrayBuffer(8));

    const imageBlob = service.loadImage(src, groupId);
    await imageBlob.blobPromise;

    spyOn(URL, 'revokeObjectURL');

    service.revokeImageBlobs();
    tick();

    expect(URL.revokeObjectURL).toHaveBeenCalledWith(imageBlob.blob as string);
    expect(service['imageBlobs'].length).toBe(0);
  }));
});
