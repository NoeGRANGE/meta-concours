# PRD — Fretlings (nom de travail : Deskmates)

> Document de référence du projet, à la racine du repo. Il décrit **quoi** construire, **pourquoi**, et **dans quelles limites**.
> Agent : lis ce fichier en entier avant toute tâche. En cas de doute entre deux options, choisis celle qui sert le mieux la section « Critères de jugement ».
> Dernière mise à jour : 12 novembre 2026.

---

## 1. Résumé en une phrase

Des petites créatures 3D posées sur ton vrai bureau, en réalité mixte, qui représentent tes mails, tes réunions, tes messages et tes tâches : elles sont calmes quand tout va bien et **stressent visiblement** (rougissent, grossissent, transpirent, prennent feu) quand quelque chose demande ton attention. **L'information ambiante remplace les notifications intrusives.**

Slogan provisoire (≤ 140 caractères, en anglais pour le formulaire) :
*"Your inbox, your meetings, your to-dos — living on your desk as tiny creatures that panic so you don't have to."*

---

## 2. Contexte : le concours

| | |
|---|---|
| Concours | **Meta VR Start Developer Competition 2026** (organisé par Meta, géré par Devpost) |
| Page | https://start-developer-competition-26.devpost.com/ |
| Règlement | https://start-developer-competition-26.devpost.com/rules |
| Date limite | **18 novembre 2026, 12:00 PT = 21:00 heure de Paris** |
| Date de soumission visée | **16 novembre 2026** (marge de sécurité) |
| Annonce des gagnants | vers le 11 décembre 2026 |
| Piste visée | **Productivity** |
| Division visée | **New Experience** (aucun code antérieur au 24 septembre 2026) |
| Prix visés | Best New Productivity (70 000 $ / 35 000 $) + prix spéciaux : **Best Reason to Come Back**, **Best First Five Minutes**, **Boldest Original Concept**, Accessibility Forward (25 000 $ chacun). Un seul prix par projet. |
| Appareils cibles | **Meta VR Glasses** (sortie printemps 2027) et **Meta Quest 3 / 3S**. Même OS (Meta Horizon OS), mêmes SDK. |

### 2.1 Exigences éliminatoires (non négociables)

1. **Hands-first** : l'app entière doit être utilisable **sans manette**, du lancement à la fin. Les manettes sont optionnelles et ne doivent jamais être nécessaires.
2. **Conformité aux politiques Meta** : Start Program Terms, Community Standards, Developer App Policies, Content Guidelines.
3. **Tout en anglais** dans l'app, ou sous-titré en anglais. Les documents de soumission sont aussi en anglais.
4. **Originalité** : tout le contenu est créé par nous, ou sous une licence compatible avec un usage commercial et marketing par Meta. **Aucun asset en licence « NonCommercial ».**
5. **Aucune marque ni logo** dans l'app : pas de logo Gmail, Outlook, Slack, Google, etc. On parle de « Mail », « Calendar », « Tasks », « Messages ».
6. **Aucune personne réelle identifiable** dans l'app, même dans les données de démo. Les noms sont inventés.
7. L'app doit être **gratuite** et accessible aux jurés jusqu'à l'annonce des gagnants.
8. **Pas un simple habillage** d'un service tiers (« take it away test ») : sans Gmail, l'app doit encore avoir du sens. C'est le rôle du mode démo.
9. **Aucun outil Meta non public** : seulement les SDK disponibles au public ou aux membres Start.

### 2.2 Recommandations de design du concours (elles comptent dans la note)

- **Seated-optimized** : tout fonctionne assis, dans un rayon d'environ 60 cm (« airplane seat test »).
- **Facile à prendre et à quitter** : démarrage rapide, pause et reprise propres, un moment satisfaisant en **≤ 10 minutes** (« one bus stop test »).
- **Original, pas un wrapper.**

### 2.3 Critères de jugement (25 % chacun)

| Critère | Ce que ça implique pour nous |
|---|---|
| **Innovation & Creativity** | Les créatures sont le concept. Le « test du laptop » : l'app ne doit **pas** pouvoir exister sur un écran classique. |
| **Experience Design** | Onboarding clair, usage qui crée une habitude, **passthrough utile** (la vraie pièce change l'expérience), **design adapté au champ de vision** des lunettes (FoV plus étroit). |
| **Technical Implementation** | Hand tracking, **regard (gaze)**, passthrough, **ancres spatiales**, détection de la pièce (**MRUK**), **≥ 60 fps sur Quest**, aucun bug, fonctionne dans des pièces jamais testées. |
| **Polish & Presentation** | DA cohérente, son, UI/UX, et une **vidéo de vrai gameplay** (pas de vidéo générée par IA). |

---

## 3. Le produit

### 3.1 Problème
Les notifications interrompent le travail en profondeur. Les badges rouges et les pop-ups exigent une attention immédiate, même pour des choses qui peuvent attendre.

### 3.2 Solution
Des **objets ambiants** dans l'espace réel. Tu travailles sur ton ordinateur avec les lunettes ; du coin de l'œil, tu vois que la créature des mails commence à chauffer. Tu sais qu'il y a quelque chose, **sans avoir été interrompu**. Tu t'en occupes quand tu le décides.

Meta positionne les Meta VR Glasses comme un appareil porté pendant le travail : environ 100 g, connexion au Mac ou au PC, passthrough couleur qui laisse voir le bureau, le clavier et le téléphone. Notre usage colle à ce positionnement.

### 3.3 Utilisateurs cibles
Travailleurs du savoir débordés (freelances, développeurs, managers) qui portent les lunettes pendant une session de travail sur ordinateur et veulent rester concentrés sans rien rater d'important.

### 3.4 Moments d'usage
- **Pendant le travail** : les créatures sont en vision périphérique, à côté de l'écran.
- **Le rituel du matin** : un coup d'œil, puis le tri de la journée avec les mains.
- **La fin de journée** : tout ramener au calme, voir le récap de la journée.

---

## 4. Les créatures

### 4.1 Direction artistique
- Petites créatures rondes, en forme de **galet ou de dumpling**, **matière vinyle mate**, sans bras ni fourrure.
- Visage minimal : yeux en perles noires, petites joues roses.
- Chaque créature porte **un accessoire qui dit à quoi elle sert**.
- Inspiration lointaine du style des mascottes « designer toy », **sans copier** aucun personnage existant. En particulier, ne jamais imiter Jolly, la mascotte de Meta Muse.
- Taille réelle : **environ 7 cm de haut** au repos.
- Palette calme et chaude au repos, **une seule couleur d'alerte** (orange vers rouge).
- **Ne jamais coder l'urgence par la seule couleur** : la taille, le mouvement et l'expression disent la même chose (accessibilité, daltonisme).

### 4.2 La famille

| Créature | Accessoire | Couleur au repos | Source de données | Ce qui fait monter le stress |
|---|---|---|---|---|
| **Facteur** (Mail) | Casquette de facteur bleue avec badge enveloppe | `#EFE6D8` crème | Gmail (Outlook plus tard) | Nombre de mails importants non lus et temps d'attente. Des flammes sortent de derrière la casquette. |
| **Réunions** (Calendar) | Page de calendrier sur la tête | `#DCE3E6` bleu pâle | Google Calendar | Minutes avant la prochaine réunion |
| **Messages** | Bulle de dialogue « … » au-dessus de la tête | `#D7E3CF` vert sauge | **Mode démo uniquement** pour le concours (Slack plus tard) | Messages directs ou mentions en attente, et depuis combien de temps |
| **Tâches** (Tasks) | Post-it jaune sur la tête, d'autres s'empilent | `#E6DCEB` lilas | Google Tasks | Tâches en retard ou dues aujourd'hui |
| **Agents** (pour développeurs) | Casque de chantier jaune avec lampe frontale, rangée de billes d'état au-dessus (§4.6) | `#D9E4EC` gris-bleu ardoise | **Mode démo uniquement** pour le concours (plus tard : sessions d'agents de code sur l'ordinateur) | Sessions qui attendent une réponse ou une autorisation, sessions terminées non consultées, sessions en erreur |

### 4.3 Le modèle de stress
Chaque créature a **une seule valeur `stress` dans [0, 1]**. Tout le rendu en découle. Les transitions sont **toujours progressives** (lissage d'environ 1 à 2 s), jamais de saut brusque.

| État | Stress | Couleur du corps | Échelle | Parties visibles | Mouvement | Effets |
|---|---|---|---|---|---|---|
| Calme | 0 | `#EFE6D8` | 1,00 | EyeClosed, Cheek, MouthSmile | Respiration lente | — |
| Attentif | 0,3 | `#F2D3AE` | 1,10 | EyeOpen, EyeShine, MouthFlat | Respiration plus rapide | Petite flamme (Facteur) |
| Stressé | 0,6 | `#F1A661` | 1,25 | EyeOpen, Brow, MouthWorried, Sweat | Petits sautillements | Goutte de sueur, flammes moyennes |
| Panique | 1 | `#E2553A` (émissif léger) | 1,40 | EyeWide, EyePupil, Brow, MouthOpen | Tremblement | Grandes flammes, halo |

Pour la couleur du corps des autres créatures : on part de leur couleur au repos et on interpole vers la même rampe orange puis rouge.

Les seuils d'expression se basculent aux valeurs intermédiaires (0,15 / 0,45 / 0,8) avec une **hystérésis d'environ 0,05**, pour éviter que le visage clignote entre deux états.

### 4.4 Calcul du stress (version initiale, à ajuster)

```
Facteur   : s = clamp01( 0.18 * importantUnread + 0.10 * hoursOldestImportant )
Réunions  : s = 0 si > 60 min ; rampe linéaire de 60 min → 5 min (0 → 0.9) ; 1.0 à ≤ 2 min ; retour à 0 quand la réunion commence
Tâches    : s = clamp01( 0.35 * overdue + 0.15 * dueToday )
Messages  : s = clamp01( 0.2 * pendingDMs + 0.15 * minutesOldest / 10 )
Agents    : s = clamp01( 0.4 * waitingForUser + 0.3 * failed + 0.15 * finishedUnseen + 0.1 * minutesOldestWaiting / 5 )
```
« Important » pour les mails : libellé Important, ou expéditeur dans une liste de contacts prioritaires. Les constantes sont dans un ScriptableObject par créature, réglable dans l'inspecteur.

### 4.5 Le soulagement (moment signature)
Quand le stress repasse sous 0,15 après avoir été au-dessus de 0,6 : la créature se dégonfle, **soupire** (son + petit nuage), puis revient au calme avec un rebond satisfaisant. C'est la récompense ; elle est importante pour la vidéo et pour « Reason to Come Back ».

---

### 4.6 Créature Agents : design
**Piste retenue le 12 novembre : A, le contremaître.** Les autres pistes restent notées pour mémoire.
Même famille que les autres : galet en vinyle mat, ~7 cm, visage minimal, un accessoire qui dit sa fonction. Couleur au repos proposée : `#D9E4EC` gris-bleu ardoise clair.

| Piste | Accessoire | Comment il montre l'état des sessions | Points forts / faibles |
|---|---|---|---|
| **A. Le contremaître** (retenue) | Petit casque de chantier jaune, avec une lampe frontale | La lampe s'allume quand une session attend ta réponse ; au-dessus de la tête, une rangée de petites billes, une par session : grise en cours, bleue terminée, orange qui attend, rouge en erreur | Lisible de loin, compte les sessions d'un coup d'œil ; les billes demandent un peu de modélisation |
| B. L'opérateur radio | Casque audio avec micro et antenne | L'antenne clignote et des ondes sortent quand une session attend | Très « je t'appelle », mais plus proche du Messages |
| C. Le chef d'orchestre | Petite baguette et nœud papillon | Bat la mesure tant que les sessions tournent, s'arrête et regarde l'utilisateur quand l'une attend | Charmant, mais le message « attend ta réponse » est moins évident |

Moment signature : quand une session se termine, la lampe (piste A) clignote deux fois et la créature fait un petit saut de joie.

## 5. Interactions (hands-first + gaze)

Principe sur les Meta VR Glasses : **regarder pour cibler, pincer pour sélectionner**. Aucune manette nécessaire.

| Geste | Résultat |
|---|---|
| **Regarder** une créature (environ 0,4 s) | Elle tourne la tête vers toi et affiche une **bulle d'aperçu** (ex. « 3 important emails · oldest 2h »). |
| **Pincer** en la regardant, ou **la toucher** du doigt | Ouvre le **panneau de détail** (les 3 éléments les plus urgents). |
| **Saisir et déplacer** | Repositionner la créature. Au lâcher, elle tombe sur le plan du bureau et s'y arrête avec un petit rebond (§5.1). |
| **Poser à l'écart** (zone « dodo » sur le bord du bureau) | Snooze 30 min : elle s'endort, avec un « zZz », et le stress est ignoré pendant ce temps. |
| **Caresser** (passer la main dessus) | Petit plaisir : elle sourit. Aucun effet fonctionnel. |
| Dans le panneau de détail : **marquer comme lu / fait** | Met à jour la source (ou le mode démo), fait baisser le stress, et peut déclencher le soulagement. |

Contraintes :
- Toutes les cibles font **≥ 44 px équivalents** et sont espacées. Pincer à distance **sélectionne** (bulle, panneau, lignes) ; **saisir** une créature se fait main à côté d'elle, pour que les deux gestes ne se confondent pas (décision du 12 novembre).
- Tout reste dans un **cône central d'environ ±25°** devant l'utilisateur assis, pour le FoV des lunettes.
- Le panneau de détail est **petit, sobre, lisible** (texte ≥ 0,9° de hauteur angulaire), et se ferme d'un geste ou en détournant le regard plusieurs secondes.

### 5.1 Calibration du bureau à la main

On ne compte pas sur le scan de la pièce pour connaître la hauteur du bureau. Ce scan n'existe que si l'utilisateur a fait la configuration de l'espace et que son bureau y est étiqueté `TABLE`. Rien ne garantit non plus qu'il existe sur les Meta VR Glasses (§8.1). La référence vient donc de la main de l'utilisateur.

- **Geste** : poser une main, ou les deux, à plat sur le bureau pendant environ 1 s. Une main fantôme animée montre le geste, sans texte.
- **Détection** : main à plat (bouts des doigts, jointures et poignet à la même hauteur à 2,5 cm près, ce qui suppose les doigts tendus et la main horizontale), immobile (moins de 2 cm/s pendant 0,6 s), au moins 25 cm sous les yeux, suivi des mains en confiance haute. Le geste n'est écouté que pendant une calibration, jamais en permanence : poser les mains sur le bureau en tapant ne doit rien déclencher.
- **Mesure** : la hauteur est celle des articulations les plus basses de la main (bouts des doigts, jointures), moins l'épaisseur de la main (environ 8 mm, réglable dans un ScriptableObject). Avec deux mains, on prend la moyenne.
- **Ce qu'on retient** :
  - un **plan horizontal**, aligné sur la gravité (on ignore l'inclinaison de la main), et sa hauteur ;
  - une **zone de bureau** rectangulaire, orientée face à l'utilisateur : 1,2 m de large sur 0,7 m de profondeur, centrée sur la main posée, ou le rectangle couvrant les deux mains avec 15 cm de marge, s'il est plus grand. On ne connaît pas les vrais bords du bureau.
  - Si une table du scan MRUK est à ±3 cm de la hauteur mesurée, on s'aligne sur sa hauteur. Reprendre aussi ses bords viendra plus tard.
- **Retour visuel** : une onde lumineuse parcourt le plan mesuré, avec un son. L'utilisateur voit tout de suite si le plan colle à son vrai bureau, et peut recommencer.
- **Persistance et changement de bureau** : voir §5.2.
- **Lâcher une créature** : elle tombe (gravité simulée par code, sans moteur physique), touche le plan, s'écrase légèrement, rebondit, puis se stabilise debout et tournée vers l'utilisateur. Lâchée hors de la zone de bureau, elle est d'abord ramenée au point le plus proche de la zone.
- **Repli tant qu'aucune calibration n'a eu lieu** : la table du scan MRUK si elle existe, sinon un point flottant devant l'utilisateur.

### 5.2 Mémoire du bureau et changement de bureau

L'app retient le bureau et la place de chaque créature d'une session à l'autre. L'utilisateur peut à tout moment en changer.

- **Repère du bureau** : une ancre spatiale posée au centre de la zone, orientée face à l'utilisateur. Le plan, la zone et la position de chaque créature sont enregistrés dans ce repère. Les créatures y sont rattachées, donc elles suivent l'ancre si le casque recale sa position. Tout reste sur l'appareil.
- **Plusieurs bureaux** : l'app garde une liste de bureaux, par exemple la maison et le travail. Au lancement, elle cherche leurs ancres. Le bureau retrouvé le plus récemment utilisé est restauré, créatures comprises, sans rien demander.
- **Aucun bureau retrouvé** (première fois, nouvelle pièce, ancre perdue) : on lance la calibration. Les bureaux non retrouvés restent en mémoire pour le jour où l'utilisateur y revient.
- **Recalibrer ou changer de bureau**, à tout moment (depuis le menu, plus tard depuis un geste) : l'utilisateur repose la main.
  - Si le bureau actuel est retrouvé, on considère qu'il a bougé : on le remplace, et les créatures gardent leur disposition, ramenée dans la nouvelle zone.
  - Sinon, on crée un nouveau bureau.
- **Oublier un bureau** : depuis le menu, supprime ses données et son ancre.
- **Positions des créatures** : chaque fois qu'une créature est posée (placement par défaut ou lâcher), sa position dans le repère du bureau est enregistrée.
- **Un service réutilisable** : le reste de l'app (onboarding, menu, mode démo) peut :
  - lancer ou annuler une calibration, recalibrer, oublier le bureau ;
  - savoir si un bureau est prêt ;
  - être prévenu quand il change ;
  - demander un emplacement par défaut ou ramener un point dans la zone.
- **Sans ancres** (éditeur sans casque, ou appareil qui les refuse) : tout fonctionne pendant la session, mais rien n'est mémorisé.

---

## 6. Parcours utilisateur

### 6.1 Premier lancement (objectif : < 60 s jusqu'au premier « wow », sans mur de texte)
1. Passthrough. Une main fantôme montre le geste « pose ta main à plat sur ton bureau ». L'utilisateur le fait, une onde parcourt le bureau (calibration, §5.1). Aucune configuration de l'espace n'est exigée.
2. Un œuf ou un petit paquet tombe sur le bureau devant l'utilisateur et rebondit. Une main animée montre ensuite le geste « pincer ».
3. L'utilisateur pince : le **Facteur** éclot, s'étire, regarde l'utilisateur.
4. Le **mode démo** démarre : un « mail » arrive, le Facteur s'agite, l'utilisateur le regarde, pince, voit le détail, marque comme lu, et le Facteur soupire de soulagement.
5. Proposition : « Add more deskmates » avec 3 œufs (Calendar, Tasks, Messages), puis « Connect your Google account (optional) ».

### 6.2 Sessions suivantes
- Les créatures **réapparaissent exactement où on les a laissées**, et le plan du bureau est conservé (ancres spatiales persistantes, §5.2). Si aucun bureau connu n'est retrouvé, l'app relance la calibration.
- Petit récap de bienvenue : « Since yesterday: 12 emails handled, 3 meetings, 5 tasks done ».

### 6.3 Fin de journée
- Bouton ou geste « Wrap up » : chaque créature montre ce qui reste, puis tout le monde s'endort. Le récap du jour s'affiche, avec un compteur de jours consécutifs (pour « Reason to Come Back »).

---

## 7. Sources de données

### 7.1 Mode démo (OBLIGATOIRE, c'est ce que voient les jurés)
- Activé par défaut au premier lancement, et toujours accessible depuis le menu.
- **Données fictives réalistes**, en anglais, avec des noms inventés, sans aucune marque.
- **Script temporel** : en 3 à 4 minutes, le stress monte, des réunions approchent, des tâches passent en retard, et les actions de l'utilisateur font baisser le tout. L'ensemble doit montrer toutes les créatures dans tous leurs états.
- Mode « accéléré » pour la vidéo et les tests.
- Interface commune avec les vraies sources : le reste de l'app **ne sait pas** si les données sont réelles ou fictives.

### 7.2 Google (vraies données, une seule connexion)
- **Gmail** (lecture, et marquer comme lu), **Google Calendar** (lecture), **Google Tasks** (lecture, et marquer comme fait).
- OAuth 2.0, flux adapté aux appareils sans clavier : **Device Authorization Grant** (code affiché, validation sur le téléphone), ou navigateur de l'OS. *À vérifier : les scopes acceptés par Google pour le flux « device ».*
- Scopes minimaux ; en mode test Google, seuls les comptes déclarés peuvent se connecter, ce qui suffit pour le concours.
- **Polling** toutes les 60 s environ, avec un cache local. Aucune donnée envoyée à un serveur tiers : tout reste sur l'appareil.
- Jetons stockés de façon sécurisée sur l'appareil. Bouton « Disconnect » qui efface tout.

### 7.2 bis Agents de code (créature Agents)
- Pour le concours : **mode démo uniquement**, comme Messages. Des sessions fictives (« refactor-auth », « fix-flaky-tests »…) passent par « en cours », « attend ta réponse », « terminée » ou « en erreur ».
- Dans l'app, jamais de nom de produit : on dit « agents » ou « sessions », jamais le nom d'un outil ou d'une marque (§2.1, règle 5).
- **Connexion réelle (recherche du 12 novembre)** : aucun des deux outils n'a d'API cloud publique pour lister les sessions d'un utilisateur et leur état. Il faut passer par l'ordinateur du développeur :
  - **Hooks** des deux outils en ligne de commande, configurés une fois par l'utilisateur :
    - en cours = envoi d'une demande (`UserPromptSubmit`) ;
    - attend ta réponse = demande d'autorisation (`PermissionRequest`), ou notification d'attente pour l'un des outils ;
    - terminée = `Stop` ;
    - en erreur = `StopFailure` pour l'un, requête au modèle en échec pour l'autre.
  - Ces hooks envoient leur JSON à un **petit relais local** sur l'ordinateur, qui tient l'état de chaque session et l'expose au casque sur le réseau local, protégé par un jeton.
  - Le relais complète avec les commandes de liste fournies par les outils : la liste des sessions de la machine pour l'un, la liste des tâches cloud pour l'autre.
  - **Limites** :
    - les sessions lancées dans le cloud ou sur mobile ne sont pas toutes visibles par ce biais ;
    - les données des hooks peuvent contenir des secrets : le relais ne transmet que l'identifiant, le dossier, l'état et l'heure.
  - Coût : un compagnon à installer sur l'ordinateur, plus un serveur réseau dans l'app. À faire après le concours, sauf si tout le reste est prêt.
- Panneau de détail : la session, son état, depuis quand ; action « vu » (fait baisser le stress, comme « marquer lu »).

### 7.3 Plus tard (hors périmètre concours)
Outlook et Microsoft Teams (Microsoft Graph), Slack, Todoist, Linear, GitHub.

### 7.4 Interface commune

```csharp
public interface IDataSource {
    string Id { get; }                     // "mail", "calendar", "tasks", "messages"
    bool IsConnected { get; }
    Task RefreshAsync(CancellationToken ct);
    SourceSnapshot Current { get; }        // éléments + métriques brutes
    Task<bool> PerformAsync(ItemAction action, string itemId); // markRead, markDone, snooze
    event Action<SourceSnapshot> Changed;
}
```

---

## 8. Stack technique

| Élément | Choix |
|---|---|
| Moteur | **Unity 6** (6000.6.5f1 installé), pipeline **URP** |
| SDK Meta | **Meta XR SDK v207+**, en paquets séparés depuis le registre Meta (`npm.developer.oculus.com`) : Core, Interaction SDK et MR Utility Kit en 207.0.1. Platform sera ajouté quand l'App ID existera. Le méta-paquet « All-in-One » est absent de ce registre. |
| Mains et regard | **Interaction SDK** (hand grab, poke, ray, pinch, gaze/eye tracking) |
| Pièce | **Calibration à la main** (§5.1) pour la hauteur du bureau. **MRUK** en complément quand un scan existe : bords de la table (`TABLE`), surfaces. |
| Persistance | **Spatial anchors** (ancres locales persistantes), via MRUK ou OVRSpatialAnchor. *À vérifier dans la doc v207.* |
| Import 3D | **glTFast** (`com.unity.cloud.gltfast`) |
| Animation | **100 % procédurale par code**, sauf éventuellement le soupir (clip Blender, en fin de projet) |
| Effets | Particle System ou VFX Graph léger (flammes, sueur, zZz, soupir) |
| Sons | Assets CC0 uniquement (ex. Freesound filtré CC0, Kenney) |
| Langage | C#, code et commentaires **en anglais** |
| Plateforme de build | Android (Quest et Meta VR Glasses), un seul APK pour les deux |
| Machine de dev | **macOS** |

### 8.1 Contraintes liées au Mac et au matériel
- **Pas de Quest pour l'instant.** Le développement se fait d'abord dans le **Meta XR Simulator** (macOS supporté), avec le **profil Meta VR Glasses** et une **pièce synthétique « office »**, qui fournit le passthrough simulé, les données de pièce et les ancres.
- Sur Mac, **la profondeur de l'environnement n'est pas disponible dans le simulateur** (Windows seulement) : ne pas en dépendre.
- *À vérifier : la configuration de l'espace (modèle de scène, tables étiquetées) est-elle disponible sur les Meta VR Glasses ?* La fiche « Compare devices » (30 sept. 2026) annonce un passthrough « avec détection de profondeur », sans parler de Space Setup. Le simulateur fournit ces données pour le profil Lunettes, mais il les fabrique. Selon la même fiche, le Quest 3S n'a pas de capteur de profondeur dédié. La calibration à la main (§5.1) rend l'app indépendante de la réponse.
- *À vérifier : Quest Link est probablement Windows uniquement* ; sur Mac, on teste sur casque en installant l'APK (ADB / Meta Quest Developer Hub).
- Un casque (Quest 3S d'occasion ou location) est prévu **au plus tard autour du 1er novembre**, pour les performances, le ressenti des mains, les ancres dans une vraie pièce et la vidéo.

### 8.2 Budget de performance
- **≥ 72 fps visés, 60 fps minimum absolu** sur Quest 3S.
- Moins de 50 000 triangles au total pour toutes les créatures, un matériau par créature si possible.
- Pas de shadows temps réel coûteuses : une ombre de contact (blob) sous chaque créature.
- Le réseau n'est jamais sur le thread principal ; le polling se fait en asynchrone.

---

## 9. Architecture Unity

```
Assets/
  _Project/
    Art/
      Models/            Facteur.glb, Reunions.glb, Messages.glb, Taches.glb
      Materials/
      VFX/               Flames, Sweat, Zzz, Sigh
      Audio/
    Prefabs/
      Creatures/         Creature_Mail, Creature_Calendar, Creature_Tasks, Creature_Messages
      UI/                PeekBubble, DetailPanel, OnboardingHand
    Scenes/
      Main.unity
      Sandbox_Creature.unity   (test d'une créature seule, stress manuel)
    Scripts/
      Core/              AppBootstrap, DeskLocator (MRUK)
      Desk/              DeskSession, DeskCalibration, FlatHandDetector, DeskPlane, DeskStore, AnchorService, DropMotion
      Creatures/         Creature, CreatureFace, CreatureMotion, CreatureVfx, StressSmoother, CreatureConfig (SO)
      Data/              IDataSource, DemoDataSource, DemoScript (SO), Google/*, StressCalculators
      Interaction/       GazeTarget, PinchSelect, GrabAndDrop, SnoozeZone, PetDetector
      UI/                PeekBubble, DetailPanel, Onboarding, DailyRecap
      Persistence/       SaveData (snooze, streak ; les positions sont dans DeskStore)
    Settings/
```

### 9.1 Composants clés
- **`Creature`** : reçoit `stress` (déjà lissé), orchestre `CreatureFace`, `CreatureMotion` et `CreatureVfx`. Expose `SetTargetStress(float)`, `PlayRelief()`, `Snooze(TimeSpan)`.
- **`StressSmoother`** : lisse la valeur cible (SmoothDamp), gère l'hystérésis et détecte le passage « > 0,6 puis < 0,15 » pour déclencher le soulagement.
- **`CreatureFace`** : active ou désactive les parties du modèle selon le stress (table de la section 4.3), gère le clignement aléatoire.
- **`CreatureMotion`** : respiration (squash & stretch), sautillements, tremblement, mise à l'échelle globale, rotation vers l'utilisateur quand il la regarde.
- **`CreatureVfx`** : flammes, sueur, halo, couleur du corps (gradient), zZz.
- **`DeskCalibration`** : écoute le geste « main à plat » pendant une calibration, mesure la hauteur, produit le `DeskPlane` (hauteur, centre, zone), l'affiche avec l'onde et le sauvegarde avec une ancre (§5.1).
- **`DeskLocator`** : propose un emplacement par défaut sur le `DeskPlane`, devant l'utilisateur, dans le cône central. Utilise la table du scan MRUK pour les bords quand elle existe, ou en repli tant qu'aucune calibration n'a eu lieu.
- **`GrabAndDrop`** : saisie d'une créature et chute avec rebond sur le `DeskPlane` au lâcher.
- **`DeskSession`** : le service du §5.2. Restaure le bureau au lancement, lance les calibrations, remplace ou crée le bureau, enregistre les positions des créatures, et prévient quand le bureau change.
- **`DeskStore`** : la liste des bureaux connus, en JSON sur l'appareil : ancre, zone, positions des créatures, date de dernière utilisation.
- **`AnchorService`** : crée, retrouve et efface l'ancre d'un bureau. Il existe une version Meta (ancres spatiales) et une version en mémoire, pour l'éditeur et les tests.
- **`FlatHandDetector`** et **`DropMotion`** : la détection du geste et la chute avec rebond, en classes simples testables sans casque.

### 9.2 Réglages par créature (`CreatureConfig`, ScriptableObject)
Couleur au repos, gradient de stress, échelles par état, amplitudes et vitesses de mouvement, seuils d'expression, constantes du calcul de stress, sons.

---

## 10. Assets 3D existants

Les modèles ont été générés dans Blender 5.2 par un script (`build_creatures.py`, dans `SourceArt/creatures_bureau/`) :
- `creatures_bureau.blend` : la scène complète (collections **Famille** et **Etats_Facteur**).
- `Facteur.glb`, `Reunions.glb`, `Messages.glb`, `Taches.glb` : **une créature par fichier, à taille réelle (environ 7 cm)**, avec **toutes les variantes de visage incluses**, en tant qu'enfants nommés de l'objet racine.

### 10.1 Nommage des parties (identique pour toutes les créatures, préfixé par le nom de la créature)

| Partie | Rôle |
|---|---|
| `Body` | Corps ; sa couleur suit le stress |
| `EyeClosed_L/R` | Yeux fermés (calme) |
| `EyeOpen_L/R`, `EyeShine_L/R` | Yeux ouverts |
| `EyeWide_L/R`, `EyePupil_L/R` | Yeux écarquillés (panique) |
| `Brow_L/R` | Sourcils froncés |
| `Cheek_L/R` | Joues (calme) |
| `MouthSmile`, `MouthFlat`, `MouthWorried`, `MouthOpen` | Les 4 bouches |
| `Sweat` | Goutte de sueur |
| `FlameMain`, `FlameLeft`, `FlameRight`, `FlameCore` | Flammes (Facteur), mises à l'échelle par le stress |
| `CapCrown`, `CapVisor`, `CapBand`, `CapBadge`, `CapBadgeV` | Casquette (Facteur) |
| `CalendarPage`, `CalendarHeader`, `CalendarLine0/1`, `CalendarRing_L/R` | Page de calendrier (Réunions) |
| `Bubble`, `BubbleTail`, `BubbleDot0/1/2` | Bulle (Messages) ; les points s'animent quand quelqu'un attend |
| `PostitMain`, `PostitLine0/1`, `PostitBlue`, `PostitPink` | Post-it (Tâches) ; Blue apparaît à partir de « stressé », Pink en « panique » |

Échelles des flammes par état (relatives à l'échelle de base) :

| État | FlameMain | FlameLeft / Right | FlameCore |
|---|---|---|---|
| Calme | cachée | cachée | cachée |
| Attentif | 0,55 | cachée | cachée |
| Stressé | 0,85 | 0,70 | 0,60 |
| Panique | 1,30 | 1,15 | 1,10 |

À l'import : au démarrage, désactiver toutes les parties variantes puis appliquer l'état calme. À terme, remplacer les flammes en mesh par des particules pour un rendu plus vivant (garder les meshes comme repli).

---

## 11. Planning (aujourd'hui : 12 octobre 2026)

| Semaine | Dates | Objectif | Critère de réussite |
|---|---|---|---|
| S1 | 12–18 oct. | Inscriptions (compte développeur Meta, **programme Start**, Devpost). Projet Unity, SDK Meta, simulateur, import des créatures. | Le Facteur s'affiche sur le bureau de la pièce synthétique « office » dans le simulateur. |
| S2 | 19–25 oct. | `Creature` complet (visage, mouvement, couleur, VFX simples), `StressSmoother`, **mode démo**, interactions de base (regarder, pincer, saisir), **calibration du bureau à la main** et lâcher avec rebond. | Le Facteur passe de calme à panique puis soupire, piloté par le mode démo, entièrement aux mains simulées. Le bureau est calibré et le Facteur, lâché au-dessus, retombe dessus. |
| S3 | 26 oct.–1er nov. | Les 4 créatures, panneau de détail, **ancres persistantes**, connexion Google. **Casque disponible** : valider la calibration sur un vrai bureau. | Les créatures réapparaissent à leur place après redémarrage ; Gmail réel fait réagir le Facteur. |
| S4 | 2–8 nov. | Onboarding (< 60 s), snooze, récap du jour et série, sons, particules, **optimisation 60 fps+**, tests dans d'autres pièces. | 3 personnes testent sans explication et comprennent seules. |
| S5 | 9–16 nov. | **Vidéo** (< 3 min, filmée dans le casque), canal de diffusion « Competition », URL d'invitation testée sur un autre compte, formulaire Devpost. **Soumission le 16.** | Soumission envoyée ; build ouverte par un compte tiers. |
| — | 17–18 nov. | Marge. **Aucune nouvelle fonctionnalité.** | — |

### 11.1 Ordre de coupe en cas de retard
1. La créature **Agents** (ajoutée le 12 novembre, à 6 jours de la date limite).
2. La créature **Messages**.
3. La connexion **Google** réelle (le mode démo suffit pour le jugement).
4. L'animation de soupir faite dans Blender (garder la version procédurale).
5. Le récap du jour et la série.

**Ne jamais couper** : le mode démo, l'onboarding, les ancres persistantes, les 60 fps, la vidéo, la marge de soumission.

---

## 12. Livrables de soumission

1. **APK** dans le Meta VR Developer Dashboard, dans un **nouveau canal de diffusion nommé exactement « Competition »**, avec une **URL d'invitation** à fournir dans le formulaire. Il doit rester accessible jusqu'à l'annonce des gagnants.
2. **Vidéo de moins de 3 minutes**, publique sur YouTube ou Vimeo, en anglais ou sous-titrée.
   - Les 10 premières secondes montrent le moment « wow » : un bureau réel, une créature qui panique, un pincement, le soulagement.
   - Uniquement de vraies images (capture dans le casque, ou simulateur en dernier recours). Pas de vidéo générée par IA.
3. **Formulaire Devpost (en anglais)** : nom, slogan de 140 caractères maximum, piste **Productivity**, division **New Experience**, description d'environ 500 mots (inspiration, construction, suite prévue), implémentation des mains, date de lancement visée, membres de l'équipe.
4. **Après la date limite, aucune modification** n'est possible : le jugement porte sur l'état au moment de la soumission.

---

## 13. Hors périmètre (pour le concours)

- Écriture ou envoi de mails, création d'événements : on lit, on marque comme lu ou fait, c'est tout.
- Multijoueur ou partage de bureau.
- Synchronisation entre appareils ou compte utilisateur propre.
- IA générative intégrée (risque de pénalité « wrapper »). Éventuellement un résumé local des mails plus tard.
- Personnalisation avancée des créatures.
- Connexion réelle aux sessions d'agents de code : relais local et hooks (§7.2 bis).
- Monétisation.

---

## 14. Risques et parades

| Risque | Parade |
|---|---|
| Ancres peu fiables (créatures qui sautent ou disparaissent) | Le tester dès que le casque est là. Une seule ancre par bureau, à laquelle les créatures sont rattachées. Si elle est perdue, on recalibre (§5.2). |
| Pas de casque avant fin octobre | Tout développer dans le simulateur ; réserver une location ou un achat d'occasion **maintenant**. |
| Pièce non scannée ou bureau non reconnu (probable chez les jurés), ou pas de scan sur les lunettes | Calibration à la main (§5.1), qui ne dépend d'aucun scan. |
| Mesure imprécise de la main posée (épaisseur, suivi au contact de la surface) | Épaisseur réglable, onde de vérification visible, recalibrage en un geste ; mesurer sur casque dès son arrivée. |
| Geste « main à plat » difficile à reproduire dans le simulateur (mains au clavier et à la souris) | Raccourci de calibration en éditeur à une hauteur donnée, mains pilotées par la webcam (simulateur 207), validation finale sur casque. |
| Gaze ou eye tracking indisponible sur Quest 3 / 3S (pas d'eye tracking matériel) | **Repli** : la direction de la tête sert de « regard » sur Quest. Les vrais yeux sont utilisés sur Meta VR Glasses. *À vérifier dans la doc ISDK v207.* |
| Les jurés ne connectent pas leur compte | Le mode démo est le chemin principal, démarré par défaut. |
| Validation OAuth Google longue | Mode test Google (comptes déclarés) ; la connexion est une option, pas le cœur. |
| Performances | Budget triangles, pas d'ombres temps réel, profiler dès la S3. |
| Créatures perçues comme « enfantines » ou distrayantes | Animations discrètes au calme, mouvements amples seulement en urgence, snooze facile. |
| Ressemblance avec la mascotte de Meta Muse | Silhouette et matière propres (vinyle mat, pas de fourrure) ; ne jamais mentionner Muse ou Jolly. |

---

## 15. Conventions pour l'agent

- Code, noms, commentaires et **tous les textes visibles dans l'app en anglais**.
- Ne jamais ajouter de logo, nom de marque commerciale ou personne réelle dans l'app ou les données de démo.
- N'ajouter un asset tiers que s'il est **CC0** ou sous une licence clairement compatible avec un usage commercial ; consigner sa source et sa licence dans `CREDITS.md`.
- Toute fonctionnalité doit être **utilisable sans manette**. Tester systématiquement avec les mains simulées.
- Garder les interactions dans le **cône central** et à portée d'une personne assise.
- Préférer les **ScriptableObjects** pour tout ce qui se règle (couleurs, seuils, timings).
- Ne jamais bloquer le thread principal (réseau, fichiers).
- Les éléments marqués *« À vérifier »* dans ce document sont des hypothèses : vérifie-les dans la documentation officielle Meta (developers.meta.com/vr) avant de coder dessus, et mets ce fichier à jour.
- Avant de considérer une tâche terminée : ça compile, ça tourne dans le simulateur, aucune erreur dans la console.

---

## 16. Liens utiles

- Concours : https://start-developer-competition-26.devpost.com/ — Règlement : `/rules` — FAQ : `/details/faqs` — Prix spéciaux : `/details/special-awards`
- Programme Start : https://developers.meta.com/vr/discover/programs/start/
- Meta VR Glasses pour développeurs : https://developers.meta.com/vr/discover/meta-vr-glasses/
- Tester sans appareil : https://developers.meta.com/vr/essentials/test-your-app/
- Meta XR Simulator (Unity) : https://developers.meta.com/vr/documentation/unity/xrsim-intro/
- Pièces synthétiques du simulateur : https://developers.meta.com/vr/documentation/unity/xrsim-ses/
- Mains et regard : https://developers.meta.com/vr/essentials/getting-started-with-hands-and-eyes/
- Politiques développeurs : https://developers.meta.com/horizon/policy/
- Seuil de performance (60 fps) : https://developers.meta.com/horizon/resources/vrc-quest-performance-1/