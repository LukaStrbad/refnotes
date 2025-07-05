/**
 * Represents the size of a line in the editor.
 */
export interface EditorLineSize {
  /**
   * The text of the line.
   */
  line: string;
  /**
   * The offset from the top of the textarea.
   */
  offset: number;
  /**
   * The index of the line in the textarea.
   */
  lineIndex: number;
  /**
   * How many lines this line spans.
   */
  wrappedLinesCount: number;
}
