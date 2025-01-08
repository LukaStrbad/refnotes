import { Component, computed, effect, ElementRef, HostListener, model, Signal, signal, ViewChild, ViewEncapsulation } from '@angular/core';
import { FormsModule } from '@angular/forms';
import markdownit from 'markdown-it';
import hljs from 'highlight.js';
import MarkdownIt from 'markdown-it/index.js';
import { SettingsService } from '../../../services/settings.service';
import { EditorMode } from '../../../model/settings';
import { TranslateDirective, TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-md-editor',
  imports: [FormsModule, TranslateDirective, TranslatePipe],
  templateUrl: './md-editor.component.html',
  styleUrl: './md-editor.component.scss',
  encapsulation: ViewEncapsulation.None
})
export class MdEditorComponent {
  value = model('');

  editorMode: Signal<EditorMode>;
  showEditor = computed(() => this.editorMode() === "EditorOnly" || this.editorMode() === "SideBySide");
  showPreview = computed(() => this.editorMode() === "PreviewOnly" || this.editorMode() === "SideBySide");

  previewContent: string = '';
  testDisplay = '';

  private readonly md: MarkdownIt;
  // TODO: Use a better cache mechanism
  private highlightCache: Map<number, string> = new Map();
  editorLines: EditorLine[] | null = null;
  previewLines: PreviewLine[] | null = null;
  @ViewChild('previewRef') previewContentElement!: ElementRef<HTMLElement>;
  isMobile: boolean = false;

  constructor(
    public settings: SettingsService
  ) {
    this.md = markdownit({
      breaks: true,
      highlight: (str, lang) => {
        const hash = this.strHash(str + lang);
        if (this.highlightCache.has(hash)) {
          return this.highlightCache.get(hash)!;
        }
        let rawCode = '';
        if (lang && hljs.getLanguage(lang)) {
          try {
            rawCode = hljs.highlight(str, { language: lang, ignoreIllegals: true }).value;
          } catch (__) {
            rawCode = this.md.utils.escapeHtml(str);
          }
        } else {
          rawCode = this.md.utils.escapeHtml(str);
        }

        let code = rawCode;
        if (settings.mdEditor().showLineNumbers) {
          const split = rawCode.split('\n');
          const lineNumberDigits = split.length.toString().length;
          code = split.reduce((acc, line, i) => {
            const lineNumber = (i + 1).toString().padStart(lineNumberDigits);
            return `${acc}<span class="line-number">${lineNumber}</span>${line}\n`;
          }, '');
        }

        const ret = '<pre class="hljs-code-block"><code class="hljs">' + code + '</code></pre>';
        this.highlightCache.set(hash, ret);
        return ret;
      }
    });

    this.editorMode = computed(() => settings.mdEditor().editorMode);

    effect(() => {
      console.time('render');
      this.previewContent = this.md.render(this.value());
      console.timeEnd('render');
    });

    this.onWindowResize();
  }

  onEditorKeydown(event: KeyboardEvent) {
    this.editorLines = null;
    this.previewLines = null;
    if (event.key === 'Tab') {
      event.preventDefault();
      const target = event.target as HTMLTextAreaElement;
      const start = target.selectionStart;
      const end = target.selectionEnd;
      const newValue =
        this.value().substring(0, start) +
        " ".repeat(4) +
        this.value().substring(end);
      this.value.set(newValue);
      setTimeout(() => {
        target.selectionStart = target.selectionEnd = start + 4;
      });
    }
  }

  onEditorInput(event: Event) {
    // TODO: Use "field-sizing: content;" in the CSS once it's supported in all browsers"
    // const textarea = event.target as HTMLTextAreaElement;
    // textarea.style.height = '';
    // textarea.style.height = `${textarea.scrollHeight}px`;
  }

  @HostListener('window:resize')
  onWindowResize() {
    this.isMobile = window.innerWidth < 640;
  }

  onEditorResize($event: UIEvent) {
    this.editorLines = null;
    this.previewLines = null;
  }

  strHash(s: string) {
    return s.split("").reduce(function (a, b) {
      a = ((a << 5) - a) + b.charCodeAt(0);
      return a & a;
    }, 0);
  }

  onEditorScroll(event: Event) {
    const target = event.target as HTMLTextAreaElement;
    const scrollOffset = target.scrollTop;
    this.editorLines ??= this.calculateEditorLines(target);
    const filtered = this.editorLines.filter(l => scrollOffset >= l.offset);
    const line = filtered[filtered.length - 1];
    this.testDisplay = line.lineIndex.toString();

    this.previewLines ??= this.calculatePreviewLines(this.previewContentElement.nativeElement);
    const lastEl = this.previewLines[this.previewLines.length - 1];
    this.testDisplay = `Editor: ${this.value().split('\n').length}, Preview: ${lastEl.lineIndex + lastEl.totalLines}`;
    console.log(this.previewLines);
    const previewLine = this.previewLines.find(l => l.lineIndex === line.lineIndex);
    if (previewLine) {
      previewLine.element.scrollIntoView({ block: 'start', behavior: 'smooth' });
    }
  }

  calculateEditorLines(textarea: HTMLTextAreaElement) {
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
    const lines: EditorLine[] = [];
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
        line: textLines[i], offset, lineIndex: i, wrappedLinesCount
      });
      prevOffset = offset;
    }

    document.body.removeChild(tempDiv);
    return lines;
  }

  calculatePreviewLines(element: HTMLElement) {
    const lines: PreviewLine[] = [];
    let totalLines = 0;
    for (let i = 0; i < element.children.length; i++) {
      const child = element.children[i] as HTMLElement;
      const lineCount = this.countLines(child);
      lines.push({
        element: child, totalLines: lineCount, lineIndex: totalLines
      });
      totalLines += lineCount;
    }
    return lines;
  }

  private countLines(element: HTMLElement): number {
    if (element instanceof HTMLUListElement || element instanceof HTMLOListElement) {
      return element.children.length;
    }

    if (element instanceof HTMLPreElement) {
      return this.countLines(element.children[0] as HTMLElement);
    }

    // Code block
    if (element instanceof HTMLElement && element.tagName === 'CODE') {
      const text = element.textContent || '';
      let newlines = 0;
      for (let i = 0; i < text.length; i++) {
        if (text[i] === '\n') {
          newlines++;
        }
      }
      return newlines;
    }

    return 1;
  }
}

interface EditorLine {
  line: string;
  offset: number;
  lineIndex: number;
  wrappedLinesCount: number;
}

interface PreviewLine {
  element: HTMLElement;
  totalLines: number;
  lineIndex: number;
}
