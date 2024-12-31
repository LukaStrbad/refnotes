// TODO: Implement cache class
export class Cache<K, V> {
    private readonly cache = new Map<K, V>();
    private readonly updates: Update<K>[] = [];

    constructor(
        private readonly maxEntries: number = 100,
    ) { }
}

interface Update<K> {
    key: K;
    updateTime: number;
    accessCount: number;
}
