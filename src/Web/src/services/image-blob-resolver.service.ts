import { inject, Injectable } from '@angular/core';
import { FileService } from './file.service';
import { splitDirAndName } from '../utils/path-utils';
import { getImageBlobUrl } from '../utils/image-utils';

@Injectable({
  providedIn: 'root'
})
export class ImageBlobResolverService {
  private readonly fileService = inject(FileService);

  private readonly privateImageBlobs: PrivateImageBlob[] = [];
  private readonly sharedImageBlobs: PrivateImageBlob[] = [];
  private readonly publicImageBlobs: PublicImageBlob[] = [];

  loadImage(src: string, groupId: number | undefined): ImageBlob {
    // Return existing blob if it exists.
    // By this point, the getImage method may or may not have finished which will be determined by the blobStatus.
    const existingBlob = this.privateImageBlobs.find(blob => blob.src === src && blob.groupId === groupId);
    if (existingBlob) {
      return existingBlob;
    }

    const [dir, name] = splitDirAndName(src);

    // Start loading the image blob
    const blobPromise = this.fileService.getImage(dir, name, groupId)
      .then(data => data ? getImageBlobUrl(name, data) : null)
      .catch(() => null);

    const imageBlob: PrivateImageBlob = {
      src,
      groupId,
      blobStatus: BlobStatus.Pending,
      blob: null,
      blobPromise: blobPromise,
    }

    this.privateImageBlobs.push(imageBlob);
    blobPromise.then(promiseResult => {
      imageBlob.blobStatus = BlobStatus.Resolved;
      imageBlob.blob = promiseResult;
    });
    return imageBlob;
  }

  loadSharedImage(src: string): ImageBlob {
    // Return existing blob if it exists.
    // By this point, the getImage method may or may not have finished which will be determined by the blobStatus.
    const existingBlob = this.sharedImageBlobs.find(blob => blob.src === src);
    if (existingBlob) {
      return existingBlob;
    }

    const [dir, name] = splitDirAndName(src);

    // Start loading the image blob
    const blobPromise = this.fileService.getSharedImage(src)
      .then(data => data ? getImageBlobUrl(name, data) : null)
      .catch(() => null);

    const imageBlob: SharedImageBlob = {
      src,
      blobStatus: BlobStatus.Pending,
      blob: null,
      blobPromise: blobPromise,
    }

    this.sharedImageBlobs.push(imageBlob);
    blobPromise.then(promiseResult => {
      imageBlob.blobStatus = BlobStatus.Resolved;
      imageBlob.blob = promiseResult;
    });
    return imageBlob;
  }

  loadPublicImage(src: string, publicFileHash: string): ImageBlob {
    const existingBlob = this.publicImageBlobs.find(blob => blob.src === src && blob.publicFileHash === publicFileHash);
    if (existingBlob) {
      return existingBlob;
    }

    const blobPromise = this.fileService.getPublicImage(publicFileHash, src)
      .then(data => data ? getImageBlobUrl(src, data) : null)
      .catch(() => null);

    const imageBlob: PublicImageBlob = {
      src,
      publicFileHash,
      blobStatus: BlobStatus.Pending,
      blob: null,
      blobPromise: blobPromise,
    }

    this.publicImageBlobs.push(imageBlob);
    blobPromise.then(promiseResult => {
      imageBlob.blobStatus = BlobStatus.Resolved;
      imageBlob.blob = promiseResult;
    });
    return imageBlob;
  }

  revokeImageBlobs() {
    [this.privateImageBlobs, this.publicImageBlobs].forEach(imageBlobsArray => {
      imageBlobsArray.forEach(async imageBlob => {
        const blob = await imageBlob.blobPromise;
        if (blob) {
          URL.revokeObjectURL(blob);
        }
      });

      // Clear the array
      imageBlobsArray.length = 0;
    });
  }
}

export interface ImageBlob {
  src: string;
  blobStatus: BlobStatus;
  blob: string | null;
  blobPromise: Promise<string | null>;
}

interface PrivateImageBlob extends ImageBlob {
  groupId?: number;
}

type SharedImageBlob = ImageBlob;

interface PublicImageBlob extends ImageBlob {
  publicFileHash: string;
}

export enum BlobStatus {
  Pending,
  Resolved,
}
