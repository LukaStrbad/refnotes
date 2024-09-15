import { Component, output, ViewEncapsulation } from '@angular/core';
import { FormsModule, NgModel } from '@angular/forms';
import markdownit from 'markdown-it';
import hljs from 'highlight.js';
import MarkdownIt from 'markdown-it/index.js';

@Component({
  selector: 'app-md-editor',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './md-editor.component.html',
  styleUrl: './md-editor.component.scss',
  encapsulation: ViewEncapsulation.None
})
export class MdEditorComponent {
  editorContent: string = `# Hello
## Hello
### Hello
Hello

[asdfdsa](https://google.com)

Use this to output to console: \`console.log("Hello world")\`
\`\`\`c
#include <stdio.h>

int main(void) {
  // Comment
  printf("Hello world");
}
\`\`\`
  `;
  previewContnet: string = '';

  private readonly md: MarkdownIt;

  constructor() {
    this.md = markdownit({
      highlight: (str, lang) => {
        if (lang && hljs.getLanguage(lang)) {
          try {
            return '<pre class="hljs-code-block"><code class="hljs">' +
              hljs.highlight(str, { language: lang, ignoreIllegals: true }).value +
              '</code></pre>';
          } catch (__) { }
        }

        return '<pre class="hljs-code-block"><code class="hljs">' + this.md.utils.escapeHtml(str) + '</code></pre>';
      }
    });

    this.onEditorChange();
  }

  onEditorChange(_?: string) {
    const result = this.md.render(this.editorContent);
    this.previewContnet = result;
  }
}
