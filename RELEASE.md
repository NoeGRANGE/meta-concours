# Mettre Fretlings en ligne pour le concours

Objectif (règlement, section 4) : l'APK dans un canal de diffusion nommé exactement **« Competition »** sur le Meta Developer Dashboard, avec un **lien d'invitation** à coller dans Devpost (champs *Try it out* et *Release channel URL*). Il doit rester accessible jusqu'à l'annonce des gagnants.

Déjà fait dans le projet : vitesse de la démo remise à 1, version 1.0.0 (code de version 1), identifiant `com.fretlings.app`, build non « Development ».

---

## 1. Créer la clé de signature (une seule fois, 5 min)

Le dashboard refuse les APK signés avec la clé de debug d'Unity.

1. Ouvre le projet dans Unity, puis *Edit > Project Settings > Player*, onglet Android, section **Publishing Settings**.
2. Clique sur **Keystore Manager…**, puis *Keystore… > Create New > Anywhere…* et enregistre-la **hors du projet**, par exemple `~/Documents/Fretlings/fretlings.keystore`.
3. Choisis un mot de passe, puis crée une **clé** (alias `fretlings`, même mot de passe, validité 50 ans, ton nom dans « First and Last Name »). Clique sur *Add key*.
4. Vérifie que **Custom Keystore** est coché et que l'alias `fretlings` est sélectionné.

**Sauvegarde ce fichier et son mot de passe** (gestionnaire de mots de passe, clé USB) : chaque mise à jour future devra être signée avec la même clé. Il n'ira jamais dans git (protégé par `.gitignore`).

## 2. Construire l'APK final (10–15 min)

1. *File > Build Profiles*, profil **Meta Quest** actif, **Development Build décoché**.
2. Vérifie dans la scène `Main`, objet « Data », que **Speed = 1**.
3. Clique sur **Build** (pas Build And Run) et enregistre sous `~/Builds/Fretlings-1.0.0.apk`.

## 3. Créer l'app sur le dashboard (5 min)

1. Va sur **developers.meta.com/horizon/manage**, connecté avec ton compte développeur (celui du programme Start).
2. *Create New App* : nom **Fretlings**, plateforme **Meta Horizon Store** (Quest / Horizon OS).
3. Tu obtiens une **App ID**. Rien d'autre n'est exigé pour un canal privé : pas de fiche store, pas de vérification Meta.

## 4. Créer le canal « Competition » et envoyer l'APK (10 min)

Le plus simple, avec **Meta Quest Developer Hub** (MQDH, application Mac, sur developers.meta.com) :
1. Connecte-toi dans MQDH avec le même compte, puis ouvre l'onglet **App Distribution**.
2. Choisis l'app Fretlings, clique sur **Upload**, sélectionne `Fretlings-1.0.0.apk`.
3. Pour le canal, crée-en un nouveau nommé exactement **`Competition`** (majuscule, sans espace).
4. Lance l'envoi. MQDH vérifie l'APK (signature, manifest) et signale tout problème : envoie-moi le message si ça bloque.

Sur le site, le dashboard propose aussi l'envoi : *ton app > Distribution > Release Channels*. Les noms exacts des menus peuvent varier.

Si le dashboard demande un **Data Use Checkup** ou une **classification d'âge** avant d'inviter des testeurs : l'app ne collecte aucune donnée, n'envoie rien en ligne, et ne contient ni violence ni contenu sensible (tout public).

## 5. Le lien d'invitation (2 min)

1. Dans le canal **Competition**, cherche *Add users* ou **lien d'invitation** (*invite link / Release Channel URL*), puis génère le lien.
2. Colle-le dans Devpost (*Try it out* et *Release channel URL*), et dans `DEVPOST.md`.

## 6. Vérifier comme un juré (15 min)

Le PRD (S5) le prévoit : ouvrir le lien avec **un autre compte Meta**, installer l'app depuis le store sur un casque, puis faire le parcours complet. Au minimum, ouvre le lien dans un navigateur privé pour voir qu'il est valide.

---

## Avant d'appuyer sur « Submit » sur Devpost

- [ ] APK 1.0.0 dans le canal **Competition**, lien d'invitation testé
- [ ] Vidéo sur YouTube (publique ou non répertoriée), moins de 3 min, lien dans Devpost
- [ ] Textes de `DEVPOST.md` collés, champs personnels remplis
- [ ] Vignette et galerie envoyées
- [ ] Soumission **avant le 18 novembre, 21 h (heure de Paris)** ; rien n'est modifiable ensuite
