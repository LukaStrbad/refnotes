import { TestBed } from '@angular/core/testing';

import { SelectFileService } from './select-file.service';
import { FileService } from './file.service';

describe('SelectFileService', () => {
  let service: SelectFileService;
  let fileService: jasmine.SpyObj<FileService>;

  beforeEach(() => {
    fileService = jasmine.createSpyObj('FileService', ['moveFile']);

    TestBed.configureTestingModule({
      providers: [
        { provide: FileService, useValue: fileService },
      ],
    });
    service = TestBed.inject(SelectFileService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should add file to move list', () => {
    const filePath = '/path/to/file.txt';
    service.addFile(filePath);
    expect(service.selectedFiles.has(filePath)).toBeTrue();
  });

  it('should remove file from move list', () => {
    const filePath = '/path/to/file.txt';
    service.addFile(filePath);
    service.removeFile(filePath);
    expect(service.selectedFiles.has(filePath)).toBeFalse();
    expect(service.selectedFiles.size).toBe(0);
  });

  it('should clear move list', () => {
    service.addFile('/path/to/file1.txt');
    service.addFile('/path/to/file2.txt');
    service.clearSelectedFiles();
    expect(service.selectedFiles.size).toBe(0);
  });

  it('should move files to destination', async () => {
    const filePath1 = '/path/to/file1.txt';
    const filePath2 = '/path/to/file2.txt';
    const destination = '/new/path/';

    service.addFile(filePath1);
    service.addFile(filePath2);

    fileService.moveFile.and.returnValue(Promise.resolve({}));

    await service.moveFiles(destination, undefined);

    expect(fileService.moveFile).toHaveBeenCalledWith(filePath1, '/new/path/file1.txt', undefined);
    expect(fileService.moveFile).toHaveBeenCalledWith(filePath2, '/new/path/file2.txt', undefined);
    expect(service.selectedFiles.size).toBe(0);
  });
});
