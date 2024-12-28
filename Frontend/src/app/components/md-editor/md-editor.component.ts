import { Component, computed, effect, HostListener, model, signal, ViewEncapsulation } from '@angular/core';
import { FormsModule } from '@angular/forms';
import markdownit from 'markdown-it';
import hljs from 'highlight.js';
import MarkdownIt from 'markdown-it/index.js';
import { EditorMode } from './editor-mode';

@Component({
  selector: 'app-md-editor',
  imports: [FormsModule],
  templateUrl: './md-editor.component.html',
  styleUrl: './md-editor.component.scss',
  encapsulation: ViewEncapsulation.None
})
export class MdEditorComponent {
  value = model('');

  showEditor = computed(() => this.editorMode() === "EditorOnly" || this.editorMode() === "SideBySide");
  showPreview = computed(() => this.editorMode() === "PreviewOnly" || this.editorMode() === "SideBySide");

  previewContent: string = '';
  editorMode = signal<EditorMode>("SideBySide");

  private readonly md: MarkdownIt;
  isMobile: boolean = false;
  lineNumbers = true;
  wrapLines = false;

  constructor() {
    this.md = markdownit({
      highlight: (str, lang) => {
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
        console.log("code", code);
        if (this.lineNumbers) {
          const split = rawCode.split('\n');
          const lineNumberDigits = split.length.toString().length;
          // code = split.map((line, i) => {
          //   const lineNumber = (i + 1).toString().padStart(lineNumberDigits);
          //   console.log("line", line);
          //   const l = `<div class="line"><span class="line-number">${lineNumber}</span>${line}</div>`;
          //   console.log(l);
          //   return l;
          // }).join('');
          code = split.reduce((acc, line, i) => {
            const lineNumber = (i + 1).toString().padStart(lineNumberDigits);
            return `${acc}<span class="line-number">${lineNumber}</span>${line}\n`;
          }, '');
        }

        return '<pre class="hljs-code-block"><code class="hljs">' + code + '</code></pre>';
      }
    });

    effect(() => {
      this.previewContent = this.md.render(this.value());
    });

    this.onWindowResize();

    this.editorMode.set(localStorage.getItem('editor-mode') as EditorMode || "SideBySide");
    effect(() => {
      localStorage.setItem('editor-mode', this.editorMode());
    });
  }

  onEditorKeydown(event: KeyboardEvent) {
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
    const textarea = event.target as HTMLTextAreaElement;
    textarea.style.height = '';
    textarea.style.height = `${textarea.scrollHeight}px`;
  }

  @HostListener('window:resize')
  onWindowResize() {
    this.isMobile = window.innerWidth < 640;
  }
}
