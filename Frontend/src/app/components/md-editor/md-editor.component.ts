import {
  AfterViewInit,
  Component,
  computed,
  effect,
  ElementRef,
  HostListener,
  input,
  model, OnDestroy,
  OnInit,
  Signal,
  ViewChild,
  ViewEncapsulation,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Marked, Token, Tokens, TokensList } from 'marked';
import { markedHighlight } from 'marked-highlight';
import hljs from 'highlight.js';
import { SettingsService } from '../../../services/settings.service';
import { EditorMode } from '../../../model/settings';
import { TranslateDirective, TranslatePipe } from '@ngx-translate/core';
import { EditorLineSize } from './editor-line.size';
import { EditorIndex } from './editor.index';
import { TestTagDirective } from '../../../directives/test-tag.directive';
import { LoggerService } from '../../../services/logger.service';
import {
  resolveRelativeFolderPath,
  splitDirAndName,
} from '../../../utils/path-utils';
import { BrowserService } from '../../../services/browser.service';
import {NgClass} from "@angular/common";

@Component({
  selector: 'app-md-editor',
  imports: [FormsModule, TranslateDirective, TranslatePipe, TestTagDirective, NgClass],
  templateUrl: './md-editor.component.html',
  styleUrl: './md-editor.component.css',
  encapsulation: ViewEncapsulation.None,
})
export class MdEditorComponent implements OnInit, AfterViewInit, OnDestroy {
  currentPath = input('/');
  value = model('');

  editorMode: Signal<EditorMode>;
  showEditor = computed(
    () =>
      this.editorMode() === 'EditorOnly' || this.editorMode() === 'SideBySide',
  );
  showPreview = computed(
    () =>
      this.editorMode() === 'PreviewOnly' || this.editorMode() === 'SideBySide',
  );

  private readonly marked: Marked;
  // TODO: Use a better cache mechanism
  private highlightCache: Map<number, string> = new Map();

  editorLines: EditorLineSize[] | null = null;
  editorIndices: EditorIndex[] = [];

  @ViewChild('editorRef') editorElementRef!: ElementRef<HTMLTextAreaElement>;
  @ViewChild('previewRef') previewContentElement!: ElementRef<HTMLElement>;
  isMobile: boolean = false;
  syncPreview = true;
  previewedTokens: PreviewToken[] = [];

  imageUrls: { elementAttr: string; src: string; blob: string | null }[] = [];

  constructor(
    public settings: SettingsService,
    private log: LoggerService,
    private browser: BrowserService,
  ) {
    this.marked = new Marked(this.highlightExtension(), {
      renderer: this.imageExtension(),
    });

    this.editorMode = computed(() => settings.mdEditor().editorMode);

    effect(() => {
      const value = this.value();
      const showPreview = this.showPreview();
      // Don't render anything if there is no preview visible
      if (!showPreview || this.isMobile || !this.previewContentElement) {
        return;
      }

      this.renderPreview(value);
    });
  }

  ngOnDestroy(): void {
    this.imageUrls.forEach((imageUrl) => {
      if (imageUrl.blob) {
        URL.revokeObjectURL(imageUrl.blob);
      }
    });
  }

  ngOnInit() {
    this.onWindowResize();
  }

  ngAfterViewInit() {
    this.renderPreview(this.value());

    const observer = new IntersectionObserver(
      (entries) => {
        const entry = entries[0];
        // Only render preview on mobile when it's visible
        // When it's not mobile, then effect in the constructor will take care of it
        if (this.isMobile && entry.isIntersecting) {
          console.log('rendering preview');
          this.renderPreview(this.value());
          this.syncScrolls(true, true);
        }
      },
      {
        threshold: 0,
      },
    );
    if (this.previewContentElement) {
      observer.observe(this.previewContentElement.nativeElement);
    }
  }

  renderPreview(text: string) {
    if (this.previewContentElement?.nativeElement == null) {
      this.log.error('Preview element is not available');
      return;
    }
    if (this.settings.mdEditor().experimentalFastRender) {
      this.experimentalFastRender(text);
      this.updateImages();
      return;
    }

    const previewContent = this.marked.parse(text) as string;
    const tokens = this.marked.lexer(text);
    this.editorIndices = this.calculateEditorIndex(tokens);
    this.previewContentElement.nativeElement.innerHTML = previewContent;
    this.updateImages();
  }

  /**
   * Updates the image elements with the correct image source
   */
  updateImages() {
    this.imageUrls.forEach((imageUrl) => {
      const elements =
        this.previewContentElement.nativeElement.querySelectorAll(
          `img[data-image-id="${imageUrl.elementAttr}"]`,
        );

      // If the image is already loaded, set the src attribute
      if (imageUrl.blob !== null) {
        elements.forEach((element) =>
          element.setAttribute('src', imageUrl.blob!),
        );
        return;
      }

      const [dir, name] = splitDirAndName(imageUrl.src);
      // Load the image from the server
      this.browser
        .getImage(dir, name)
        .then((data) => {
          if (!data) {
            return;
          }
          const imageType = name.split('.').pop();
          const blob = new Blob([data], { type: `image/${imageType}` });
          const objectURL = URL.createObjectURL(blob);
          // Save the blob URL to the image URL
          imageUrl.blob = objectURL;
          elements.forEach((element) => element.setAttribute('src', objectURL));
        });
    });
  }

  experimentalFastRender(text: string) {
    const tokens = this.marked
      .lexer(text)
      .filter((t) => t.type !== 'space') as TokensList;

    let tokenHashes: {
      key: number;
      raw: string;
      token: Token;
      occurrence: number;
    }[] = [];
    for (let i = 0; i < tokens.length; i++) {
      const token = tokens[i];
      const tokenHash = this.strHash(token.raw);
      const existingHash = tokenHashes.findLast((t) => t.key == tokenHash);
      const occurrence = existingHash ? existingHash.occurrence + 1 : 0;
      tokenHashes.push({ key: tokenHash, raw: token.raw, token, occurrence });
    }

    // Delete previous tokens that are not in the new tokens
    for (let i = this.previewedTokens.length - 1; i >= 0; i--) {
      const token = this.previewedTokens[i];
      if (!tokens.some((t) => t.raw == token.raw)) {
        this.previewContentElement.nativeElement.removeChild(token.element);
        this.previewedTokens.splice(i, 1);
      }
    }

    // Add new tokens
    const layoutElements: { element: HTMLElement; token: Token }[] = [];

    for (const hash of tokenHashes) {
      const previewedTokens = this.previewedTokens.filter(
        (t) => t.raw == hash.raw,
      );
      const previewedToken = previewedTokens[hash.occurrence];
      if (previewedToken) {
        layoutElements.push({
          element: previewedToken.element,
          token: hash.token,
        });
      } else {
        const tempDiv = document.createElement('div');
        tempDiv.innerHTML = this.marked.parse(hash.token.raw) as string;
        const newElement = tempDiv.children[0] as HTMLElement;
        layoutElements.push({ element: newElement, token: hash.token });

        const previousElement = layoutElements[layoutElements.length - 2];
        if (previousElement) {
          previousElement.element.after(newElement);
        } else {
          this.previewContentElement.nativeElement.insertBefore(
            newElement,
            this.previewContentElement.nativeElement.firstChild,
          );
        }
      }
    }

    if (this.previewedTokens.length == 0) {
      for (const element of layoutElements) {
        this.previewContentElement.nativeElement.appendChild(element.element);
      }
    }

    this.previewedTokens = layoutElements.map((e) => {
      return { ...e.token, element: e.element };
    });

    this.editorIndices = this.calculateEditorIndex(tokens);
  }

  calculateEditorIndex(tokens: TokensList): EditorIndex[] {
    const indices: EditorIndex[] = [];

    let editorLineIndex = 0;
    let previewElementIndex = 0;

    for (const token of tokens) {
      let editorLineCount = 0;
      const raw: string | undefined = token['raw'];
      if (raw) {
        for (let i = 0; i < raw.length; i++) {
          if (raw[i] === '\n') {
            editorLineCount++;
          }
        }
      }

      if (token.type !== 'space') {
        const index: EditorIndex = {
          editorLineIndex,
          editorLineCount,
          previewElementIndex,
        };

        indices.push(index);
        previewElementIndex++;
      }
      editorLineIndex += editorLineCount;
    }

    return indices;
  }

  highlightExtension() {
    return markedHighlight({
      emptyLangClass: 'hljs',
      langPrefix: 'hljs-',
      highlight: (str, lang, info) => {
        const hash = this.strHash(str + lang);
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
          } catch (__) {
            rawCode = this.escapeHtml(str);
          }
        } else {
          rawCode = this.escapeHtml(str);
        }

        let code = rawCode;
        if (this.settings.mdEditor().showLineNumbers) {
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

  escapeHtml(unsafe: string) {
    return unsafe
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');
  }

  imageExtension() {
    return {
      image: (token: Tokens.Image) => {
        let src = token.href;
        const title = token.title ?? '';
        const alt = token.text;

        // If the image points to an external URL, just return the image tag
        if (src.startsWith('http')) {
          return `<img src="${src}" alt="${alt}" title="${title}" />`;
        }

        src = resolveRelativeFolderPath(this.currentPath(), src);
        let imageUrl = this.imageUrls.find((i) => i.src === src);
        if (!imageUrl) {
          const elementAttr = `image-${this.imageUrls.length}`;
          imageUrl = { elementAttr, src, blob: null };
          this.imageUrls.push(imageUrl);
        }

        // Check if the image is already loaded
        if (imageUrl.blob) {
          return `<img src="${imageUrl.blob}" alt="${alt}" title="${title}" />`;
        }
        return `<img alt="${alt}" title="${title}" data-image-id="${imageUrl.elementAttr}" />`;
      },
    };
  }

  /**
   * This is used to insert tab character if it's clicked
   * @param event
   */
  onEditorKeydown(event: KeyboardEvent) {
    this.editorLines = null;
    if (event.key === 'Tab') {
      event.preventDefault();
      const target = event.target as HTMLTextAreaElement;
      const start = target.selectionStart;
      const end = target.selectionEnd;
      const newValue =
        this.value().substring(0, start) +
        ' '.repeat(4) +
        this.value().substring(end);
      this.value.set(newValue);
      setTimeout(() => {
        target.selectionStart = target.selectionEnd = start + 4;
      });
    }
  }

  @HostListener('window:resize')
  onWindowResize() {
    this.isMobile = window.innerWidth < 640;
  }

  onEditorResize() {
    this.editorLines = null;
  }

  strHash(s: string) {
    return s.split('').reduce(function (a, b) {
      a = (a << 5) - a + b.charCodeAt(0);
      return a & a;
    }, 0);
  }

  syncScrolls(force = false, instant = false) {
    if (this.isMobile && !force) {
      return;
    }
    const editor = this.editorElementRef.nativeElement;
    const scrollOffset = editor.scrollTop;
    this.editorLines ??= this.calculateEditorLineSizes(editor);
    const filtered = this.editorLines.filter((l) => scrollOffset >= l.offset);
    const line = filtered[filtered.length - 1];

    let editorIndex = this.editorIndices.length - 1;
    while (
      editorIndex >= 0 &&
      this.editorIndices[editorIndex].editorLineIndex > line.lineIndex
    ) {
      editorIndex--;
    }
    const editorLine: EditorIndex | undefined = this.editorIndices[editorIndex];

    if (!editorLine) {
      return;
    }

    const element = this.previewContentElement.nativeElement.children[
      editorLine.previewElementIndex
    ] as HTMLElement;
    if (element && (this.syncPreview || force)) {
      if (instant) {
        this.previewContentElement.nativeElement.scrollTop = element.offsetTop;
      } else {
        element.scrollIntoView({
          block: 'start',
          behavior: 'smooth',
        });
      }
    }
  }

  calculateEditorLineSizes(textarea: HTMLTextAreaElement) {
    const textareaStyle = window.getComputedStyle(textarea);

    // Create one container for all lines
    const tempDiv = document.createElement('div');
    tempDiv.style.whiteSpace = 'pre-wrap';
    tempDiv.style.visibility = 'hidden';
    tempDiv.style.position = 'absolute';
    tempDiv.style.width = textarea.clientWidth + 'px';
    tempDiv.style.font = textareaStyle.font;
    tempDiv.style.paddingLeft = textareaStyle.paddingLeft;
    tempDiv.style.paddingRight = textareaStyle.paddingRight;
    document.body.appendChild(tempDiv);

    const textLines = textarea.value.split('\n');
    const lineHeight = parseFloat(textareaStyle.lineHeight);

    // Populate container with child elements for each line
    textLines.forEach((line) => {
      const lineElem = document.createElement('div');
      lineElem.textContent = line || ' ';
      tempDiv.appendChild(lineElem);
    });

    // Now measure each child in one pass
    const lines: EditorLineSize[] = [];
    let offset = 0;
    let prevOffset = 0;
    for (let i = 0; i < textLines.length; i++) {
      const child = tempDiv.children[i] as HTMLElement;
      const rect = child.getBoundingClientRect();
      let wrappedLinesCount = Math.ceil(rect.height / lineHeight) || 1;

      if (i > 0) {
        const prevLine = lines[i - 1];
        offset = prevOffset + prevLine.wrappedLinesCount * lineHeight;
      }
      lines.push({
        line: textLines[i],
        offset,
        lineIndex: i,
        wrappedLinesCount,
      });
      prevOffset = offset;
    }

    document.body.removeChild(tempDiv);
    return lines;
  }

  toggleSyncPreview() {
    this.syncPreview = !this.syncPreview;
    if (this.syncPreview) {
      this.syncScrolls();
    }
  }
}

type PreviewToken = Token & {
  element: HTMLElement;
};
