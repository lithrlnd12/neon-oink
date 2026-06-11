// api/scores.js
// GET ?day=YYYY-MM-DD -> {daily:[...], alltime:[...]}
// POST {username, token, day, score, lvl, chain} -> {ok, daily, alltime}
import { readDb, writeDb } from './_db.js';
import { normName, auth, validScore, addScore, pruneDaily, utcDay, withErr } from './_lib.js';

const EMPTY = { alltime: [], daily: {} };
const DAY_RE = /^\d{4}-\d{2}-\d{2}$/;

async function handler(req, res) {
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

  const scores = await readDb('scores', EMPTY);
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

export default withErr(handler);
