# Deskmates — instructions pour Claude

Lis `PRD.md` en entier avant toute tâche. En cas de doute entre deux options, choisis celle qui sert le mieux les critères de jugement (PRD §2.3).

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
  - Espace déclenche le soupir de toutes.
- Le test dans le XR Simulator se fait à la main : ouvrir `Main.unity`, Window > Meta > Meta XR Simulator > Activate, choisir l'environnement « Office », puis Play.

Pièges déjà rencontrés :
- Ne pas créer ni modifier de fichier sous `Assets/` pendant un lancement batch. Un `AssetDatabase.Refresh()` déclenche alors un rechargement qui tue la méthode en cours, et Unity ne quitte plus.
- Lis les vraies API dans `Library/PackageCache/com.meta.xr.*` plutôt que de mémoire : la v207 diffère des anciennes docs (par exemple, `OVRPassthroughLayer` est toujours en underlay et n'a plus d'`overlayType`).
- `com.meta.xr.sdk.all` n'existe pas sur le registre Meta : on utilise les paquets `com.meta.xr.*` séparés.
- Les modules intégrés d'Unity sont déclarés explicitement dans `Packages/manifest.json`, car le Core SDK a besoin d'`assetbundle` sans le déclarer.
- macOS ne distingue pas les majuscules dans les noms de fichiers : rien ne doit s'appeler `assets/` à côté d'`Assets/`. Les sources 3D sont dans `SourceArt/`.
- Unity 6.6 peut planter en se fermant après un `-executeMethod` (code 134, `libc++abi: terminate_handler`), alors que tout le travail est fait. Fie-toi à la ligne `[Deskmates] Batch setup finished.` du log plutôt qu'au code de sortie.

## Journal des features

Modèle d'entrée : titre et statut, date, objectif (renvoi au PRD), ce qui a été fait, fichiers, ce qui est vérifié et ce qui ne l'est pas, décisions à valider.

Découpage de la semaine 2 (PRD §11) : 2. créature vivante, 3. mode démo, 4. calibration du bureau et lâcher avec rebond, 5. regard et pincement (bulle d'aperçu).

### 2.1 Visière de la casquette du Facteur — En revue

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
