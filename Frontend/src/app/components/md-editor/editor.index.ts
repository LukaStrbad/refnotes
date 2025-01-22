/**
 * Represents the index of a token in the editor and preview.
 */
export interface EditorIndex {
  /**
   * The line index in the editor where this token is located.
   */
  editorLineIndex: number;
  /**
   * The number of lines this token spans in the editor.
   */
  editorLineCount: number;
  /**
   * The element index in the preview where this token is rendered.
   */
  previewElementIndex: number;
}
