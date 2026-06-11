// api/_lib.js — pure logic shared by all endpoints. No I/O here.
import crypto from 'node:crypto';

export const NAME_RE = /^[A-Z0-9_]{3,10}$/;
export const normName = n => String(n || '').trim().toUpperCase();
export const hashToken = t => crypto.createHash('sha256').update(String(t)).digest('hex');
export const newToken = () => crypto.randomBytes(24).toString('hex');
export const utcDay = (ts = Date.now()) => new Date(ts).toISOString().slice(0, 10);

export function dailySeed(day) {
  let h = 0x811c9dc5;
  for (const c of day) { h ^= c.charCodeAt(0); h = Math.imul(h, 0x01000193) >>> 0; }
  return h & 0xFFFFFF;
}

export const validScore = s => Number.isFinite(s) && s >= 0 && s <= 100000;

export function addScore(board, entry, max = 50) {
  const i = board.findIndex(e => e.name === entry.name);
  if (i >= 0) {
    if (board[i].score >= entry.score) return board;
    board.splice(i, 1);
  }
  board.push(entry);
  board.sort((a, b) => b.score - a.score);
  return board.slice(0, max);
}

export function pruneDaily(daily, today, keep = 7) {
  const cutoff = new Date(today + 'T00:00:00Z').getTime() - (keep - 1) * 86400000;
  const out = {};
  for (const [day, board] of Object.entries(daily)) {
    if (new Date(day + 'T00:00:00Z').getTime() >= cutoff) out[day] = board;
  }
  return out;
}

export function pruneDuels(duels, now = Date.now()) {
  const out = {};
  for (const [id, d] of Object.entries(duels)) {
    if (now - d.ts <= 30 * 86400000) out[id] = d;
  }
  return out;
}

export function duelRecords(duels, me) {
  const rec = {};
  for (const d of Object.values(duels)) {
    if (d.state !== 'done') continue;
    const opp = d.from === me ? d.to : d.to === me ? d.from : null;
    if (!opp) continue;
    rec[opp] = rec[opp] || { w: 0, l: 0 };
    if (d.winner === me) rec[opp].w++;
    else if (d.winner !== 'tie') rec[opp].l++;
  }
  return rec;
}

export function auth(users, username, token) {
  const u = users[username];
  return !!u && u.tokenHash === hashToken(token);
}
