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
- **Tests PlayMode**, qui chargent `Main.unity` avec les pièces Office fournies par MRUK :
  `Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults <xml> -logFile <log>`
- Le test dans le XR Simulator se fait à la main : ouvrir `Main.unity`, Window > Meta > Meta XR Simulator > Activate, choisir l'environnement « Office », puis Play.

Pièges déjà rencontrés :
- Ne pas créer ni modifier de fichier sous `Assets/` pendant un lancement batch. Un `AssetDatabase.Refresh()` déclenche alors un rechargement qui tue la méthode en cours, et Unity ne quitte plus.
- Lis les vraies API dans `Library/PackageCache/com.meta.xr.*` plutôt que de mémoire : la v207 diffère des anciennes docs (par exemple, `OVRPassthroughLayer` est toujours en underlay et n'a plus d'`overlayType`).
- `com.meta.xr.sdk.all` n'existe pas sur le registre Meta : on utilise les paquets `com.meta.xr.*` séparés.
- Les modules intégrés d'Unity sont déclarés explicitement dans `Packages/manifest.json`, car le Core SDK a besoin d'`assetbundle` sans le déclarer.
- macOS ne distingue pas les majuscules dans les noms de fichiers : rien ne doit s'appeler `assets/` à côté d'`Assets/`. Les sources 3D sont dans `SourceArt/`.

## Journal des features

Modèle d'entrée : titre et statut, date, objectif (renvoi au PRD), ce qui a été fait, fichiers, ce qui est vérifié et ce qui ne l'est pas, décisions à valider.

### 1. Socle technique (PRD §11, S1) — En revue

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
