import { resolveRelativeFolderPath } from "./path-utils";

export function resolveImageUrl(currentPath: string, src: string): ResolvedImageUrl {
  if (src.startsWith('http')) {
    return {
      url: src,
      isHttp: true,
    };
  }

  const relativeSrc = resolveRelativeFolderPath(currentPath, src);
  return {
    url: relativeSrc,
    isHttp: false,
  };
}

interface ResolvedImageUrl {
  url: string;
  isHttp: boolean;
}

export function getImageBlobUrl(name: string, data: ArrayBuffer): string {
  const imageType = name.split('.').pop() ?? 'png';
  const blob = new Blob([data], { type: `image/${imageType}` });
  const objectURL = URL.createObjectURL(blob);
  return objectURL;
}
