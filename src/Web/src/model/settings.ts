export type Theme = 'auto' | 'light' | 'dark';

export type EditorMode = 'SideBySide' | 'EditorOnly' | 'PreviewOnly';

export interface MdEditorSettings {
  useWysiwyg: boolean;
  editorMode: EditorMode;
  showLineNumbers: boolean;
  wrapLines: boolean;
  experimentalFastRender: boolean;
}

export interface SearchSettings {
  fullTextSearch: boolean;
  onlySearchCurrentDir: boolean;
}

export interface GroupSettings {
  rememberGroupPath: boolean;
  groupPath?: string;
}
