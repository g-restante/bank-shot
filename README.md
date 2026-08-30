# BANK SHOOT

> I colpi diretti non fanno danno. Rimbalza o muori.

Shooter multiplayer friendslop: i proiettili fanno danno **solo** se rimbalzano almeno una volta o se partono da un trickshot (salto, flick, no-scope, behind-the-back). Mappe piccole simmetriche, Deathmatch e modalità Bomba, 2-8 giocatori via Steam.

## Stack

- **Unity 6 LTS** — URP, Input System (nuovo), assembly definitions
- **FishNet** — netcode, listen server
- **Facepunch.Steamworks + FishySteamworks** — lobby, inviti e relay via Steam
- **ProBuilder** — greybox mappe

## Struttura

Il piano di progetto completo (fasi, gate di validazione, budget, rischi) è in [`.claude/plan.md`](.claude/plan.md).

| Fase | Obiettivo | Gate |
|------|-----------|------|
| 0 | Setup + basi Unity | capsule che spara raycast, su repo |
| 1 | Core meccanica SP (proiettili, trick, parry, feel) | **fa ridere?** |
| 2 | Multiplayer FishNet + Steam | **regge in rete 20 min?** |
| 3 | Mappa, Deathmatch, Bomba, arsenale v1 | **playtest di gruppo** |
| 4 | Polish, arte, identità | — |
| 5 | Steam page, devlog, demo, Next Fest | wishlist |
| 6 | Lancio | — |

## Setup sviluppo

1. Unity Hub + Unity 6 LTS
2. `git lfs install` (una volta per macchina)
3. Aprire il progetto Unity dalla cartella del progetto
