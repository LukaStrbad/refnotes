import { File } from "../../model/file";
import { FileService } from "../../services/file.service";
import { ImageBlob } from "../../services/image-blob-resolver.service";
import { TagService } from "../../services/tag.service";
import { MarkdownHighlighter } from "../../utils/markdown-highlighter";
import { splitDirAndName } from "../../utils/path-utils";

export class FileProvider {
  private constructor(
    public readonly filePath: Promise<string>,
    public readonly listTags: () => Promise<string[]>,
    public readonly getFileInfo: () => Promise<File>,
    public readonly getFile: () => Promise<ArrayBuffer>,
    public readonly createSyncSocket: () => WebSocket,
  ) { }

  async createMarkdownHighlighter(
    showLineNumbers: boolean,
    loadImage: (src: string) => ImageBlob,
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
      listTags,
      getFileInfo,
      getFile,
      createSyncSocket,
    );
  }
}
