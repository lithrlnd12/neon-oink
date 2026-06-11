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
    const duels = pruneDuels(await readDb('duels', {}));
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
    const duels = pruneDuels(await readDb('duels', {}));
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
