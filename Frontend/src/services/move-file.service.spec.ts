import { TestBed } from '@angular/core/testing';

import { MoveFileService } from './move-file.service';
import { FileService } from './file.service';

describe('MoveFileService', () => {
  let service: MoveFileService;
  let fileService: jasmine.SpyObj<FileService>;

  beforeEach(() => {
    fileService = jasmine.createSpyObj('FileService', ['moveFile']);

    TestBed.configureTestingModule({
      providers: [
        { provide: FileService, useValue: fileService },
      ],
    });
    service = TestBed.inject(MoveFileService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should add file to move list', () => {
    const filePath = '/path/to/file.txt';
    service.addFile(filePath);
    expect(service.filesToMove.has(filePath)).toBeTrue();
  });

  it('should remove file from move list', () => {
    const filePath = '/path/to/file.txt';
    service.addFile(filePath);
    service.removeFile(filePath);
    expect(service.filesToMove.has(filePath)).toBeFalse();
    expect(service.filesToMove.size).toBe(0);
  });

  it('should clear move list', () => {
    service.addFile('/path/to/file1.txt');
    service.addFile('/path/to/file2.txt');
    service.clearFilesToMove();
    expect(service.filesToMove.size).toBe(0);
  });

  it('should move files to destination', async () => {
    const filePath1 = '/path/to/file1.txt';
    const filePath2 = '/path/to/file2.txt';
    const destination = '/new/path/';

    service.addFile(filePath1);
    service.addFile(filePath2);

    fileService.moveFile.and.returnValue(Promise.resolve({}));

    await service.moveFiles(destination);

    expect(fileService.moveFile).toHaveBeenCalledWith(filePath1, '/new/path/file1.txt');
    expect(fileService.moveFile).toHaveBeenCalledWith(filePath2, '/new/path/file2.txt');
    expect(service.filesToMove.size).toBe(0);
  });
});
