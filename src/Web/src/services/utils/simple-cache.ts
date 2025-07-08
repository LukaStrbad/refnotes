export class SimpleCache<T> {
  private value?: T;
  private updatedAt: number = 0;

  constructor(private readonly cacheDuration: number) { }

  get(): T | undefined {
    if (this.isExpired()) {
      return undefined;
    }
    return this.value;
  }

  set(value: T): void {
    this.value = value;
    this.updatedAt = Date.now();
  }

  clear(): void {
    this.value = undefined;
    this.updatedAt = 0;
  }

  async getOrProvide(valueProvider: () => Promise<T>): Promise<T> {
    if (this.value === undefined || this.isExpired()) {
      this.set(await valueProvider());
    }
    return this.value!;
  }

  private isExpired(): boolean {
    return Date.now() - this.updatedAt > this.cacheDuration;
  }
}
