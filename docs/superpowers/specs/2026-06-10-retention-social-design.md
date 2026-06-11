# Neon Oink — Retention & Social Design

**Date:** 2026-06-10
**Status:** Approved
**Goal:** Give players reasons to come back daily and pull friends in. Audience is friends & social circles (tens–hundreds of players).

## Overview

Five features built on one foundation (deterministic seeded runs, which already exist):

1. **Daily Challenge** — everyone plays the same date-seeded run each day.
2. **Shared leaderboards** — daily / all-time / friends boards, stored centrally.
3. **Duel links** — share a run via URL; opening it loads that exact run with a "beat my score" banner.
4. **Usernames** — claim-a-name identity, no login.
5. **Friends list + async duels** — invite by username, take turns on the same run, head-to-head records.

Explicitly out of scope for v1: real-time multiplayer, push notifications, cross-device account recovery, unlockables/cosmetics, anti-cheat beyond sanity checks.

## Architecture

- Game remains a single `index.html` (static, PWA).
- Three Vercel serverless functions under `api/`.
- Storage: **Vercel Blob**, three JSON files. Last-write-wins; acceptable at friends-scale. If the game outgrows it, only the storage layer changes (API routes keep their contracts).
- Daily seed is derived client- and server-side from the UTC date string (e.g. `FNV-1a("2026-06-10")`) — no coordination needed.

```
index.html ──▶ api/user.js    ──▶ Blob: users.json    {username: {tokenHash, friends[], createdAt}}
           ──▶ api/social.js  ──▶ Blob: duels.json    {id: {from, to, seed, code, fromScore, toScore, state, ts}}
           ──▶ api/scores.js  ──▶ Blob: scores.json   {alltime: [...], daily: {"2026-06-10": [...]}}
```

## Identity

- First launch (or first action needing identity): prompt **"CLAIM YOUR PILOT NAME"** — 3–10 chars, `A–Z 0–9 _`, stored uppercase, unique.
- `POST /api/user {action:"claim", username}` → reserves name, returns a random secret token. Client stores `{username, token}` in localStorage. Token is hashed (SHA-256) server-side.
- All authenticated calls send `{username, token}`; server verifies hash match.
- Name taken → 409, client prompts again. Device wiped = name lost (recovery out of scope).

## Friends

- Add by exact username: `POST /api/social {action:"addFriend", friend}`. No approval step — it is a contact list, one-directional.
- FRIENDS panel on start screen: each friend shows duel record vs. you (`JAKE · 3W-2L`) + DUEL button.
- Leaderboard gains a FRIENDS tab (filters all-time board to your friends + you).

## Async duels

1. After any finished run, **DUEL** → pick friend or type a username. `POST /api/social {action:"createDuel", to, seed, code, score}`.
2. Opponent opens game → `GET /api/social?inbox=1` returns pending duels → banner: **"⚔️ AARON challenged you — 187. FLY."** Tapping it starts the same-seed run.
3. Opponent finishes → `POST {action:"resolveDuel", id, score}` → duel state becomes `done`, winner recorded. Challenger sees the result on next open.
4. DUELS panel lists: incoming (playable), outgoing (waiting), recent results. Duels older than 30 days are pruned.

## Daily challenge & leaderboards

- **TODAY'S CHALLENGE** button beside FLY; shows current UTC date. Uses daily seed.
- After a daily run: auto `POST /api/scores {day, score, lvl, chain}` (requires claimed name), then board renders: today's top 10 + your rank, with TODAY / ALL-TIME / FRIENDS tabs.
- Free-play runs keep the existing local-only board.
- `scores.json` keeps top 50 per board; daily entries older than 7 days pruned. One score per user per day (best kept).

## Duel links (no account needed by recipient to play)

- SHARE button after a run: Web Share API (clipboard fallback) with `https://<host>/?c=CODE&s=SCORE&n=NAME`.
- On load with `?c=`: start screen shows **"⚔️ BEAT NAME'S SCORE"** and loads that seed (extends existing challenge-code mechanism).

## API contracts (summary)

| Endpoint | Action | Auth | Effect |
|---|---|---|---|
| POST /api/user | claim | – | Reserve username, return token |
| POST /api/social | addFriend | ✓ | Append to caller's friends[] |
| POST /api/social | createDuel | ✓ | Create duel (state: pending) |
| POST /api/social | resolveDuel | ✓ | Record opponent score, state: done |
| GET /api/social?inbox=1 | – | ✓ | Pending duels, recent results, friends+records |
| POST /api/scores | – | ✓ | Submit daily score (validated) |
| GET /api/scores?day=D | – | – | Daily + all-time boards |

Validation on every write: username format, token hash match, score is a finite number within a plausible cap, day within last 2 days, strings length-capped and sanitized. No further anti-cheat (honor system).

## Error handling

- API unreachable / Blob unconfigured → game never blocks: local leaderboard shown with an "offline" note; duel/friend actions show a brief "can't reach server" toast.
- Service worker **bypasses `/api/*`** (network-only) — required, current SW is cache-first.
- Claim race / duplicate name → 409 → re-prompt.

## Testing

- API functions exercised locally with `vercel dev` (Blob token pulled via `vercel env pull`).
- End-to-end on a preview deployment with two browser profiles (two identities) before promoting to production.

## One-time setup

- Create a Blob store in the Vercel dashboard (auto-injects `BLOB_READ_WRITE_TOKEN`).
- `npm` dependency for the API only: `@vercel/blob` (game itself stays dependency-free).
