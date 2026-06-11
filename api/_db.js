// api/_db.js - JSON documents on a private Vercel Blob store.
// Reads use get() with useCache:false (authed + cache-busted), so a fixed
// pathname with allowOverwrite is safe. Last-write-wins; fine at friends-scale.
import { put, get } from '@vercel/blob';

const path = name => `db/${name}.json`;

export async function readDb(name, fallback) {
  const r = await get(path(name), { access: 'private', useCache: false });
  if (!r || !r.stream) return structuredClone(fallback);
  return new Response(r.stream).json();
}

export async function writeDb(name, data) {
  await put(path(name), JSON.stringify(data), {
    access: 'private',
    allowOverwrite: true,
    contentType: 'application/json'
  });
}
