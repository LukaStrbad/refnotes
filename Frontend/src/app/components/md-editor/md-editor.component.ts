import {
  Component,
  computed,
  effect,
  ElementRef,
  HostListener,
  model,
  Signal,
  ViewChild,
  ViewEncapsulation,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Marked, TokensList } from 'marked';
import { markedHighlight } from 'marked-highlight';
import hljs from 'highlight.js';
import { SettingsService } from '../../../services/settings.service';
import { EditorMode } from '../../../model/settings';
import { TranslateDirective, TranslatePipe } from '@ngx-translate/core';
import { EditorLineSize } from './editor-line.size';
import { EditorIndex } from './editor.index';

@Component({
  selector: 'app-md-editor',
  imports: [FormsModule, TranslateDirective, TranslatePipe],
  templateUrl: './md-editor.component.html',
  styleUrl: './md-editor.component.scss',
  encapsulation: ViewEncapsulation.None,
})
export class MdEditorComponent {
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

  previewContent: string = '';

  private readonly marked: Marked;
  // TODO: Use a better cache mechanism
  private highlightCache: Map<number, string> = new Map();

  editorLines: EditorLineSize[] | null = null;
  editorIndices: EditorIndex[] = [];

  @ViewChild('editorRef') editorElementRef!: ElementRef<HTMLTextAreaElement>;
  @ViewChild('previewRef') previewContentElement!: ElementRef<HTMLElement>;
  isMobile: boolean = false;
  syncPreview = true;

  constructor(public settings: SettingsService) {
    this.marked = new Marked(this.highlightExtension());

    this.editorMode = computed(() => settings.mdEditor().editorMode);

    effect(() => {
      this.previewContent = this.marked.parse(this.value()) as string;
      const tokens = this.marked.lexer(this.value());
      this.editorIndices = this.calculateEditorIndex(tokens);
    });

    this.onWindowResize();
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
        let rawCode = '';
        if (lang && hljs.getLanguage(lang)) {
          try {
            rawCode = hljs.highlight(str, {
              language: lang,
              ignoreIllegals: true,
            }).value;
          } catch (__) {
            // TODO: escape HTML
            rawCode = str;
          }
        } else {
          // TODO: escape HTML
          rawCode = str;
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

        const ret =
          '<pre class="hljs-code-block"><code class="hljs">' +
          code +
          '</code></pre>';
        this.highlightCache.set(hash, ret);
        return ret;
      },
    });
  }

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

  onEditorResize($event: UIEvent) {
    this.editorLines = null;
  }

  strHash(s: string) {
    return s.split('').reduce(function (a, b) {
      a = (a << 5) - a + b.charCodeAt(0);
      return a & a;
    }, 0);
  }

  syncScrolls() {
    const target = this.editorElementRef.nativeElement;
    const scrollOffset = target.scrollTop;
    this.editorLines ??= this.calculateEditorLineSizes(target);
    const filtered = this.editorLines.filter((l) => scrollOffset >= l.offset);
    const line = filtered[filtered.length - 1];

    let editorIndex = this.editorIndices.length - 1;
    while (
      editorIndex >= 0 &&
      this.editorIndices[editorIndex].editorLineIndex > line.lineIndex
      ) {
      editorIndex--;
    }
    const editorLine = this.editorIndices[editorIndex];

    const element =
      this.previewContentElement.nativeElement.children[
        editorLine.previewElementIndex
        ];
    if (element && this.syncPreview) {
      element.scrollIntoView({
        block: 'start',
        behavior: 'smooth',
      });
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
