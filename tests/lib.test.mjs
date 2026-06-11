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
  b = addScore(b, { name: 'B', score: 5, lvl: 1, chain: 0, ts: 3 });
  assert.equal(b.find(e => e.name === 'B').score, 20);
  b = addScore(b, { name: 'A', score: 30, lvl: 2, chain: 1, ts: 4 });
  assert.equal(b.length, 2);
  assert.equal(b[0].name, 'A');
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
