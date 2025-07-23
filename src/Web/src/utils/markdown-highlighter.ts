import { LRUCache } from "lru-cache";
import { markedHighlight } from "marked-highlight";
import hljs from 'highlight.js';
import { Marked, Tokens } from "marked";
import { resolveImageUrl } from "./image-utils";
import { ElementRef } from "@angular/core";
import { BlobStatus, ImageBlob } from "../services/image-blob-resolver.service";

function escapeHtml(unsafe: string) {
  return unsafe
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}

function strHash(s: string) {
  return s.split('').reduce(function (a, b) {
    a = (a << 5) - a + b.charCodeAt(0);
    return a & a;
  }, 0);
}

export class MarkdownHighlighter {
  private readonly highlightCache = new LRUCache<number, string>({ max: 1000 });
  private readonly marked: Marked;
  // imageUrls: { elementAttr: string; src: string; blob: string | null }[] = [];

  get showLineNumbers() {
    return this._showLineNumbers;
  }

  set showLineNumbers(value: boolean) {
    this._showLineNumbers = value;
  }

  constructor(
    private _showLineNumbers: boolean,
    private currentPath: string,
    private loadImage: (src: string) => ImageBlob
  ) {
    this.marked = new Marked(this.highlightExtension(), {
      renderer: this.imageExtension()
    });
  }

  parse(text: string) {
    return this.marked.parse(text);
  }

  lexer(text: string) {
    return this.marked.lexer(text);
  }

  /**
   * Updates the image elements with the correct image source
   */
  updateImages(previewContentElement: ElementRef<HTMLElement>) {
    const imagesWithDataId = previewContentElement.nativeElement.querySelectorAll('img[data-image-id]');

    imagesWithDataId.forEach((imageElement) => {
      const imageId = imageElement.getAttribute('data-image-id');
      if (!imageId) {
        return;
      }
      let imgSrc = imageId.split('image-')[1];
      if (!imgSrc) {
        return;
      }
      imgSrc = atob(imgSrc);

      const imageUrl = this.loadImage(imgSrc);

      imageUrl.blobPromise.then((blobUrl) => {
        if (!blobUrl) {
          return;
        }

        imageElement.setAttribute('src', blobUrl);
      });
    });
  }

  private highlightExtension() {
    return markedHighlight({
      emptyLangClass: 'hljs',
      langPrefix: 'hljs-',
      highlight: (str, lang) => {
        const hash = strHash(str + lang);
        if (this.highlightCache.has(hash)) {
          return this.highlightCache.get(hash)!;
        }
        let rawCode: string;
        if (lang && hljs.getLanguage(lang)) {
          try {
            rawCode = hljs.highlight(str, {
              language: lang,
              ignoreIllegals: true,
            }).value;
          } catch (e) {
            console.error('Error highlighting code', e);
            rawCode = escapeHtml(str);
          }
        } else {
          rawCode = escapeHtml(str);
        }

        let code = rawCode;
        if (this.showLineNumbers) {
          const split = rawCode.split('\n');
          const lineNumberDigits = split.length.toString().length;
          code = split.reduce((acc, line, i) => {
            const lineNumber = (i + 1).toString().padStart(lineNumberDigits);
            return `${acc}<span class="line-number">${lineNumber}</span>${line}\n`;
          }, '');
        }

        this.highlightCache.set(hash, code);
        return code;
      },
    });
  }

  private imageExtension() {
    return {
      image: (token: Tokens.Image) => {
        let src = token.href;
        const title = token.title ?? '';
        const alt = token.text;

        const resolvedImageUrl = resolveImageUrl(this.currentPath, src);
        // If the image points to an external URL, just return the image tag
        if (resolvedImageUrl.isHttp) {
          return `<img src="${resolvedImageUrl.url}" alt="${alt}" title="${title}" />`;
        }

        src = resolvedImageUrl.url;

        const imageBlob = this.loadImage(src);

        // Check if the image is already loaded
        if (imageBlob.blobStatus === BlobStatus.Resolved) {
          return `<img src="${imageBlob.blob}" alt="${alt}" title="${title}" />`;
        }

        const imageId = `image-${btoa(src)}`

        return `<img alt="${alt}" title="${title}" data-image-id="${imageId}" />`;
      },
    };
  }
}
