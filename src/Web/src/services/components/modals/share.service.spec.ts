import { TestBed } from '@angular/core/testing';

import { ShareService } from './share.service';
import { PublicFileService } from '../../public-file.service';

describe('ShareService', () => {
  let service: ShareService;
  let publicFileService: jasmine.SpyObj<PublicFileService>;

  beforeEach(() => {
    publicFileService = jasmine.createSpyObj('PublicFileService', ['getUrl', 'createPublicFile', 'deletePublicFile']);

    TestBed.configureTestingModule({
      providers: [
        ShareService,
        { provide: PublicFileService, useValue: publicFileService }
      ]
    });
    service = TestBed.inject(ShareService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should set filename and directory path when setFilePath is called', () => {
    const filePath = '/path/to/file.txt';
    service.setFilePath(filePath);

    expect(service.filePath()).toBe(filePath);
    expect(service.directoryPath()).toBe('/path/to');
    expect(service.fileName()).toBe('file.txt');
    expect(service.publicLink()).toBeNull(); // Should reset public link
  });

  it('should set public state and create public link when setPublicState is called with true', async () => {
    const exampleLink = 'https://example.com/public/test-hash';
    const filePath = '/path/to/file.txt';
    service.setFilePath(filePath);
    publicFileService.createPublicFile.and.resolveTo(exampleLink);

    await service.setPublicState(true);

    expect(service.isPublic()).toBeTrue();
    expect(service.publicLink()).toBe(exampleLink);
    expect(publicFileService.createPublicFile).toHaveBeenCalledWith(filePath);
  });

  it('should delete public file and clear public link when setPublicState is called with false', async () => {
    const filePath = '/path/to/file.txt';
    const exampleLink = 'https://example.com/public/test-hash';
    service.setFilePath(filePath);

    // Set up the file as public first using the public API
    publicFileService.createPublicFile.and.resolveTo(exampleLink);
    await service.setPublicState(true);
    expect(service.publicLink()).toBe(exampleLink); // Verify public link is set

    // Now test making it private
    publicFileService.deletePublicFile.and.resolveTo();
    await service.setPublicState(false);

    expect(service.isPublic()).toBeFalse();
    expect(service.publicLink()).toBeNull(); // Should clear public link
    expect(publicFileService.deletePublicFile).toHaveBeenCalledWith(filePath);
  });

  it('should load public link when loadPublicLink is called', async () => {
    const filePath = '/path/to/file.txt';
    const exampleLink = 'https://example.com/public/test-hash';
    service.setFilePath(filePath);

    publicFileService.getUrl.and.resolveTo(exampleLink);

    await service.loadPublicLink();

    expect(service.publicLink()).toBe(exampleLink);
    expect(service.isPublic()).toBeTrue(); // Should be public if link is returned
    expect(publicFileService.getUrl).toHaveBeenCalledWith(filePath);
  });
});
