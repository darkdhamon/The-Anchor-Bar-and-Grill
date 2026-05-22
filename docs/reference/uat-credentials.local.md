# Local Development UAT Credentials

This file is intentionally tracked in the repository so every Codex conversation working in this repo can access the current shared local development and UAT account credentials.

- Environment: local development database `AnchorBarAndGrillDev`
- Last verified and reset to match this document: `2026-05-22`
- Scope: Codex-created local development and UAT accounts only
- Important: these accounts are not production accounts and are not part of the application seed data

## Accounts

| Username | Current password | Roles | Notes |
| --- | --- | --- | --- |
| `admin@anchor.local` | `AnchorAdmin!234` | `Admin`, `EventManager`, `IT`, `MenuManager` | Bootstrap/local full-access account used during admin and role UAT |
| `uat.admin@anchor.test` | `UatAdmin!234` | `Admin` | Admin-only UAT account |
| `uat.events@anchor.test` | `UatEvents!234` | `EventManager` | Event editor UAT account |
| `uat.menu@anchor.test` | `UatMenu!234` | `MenuManager` | Menu editor UAT account |
| `uat.it@anchor.test` | `UatIt!234` | `IT` | IT/system UAT account |

## Maintenance Rule

If any of these passwords or role assignments are changed during future UAT or debugging, update this file in the same change so later Codex conversations do not lose access to the current local account state.
