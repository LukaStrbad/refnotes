import { ByteSizePipe } from './byte-size.pipe';

describe('ByteSizePipe', () => {
  it('create an instance', () => {
    const pipe = new ByteSizePipe();
    expect(pipe).toBeTruthy();
  });

  it('should format values less than 1024 bytes correctly', () => {
    const pipe = new ByteSizePipe();
    expect(pipe.transform(12)).toBe('12 B');
  });

  it('should format values in base2 correctly', () => {
    const pipe = new ByteSizePipe();
    expect(pipe.transform(1024)).toBe('1.00 kB');
    expect(pipe.transform(1048576)).toBe('1.00 MB');
  });

  it('should format values in base10 correctly', () => {
    const pipe = new ByteSizePipe();
    expect(pipe.transform(1000, false)).toBe('1.00 kB');
    expect(pipe.transform(1000000, false)).toBe('1.00 MB');
  });

  it('should throw an error for negative values', () => {
    const pipe = new ByteSizePipe();
    expect(() => pipe.transform(-1)).toThrowError('Negative numbers are not supported');
  });
});
