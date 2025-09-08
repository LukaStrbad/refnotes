import { File } from "../../model/file";
import { FileService } from "../../services/file.service";
import { ImageBlob, ImageBlobResolverService } from "../../services/image-blob-resolver.service";
import { TagService } from "../../services/tag.service";
import { MarkdownHighlighter } from "../../utils/markdown-highlighter";
import { resolveRelativeFolderPath, splitDirAndName } from "../../utils/path-utils";

export class FileProvider {
  private constructor(
    public readonly filePath: Promise<string>,
    public readonly listTags: () => Promise<string[]>,
    public readonly getFileInfo: () => Promise<File>,
    public readonly getFile: () => Promise<ArrayBuffer>,
    public readonly createSyncSocket: () => WebSocket,
    public readonly loadImage: (src: string) => ImageBlob,
  ) { }

  async createMarkdownHighlighter(
    showLineNumbers: boolean,
  ): Promise<MarkdownHighlighter> {
    const directoryPath = await this.filePath;
    const [dirPath,] = splitDirAndName(directoryPath);

    return new MarkdownHighlighter(
      showLineNumbers,
      dirPath,
      this.loadImage,
    );
  }

  static createRegularFileProvider(
    fileService: FileService,
    tagService: TagService,
    imageBlobResolver: ImageBlobResolverService,
    filePath: string,
    groupId?: number,
  ): FileProvider {
    const [directoryPath, fileName] = splitDirAndName(filePath);

    const listTags = () => tagService.listFileTags(directoryPath, fileName, groupId);
    const getFileInfo = () => fileService.getFileInfo(filePath, groupId);
    const getFile = () => fileService.getFile(directoryPath, fileName, groupId);
    const createSyncSocket = () => fileService.createFileSyncSocket(filePath, groupId);
    const loadImage = (src: string) => {
      const imagePath = resolveRelativeFolderPath(directoryPath, src);
      return imageBlobResolver.loadImage(imagePath, groupId);
    }

    return new FileProvider(
      Promise.resolve(filePath),
      listTags,
      getFileInfo,
      getFile,
      createSyncSocket,
      loadImage,
    );
  }

  static createSharedFileProvider(
    fileService: FileService,
    imageBlobResolver: ImageBlobResolverService,
    filePath: string,
  ): FileProvider {
    const [directoryPath] = splitDirAndName(filePath);

    const listTags = () => Promise.resolve([]); // Shared files do not have tags.
    const getFileInfo = () => fileService.getSharedFileInfo(filePath);
    const getFile = () => fileService.getSharedFile(filePath);
    const createSyncSocket = () => fileService.createSharedFileSyncSocket(filePath);
    const loadImage = (src: string) => {
      const imagePath = resolveRelativeFolderPath(directoryPath, src);
      return imageBlobResolver.loadSharedImage(imagePath);
    }

    return new FileProvider(
      Promise.resolve(filePath),
      listTags,
      getFileInfo,
      getFile,
      createSyncSocket,
      loadImage,
    );
  }

  static createPublicFileProvider(
    fileService: FileService,
    imageBlobResolver: ImageBlobResolverService,
    fileHash: string,
  ): FileProvider {
    const fileInfoPromise = fileService.getPublicFileInfo(fileHash);

    const filePath = fileInfoPromise.then(info => info.path);
    const listTags = () => Promise.resolve([]);
    const getFileInfo = () => fileInfoPromise;
    const getFile = () => fileService.getPublicFile(fileHash);
    const createSyncSocket = () => fileService.createPublicFileSyncSocket(fileHash);
    const loadImage = (src: string) => imageBlobResolver.loadPublicImage(src, fileHash);

    return new FileProvider(
      filePath,
      listTags,
      getFileInfo,
      getFile,
      createSyncSocket,
      loadImage,
    );
  }
}
