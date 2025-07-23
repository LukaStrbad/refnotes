import { inject, Injectable } from '@angular/core';
import { FileService } from './file.service';
import { splitDirAndName } from '../utils/path-utils';
import { getImageBlobUrl } from '../utils/image-utils';

@Injectable({
  providedIn: 'root'
})
export class ImageBlobResolverService {
  private readonly fileService = inject(FileService);

  private readonly imageBlobs: ImageBlob[] = [];

  loadImage(src: string, groupId: number | undefined): ImageBlob {
    // Return existing blob if it exists.
    // By this point, the getImage method may or may not have finished which will be determined by the blobStatus.
    const existingBlob = this.imageBlobs.find(blob => blob.src === src && blob.groupId === groupId);
    if (existingBlob) {
      return existingBlob;
    }

    const [dir, name] = splitDirAndName(src);

    // Start loading the image blob
    const blobPromise = this.fileService.getImage(dir, name, groupId)
      .then(data => data ? getImageBlobUrl(name, data) : null)
      .catch(() => null);

    const imageBlob: ImageBlob = {
      src,
      groupId,
      blobStatus: BlobStatus.Pending,
      blob: null,
      blobPromise: blobPromise,
    }
    this.imageBlobs.push(imageBlob);
    blobPromise.then(promiseResult => {
      imageBlob.blobStatus = BlobStatus.Resolved;
      imageBlob.blob = promiseResult;
    });
    return imageBlob;
  }

  revokeImageBlobs() {
    this.imageBlobs.forEach(async imageBlob => {
      const blob = await imageBlob.blobPromise;
      if (blob) {
        URL.revokeObjectURL(blob);
      }
    });

    // Clear the array
    this.imageBlobs.length = 0;
  }
}

export interface ImageBlob {
  src: string;
  groupId?: number;
  blobStatus: BlobStatus;
  blob: string | null;
  blobPromise: Promise<string | null>;
}

export enum BlobStatus {
  Pending,
  Resolved,
}
