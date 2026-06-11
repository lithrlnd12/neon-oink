// api/user.js - POST {action:'claim', username} -> {username, token}
import { readDb, writeDb } from './_db.js';
import { NAME_RE, normName, hashToken, newToken, withErr } from './_lib.js';

async function handler(req, res) {
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

export default withErr(handler);
