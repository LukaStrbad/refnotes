export function isTextFile(filename: string) {
    return filename.endsWith('.txt');
}

export function isMarkdownFile(filename: string) {
    return filename.endsWith('.md') || filename.endsWith('.markdown');
}

export function isImage(filename: string) {
    const imageExtensions = ['.png', '.jpg', '.jpeg', '.gif', '.bmp', '.svg', '.webp', '.tiff', '.ico', '.heic', '.avif'];
    return imageExtensions.some(ext => filename.endsWith(ext));
}

export function isEditable(filename: string) {
    return isTextFile(filename) || isMarkdownFile(filename);
}

export function isViewable(filename: string) {
    return isTextFile(filename) || isMarkdownFile(filename) || isImage(filename);
}

export type FileType = 'text' | 'markdown' | 'image' | 'unknown';

export function getFileType(filename: string): FileType {
    if (isTextFile(filename)) {
        return 'text';
    } else if (isMarkdownFile(filename)) {
        return 'markdown';
    } else if (isImage(filename)) {
        return 'image';
    } else {
        return 'unknown';
    }
}
