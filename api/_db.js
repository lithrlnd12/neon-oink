// api/_db.js — versioned JSON documents on Vercel Blob.
// Write = new blob with random suffix (unique URL beats CDN caching).
// Read = newest blob under the prefix. Last-write-wins; fine at friends-scale.
import { list, put, del } from '@vercel/blob';

const PREFIX = 'db/';

async function sortedBlobs(name) {
  const { blobs } = await list({ prefix: PREFIX + name });
  if (!blobs.length) return null;
  blobs.sort((a, b) => new Date(b.uploadedAt) - new Date(a.uploadedAt));
  return blobs;
}

export async function readDb(name, fallback) {
  const blobs = await sortedBlobs(name);
  if (!blobs) return structuredClone(fallback);
  const res = await fetch(blobs[0].url);
  if (!res.ok) return structuredClone(fallback);
  return res.json();
}

export async function writeDb(name, data) {
  await put(PREFIX + name + '.json', JSON.stringify(data), {
    access: 'public',
    addRandomSuffix: true,
    contentType: 'application/json'
  });
  const blobs = await sortedBlobs(name);
  if (blobs && blobs.length > 3) {
    await Promise.all(blobs.slice(3).map(b => del(b.url)));
  }
}
