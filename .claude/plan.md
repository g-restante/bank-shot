# BANK SHOOT — Piano di progetto completo

Shooter multiplayer friendslop dove i proiettili fanno danno solo se rimbalzano almeno una volta o se partono da un trickshot. Mappe piccole simmetriche, modalità Deathmatch e Bomba (Search & Destroy). Sviluppatore solo, part-time, budget minimo.

Assunzione di base: ~10-12 ore/settimana (sere + weekend). Se ne hai di più, comprimi le fasi; se ne hai meno, allunga ma non saltare i gate di validazione.

---

## Fase 0 — Setup e apprendimento (settimane 1-2)

Obiettivo: ambiente pronto e fondamenta Unity acquisite.

1. Installa Unity Hub + Unity 6 LTS, Rider o VS con tooling C#.
2. Repo Git + Git LFS su GitHub (`.gitignore` per Unity, LFS per asset binari).
3. Fai UN tutorial FPS controller completo (movimento, camera, sparo) — non dieci tutorial, uno solo, finito.
4. Impara i concetti chiave: GameObject/Component, Prefab, ScriptableObject, Input System (nuovo, non legacy), fisica base, Update vs FixedUpdate.
5. Crea il progetto vero: URP (Universal Render Pipeline), Input System, assembly definition per tenere il codice ordinato.

Deliverable: capsule che si muove e spara raycast in una scena vuota, su repo.

## Fase 1 — Core meccanica in single player (settimane 3-6)

Obiettivo: il sistema proiettili completo, giocabile da solo contro bersagli. Questa fase È il gioco: se qui non diverte, il resto non conta.

### 1.1 Sistema proiettili (il cuore)
- Proiettile NON rigidbody: traiettoria custom con raycast per-frame + `Vector3.Reflect` sui rimbalzi. Determinismo totale.
- Stati del proiettile: DISARMATO (grigio, 0 danni) → ARMATO (rosso, dopo 1° rimbalzo o se nato da trick) → POTENZIATO (per ogni rimbalzo extra: +danno, +velocità, scia più intensa).
- Parametri iniziali da tarare in playtest: velocità base ~25-35 m/s (lento e visibile), danno base 34 (3 colpi per kill), +25% danno e +10% velocità per rimbalzo, cap a 5-6 rimbalzi, vita proiettile 8-12 secondi (persistenza = mappa che diventa un flipper).
- Colpo diretto disarmato: 0 danni, il proiettile rimbalza sul corpo e SI ARMA (punizione comica per chi spara in faccia da un metro).
- Superfici: metallo = rimbalzo perfetto, legno = smorzato (-20% velocità), gomma = accelerato. Tag/layer per materiale.

### 1.2 Sistema trickshot
Il colpo nasce ARMATO se al momento dello sparo vale almeno una condizione:
- **Airborne**: non a terra da ≥0,3s. Precisione ridotta in aria per evitare bunny-hop permanente.
- **Flick**: rotazione camera ≥150° orizzontali negli ultimi 0,5s.
- **No-scope/quickscope**: sparo entro 0,2s dall'apertura mirino (solo armi con scope).
- **Behind-the-back**: direzione di tiro opposta (dot product < -0,5) alla velocità di movimento.

Regole di bilanciamento:
- Danno trick puro = 80% del danno da rimbalzo singolo (il trick è la via veloce ma debole).
- Trick + rimbalzi successivi = moltiplicatore ×1,5 (il colpo leggendario).
- Decay anti-spam: stesso trick ripetuto entro 3s → terzo colpo vale 50%.
- HUD: badge combo ("AIRBORNE!", "FLICK!") a ogni trick riconosciuto — feedback immediato, leggibilità nei clip.

### 1.3 Ribattuta
Colpire un proiettile in volo (melee o proiettile contro proiettile) lo rilancia nella direzione di mira e conta come rimbalzo (quindi lo potenzia). Finestra di parry generosa (~0,3s, hitbox larga): deve riuscire spesso, è la meccanica più divertente.

### 1.4 Game feel (non negoziabile nel friendslop)
- Scia colorata sul proiettile, cambio colore netto disarmato→armato.
- Audio: pitch del fischio che sale a ogni rimbalzo (il proiettile a 5 rimbalzi "urla" attraverso la mappa).
- Screen shake leggero, hit marker, ragdoll alla morte (qui sì rigidbody).
- Killcam che ripercorre TUTTA la traiettoria del proiettile letale, con replay geometrico. È il generatore di clip: prioritaria, non un nice-to-have.

Deliverable/gate: greybox con bersagli mobili e bot stupidi. Test: fai vedere il feel a 2-3 persone. Se il duello a ribattute non strappa una reazione, itera QUI prima di andare avanti.

## Fase 2 — Multiplayer (settimane 7-12)

Obiettivo: 2-8 giocatori in rete via Steam. La fase più rischiosa tecnicamente.

### 2.1 Architettura
- FishNet (gratuito) come netcode, architettura listen server (un giocatore hosta).
- Prima in locale: due istanze sulla stessa macchina (ParrelSync o build+editor).
- Poi Steam: Facepunch.Steamworks + FishySteamworks transport → Steam Datagram Relay (NAT traversal e relay gratis, inviti via Steam).

### 2.2 Sincronizzazione
- Movimento giocatori: client-side prediction + reconciliation (FishNet lo supporta).
- Proiettili: il server è autorità su spawn e hit; i client simulano la traiettoria in locale da (posizione, direzione, tick, seed) — essendo deterministica, serve pochissima banda. Riconciliazione solo su eventi (rimbalzo su oggetto mobile, parry, hit).
- Validazione trick lato server (i dati di input/camera passano comunque dal server): anti-cheat base gratis.
- Lag compensation semplice per il parry (rewind della finestra).

### 2.3 Lobby e flusso
- Lobby Steam: crea/invita/join tramite overlay, max 8 giocatori.
- Host migration: NON al lancio (accetta che se l'host esce la partita muore — lo fanno tutti nel genere). Segnala solo chiaramente chi è host.

Deliverable/gate: deathmatch grezzo in rete con un amico vero via Steam, stabile per 20 minuti. Se il netcode dei proiettili regge qui, il rischio tecnico del progetto è sostanzialmente chiuso.

## Fase 3 — Modalità e mappa (settimane 13-18)

### 3.1 Mappa 1 "cortile" (ispirazione layout Nuketown, MAI asset o nome — è IP Activision)
- Greybox con ProBuilder: due edifici speculari a due piani, cortile centrale con relitto/ostacolo dal tetto curvo, spawn contrapposti.
- Design per rimbalzi: cornici metalliche alle finestre (tiri casa-a-casa), pannelli 45° leggibili agli angoli, interni = alta densità di sponde, cortile = zona "povera di rimbalzi" (inversione tattica: al chiuso letale, all'aperto quasi disarmato).
- Iterare la mappa DOPO i playtest, non prima.

### 3.2 Deathmatch
- FFA e a squadre, 8 giocatori, tempo/score limit.
- Respawn con 2s di immunità anche dai proiettili residui (la mappa resta piena di proiettili armati vaganti).
- Scoreboard con statistiche del genere: "kill a più rimbalzi", "trick della partita".

### 3.3 Bomba (Search & Destroy)
- Round senza respawn, pianta/disinnesca, economia a round.
- Sito di piazzamento su pavimento metallico (difendere il piazzamento riempie il sito di proiettili impazziti).
- Economia: round 1 pistola base, poi acquisti che cambiano il COMPORTAMENTO del rimbalzo, non i numeri.

### 3.4 Arsenale v1 (4-5 armi, non di più)
1. Pistola base — rimbalzo standard, infinita.
2. Sdoppiatore — il proiettile si divide in 2 a ogni rimbalzo (flipper mode, cap ai proiettili).
3. Sticky — si incolla al muro, riparte quando un nemico passa vicino (trappola/tiro a orologeria per il defuse).
4. Sniper — velocissimo ma si arma SOLO con trick o dopo 2 rimbalzi (l'arma degli acrobati).
5. Pannello-sponda (equip da lancio) — piazza una superficie riflettente temporanea: costruisci la tua linea di tiro.

Deliverable/gate: serata di playtest con 6-8 amici, entrambe le modalità. Registra tutto: i momenti in cui ridono sono la tua roadmap.

## Fase 4 — Contenuto, polish e identità (settimane 19-26)

- Direzione artistica: low-poly stilizzato, palette satura, personaggi buffi (asset pack tipo Synty come base + personalizzazione). Niente realismo: il tono È il marketing.
- Vestire la mappa greybox; seconda mappa solo se il tempo lo consente (meglio 1 mappa ottima che 3 mediocri).
- Menu, settings (audio, sensibilità, keybind, FOV), pausa.
- Progressione leggera: statistiche, achievement Steam, magari skin colore. NIENTE battle pass/monetizzazione extra al lancio: prezzo basso una tantum, come tutto il genere.
- Ottimizzazione: target 60fps stabili su hardware modesto e Steam Deck (il pubblico friendslop gioca su portatili).
- Accessibilità minima: daltonismo (il colore disarmato/armato deve avere anche differenza di forma/scia), rebind completo.

## Fase 5 — Steam, marketing, demo (settimane 20-28, in parallelo alla Fase 4)

Il marketing nel friendslop non è una fase finale: è metà del prodotto.

1. **Steam page SUBITO** (settimana ~20): 100$, trailer anche grezzo, GIF della killcam, descrizione in una riga ("I colpi diretti non fanno danno. Rimbalza o muori."). La wishlist parte da qui e serve tempo per crescere.
2. **Devlog pubblici**: 2-3 clip/settimana su TikTok + X + YouTube Shorts. Formato: clip di 15s di un momento assurdo (parry a catena, kill a 6 rimbalzi, fail comico). How to Fish ha costruito così per mesi prima del lancio.
3. **Discord** del gioco appena i clip iniziano a girare: i primi 100 membri sono i tuoi playtester.
4. **Demo su Steam** + evento Next Fest (guarda le date della prossima edizione): è il singolo acceleratore di wishlist più potente per un indie.
5. **Playtest pubblici** via Steam Playtest branch: stress test del netcode con sconosciuti.
6. Contatta micro-streamer del genere (1k-50k follower) con key: il friendslop vive di streamer, non di stampa.
7. Soglia indicativa: lancia quando hai ≥7.000-10.000 wishlist o un clip andato genuinamente virale. Sotto, meglio rimandare e continuare i devlog.

## Fase 6 — Lancio (settimana ~28-32)

- Prezzo: 4,99-7,99€ con sconto lancio ~15-20% (standard del genere).
- Lancio in versione 1.0 piccola ma solida: 1-2 mappe, 2 modalità, 5 armi, netcode stabile. Nel friendslop la stabilità multiplayer al day-one vale più di qualunque contenuto extra.
- Settimana di lancio: presidia Discord e discussioni Steam, hotfix rapidi quotidiani (Dazed Games ha fatto 10 patch in una settimana — la reattività visibile è marketing).
- Primo update contenuti entro 2-3 settimane (una mappa o un'arma): mantiene la curva viva dopo il picco.

---

## Budget indicativo

- Steam Direct: 100$ (una tantum, per gioco)
- Asset pack grafici/audio: 100-300€
- Unity: 0€ (gratis sotto 200k$/anno di revenue)
- FishNet, Facepunch.Steamworks, ProBuilder, Blender: 0€
- Capture/editing clip: 0€ (OBS + CapCut/DaVinci)
- Eventuale trailer/logo professionale pre-lancio: 200-500€ opzionali
- Totale realistico: **300-900€**

## Rischi principali e mitigazioni

1. **La meccanica non diverte** → gate a fine Fase 1: se il feel non strappa reazioni, pivot o stop. Costo: 6 settimane, non 8 mesi.
2. **Netcode dei proiettili instabile** → gate a fine Fase 2 con test reale via Steam; proiettili deterministici scelti apposta per minimizzare questo rischio.
3. **Il trick-system degenera (salto-sparo spam)** → decay anti-spam + danno trick < danno rimbalzo + precisione ridotta in aria; monitorare nei playtest quale via domina.
4. **Trend friendslop saturo al momento del lancio** → mitigazione: lancio entro ~8 mesi, prezzo basso, e il gioco deve reggere anche come "shooter party originale" fuori dall'etichetta friendslop.
5. **Burnout / conflitto con lavoro e altri progetti** → gli scope gate esistono per questo: ogni fase ha un deliverable che giustifica (o no) la successiva. Nessun impegno oltre la fase corrente.
6. **IP**: nessun riferimento a Nuketown/CoD in nomi, asset, marketing. Layout ispirato = ok, riproduzione = no.

## Timeline riassuntiva (part-time, ~10-12h/settimana)

- Sett. 1-2: setup + basi Unity
- Sett. 3-6: core meccanica SP → **GATE 1: fa ridere?**
- Sett. 7-12: multiplayer Steam → **GATE 2: regge in rete?**
- Sett. 13-18: mappa + DM + Bomba → **GATE 3: playtest di gruppo**
- Sett. 19-26: polish + arte + contenuto
- Sett. 20-28: Steam page, devlog, demo, Next Fest (in parallelo)
- Sett. 28-32: lancio quando wishlist/viralità lo giustificano

Totale: **7-8 mesi** part-time fino al lancio. Comprimibile a 5-6 con più ore; sotto non ci scendo onestamente per un multiplayer, nemmeno con l'AI a scrivere metà del codice — i playtest e l'iterazione sul feel hanno tempi umani, non di generazione.