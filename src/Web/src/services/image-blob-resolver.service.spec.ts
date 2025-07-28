import { fakeAsync, TestBed, tick } from '@angular/core/testing';

import { BlobStatus, ImageBlobResolverService } from './image-blob-resolver.service';
import { FileService } from './file.service';

describe('ImageBlobResolverService', () => {
  let service: ImageBlobResolverService;
  let fileService: jasmine.SpyObj<FileService>;

  beforeEach(() => {
    fileService = jasmine.createSpyObj<FileService>('FileService', ['getImage', 'getPublicImage']);

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
    expect(imageBlob.blobStatus).toBe(BlobStatus.Pending);
    expect(imageBlob.blobPromise).toBeDefined();

    await imageBlob.blobPromise;

    expect(imageBlob.blobStatus).toBe(BlobStatus.Resolved);
    expect(imageBlob.blob).toContain('blob:http')
  });

  it('should load a public image blob', async () => {
    const src = 'path/to/public-image.jpg';
    const publicFileHash = 'publicFileHash123';

    fileService.getPublicImage.and.resolveTo(new ArrayBuffer(8));

    const publicImageBlob = service.loadPublicImage(src, publicFileHash);

    expect(publicImageBlob.src).toBe(src);
    expect(publicImageBlob.blobStatus).toBe(BlobStatus.Pending);
    expect(publicImageBlob.blobPromise).toBeDefined();

    await publicImageBlob.blobPromise;

    expect(publicImageBlob.blobStatus).toBe(BlobStatus.Resolved);
    expect(publicImageBlob.blob).toContain('blob:http');
  });

  it('should revoke image blobs', fakeAsync(async () => {
    const src = 'path/to/image.jpg';
    const groupId = 1;

    fileService.getImage.and.resolveTo(new ArrayBuffer(8));
    fileService.getPublicImage.and.resolveTo(new ArrayBuffer(8));

    const privateImageBlob = service.loadImage(src, groupId);
    const publicImageBlobs = service.loadPublicImage(src, 'publicFileHash');
    await privateImageBlob.blobPromise;
    await publicImageBlobs.blobPromise;

    spyOn(URL, 'revokeObjectURL');

    service.revokeImageBlobs();
    tick();

    expect(URL.revokeObjectURL).toHaveBeenCalledWith(privateImageBlob.blob as string);
    expect(service['privateImageBlobs'].length).toBe(0);
    expect(service['publicImageBlobs'].length).toBe(0);
  }));
});
