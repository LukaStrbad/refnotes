import { File } from "../../model/file";
import { FileService } from "../../services/file.service";
import { TagService } from "../../services/tag.service";
import { MarkdownHighlighter } from "../../utils/markdown-highlighter";
import { splitDirAndName } from "../../utils/path-utils";

export class FileProvider {
  private constructor(
    public filePath: Promise<string>,
    public getImage: (path: string) => Promise<ArrayBuffer | null>,
    public listTags: () => Promise<string[]>,
    public getFileInfo: () => Promise<File>,
    public getFile: () => Promise<ArrayBuffer>,
    public createSyncSocket: () => WebSocket,
  ) { }

  async createMarkdownHighlighter(
    showLineNumbers: boolean,
    loadImage: (dirPath: string, fileName: string) => Promise<ArrayBuffer | null>,
  ): Promise<MarkdownHighlighter> {
    const directoryPath = await this.filePath;
    const [dirPath,] = splitDirAndName(directoryPath);

    return new MarkdownHighlighter(
      showLineNumbers,
      dirPath,
      loadImage,
    );
  }

  static createRegularFileProvider(
    fileService: FileService,
    tagService: TagService,
    filePath: string,
    groupId?: number,
  ): FileProvider {
    const [directoryPath, fileName] = splitDirAndName(filePath);

    const getImage = (path: string) => {
      const [dirPath, fileName] = splitDirAndName(path);
      return fileService.getImage(dirPath, fileName, groupId);
    };
    const listTags = () => tagService.listFileTags(directoryPath, fileName, groupId);
    const getFileInfo = () => fileService.getFileInfo(filePath, groupId);
    const getFile = () => fileService.getFile(directoryPath, fileName, groupId);
    const createSyncSocket = () => fileService.createFileSyncSocket(filePath, groupId);

    return new FileProvider(
      Promise.resolve(filePath),
      getImage,
      listTags,
      getFileInfo,
      getFile,
      createSyncSocket,
    );
  }

  static createPublicFileProvider(
    fileService: FileService,
    fileHash: string,
  ): FileProvider {
    const fileInfoPromise = fileService.getPublicFileInfo(fileHash);

    const filePath = fileInfoPromise.then(info => info.path);
    const getImage = (path: string) => fileService.getPublicImage(fileHash, path);
    const listTags = () => Promise.resolve([]);
    const getFileInfo = () => fileInfoPromise;
    const getFile = () => fileService.getPublicFile(fileHash);
    const createSyncSocket = () => fileService.createPublicFileSyncSocket(fileHash);

    return new FileProvider(
      filePath,
      getImage,
      listTags,
      getFileInfo,
      getFile,
      createSyncSocket,
    );
  }
}
