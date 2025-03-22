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
