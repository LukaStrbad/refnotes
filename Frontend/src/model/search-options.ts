export interface SearchOptions {
  searchTerm: string;
  page: number;
  pageSize: number;
  tags?: string[];
  includeFullText?: boolean;
  directoryPath?: string;
  fileTypes?: string[];
  modifiedFrom?: Date;
  modifiedTo?: Date;
}
