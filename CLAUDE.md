# Deskmates — instructions pour Claude

Lis `PRD.md` en entier avant toute tâche, et `STYLE.md` avant tout travail visuel (interface, icône, vidéo, textes de soumission). En cas de doute entre deux options, choisis celle qui sert le mieux les critères de jugement (PRD §2.3).

## Règles de travail

- Une feature à la fois. Quand elle est terminée, ajoute une entrée en haut du journal ci-dessous avec le statut **En revue**.
- **Ne jamais commiter.** L'utilisateur relit le code. S'il valide, on commite, l'entrée passe en **Validée** avec le hash du commit, puis on attaque la feature suivante.
- Ne pas commencer la feature suivante tant que la précédente n'est pas validée. Les retouches demandées pendant la revue vont dans la même entrée.

## Vérifier sans casque

Unity : `/Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app/Contents/MacOS/Unity`. Chaque lancement prend une à deux minutes. Lance-le en arrière-plan, puis cherche `error CS` et `[Deskmates]` dans le log.

- **Configuration et génération** des prefabs et de la scène manquants (pas de `-quit`, la méthode quitte seule) :
  `Unity -batchmode -projectPath . -executeMethod Deskmates.Editor.ProjectSetup.RunBatch -logFile <log>`
- **Tests EditMode** (logique pure, par exemple le lissage du stress) :
  `Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults <xml> -logFile <log>`
- **Tests PlayMode** (prefabs des créatures, et `Main.unity` avec les pièces Office fournies par MRUK) :
  `Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults <xml> -logFile <log>`
- **Si l'utilisateur a Unity ouvert sur le projet**, un batch sur le même dossier est refusé. Clone le projet dans le scratchpad (`cp -c -R` de `Assets`, `Packages`, `ProjectSettings` et `Library` : copie instantanée sur APFS), lance le batch sur le clone, puis rapatrie `Assets/_Project/` avec ses `.meta`. Vérifie d'abord, avec `rsync -n`, que rien d'autre n'a changé.
- **Scène `Sandbox_Creature`** : les quatre créatures sur un bureau, sans casque. Sur l'objet « Stress Sandbox » :
  - case *Same Stress For All* cochée : le curseur *Stress*, ou les touches 1 à 4 (calme à panique), règlent toutes les créatures ;
  - case décochée : chaque créature suit son curseur dans *Individual Stress* (ordre : Mail, Calendar, Tasks, Messages) ;
  - touche D, ou case *Use Demo* : les quatre créatures suivent le scénario de démo ;
  - Espace déclenche le soupir de toutes.
- **Mode démo** : l'objet « Data » (`DataHub`) des scènes joue `Settings/Data/DemoScript_Default.asset`. Son champ *Speed* accélère le scénario, pour la vidéo ou un test rapide.
- **Bureau, dans `Main`** : sans main à poser, on utilise les raccourcis d'éditeur, vue Game active :
  - C calibre sur la table scannée devant l'utilisateur, ou à défaut 40 cm sous les yeux ;
  - R relance une calibration ;
  - F oublie le bureau.

  Dans le simulateur, mets *Left/Right input* sur *Hand* pour saisir les créatures. Les bureaux mémorisés sont dans `desks.json` (`Application.persistentDataPath`), mais seulement avec de vraies ancres (simulateur ou casque) : dans l'éditeur sans casque, rien n'est mémorisé.
- Le test dans le XR Simulator se fait à la main : ouvrir `Main.unity`, Window > Meta > Meta XR Simulator > Activate, choisir l'environnement « Office », puis Play.

Pièges déjà rencontrés :
- Ne pas créer ni modifier de fichier sous `Assets/` pendant un lancement batch. Un `AssetDatabase.Refresh()` déclenche alors un rechargement qui tue la méthode en cours, et Unity ne quitte plus.
- Lis les vraies API dans `Library/PackageCache/com.meta.xr.*` plutôt que de mémoire : la v207 diffère des anciennes docs (par exemple, `OVRPassthroughLayer` est toujours en underlay et n'a plus d'`overlayType`).
- `com.meta.xr.sdk.all` n'existe pas sur le registre Meta : on utilise les paquets `com.meta.xr.*` séparés.
- Les modules intégrés d'Unity sont déclarés explicitement dans `Packages/manifest.json`, car le Core SDK a besoin d'`assetbundle` sans le déclarer.
- macOS ne distingue pas les majuscules dans les noms de fichiers : rien ne doit s'appeler `assets/` à côté d'`Assets/`. Les sources 3D sont dans `SourceArt/`.
- Dans un script éditeur, `EditorSceneManager.OpenScene` décharge les assets que rien ne référence encore. Une variable qui pointait sur un asset tout juste créé devient alors nulle. Recharge l'asset par son chemin après avoir ouvert la scène.
- Unity 6.6 peut planter en se fermant après un `-executeMethod` (code 134, `libc++abi: terminate_handler`), alors que tout le travail est fait. Fie-toi à la ligne `[Deskmates] Batch setup finished.` du log plutôt qu'au code de sortie.

## Journal des features

Modèle d'entrée : titre et statut, date, objectif (renvoi au PRD), ce qui a été fait, fichiers, ce qui est vérifié et ce qui ne l'est pas, décisions à valider.

Découpage de la semaine 2 (PRD §11) : 2. créature vivante, 3. mode démo, 4. calibration du bureau et lâcher avec rebond, 5. regard et pincement (bulle d'aperçu).

### 9. Créature Agents, en mode démo (PRD §4.2, §4.4, §4.6 et §7.2 bis) — Validée (hash dans le commit suivant)

- **Date** : 16 novembre 2026.
- **Objectif** : la cinquième créature, pour les développeurs : elle s'inquiète quand une session d'agent de code attend une réponse, échoue ou a fini sans être consultée. Ici en mode démo ; la connexion réelle (relais local et hooks) vient ensuite.
- **Fait** :
  - Modèle (`SourceArt/creatures_bureau/build_creatures.py`, `Agents.glb`) : casque de chantier jaune, lampe frontale (matériau `LampLens`), arceau de 5 billes (`Agents_Bead0` à `4`, un matériau chacune). Les quatre autres `.glb` sont inchangés. Rendus `rendu_agents.png` et `rendu_famille.png`.
  - Données : `SourceIds.Agents`, `AgentStates` (running, waiting, done, failed), champ `State` optionnel sur `SourceItem` et sur les événements de démo. `AgentsStress` suit la formule du PRD §4.4.
  - Scénario de démo : trois sessions inventées (`refactor-auth`, `fix-flaky-tests`, `docs-site`) ; l'une demande une autorisation, une échoue, une finit ; l'utilisateur répond et les consulte ; panique vers 1:00, soupir vers 2:12. Ajouté une fois aux scénarios existants par `ProjectSetup`.
  - `AgentsLamp` : une bille par session dans l'ordre d'arrivée (grise en cours, bleue terminée, orange en attente, rouge en erreur), lampe qui pulse doucement tant qu'une session attend, deux clignements quand une session se termine.
  - Bulle « 1 waiting for you · 1 failed », panneau trié (attente, erreur, terminée, en cours), pincer une ligne la marque « seen ».
  - `Main` : cinq créatures, rangée resserrée à 12 cm pour tenir dans le cône central. L'onboarding présente aussi Agents.
- **Vérifié** : 49 tests EditMode et 25 PlayMode passent. Le scénario d'Agents passe par les quatre états, soupire et finit calme ; noms d'outils de code ajoutés à la liste des marques interdites.
- **Pas encore vérifié** : sur le casque (lisibilité des billes, éclat de la lampe), performances avec cinq créatures.
- **Décisions à valider** :
  - Environ 22 000 triangles par créature, 113 000 au total, au-dessus du budget prudent du PRD (50 000). On ne simplifie que si les fps le demandent.
  - Jamais de nom d'outil dans l'app : « session », « waiting for you ».

### 8. Finition de l'interface (STYLE.md) — Validée (hash dans le commit suivant)

- **Date** : 16 novembre 2026.
- **Fait** : `PopIn` fait apparaître la bulle, le panneau et la carte d'onboarding avec un petit rebond. La bulle a une pointe vers sa créature. Le panneau porte en haut une pastille à la couleur de la créature (`CreatureInfo.Accent`, réglée par `ProjectSetup` : bleu casquette, bleu-gris, lilas, sauge, jaune casque).
- **Vérifié** : tests ci-dessus. Pas encore vu sur le casque.

### 7. Onboarding du premier lancement (PRD §6.1, S4) — Validée (`5714c6e`)

- **Date** : 16 novembre 2026.
- **Objectif** : un premier lancement clair en moins de 60 s, sans mur de texte : poser la main, rencontrer le Facteur, le regarder, le pincer, le calmer, puis découvrir la famille.
- **Fait** :
  - `Onboarding` (dans `Scripts/UI/`) : une carte crème flottant 60 cm devant l'utilisateur, 12 cm sous les yeux, qui suit doucement la tête. Étapes :
    1. Aucune créature, démo en pause : « Place your hand flat on your desk ».
    2. Bureau calibré : le Facteur tombe seul, la démo démarre : « This is your Mail Fretling ».
    3. « Keep working », jusqu'à ce qu'il soit stressé ; puis « It's worried! Look at it, then pinch ».
    4. Panneau ouvert : « Pinch each line to mark it read ».
    5. Premier soupir (action de l'utilisateur ou *Auto Resolve*) : les trois autres tombent sur le bureau, « Meet your Fretlings! », la carte disparaît après 6 s.
  - Aux lancements suivants, pas de tutoriel (`PlayerPrefs`). La carte de la main réapparaît dès qu'une calibration attend.
  - `AppBootstrap` : `HideAll` et `Reveal(n)` ; une créature révélée tombe de 20 cm sur le bureau.
  - `ProjectSetup` crée l'objet « Onboarding » et sa carte dans `Main`. Les tests PlayMode sautent le tutoriel par défaut (`Onboarding.DoneOverride`).
- **Fichiers** : `Scripts/UI/Onboarding.cs`, `Scripts/Core/AppBootstrap.cs`, `Editor/ProjectSetup.cs`, `Tests/PlayMode/OnboardingTests.cs`. Généré : `Scenes/Main.unity`.
- **Vérifié** : 46 tests EditMode et 24 PlayMode passent. Le nouveau test joue le parcours dans `Main` : rien avant le bureau, démo en pause ; le Facteur seul ensuite ; toute la famille après son soupir.
- **Pas encore vérifié** : sur le casque (lisibilité et place de la carte, durée réelle du parcours).
- **Décisions à valider** :
  - Pas encore d'œuf qui éclot ni de main fantôme animée (PRD §6.1) : texte seul, à ajouter s'il reste du temps.
  - Pas d'étape « Connect your Google account » : la connexion Google n'existe pas.
  - Pour revoir le tutoriel sur le casque, il faut désinstaller l'app.
- **Retouche du 16 novembre**, après l'essai sur casque. La carte « Keep working » restait affichée, et les autres créatures n'apparaissaient jamais : le soupir n'arrive que si le stress a d'abord dépassé 0,6, ce qui n'était pas le cas quand la boîte avait été vidée tôt.
  - « Keep working » disparaît après 4 s ; pendant cette attente, la démo tourne 4 fois plus vite pour que le Facteur s'inquiète en une dizaine de secondes.
  - La famille arrive au soupir, ou quand l'utilisateur a vidé la boîte depuis le panneau, ou au plus tard 90 s après l'arrivée du Facteur. Ouvrir le panneau avant l'inquiétude saute directement à l'étape « Pinch each line ».
  - Test ajouté : vider la boîte sans soupir amène la famille. 46 tests EditMode et 25 PlayMode passent.
  - Mémoire du bureau (feature 4) : sur le casque, le bureau n'était jamais retrouvé. `DeskSession` choisissait les ancres selon `OVRManager.isHmdPresent`, encore faux à ce moment sur l'appareil : l'app prenait les ancres en mémoire et n'écrivait jamais `desks.json` (« Desk anchored » s'affichait une milliseconde après la calibration). Hors éditeur, les vraies ancres sont maintenant toujours utilisées. Pas encore vérifié sur le casque.

### 6. Toute la famille dans `Main` (PRD §4.2 et §7.1) — Validée (`650c3ec`)

- **Date** : 16 novembre 2026.
- **Objectif** : les quatre créatures (Mail, Calendar, Tasks, Messages) sur le bureau dans `Main`, pour que le mode démo montre toute la famille.
- **Fait** :
  - `AppBootstrap` fait apparaître les quatre prefabs (`creaturePrefabs`, le Facteur en premier ; `MailCreature` reste pour les tests). Avant calibration, chaque créature est posée au point de la table du scan le plus proche de sa place dans la rangée, donc jamais hors de la table, et tournée vers l'utilisateur. Après calibration, chacune retrouve sa place mémorisée ou tombe sur son emplacement par défaut (`DeskLocator.DefaultSpot`, rangée espacée de 14 cm).
  - Regard : quand le rayon large touche plusieurs créatures voisines, la plus proche de l'axe du regard l'emporte (sans allocation par image).
  - `ProjectSetup` branche les quatre prefabs dans la scène. La bulle et le panneau ne sont plus reconstruits s'ils ont déjà le style arrondi, pour éviter des diffs à chaque génération.
- **Fichiers** : `Scripts/Core/AppBootstrap.cs`, `Scripts/Core/DeskLocator.cs`, `Scripts/Interaction/GazeSelector.cs`, `Editor/ProjectSetup.cs`, `Tests/PlayMode/MainDeskFlowTests.cs`. Généré : `Scenes/Main.unity`.
- **Vérifié** : 46 tests EditMode et 23 PlayMode passent. `MainDeskFlowTests` vérifie les quatre créatures sur la table, puis toutes posées sur le bureau avec leur place mémorisée.
- **Pas encore vérifié** : sur le casque (lisibilité de la rangée, regard entre deux créatures voisines).

### 5. Regarder et pincer : bulle d’aperçu et panneau de détail (PRD §5, S2) — Validée (`a0d5783`)

- **Date** : 12 novembre 2026.
- **Objectif** : regarder une créature affiche un aperçu ; pincer en la regardant ouvre ses éléments les plus urgents ; regarder une ligne et pincer la marque lue ou faite. C'est l'action de l'utilisateur qui fait retomber le stress et déclenche le soupir.
- **Fait** :
  - `GazeSelector` : regard par la direction de la tête (Quest 3 et 3S n'ont pas de suivi des yeux, c'est le repli prévu par le PRD), fixation de 0,4 s, puis pincement pouce-index de l'une ou l'autre main via l'Interaction SDK. En éditeur, la touche P remplace le pincement (onglet Game actif).
  - `CreatureInfo` sur chaque prefab : bulle d'aperçu au-dessus de la créature, par exemple « 3 important emails · oldest 2 h ».
  - `DetailPanel`, un seul dans `Main` : titre, trois lignes et une aide. Il suit la créature, se tourne vers l'utilisateur et se ferme après 3 s de regard ailleurs. Pincer une ligne marque l'élément lu (mails, messages) ou fait (tâches). Les réunions sont en lecture seule.
  - `ItemText` : les textes en anglais et l'ordre des éléments (importants puis plus anciens ; prochaine réunion ; tâches les plus en retard).
  - Saisie à distance retirée des prefabs : pincer à distance sélectionne, saisir se fait main près de la créature (PRD §5 mis à jour).
- **Fichiers** : `Scripts/Interaction/GazeSelector.cs`, `Scripts/UI/` (nouveau), `Scripts/Desk/IsdkHandSampler.cs` (pincement), `Editor/ProjectSetup.cs`, `PRD.md`, tests `EditMode/ItemTextTests.cs` et `PlayMode/LookAndPinchTests.cs`. Générés : les 4 prefabs (bulle), `Scenes/Main.unity` (regard, panneau).
- **Vérifié** : 46 tests EditMode et 23 PlayMode passent. Le nouveau test PlayMode, dans `Main` : le Facteur panique, le regarder affiche la bulle, pincer ouvre le panneau avec 3 mails, tout marquer lu vide la boîte et il soupire une fois.
- **Pas encore vérifié** : à l'œil dans le simulateur (lisibilité, taille, position du panneau) ; le pincement avec de vraies mains.
- **Retouche du 15 novembre**, après le premier essai sur Quest 3S. Le regard ne réagissait pas, le pincement non plus, la calibration ne se déclenchait jamais, et une créature lâchée restait en l'air, faute de bureau.
  - Cause probable : le rig de Meta a deux composants `Hand` par côté (mains suivies et mains pilotées par les manettes), et `IsdkHandSampler` gardait le premier trouvé. Il prend maintenant, à chaque image, celui dont le suivi est valide.
  - Regard : si le rayon précis (3 cm) ne touche rien, un second rayon plus large (10 cm) est essayé, car la tête vise moins bas que les yeux.
  - Pendant une calibration, une ligne `[Deskmates] Calibrating…` toutes les 2 s dit ce que l'app voit des mains (lisible avec `adb logcat -s Unity`).
  - Limite de sécurité (Guardian) : en sortir effaçait les créatures. Comme le passthrough est toujours actif, l'app demande à la masquer (`shouldBoundaryVisibilityBeSuppressed` sur l'`OVRManager`, et le support *Boundary Visibility* dans la configuration Meta, aussi réglés par `ProjectSetup`).
  - Vérifié sur le casque par l'utilisateur le 16 novembre : calibration, regard, pincement et chute fonctionnent.
- **Retouche du 16 novembre** (panneau et style), après l'essai sur casque :
  - Fermeture : pincer la créature une seconde fois referme son panneau. Sinon, il reste ouvert tant que l'utilisateur regarde la créature ou le panneau, et se ferme après 5 s de regard ailleurs. L'aide le dit : « Pinch a line to mark it read · pinch me to close ».
  - Style : cartes crème arrondies, texte cacao en Lato (SIL OFL, copiée des exemples URP, notée dans `CREDITS.md`), lignes couleur crème qui passent en pêche au regard. Le sprite arrondi est généré par `ProjectSetup` (`Art/UI/Rounded.png`). La bulle et le panneau sont reconstruits à chaque génération.
  - Nouveau `STYLE.md` : charte visuelle (couleurs, typo, interface, icône, couverture, vidéo, ton).
  - Nom de l'app : **Fretlings** (choisi par l'utilisateur). Titre du PRD et `STYLE.md` mis à jour. `ProjectSetup` règle le nom affiché et le nouvel identifiant Android `com.fretlings.app` ; ils sont aussi dans les réglages du projet et dans le profil de build Meta Quest, qui a ses propres réglages. Le code garde l'espace de noms `Deskmates`.
  - Chute : une créature lâchée avant toute calibration restait en l'air. Elle retombe maintenant sur la table du scan (repli du PRD §5.1, `DeskSession.FallbackPlane`, zone de 50 × 30 cm autour de son emplacement). Test ajouté dans `MainDeskFlowTests`.
  - Constat sur casque : aucun `desks.json` n'a été écrit, donc le bureau n'est pas retrouvé au lancement suivant. Cause à lire dans les logs à la prochaine calibration (« Desk anchored » ou un avertissement d'ancre).
  - Vérifié : 46 tests EditMode et 23 PlayMode passent.
  - Vu sur le casque le 16 novembre : style jugé meilleur par l'utilisateur.
- **Décisions à valider** :
  - Regard par la tête partout, y compris sur les lunettes, tant qu'on n'a pas pu tester leurs yeux.
  - Plus de saisie à distance.
  - *Auto Resolve* reste activé : sans action du juré, la démo se calme quand même ; ses actions la calment plus tôt.
  - Texte en police Unity par défaut (pas de TextMeshPro, qui demande d'importer des ressources).
  - La créature ne tourne pas la tête vers l'utilisateur quand il la regarde : elle lui fait déjà face.

### 4. Bureau : calibration à la main, mémoire et lâcher avec rebond (PRD §5.1 et §5.2, S2) — Validée (`764e6f5`)

- **Date** : 18 octobre 2026.
- **Objectif** : l'utilisateur indique son bureau en posant la main dessus. L'app retient ce bureau et la place des créatures d'une session à l'autre, et l'utilisateur peut en changer à tout moment. Le tout est un service que l'onboarding et le menu pourront appeler. Les créatures se saisissent à la main et retombent sur le bureau avec un rebond.
- **Fait** :
  - **PRD** :
    - nouvelle §5.2 : mémoire du bureau, plusieurs bureaux, recalibrage, oubli, service réutilisable ;
    - §5.1 ajustée : zone rectangulaire, main à plat détectée par les hauteurs, table MRUK pour la hauteur seulement ;
    - composants (§9), parcours (§6.2) et risques (§14) mis à jour.
  - **`FlatHandDetector`** : une ou deux mains à plat (articulations à la même hauteur à 2,5 cm près), immobiles 0,6 s, au moins 25 cm sous les yeux. L'immobilité se mesure comme une dérive de moins de 1,5 cm, pour tolérer les tremblements du suivi des mains. Hauteur = articulations les plus basses moins 8 mm. Classe simple, testable sans casque.
  - **`DeskCalibration`** : n'écoute les mains (via l'Interaction SDK) que pendant une calibration. Construit le plan, orienté face à l'utilisateur, et le cale sur une table du scan MRUK à ±3 cm. Une onde lumineuse (`DeskRipple`) parcourt le plan mesuré.
  - **`DeskSession`**, le service du §5.2. Son API :
    - `RestoreAsync`, `Recalibrate`, `CancelCalibration`, `ForgetCurrentDeskAsync` ;
    - `SavePlacement`, `TryGetPlacement`, `ClampToDesk` ;
    - `Plane`, `HasDesk`, `IsCalibrating`, `Root`, et l'événement `DeskChanged`.

    Une ancre par bureau, à laquelle les créatures sont rattachées. Recalibrer un bureau retrouvé le déplace en gardant la disposition des créatures ; sinon un nouveau bureau est créé et les anciens sont conservés.
  - **`DeskStore`** : la liste des bureaux, en JSON sur l'appareil. **`IAnchorService`** : ancres spatiales Meta, ou ancres en mémoire pour l'éditeur et les tests.
  - **`GrabAndDrop`** et **`DropMotion`** :
    - saisie par l'Interaction SDK, à la main ou à distance (la créature vient dans la main) ;
    - au lâcher : chute (gravité ×0,7), deux rebonds environ, retour dans la zone si besoin, puis la créature se redresse face à l'utilisateur ;
    - sa place est enregistrée ;
    - `CreatureMotion` écrase la créature à chaque impact.
  - **`AppBootstrap`** : restaure le bureau connu, sinon lance la calibration. En attendant, le Facteur est sur la table du scan ; dès que le bureau est calibré, il tombe sur son emplacement par défaut (`DeskLocator.DefaultSpot`).
  - **Scène `Main`** :
    - rig d'interaction complet de Meta (mains, saisie, rayons), ajouté avec son API publique ;
    - locomotion du rig désactivée, l'app s'utilisant assis ;
    - objet « Desk Session » (calibration, onde, raccourcis d'éditeur C, R, F).
- **Fichiers** :
  - `Scripts/Desk/` (nouveau dossier), `Scripts/Interaction/GrabAndDrop.cs`.
  - `Scripts/Core/AppBootstrap.cs` et `DeskLocator.cs`, `Scripts/Creatures/CreatureMotion.cs`, les asmdef.
  - `Editor/ProjectSetup.cs`, `PRD.md`.
  - Tests : `Tests/EditMode/FlatHandDetectorTests.cs` et `DeskGeometryTests.cs` ; `Tests/PlayMode/DeskSessionTests.cs` et `MainDeskFlowTests.cs`.
  - Générés : `Settings/Desk/DeskCalibrationConfig.asset`, les 4 prefabs (collider, saisie Meta, `GrabAndDrop`), `Scenes/Main.unity`.
- **Vérifié** :
  - 43 tests EditMode, dont 14 nouveaux :
    - le geste : main à plat et immobile, durée minimale, main qui glisse, tremblement toléré, main inclinée, main trop près des yeux, perte de suivi, deux mains moyennées ;
    - les repères du plan et la zone ;
    - la chute : rebonds, repos en moins d'une seconde ;
    - la mémoire JSON : aller-retour, fichier corrompu.
  - 22 tests PlayMode, dont 5 nouveaux, avec des ancres simulées en mémoire :
    - un bureau calibré est retrouvé après redémarrage avec la place du Facteur ;
    - un bureau déplacé emmène la créature, et seul le nouveau est gardé ;
    - dans une autre pièce, un nouveau bureau est créé, l'ancien est conservé, puis on peut l'oublier ;
    - une créature lâchée hors de la zone y revient ;
    - dans `Main`, sans bureau connu, la calibration démarre ; une fois le bureau calibré, le Facteur y tombe et sa place est retenue.
  - Aucune exception dans les logs, seulement l'absence de casque côté OpenXR.
- **Retouche du 18 octobre**, après un premier essai dans le simulateur où C semblait sans effet :
  - Le bureau est utilisable dès la calibration ; l'ancre est créée et enregistrée ensuite, en arrière-plan. `DeskSession.Persisting` permet d'attendre cette étape.
  - Messages `[Deskmates]` dans la Console : touches C, R, F, bureau calibré, ancré, restauré.
  - Les 65 tests repassent.
- **Pas encore vérifié** :
  - le geste avec de vraies mains, et l'épaisseur de 8 mm (casque) ;
  - les vraies ancres Meta : sauvegarde, puis recherche au lancement (simulateur ou casque) ;
  - la saisie à la main et à distance dans le simulateur ;
  - l'aspect de l'onde et de la chute à l'œil.
- **Décisions à valider** :
  - Zone rectangulaire de 1,2 × 0,7 m au lieu du disque de 60 cm (PRD mis à jour).
  - Main à plat acceptée paume vers le bas ou vers le haut : seules les hauteurs comptent.
  - Pour une table MRUK, on reprend la hauteur mais pas encore les bords.
  - Dans une même pièce, recalibrer remplace le bureau, puisqu'on considère qu'il a bougé. Pour garder deux bureaux dans la même pièce, il faudrait un choix explicite, dans le futur menu.
  - Saisie à distance : la créature vient dans la main (*PullToHand*).
  - Locomotion du rig de Meta désactivée.
  - Pas encore d'indication visuelle pendant la calibration (main fantôme) : elle viendra avec l'onboarding (S4).
  - Au premier placement, le Facteur tombe de 20 cm ; l'onde s'étend sur 30 cm de rayon. Réglés par l'utilisateur après essai dans le simulateur.

### 3. Mode démo (PRD §7.1, §7.4 et §4.4, S2) — Validée (`9a103f7`)

- **Date** : 18 octobre 2026.
- **Objectif** : les créatures réagissent à de fausses données réalistes qui évoluent sur 3 minutes, à travers la même interface que les futures vraies sources.
- **Fait** :
  - Interface commune `IDataSource` (PRD §7.4), avec `SourceSnapshot`, `SourceItem`, `ItemAction`, `SourceIds` et une horloge `IClock`.
  - Formules de stress du PRD §4.4 : `MailStress`, `CalendarStress`, `TasksStress`, `MessagesStress`. Une par créature dans `Settings/Stress/`, avec des constantes réglables.
  - `DemoScript` (ScriptableObject) décrit le scénario minuté. `DemoPlayer` le joue avec une horloge de démo plus rapide que le temps réel (0,5 minute de démo par seconde, journée qui commence à 9 h 30). `DemoDataSource` implémente `IDataSource`.
  - Scénario par défaut (`Settings/Data/DemoScript_Default.asset`, 3 minutes ; minutage à vitesse 1) :
    - **Mail** : cinq mails importants arrivent entre 0:08 et 1:20, panique vers 1:20, boîte vidée à 1:55.
    - **Réunion** « Design review » (10 h 40 en heure de démo) : inquiétude dès 0:20, panique vers 2:00, la réunion commence à 2:20 et la créature soupire.
    - **Tâches** : deux dues aujourd'hui dès le début, deux en retard à 1:30 et 1:50 (panique), cochées vers 2:30.
    - **Messages** : deux messages directs à 0:30 et 1:10, panique vers 1:30, réponses à 2:40.
  - *Auto Resolve* : tant que les interactions à la main n'existent pas (feature 5), le scénario joue lui-même les actions de l'utilisateur (marquer lu, marquer fait).
  - `DataHub`, un par scène : point d'accès aux sources et à l'horloge, avec un champ *Speed*.
  - `CreatureDataLink`, sur chaque prefab : lit la source de sa créature et en règle le stress à chaque image.
  - Scène `Main` : le Facteur suit la démo dès le lancement. Scène `Sandbox_Creature` : la touche D lance la démo sur les quatre créatures.
  - `ProjectSetup` crée les formules et le scénario, relie les prefabs à leur source et ajoute le DataHub aux scènes.
- **Fichiers** :
  - `Scripts/Data/` (nouveau dossier, avec `Demo/` et `Stress/`).
  - `Scripts/Sandbox/StressSandbox.cs`, `Editor/ProjectSetup.cs`.
  - Tests : `Tests/EditMode/StressCalculatorTests.cs`, `DemoPlayerTests.cs` et `DemoScenarioTests.cs` ; `Tests/PlayMode/DemoModeTests.cs`.
  - Générés : `Settings/Stress/*.asset`, `Settings/Data/DemoScript_Default.asset`, les 4 prefabs (lien vers leur source), `Scenes/Main.unity` et `Scenes/Sandbox_Creature.unity` (objet « Data »).
- **Vérifié** :
  - 29 tests EditMode, dont 21 nouveaux :
    - les formules donnent les valeurs du PRD ;
    - le lecteur gère l'horloge, les événements, la résolution automatique, les actions de l'utilisateur et la boucle ;
    - simulation complète du scénario avec les vraies formules et le vrai lissage : chaque créature passe par calme, attentif, stressé et panique, soupire au moins une fois et finit calme ;
    - aucun nom de marque dans les textes.
  - 17 tests PlayMode, dont 1 nouveau : dans `Main`, la démo seule amène le Facteur en panique puis au soupir. C'est le critère de la semaine 2, joué ici en accéléré ×40.
  - Ce test a trouvé un bug, corrigé : le DataHub de `Main` n'avait pas de scénario, la référence se perdait à l'ouverture de la scène.
- **Pas encore vérifié** : l'aspect à l'œil, dans le simulateur ou dans la sandbox.
- **Décisions à valider** :
  - Le contenu du scénario : textes en anglais, noms inventés.
  - *Auto Resolve* activé par défaut pour l'instant.
  - Entre 5 et 2 minutes avant une réunion, le PRD ne dit rien : le stress y monte de 0,9 à 1.
  - Les métriques (mails importants non lus, minutes avant la réunion…) sont calculées par les formules à partir des éléments, et non stockées dans le snapshot comme le laisse entendre le PRD §7.4. Ainsi, une réunion continue d'approcher entre deux mises à jour.
  - L'action *Snooze* sur un élément renvoie `false` : le snooze s'applique à une créature entière (PRD §5).
  - La démo ne boucle pas ; la case *Loop* du scénario permet de la faire boucler.

### 2.1 Visière de la casquette du Facteur — Validée (`1eb87c0`)

- **Date** : 18 octobre 2026.
- **Objectif** : corriger un défaut repéré par l'utilisateur : la partie plate à l'avant de la casquette semblait disparaître dans le corps.
- **Cause** : dans `build_creatures.py`, la visière était un demi-cylindre obtenu en supprimant des sommets. Cette suppression effaçait aussi les faces du dessus et du dessous. La visière n'était donc qu'un ruban, et on voyait le corps à travers.
- **Fait** :
  - Le script coupe maintenant le cylindre par un plan, referme la coupe et rend la visière en faces plates. Elle devient un demi-disque plein (138 sommets au lieu de 42).
  - Modèles reconstruits avec Blender 5.1.1, sans interface.
  - Seul `Facteur.glb` a été recopié ; les trois autres créatures ne changent pas.
- **Fichiers** :
  - `SourceArt/creatures_bureau/` : `build_creatures.py`, `Facteur.glb`, `creatures_bureau.blend`, et les deux rendus `rendu_*.png`.
  - `Assets/_Project/Art/Models/Facteur.glb`.
- **Vérifié** :
  - Comparaison nœud par nœud de l'ancien et du nouveau `Facteur.glb` : seule `Facteur_CapVisor` change.
  - Gros plan rendu dans Blender avant et après : la visière est pleine et le corps ne se voit plus au travers.
  - Les 16 tests PlayMode passent avec le nouveau modèle, et le prefab `Creature_Mail` n'a pas eu besoin de changer.
- **Pas encore vérifié** : l'aspect dans Unity, à l'œil.
- **Décisions à valider** :
  - Les trois autres `.glb` de `SourceArt` restent ceux de l'export d'origine (Blender 5.2), alors que le `.blend` et les rendus viennent du nouvel export (Blender 5.1.1).
  - `SourceArt/creatures_bureau.zip` contient toujours l'ancienne version. Laissé tel quel, faute de décision.

### 2. Créature vivante (PRD §4.3 à 4.5, S2) — Validée (`38b1192`)

- **Date** : 18 octobre 2026.
- **Objectif** : la créature réagit à une valeur de stress par son visage, son mouvement, sa couleur et ses effets, puis soupire de soulagement. Le mode démo qui pilotera ce stress vient avec la feature 3.
- **Fait** :
  - `CreatureConfig` (ScriptableObject) : un par créature dans `Settings/Creatures/`, avec la couleur au repos de chacune (PRD §4.2), la rampe orange-rouge, les seuils, les tailles par état, les amplitudes de mouvement et la durée du soulagement.
  - `StressSmoother` :
    - lisse le stress, qui se stabilise en environ 1,5 s ;
    - choisit l'expression aux seuils 0,15, 0,45 et 0,8, avec une hystérésis de ±0,025 ;
    - déclenche le soulagement quand le stress repasse sous 0,15 après avoir dépassé 0,6.
  - `CreatureFace` : en plus de l'expression, cligne des yeux au hasard quand ils sont ouverts (attentif, stressé).
  - `CreatureMotion` : respiration (étirement et écrasement), taille de 1 à 1,4 selon le stress, petits sautillements en « stressé », tremblement en panique, et soulagement (la créature se dégonfle puis rebondit). Seul le modèle bouge, jamais la racine, pour ne pas gêner le placement ni la saisie.
  - `CreatureVfx` :
    - couleur du corps sur la rampe, avec une légère lueur en panique ;
    - flammes du Facteur aux tailles du PRD §10, avec un scintillement ;
    - post-it bleu puis rose des Tâches ;
    - goutte de sueur qui glisse ;
    - points de la bulle des Messages qui s'animent dès qu'il y a du stress ;
    - petit nuage de particules qui sort de la bouche au soupir.
  - `Creature` orchestre l'ensemble et expose `SetTargetStress`, `PlayRelief` et l'événement `Relieved`.
  - `ProjectSetup` crée les configs, complète les prefabs existants (composants, références, nuage) et crée la scène `Sandbox_Creature`.
  - `StressSandbox` : un curseur commun, ou un curseur par créature (case *Same Stress For All* décochée), ajouté à la demande de l'utilisateur pour tester chaque créature séparément.
- **Fichiers** :
  - `Scripts/Creatures/` : `Creature.cs`, `CreatureFace.cs`, `CreatureConfig.cs`, `CreatureMotion.cs`, `CreatureVfx.cs`, `StressRamp.cs`, `StressSmoother.cs`.
  - `Scripts/Sandbox/StressSandbox.cs` ; `Deskmates.asmdef` référence maintenant l'Input System.
  - `Editor/ProjectSetup.cs`.
  - `Tests/EditMode/StressSmootherTests.cs` et `Tests/PlayMode/CreatureStressTests.cs` (nouveaux), `Tests/PlayMode/DeskPlacementTests.cs` (corrigé).
  - Générés : les 4 prefabs, `Settings/Creatures/*.asset`, `Scenes/Sandbox_Creature.unity`. Tout chemin est relatif à `Assets/_Project/`.
- **Vérifié** :
  - 8 tests EditMode : le lissage est progressif ; l'expression correspond à chaque état ; pas de clignotement autour d'un seuil ; un seul soulagement après une panique, aucun sans pic de stress.
  - 16 tests PlayMode. Sur les 4 prefabs : panique puis calme, avec le bon visage, une taille ×1,4, un corps rouge et un seul soulagement. Les flammes du Facteur et les post-it des Tâches apparaissent aux bons états. Les 10 tests de placement de la feature 1 passent toujours.
  - Correction du test de placement de la feature 1 : sa mesure de hauteur ne prend plus que les maillages du modèle. Elle incluait le nouvel émetteur de particules, qui sans particules se place à l'origine.
  - Le 18 octobre, l'utilisateur a jugé l'animation bonne à l'œil.
- **Pas encore vérifié** : les performances sur casque.
- **Décisions à valider** :
  - Pas encore de son pour le soupir : aucun son CC0 choisi.
  - Les flammes restent en maillages ; le passage aux particules est prévu plus tard (PRD §10).
  - Pas encore d'ombre de contact sous les créatures.
  - Les diffs des prefabs sont longs : c'est surtout la sérialisation par défaut du système de particules.
- **Aussi dans ce commit** :
  - `.gitignore` ignore les fichiers `RuntimeActionBindings.json` que le SDK Meta écrit à chaque Play.
  - Le profil de build Meta Quest passe à l'API Android cible 34, réglage appliqué par le Project Setup Tool.

### 1. Socle technique (PRD §11, S1) — Validée (`ee0df9a`)

- **Date** : 13 octobre 2026.
- **Objectif** : projet Unity, SDK Meta, import des créatures. Critère : le Facteur s'affiche sur le bureau de la pièce synthétique « office » dans le simulateur.
- **Fait** :
  - Projet Unity 6 URP à la racine du dépôt, créé à partir du modèle URP fourni avec l'éditeur. Paquets Meta Core, Interaction et MRUK 207.0.1, glTFast 6.14.1, OpenXR 1.18.0.
  - Configuration : OpenXR sur Mac et Android, mains seulement, passthrough, scène et ancres activés. Le reste a été appliqué par le « Fix all » du Project Setup Tool de Meta.
  - Un prefab par créature (`Creature_Mail`, `_Calendar`, `_Tasks`, `_Messages`). Le modèle est placé sous une racine à l'échelle 1, visage vers +Z, base à y = 0.
  - `CreatureFace` affiche les parties du visage d'une expression (PRD §4.3). `Creature` masque les accessoires de stress et met la créature au calme au démarrage.
  - `DeskLocator` choisit, via MRUK, la table la plus proche d'un point situé 55 cm devant l'utilisateur et place la créature dessus, à 8 cm des bords, tournée vers lui. Sans table, la créature flotte devant l'utilisateur, 35 cm sous les yeux.
  - `AppBootstrap` attend le chargement de la pièce, puis pose le Facteur.
  - Scène `Main` : rig caméra, passthrough, MRUK, lumière, `App`.
- **Fichiers** : `Assets/_Project/Scripts/` (`Core/DeskLocator.cs`, `Core/AppBootstrap.cs`, `Creatures/Creature.cs`, `Creatures/CreatureFace.cs`), `Assets/_Project/Editor/ProjectSetup.cs`, `Assets/_Project/Tests/PlayMode/DeskPlacementTests.cs`, `Assets/_Project/Prefabs/Creatures/`, `Assets/_Project/Scenes/Main.unity`, `Packages/manifest.json`, `ProjectSettings/`, `CREDITS.md`, et quatre lignes du PRD (version Unity, paquets Meta, chemin `SourceArt/`).
- **Vérifié** : compilation sans erreur. Les 10 tests PlayMode passent : dans les pièces Office00 à Office09, le Facteur est posé sur une table, tourné vers l'utilisateur, au calme.
- **Vérifié le 18 octobre** :
  - Critère de S1 atteint : dans le XR Simulator, environnement Office, le Facteur est bien visible sur le bureau (constaté par l'utilisateur).
  - Le projet s'ouvre dans l'éditeur depuis Unity Hub, sans erreur de compilation ni exception.
  - Module Android Build Support installé (SDK, NDK, OpenJDK).
  - Plateforme passée sur Meta Quest (`Assets/Settings/Build Profiles/Meta Quest.asset`), sans paquet supplémentaire.
  - Corrections « Recommended » du Project Setup Tool appliquées, dont Dynamic Resolution et la fonction OpenXR Application SpaceWarp. Cette fonction reste inactive tant que l'app ne l'allume pas ; c'est à décider en S4 (optimisation).
- **Pas encore vérifié** : build APK et installation sur casque.
- **Décisions à valider** :
  - `assets/` renommé en `SourceArt/`.
  - Identifiant Android `com.deskmates.app`, figé au premier envoi sur le dashboard Meta.
  - Mains seulement, sans manettes.
  - Platform SDK reporté jusqu'à ce qu'un App ID existe.
  - Meta XR Operator activé par le « Fix all ». Il permet à un agent IA d'observer la scène en mode Play dans l'éditeur.
