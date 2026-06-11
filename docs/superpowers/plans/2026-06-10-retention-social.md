# Neon Oink Retention & Social Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add daily challenge, shared leaderboards, claim-a-name identity, friends list, async duels, and share links to Neon Oink, per `docs/superpowers/specs/2026-06-10-retention-social-design.md`.

**Architecture:** Game stays a single static `index.html` (PWA). Three Vercel serverless functions in `api/` share two helper modules (`api/_lib.js` pure logic, `api/_db.js` Vercel Blob storage). Blob holds three JSON documents (users, duels, scores) written with random-suffix versioning (newest wins, old versions pruned) to avoid CDN cache staleness. Daily seed = FNV-1a hash of the UTC date, computed identically client- and server-free.

**Tech Stack:** Vanilla JS (client), Vercel Functions (Node, ESM), `@vercel/blob`, `node --test` for unit tests.

**Key existing integration points in `index.html`:**
- `runSeed` (int 0..0xFFFFFF) drives world gen; `seedToCode(s)` / `codeToSeed(c)` convert to `PIG-XXXXX` codes (lines ~582-587)
- `die()` (~line 630) ends a run, then **randomizes `runSeed`** — duel/share data must be captured before that
- `renderLB(lb)` renders the local board into `#lb`; `saveScore()` persists locally
- Overlay markup: `#overlay` contains `#runCode`, `#codeRow`, `#lb`, `#startBtn`
- Service worker `sw.js` is cache-first for everything (must bypass `/api/*`), currently `neon-oink-v3`

---

### Task 1: Project scaffolding

**Files:**
- Create: `package.json`
- Modify: `.gitignore`

- [ ] **Step 1: Create `package.json`**

```json
{
  "name": "neon-oink",
  "private": true,
  "type": "module",
  "scripts": {
    "test": "node --test tests/"
  }
}
```

- [ ] **Step 2: Install the Blob SDK**

Run: `npm install @vercel/blob`
Expected: adds `dependencies.@vercel/blob` to package.json, creates `package-lock.json` and `node_modules/`.

- [ ] **Step 3: Update `.gitignore`**

Current content is just `*.log`. Replace with:

```
*.log
node_modules/
.vercel
.env*.local
```

- [ ] **Step 4: Commit**

```bash
git add package.json package-lock.json .gitignore
git commit -m "chore: scaffold npm project for api functions"
```

---

### Task 2: Pure logic module `api/_lib.js` (TDD)

Files starting with `_` in `api/` are not exposed as endpoints by Vercel.

**Files:**
- Create: `api/_lib.js`
- Test: `tests/lib.test.mjs`

- [ ] **Step 1: Write the failing tests**

```js
// tests/lib.test.mjs
import test from 'node:test';
import assert from 'node:assert/strict';
import {
  NAME_RE, normName, hashToken, newToken, utcDay, dailySeed,
  validScore, addScore, pruneDaily, pruneDuels, duelRecords, auth
} from '../api/_lib.js';

test('normName uppercases and trims', () => {
  assert.equal(normName('  jake_1 '), 'JAKE_1');
});

test('NAME_RE accepts 3-10 of A-Z 0-9 _', () => {
  assert.ok(NAME_RE.test('ABC'));
  assert.ok(NAME_RE.test('JAKE_99'));
  assert.ok(!NAME_RE.test('AB'));
  assert.ok(!NAME_RE.test('ABCDEFGHIJK'));
  assert.ok(!NAME_RE.test('BAD NAME'));
  assert.ok(!NAME_RE.test('pig!'));
});

test('newToken is 48 hex chars, hashToken is sha256 hex', () => {
  const t = newToken();
  assert.match(t, /^[0-9a-f]{48}$/);
  assert.match(hashToken(t), /^[0-9a-f]{64}$/);
  assert.equal(hashToken('x'), hashToken('x'));
  assert.notEqual(hashToken('x'), hashToken('y'));
});

test('utcDay formats YYYY-MM-DD', () => {
  assert.equal(utcDay(Date.UTC(2026, 5, 10, 12)), '2026-06-10');
});

test('dailySeed is deterministic and in 0..0xFFFFFF', () => {
  assert.equal(dailySeed('2026-06-10'), dailySeed('2026-06-10'));
  assert.notEqual(dailySeed('2026-06-10'), dailySeed('2026-06-11'));
  const s = dailySeed('2026-06-10');
  assert.ok(Number.isInteger(s) && s >= 0 && s <= 0xFFFFFF);
});

test('validScore rejects junk', () => {
  assert.ok(validScore(0));
  assert.ok(validScore(187));
  assert.ok(!validScore(-1));
  assert.ok(!validScore(100001));
  assert.ok(!validScore(NaN));
  assert.ok(!validScore('187'));
});

test('addScore inserts sorted, keeps best per name, caps length', () => {
  let b = [];
  b = addScore(b, { name: 'A', score: 10, lvl: 1, chain: 0, ts: 1 });
  b = addScore(b, { name: 'B', score: 20, lvl: 1, chain: 0, ts: 2 });
  assert.deepEqual(b.map(e => e.name), ['B', 'A']);
  // same user lower score: ignored
  b = addScore(b, { name: 'B', score: 5, lvl: 1, chain: 0, ts: 3 });
  assert.equal(b.find(e => e.name === 'B').score, 20);
  // same user higher score: replaces
  b = addScore(b, { name: 'A', score: 30, lvl: 2, chain: 1, ts: 4 });
  assert.equal(b.length, 2);
  assert.equal(b[0].name, 'A');
  // cap
  let big = [];
  for (let i = 0; i < 60; i++) big = addScore(big, { name: 'N' + i, score: i, lvl: 1, chain: 0, ts: i });
  assert.equal(big.length, 50);
  assert.equal(big[0].score, 59);
});

test('pruneDaily keeps only last 7 days', () => {
  const daily = { '2026-06-01': [], '2026-06-05': [], '2026-06-10': [] };
  const out = pruneDaily(daily, '2026-06-10', 7);
  assert.deepEqual(Object.keys(out).sort(), ['2026-06-05', '2026-06-10']);
});

test('pruneDuels drops duels older than 30 days', () => {
  const now = Date.now();
  const duels = {
    a: { ts: now - 31 * 86400000 },
    b: { ts: now - 1 * 86400000 }
  };
  const out = pruneDuels(duels, now);
  assert.deepEqual(Object.keys(out), ['b']);
});

test('duelRecords computes W-L per opponent for done duels', () => {
  const duels = {
    d1: { from: 'ME', to: 'JAKE', state: 'done', winner: 'ME', ts: 1 },
    d2: { from: 'JAKE', to: 'ME', state: 'done', winner: 'JAKE', ts: 2 },
    d3: { from: 'ME', to: 'JAKE', state: 'done', winner: 'ME', ts: 3 },
    d4: { from: 'ME', to: 'SAM', state: 'pending', ts: 4 },
    d5: { from: 'ME', to: 'JAKE', state: 'done', winner: 'tie', ts: 5 }
  };
  const r = duelRecords(duels, 'ME');
  assert.deepEqual(r.JAKE, { w: 2, l: 1 });
  assert.equal(r.SAM, undefined);
});

test('auth verifies token hash', () => {
  const t = newToken();
  const users = { AARON: { tokenHash: hashToken(t) } };
  assert.ok(auth(users, 'AARON', t));
  assert.ok(!auth(users, 'AARON', 'wrong'));
  assert.ok(!auth(users, 'NOBODY', t));
});
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `npm test`
Expected: FAIL — `Cannot find module '../api/_lib.js'`

- [ ] **Step 3: Implement `api/_lib.js`**

```js
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `npm test`
Expected: all tests PASS.

- [ ] **Step 5: Commit**

```bash
git add api/_lib.js tests/lib.test.mjs
git commit -m "feat: pure logic for identity, scores, duels (TDD)"
```

---

### Task 3: Blob storage helper `api/_db.js`

Versioned-write pattern: every write creates a new blob (random suffix → unique URL → no stale CDN cache); reads list the prefix and fetch the newest; old versions beyond 3 are deleted. No unit test (thin I/O wrapper) — integration-tested in Task 12.

**Files:**
- Create: `api/_db.js`

- [ ] **Step 1: Implement `api/_db.js`**

```js
// api/_db.js — versioned JSON documents on Vercel Blob.
// Write = new blob with random suffix (unique URL beats CDN caching).
// Read = newest blob under the prefix. Last-write-wins; fine at friends-scale.
import { list, put, del } from '@vercel/blob';

const PREFIX = 'db/';

async function newestBlob(name) {
  const { blobs } = await list({ prefix: PREFIX + name });
  if (!blobs.length) return null;
  blobs.sort((a, b) => new Date(b.uploadedAt) - new Date(a.uploadedAt));
  return blobs;
}

export async function readDb(name, fallback) {
  const blobs = await newestBlob(name);
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
  const blobs = await newestBlob(name);
  if (blobs && blobs.length > 3) {
    await Promise.all(blobs.slice(3).map(b => del(b.url)));
  }
}
```

- [ ] **Step 2: Commit**

```bash
git add api/_db.js
git commit -m "feat: versioned blob storage helper"
```

---

### Task 4: `api/user.js` — claim a username

**Files:**
- Create: `api/user.js`

- [ ] **Step 1: Implement `api/user.js`**

```js
// api/user.js — POST {action:'claim', username} → {username, token}
import { readDb, writeDb } from './_db.js';
import { NAME_RE, normName, hashToken, newToken } from './_lib.js';

export default async function handler(req, res) {
  if (req.method !== 'POST') return res.status(405).json({ error: 'method' });
  const { action } = req.body || {};
  if (action !== 'claim') return res.status(400).json({ error: 'bad_action' });

  const username = normName(req.body.username);
  if (!NAME_RE.test(username)) return res.status(400).json({ error: 'bad_name' });

  const users = await readDb('users', {});
  if (users[username]) return res.status(409).json({ error: 'taken' });

  const token = newToken();
  users[username] = { tokenHash: hashToken(token), friends: [], createdAt: Date.now() };
  await writeDb('users', users);
  return res.status(200).json({ username, token });
}
```

- [ ] **Step 2: Quick syntax check**

Run: `node --check api/user.js`
Expected: no output (valid).

- [ ] **Step 3: Commit**

```bash
git add api/user.js
git commit -m "feat: claim-a-name endpoint"
```

---

### Task 5: `api/social.js` — friends, duels, inbox

All actions are POST with `{username, token}` auth. Actions: `addFriend`, `createDuel`, `resolveDuel`, `inbox`.

**Files:**
- Create: `api/social.js`

- [ ] **Step 1: Implement `api/social.js`**

```js
// api/social.js — POST {action, username, token, ...}
// addFriend {friend} · createDuel {to, seed, code, score} · resolveDuel {id, score} · inbox {}
import crypto from 'node:crypto';
import { readDb, writeDb } from './_db.js';
import { normName, NAME_RE, auth, validScore, pruneDuels, duelRecords } from './_lib.js';

export default async function handler(req, res) {
  if (req.method !== 'POST') return res.status(405).json({ error: 'method' });
  const b = req.body || {};
  const me = normName(b.username);

  const users = await readDb('users', {});
  if (!auth(users, me, b.token)) return res.status(401).json({ error: 'auth' });

  if (b.action === 'addFriend') {
    const friend = normName(b.friend);
    if (!NAME_RE.test(friend) || friend === me) return res.status(400).json({ error: 'bad_name' });
    if (!users[friend]) return res.status(404).json({ error: 'no_user' });
    if (!users[me].friends.includes(friend)) {
      users[me].friends.push(friend);
      await writeDb('users', users);
    }
    return res.status(200).json({ ok: true });
  }

  if (b.action === 'createDuel') {
    const to = normName(b.to);
    if (!users[to]) return res.status(404).json({ error: 'no_user' });
    if (to === me) return res.status(400).json({ error: 'self' });
    if (!validScore(b.score) || !Number.isInteger(b.seed)) return res.status(400).json({ error: 'bad_data' });
    let duels = pruneDuels(await readDb('duels', {}));
    const id = crypto.randomUUID();
    duels[id] = {
      id, from: me, to, seed: b.seed >>> 0,
      code: String(b.code || '').slice(0, 12),
      fromScore: b.score, toScore: null, state: 'pending', winner: null, ts: Date.now()
    };
    await writeDb('duels', duels);
    return res.status(200).json({ ok: true, id });
  }

  if (b.action === 'resolveDuel') {
    let duels = pruneDuels(await readDb('duels', {}));
    const d = duels[b.id];
    if (!d || d.state !== 'pending') return res.status(404).json({ error: 'no_duel' });
    if (d.to !== me) return res.status(403).json({ error: 'not_yours' });
    if (!validScore(b.score)) return res.status(400).json({ error: 'bad_data' });
    d.toScore = b.score;
    d.state = 'done';
    d.winner = b.score > d.fromScore ? d.to : b.score < d.fromScore ? d.from : 'tie';
    await writeDb('duels', duels);
    return res.status(200).json({ ok: true, winner: d.winner });
  }

  if (b.action === 'inbox') {
    const duels = pruneDuels(await readDb('duels', {}));
    const all = Object.values(duels).sort((a, c) => c.ts - a.ts);
    const rec = duelRecords(duels, me);
    return res.status(200).json({
      incoming: all.filter(d => d.to === me && d.state === 'pending'),
      outgoing: all.filter(d => d.from === me && d.state === 'pending'),
      results: all.filter(d => d.state === 'done' && (d.from === me || d.to === me)).slice(0, 10),
      friends: users[me].friends.map(name => ({ name, w: rec[name]?.w || 0, l: rec[name]?.l || 0 }))
    });
  }

  return res.status(400).json({ error: 'bad_action' });
}
```

- [ ] **Step 2: Quick syntax check**

Run: `node --check api/social.js`
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add api/social.js
git commit -m "feat: friends and async duels endpoint"
```

---

### Task 6: `api/scores.js` — daily & all-time boards

GET is public (no auth) so boards render before a name is claimed. POST requires auth.

**Files:**
- Create: `api/scores.js`

- [ ] **Step 1: Implement `api/scores.js`**

```js
// api/scores.js
// GET ?day=YYYY-MM-DD → {daily:[...], alltime:[...]}
// POST {username, token, day, score, lvl, chain} → {ok, daily, alltime}
import { readDb, writeDb } from './_db.js';
import { normName, auth, validScore, addScore, pruneDaily, utcDay } from './_lib.js';

const EMPTY = { alltime: [], daily: {} };
const DAY_RE = /^\d{4}-\d{2}-\d{2}$/;

export default async function handler(req, res) {
  if (req.method === 'GET') {
    const day = DAY_RE.test(req.query.day || '') ? req.query.day : utcDay();
    const scores = await readDb('scores', EMPTY);
    return res.status(200).json({ daily: scores.daily[day] || [], alltime: scores.alltime });
  }

  if (req.method !== 'POST') return res.status(405).json({ error: 'method' });
  const b = req.body || {};
  const me = normName(b.username);

  const users = await readDb('users', {});
  if (!auth(users, me, b.token)) return res.status(401).json({ error: 'auth' });
  if (!validScore(b.score)) return res.status(400).json({ error: 'bad_score' });

  const today = utcDay();
  const yesterday = utcDay(Date.now() - 86400000);
  const day = b.day === today || b.day === yesterday ? b.day : today;

  let scores = await readDb('scores', EMPTY);
  const entry = {
    name: me, score: b.score,
    lvl: Math.max(1, Math.min(99, b.lvl | 0)),
    chain: Math.max(0, Math.min(999, b.chain | 0)),
    ts: Date.now()
  };
  scores.daily[day] = addScore(scores.daily[day] || [], entry);
  scores.alltime = addScore(scores.alltime, entry);
  scores.daily = pruneDaily(scores.daily, today);
  await writeDb('scores', scores);
  return res.status(200).json({ ok: true, daily: scores.daily[day], alltime: scores.alltime });
}
```

- [ ] **Step 2: Quick syntax check**

Run: `node --check api/scores.js`
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add api/scores.js
git commit -m "feat: shared daily and all-time leaderboards endpoint"
```

---

### Task 7: Service worker — bypass `/api/*`, bump cache

**Files:**
- Modify: `sw.js`

- [ ] **Step 1: Edit `sw.js`**

Change line 1 from `const CACHE='neon-oink-v3';` to:

```js
const CACHE='neon-oink-v4';
```

In the fetch handler, add an API bypass as the FIRST line of the listener body (before the navigate check):

```js
self.addEventListener('fetch',e=>{
  if(new URL(e.request.url).pathname.startsWith('/api/'))return;
  if(e.request.mode==='navigate'){
    e.respondWith(fetch(e.request).then(r=>{const cp=r.clone();caches.open(CACHE).then(c=>c.put('/index.html',cp));return r;}).catch(()=>caches.match('/index.html')));
    return;
  }
  e.respondWith(caches.match(e.request).then(r=>r||fetch(e.request)));
});
```

(Not calling `respondWith` means the browser performs a normal network fetch — exactly what we want for API calls.)

- [ ] **Step 2: Commit**

```bash
git add sw.js
git commit -m "fix: service worker bypasses /api/, bump cache to v4"
```

---

### Task 8: Client — markup, styles, identity & API layer

**Files:**
- Modify: `index.html`

- [ ] **Step 1: Add CSS**

In the `<style>` block, immediately after the `#lb .me` rule (line ~46), insert:

```css
  #lbTabs{display:none;gap:6px;justify-content:center;margin-top:10px;}
  .lbTab{background:#11142a;border:1px solid #2a3060;color:#9aa3d8;border-radius:8px;padding:5px 10px;font-size:11px;font-weight:800;cursor:pointer;letter-spacing:1px;}
  .lbTab.on{color:#39d2ff;border-color:#39d2ff;}
  #social{margin-top:10px;color:#cfd6ff;font-size:12px;min-width:230px;display:none;}
  #social .row{display:flex;justify-content:space-between;align-items:center;padding:2px 6px;}
  .mini{background:#1d2240;border:1px solid #4a5388;color:#ff2fb9;border-radius:6px;padding:2px 8px;cursor:pointer;font-size:11px;font-weight:800;}
  #friendAdd{display:flex;gap:6px;justify-content:center;margin-top:6px;}
  #friendInput{background:#11142a;border:1px solid #2a3060;border-radius:8px;color:#fff;padding:6px 10px;font-size:12px;width:120px;text-transform:uppercase;text-align:center;}
  #duelBanner{display:none;margin-top:8px;color:#ff9de2;font-weight:800;font-size:14px;}
  #duelBanner button{background:linear-gradient(135deg,#ff2fb9,#39d2ff);border:none;color:#fff;font-weight:900;border-radius:8px;padding:6px 14px;cursor:pointer;margin-left:6px;}
  #claimModal{position:fixed;inset:0;background:rgba(7,7,15,.88);z-index:20;display:none;align-items:center;justify-content:center;flex-direction:column;gap:10px;}
  #claimModal input{background:#11142a;border:1px solid #39d2ff;border-radius:8px;color:#fff;padding:10px 14px;font-size:16px;width:180px;text-transform:uppercase;letter-spacing:2px;text-align:center;}
  #claimErr{color:#ff5577;font-size:12px;font-weight:700;min-height:16px;}
  #dailyBtn{margin-top:8px;background:#11142a;border:1px solid #9ef01a;color:#9ef01a;font-weight:800;border-radius:10px;padding:9px 22px;cursor:pointer;letter-spacing:1px;font-size:13px;}
  #shareBtn,#duelBtn{display:none;background:#11142a;border:1px solid #39d2ff;color:#39d2ff;font-weight:800;border-radius:10px;padding:8px 16px;cursor:pointer;margin:4px 3px 0;font-size:12px;}
  #pilotTag{color:#9ef01a;font-size:11px;font-weight:800;margin-top:6px;letter-spacing:1px;}
```

- [ ] **Step 2: Add HTML**

Inside `#overlay`, directly after `<div id="runCode"></div>` (line ~99), insert:

```html
  <div id="duelBanner"></div>
  <div><button id="shareBtn">📤 SHARE RUN</button><button id="duelBtn">⚔️ DUEL A FRIEND</button></div>
```

Directly after `<div id="lb"></div>` (line ~104), insert:

```html
  <div id="lbTabs">
    <button class="lbTab" data-tab="today">TODAY</button>
    <button class="lbTab" data-tab="alltime">ALL-TIME</button>
    <button class="lbTab" data-tab="friends">FRIENDS</button>
    <button class="lbTab on" data-tab="local">LOCAL</button>
  </div>
  <div id="social">
    <div style="color:#ff2fb9;font-weight:800;margin-bottom:4px;">👥 FRIENDS</div>
    <div id="friendsList"></div>
    <div id="friendAdd"><input id="friendInput" placeholder="ADD BY NAME" maxlength="10"><button id="friendBtn" class="mini">+</button></div>
    <div id="duelsList" style="margin-top:6px;"></div>
  </div>
  <div id="pilotTag"></div>
```

Directly after `<button id="startBtn">FLY</button>` (line ~105), insert:

```html
  <button id="dailyBtn">📅 TODAY'S RUN</button>
```

Directly after `</div>` that closes `#overlay` (before the three.js `<script>` tag), insert:

```html
<div id="claimModal">
  <div style="color:#39d2ff;font-weight:900;font-size:18px;letter-spacing:2px;">CLAIM YOUR PILOT NAME</div>
  <input id="claimInput" placeholder="3-10 CHARS" maxlength="10">
  <div id="claimErr"></div>
  <div>
    <button id="claimGo" style="background:linear-gradient(135deg,#ff2fb9,#39d2ff);border:none;color:#fff;font-weight:900;border-radius:10px;padding:10px 28px;cursor:pointer;">CLAIM</button>
    <button id="claimSkip" class="mini" style="margin-left:8px;">SKIP</button>
  </div>
</div>
```

- [ ] **Step 3: Add identity + API layer JS**

In the main `<script>`, immediately after the `codeBtn` click listener (line ~607), insert:

```js
// ---------- online: api & identity ----------
async function api(path,body){
  const r=await fetch('/api/'+path,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(body)});
  const d=await r.json().catch(()=>({}));
  if(!r.ok) throw Object.assign(new Error(d.error||'api_error'),{status:r.status});
  return d;
}
async function apiGet(path){
  const r=await fetch('/api/'+path);
  if(!r.ok) throw new Error('api_error');
  return r.json();
}
let ident=null;
try{ident=JSON.parse(localStorage.getItem('neon-oink-id'));}catch(e){}
function pilotTag(){document.getElementById('pilotTag').textContent=ident?('PILOT: '+ident.username):'';}
function ensureIdent(){
  if(ident) return Promise.resolve(ident);
  return new Promise(done=>{
    const m=document.getElementById('claimModal');
    m.style.display='flex';
    const inp=document.getElementById('claimInput'),err=document.getElementById('claimErr');
    inp.focus();
    document.getElementById('claimGo').onclick=async()=>{
      try{
        const d=await api('user',{action:'claim',username:inp.value});
        ident=d; localStorage.setItem('neon-oink-id',JSON.stringify(d));
        m.style.display='none'; pilotTag(); refreshSocial(); done(ident);
      }catch(e){
        err.textContent=e.message==='taken'?'NAME TAKEN — TRY ANOTHER':
          e.message==='bad_name'?'3-10 LETTERS / NUMBERS / _':'SERVER UNREACHABLE — TRY LATER';
      }
    };
    document.getElementById('claimSkip').onclick=()=>{m.style.display='none';done(null);};
  });
}
function utcDayStr(){return new Date().toISOString().slice(0,10);}
function dailySeedOf(day){let h=0x811c9dc5;for(const c of day){h^=c.charCodeAt(0);h=Math.imul(h,0x01000193)>>>0;}return h&0xFFFFFF;}
let mode='free',lastRun=null;
```

- [ ] **Step 4: Manual check**

Run: `python -m http.server 8123 --directory C:\neon-oink` and open http://localhost:8123 — overlay shows TODAY'S RUN button, FRIENDS section hidden, no console errors. (API calls will fail locally without `vercel dev` — that's expected here.)

- [ ] **Step 5: Commit**

```bash
git add index.html
git commit -m "feat: claim-a-name identity UI and api client layer"
```

---

### Task 9: Client — daily challenge & shared leaderboard tabs

**Files:**
- Modify: `index.html`

- [ ] **Step 1: Capture finished runs in `die()`**

In `die()`, the current tail is:

```js
  await saveScore();
  runSeed=Math.floor(Math.random()*0xFFFFFF);
```

Replace with:

```js
  await saveScore();
  lastRun={seed:runSeed,code:seedToCode(runSeed),score,lvl:level,chain:bestChain,mode};
  onRunEnd(lastRun);
  mode='free';
  runSeed=Math.floor(Math.random()*0xFFFFFF);
```

- [ ] **Step 2: Add run-end handling, board loading, tabs**

Immediately after the `let mode='free',lastRun=null;` line added in Task 8, insert:

```js
async function onRunEnd(fin){
  document.getElementById('shareBtn').style.display='inline-block';
  document.getElementById('duelBtn').style.display='inline-block';
  if(fin.mode==='daily'){
    const id=await ensureIdent();
    if(id){
      try{await api('scores',{username:id.username,token:id.token,day:utcDayStr(),score:fin.score,lvl:fin.lvl,chain:fin.chain});}catch(e){}
    }
    await loadBoards();
    showTab('today');
  } else if(fin.mode.startsWith('duel:')){
    const id=await ensureIdent();
    if(id){
      try{
        const r=await api('social',{username:id.username,token:id.token,action:'resolveDuel',id:fin.mode.slice(5),score:fin.score});
        document.getElementById('runCode').textContent=r.winner==='tie'?'🤝 DUEL TIED':r.winner===id.username?'🏆 YOU WON THE DUEL!':'💀 DUEL LOST — REMATCH?';
      }catch(e){}
    }
    refreshSocial();
  }
}
let boards=null,curTab='local';
async function loadBoards(){
  try{boards=await apiGet('scores?day='+utcDayStr());}catch(e){boards=null;}
  renderTabs();
}
function showTab(t){curTab=t;renderTabs();}
function renderTabs(){
  document.getElementById('lbTabs').style.display='flex';
  document.querySelectorAll('.lbTab').forEach(b=>b.classList.toggle('on',b.dataset.tab===curTab));
  const el=document.getElementById('lb');
  if(curTab==='local'){store.get('neon-oink-lb').then(lb=>renderLB(lb||[]));return;}
  if(!boards){el.innerHTML='<div style="color:#888;padding:4px;">offline — shared board unavailable</div>';return;}
  let rows=curTab==='today'?(boards.daily||[]):(boards.alltime||[]);
  if(curTab==='friends'){
    const fs=new Set(myFriends.concat(ident?[ident.username]:[]));
    rows=(boards.alltime||[]).filter(r=>fs.has(r.name));
  }
  const title=curTab==='today'?'🏆 TODAY':curTab==='friends'?'🏆 FRIENDS':'🏆 ALL-TIME';
  el.innerHTML=`<div style="color:#39d2ff;font-weight:800;margin-bottom:4px;">${title}</div>`+
    (rows.length?rows.slice(0,10).map((r,i)=>`<div class="row${ident&&r.name===ident.username?' me':''}"><span>#${i+1} ${r.name}</span><span>LVL${r.lvl} · ${r.score}</span></div>`).join(''):
     '<div style="color:#888;padding:4px;">no scores yet — be first</div>');
}
document.querySelectorAll('.lbTab').forEach(b=>b.addEventListener('click',()=>showTab(b.dataset.tab)));
document.getElementById('dailyBtn').addEventListener('click',()=>{
  runSeed=dailySeedOf(utcDayStr());
  mode='daily';
  start();
});
```

- [ ] **Step 3: Manual check**

Serve locally, play a run, die: SHARE/DUEL buttons appear, tabs render, LOCAL tab shows the existing local board, other tabs show the offline note. Console: no uncaught errors.

- [ ] **Step 4: Commit**

```bash
git add index.html
git commit -m "feat: daily challenge mode and shared leaderboard tabs"
```

---

### Task 10: Client — share links

**Files:**
- Modify: `index.html`

- [ ] **Step 1: Add share button + URL param handling**

Immediately after the `dailyBtn` listener added in Task 9, insert:

```js
document.getElementById('shareBtn').addEventListener('click',async()=>{
  if(!lastRun)return;
  const u=`${location.origin}/?c=${encodeURIComponent(lastRun.code)}&s=${lastRun.score}${ident?'&n='+encodeURIComponent(ident.username):''}`;
  const txt=`I scored ${lastRun.score} in NEON OINK 🐷 — beat my exact run:`;
  try{
    if(navigator.share) await navigator.share({title:'NEON OINK',text:txt,url:u});
    else{await navigator.clipboard.writeText(txt+' '+u);document.getElementById('runCode').textContent='🔗 LINK COPIED — SEND IT!';}
  }catch(e){}
});
const qp=new URLSearchParams(location.search);
if(qp.get('c')){
  const s=codeToSeed(qp.get('c'));
  if(s!=null){
    runSeed=s; mode='link';
    const who=(qp.get('n')||'').replace(/[^A-Z0-9_]/gi,'').slice(0,10);
    const sc=parseInt(qp.get('s'),10);
    document.getElementById('runCode').textContent=`⚔️ BEAT ${who||'THIS RUN'}${Number.isFinite(sc)?` — ${sc} TO WIN`:''}`;
  }
}
```

- [ ] **Step 2: Manual check**

Serve locally, open `http://localhost:8123/?c=PIG-1A2B3&s=187&n=AARON` — start screen shows "⚔️ BEAT AARON — 187 TO WIN". Play: the run uses that seed (die → run code shown matches `PIG-1A2B3`).

- [ ] **Step 3: Commit**

```bash
git add index.html
git commit -m "feat: shareable beat-my-run links"
```

---

### Task 11: Client — friends panel & duel flow

**Files:**
- Modify: `index.html`

- [ ] **Step 1: Add social panel logic**

Immediately after the URL-param block added in Task 10, insert:

```js
// ---------- social: friends & duels ----------
let myFriends=[];
function flash(msg){document.getElementById('runCode').textContent=msg;}
async function refreshSocial(){
  pilotTag();
  if(!ident)return;
  document.getElementById('social').style.display='block';
  let inbox;
  try{inbox=await api('social',{action:'inbox',username:ident.username,token:ident.token});}catch(e){return;}
  myFriends=inbox.friends.map(f=>f.name);
  document.getElementById('friendsList').innerHTML=
    inbox.friends.map(f=>`<div class="row"><span>${f.name} · ${f.w}W-${f.l}L</span><button class="mini duelF" data-n="${f.name}">⚔️ DUEL</button></div>`).join('')||
    '<div style="color:#888;">no friends yet — add by name ↓</div>';
  document.querySelectorAll('.duelF').forEach(b=>b.addEventListener('click',()=>sendDuel(b.dataset.n)));
  const inc=inbox.incoming[0];
  const ban=document.getElementById('duelBanner');
  if(inc){
    ban.style.display='block';
    ban.innerHTML=`⚔️ ${inc.from} CHALLENGED YOU — ${inc.fromScore} <button id="acceptDuel">FLY IT</button>`;
    document.getElementById('acceptDuel').addEventListener('click',()=>{
      runSeed=inc.seed; mode='duel:'+inc.id; ban.style.display='none'; start();
    });
  } else ban.style.display='none';
  document.getElementById('duelsList').innerHTML=[
    ...inbox.incoming.slice(1).map(d=>`<div class="row"><span>⚔️ ${d.from} waits · ${d.fromScore}</span></div>`),
    ...inbox.outgoing.map(d=>`<div class="row"><span>⏳ vs ${d.to} · you flew ${d.fromScore}</span></div>`),
    ...inbox.results.slice(0,5).map(d=>{
      const me=ident.username,opp=d.from===me?d.to:d.from;
      const mark=d.winner==='tie'?'🤝':d.winner===me?'🏆':'💀';
      const mine=d.from===me?d.fromScore:d.toScore,theirs=d.from===me?d.toScore:d.fromScore;
      return `<div class="row"><span>${mark} vs ${opp} · ${mine}-${theirs}</span></div>`;
    })
  ].join('');
}
async function sendDuel(to){
  if(!lastRun){flash('FINISH A RUN FIRST — THEN DUEL');return;}
  const id=await ensureIdent(); if(!id)return;
  try{
    await api('social',{action:'createDuel',username:id.username,token:id.token,to,seed:lastRun.seed,code:lastRun.code,score:lastRun.score});
    flash('⚔️ DUEL SENT TO '+to.toUpperCase()); refreshSocial();
  }catch(e){
    flash(e.message==='no_user'?'NO PILOT NAMED '+to.toUpperCase():'COULD NOT SEND DUEL');
  }
}
document.getElementById('duelBtn').addEventListener('click',async()=>{
  const id=await ensureIdent(); if(!id)return;
  const inp=document.getElementById('friendInput');
  if(myFriends.length===0&&!inp.value){flash('ADD A FRIEND BELOW, THEN HIT THEIR ⚔️');inp.focus();return;}
  if(inp.value) sendDuel(inp.value); else flash('HIT ⚔️ NEXT TO A FRIEND BELOW');
});
document.getElementById('friendBtn').addEventListener('click',async()=>{
  const id=await ensureIdent(); if(!id)return;
  const inp=document.getElementById('friendInput');
  if(!inp.value)return;
  try{
    await api('social',{action:'addFriend',username:id.username,token:id.token,friend:inp.value});
    inp.value=''; refreshSocial();
  }catch(e){flash(e.message==='no_user'?'NO PILOT BY THAT NAME':'COULD NOT ADD');}
});
refreshSocial();
loadBoards();
```

(Note: `refreshSocial` and `loadBoards` are invoked here at startup — this replaces nothing; the existing local-board bootstrap at the bottom of the file stays.)

- [ ] **Step 2: Manual check**

Serve locally: with no identity, FRIENDS panel hidden; after a run SHARE/DUEL appear; DUEL → claim modal opens; SKIP closes it. No console errors besides failed `/api/` fetches (expected without vercel dev).

- [ ] **Step 3: Commit**

```bash
git add index.html
git commit -m "feat: friends list and async duel flow"
```

---

### Task 12: Blob store setup + local integration test

**Files:** none created (environment setup + verification)

- [ ] **Step 1: Create the Blob store (USER ACTION — dashboard)**

Vercel dashboard → project `neon-oink` → Storage tab → Create Database → **Blob** → name it (e.g. `neon-oink-db`) → connect to the project. This auto-adds `BLOB_READ_WRITE_TOKEN` to the project's env vars.

- [ ] **Step 2: Link and pull env locally**

Run:
```bash
npx vercel link --yes --project neon-oink
npx vercel env pull .env.development.local
```
Expected: `.env.development.local` contains `BLOB_READ_WRITE_TOKEN=vercel_blob_rw_...`

- [ ] **Step 3: Run `vercel dev` and exercise the API**

Run: `npx vercel dev --listen 3000` (background), then:

```bash
curl -s -X POST http://localhost:3000/api/user -H "Content-Type: application/json" -d '{"action":"claim","username":"TESTPIG"}'
# expect: {"username":"TESTPIG","token":"<48 hex>"}
curl -s -X POST http://localhost:3000/api/user -H "Content-Type: application/json" -d '{"action":"claim","username":"TESTPIG"}'
# expect: {"error":"taken"} (409)
curl -s -X POST http://localhost:3000/api/scores -H "Content-Type: application/json" -d '{"username":"TESTPIG","token":"<token>","day":"<today>","score":42,"lvl":3,"chain":5}'
# expect: {"ok":true,...} with TESTPIG in daily board
curl -s "http://localhost:3000/api/scores?day=<today>"
# expect: daily board containing TESTPIG
curl -s -X POST http://localhost:3000/api/social -H "Content-Type: application/json" -d '{"action":"inbox","username":"TESTPIG","token":"<token>"}'
# expect: {"incoming":[],"outgoing":[],"results":[],"friends":[]}
```

Also claim a second user, addFriend, createDuel, resolveDuel — verify winner logic end-to-end.

- [ ] **Step 4: Clean up test data**

Re-claim pollution is fine to leave (TESTPIG etc. will sit in users.json) — OR delete the `db/` blobs from the dashboard Storage browser before launch. Note it either way.

- [ ] **Step 5: Commit anything outstanding**

```bash
git status   # expect clean (env files are gitignored)
```

---

### Task 13: Preview E2E, then ship

- [ ] **Step 1: Preview deploy**

Run: `npx vercel` (preview). Open the preview URL in two browser profiles:
1. Profile A: claim name, play TODAY'S RUN, see self on TODAY board.
2. Profile B: claim name, play TODAY'S RUN, both names on board.
3. A adds B as friend, plays a run, duels B.
4. B opens game → duel banner → FLY IT → finishes → result shown.
5. A reloads → result + W-L record visible.
6. A: SHARE RUN → link opens in B with "BEAT A" banner and identical run.

- [ ] **Step 2: Production**

```bash
git push
```
GitHub integration deploys master to production. Verify production URL loads and `/api/scores` responds.

- [ ] **Step 3: Final commit of any fixes, update plan checkboxes**

---

## Self-review notes

- Spec coverage: identity (T4/T8), friends (T5/T11), duels (T5/T9/T11), daily+boards (T6/T9), share links (T10), SW bypass (T7), offline fallback (renderTabs offline branch, flash messages), Blob setup (T12), E2E (T13). One-score-per-user-per-day = `addScore` keep-best semantics.
- `mode` reset happens in `die()` after capture, so FLY AGAIN is a fresh free run; link/daily/duel modes are armed per-entry-point. Matches design.
- GET scores is unauthenticated by design (public boards). POST social `inbox` (not GET) — deliberate deviation from spec table to keep tokens out of URLs; spec semantics unchanged.
