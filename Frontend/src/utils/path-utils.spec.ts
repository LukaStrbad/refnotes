import {resolveRelativeFolderPath, splitDirAndName} from './path-utils';

describe('PathUtils', () => {
  [
    ['/folder1', '/folder1/folder2', '..'],
    ['/', '/folder1/folder2', '../..'],
    ['/folder1', '/folder1/folder2/folder3', '../..'],
    ['/folder1/folder2/folder3', '/folder1/folder2', './folder3'],
    ['/folder1/folder2/folder3', '/folder1/folder2', 'folder3'],
    ['/folder3', '/folder1/folder2', '/folder3'], // This should ignore the root
    ['/folder1/folder2', '/folder1/folder2', '.'],
    ['/', '/', '.'],
    ['/folder1', '/folder1', '../folder1'],
    ['/folder1', '/', './folder1'],
  ].forEach(([expected, root, relativePath]) => {
    it(`resolveRelativeFolderPath should return "${expected}" for root "${root}" and relativePath "${relativePath}"`, () => {
      expect(resolveRelativeFolderPath(root, relativePath)).toBe(expected);
    });
  });

  [
    ['/folder1/folder2', 'file.txt', '/folder1/folder2/file.txt'],
    ['/', 'file.txt', '/file.txt'],
    ['/', 'folder1', '/folder1'],
    ['/', '', '/'],
  ].forEach(([expectedDir, expectedFile, path]) => {
    it(`splitDirAndName should return ["${expectedDir}", "${expectedFile}"] for path "${path}"`, () => {
      const [dir, name] = splitDirAndName(path);
      expect(dir).toBe(expectedDir);
      expect(name).toBe(expectedFile);
    });
  })
});
