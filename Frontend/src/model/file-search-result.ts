export interface FileSearchResult {
  path: string;
  tags: string[];
  modified: Date;
  foundByFullText: boolean;
}
