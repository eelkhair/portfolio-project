type Entry = { key: string; expiresAt: number };
export class IdempotencyCache {
  private readonly ttlMs = 5 * 60 * 1000;
  private readonly map = new Map<string, Entry>();
  getOrCreate(fingerprint: string): string {
    const now = Date.now();
    const hit = this.map.get(fingerprint);
    if (hit && hit.expiresAt > now) return hit.key;
    const key = crypto.randomUUID();
    this.map.set(fingerprint, { key, expiresAt: now + this.ttlMs });
    if (this.map.size > 500) for (const [k, v] of this.map) if (v.expiresAt <= now) this.map.delete(k);
    return key;
  }
}
