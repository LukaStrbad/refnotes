export type Theme = 'auto' | 'light' | 'dark';


export type EditorMode = 'SideBySide' | 'EditorOnly' | 'PreviewOnly';

export interface MdEditorSettings {
    editorMode: EditorMode;
    showLineNumbers: boolean;
    wrapLines: boolean;
}
