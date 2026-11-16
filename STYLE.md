# Charte visuelle et ton

> Règles de style pour l'app, la vidéo, l'image de couverture, l'icône et les textes de soumission.
> Elles complètent le PRD (§4.1 pour les créatures). En cas de doute : **calme, chaleureux, tactile, un peu drôle**.
> Dernière mise à jour : 16 novembre 2026.

## 1. Nom

**Fretlings** (choisi le 16 novembre). De petits êtres (*-ling*, comme *duckling*) qui s'inquiètent (*to fret*) à la place de l'utilisateur : c'est le slogan en un mot. Aucune app de ce nom trouvée à cette date (recherche rapide, pas de recherche de marque).

- L'app : **Fretlings**, une majuscule, un seul mot, sans article.
- Une créature : **a Fretling** ; avec son rôle : *the Mail Fretling*, *your Calendar Fretling*. Jamais « a fret ».
- Les rôles restent en anglais : *Mail*, *Calendar*, *Tasks*, *Messages*, *Agents*.
- Le code garde son espace de noms `Deskmates` (nom de travail) : invisible pour l'utilisateur.

Écartés : *Deskmates* (terrain encombré, dont un projet « Deskmate » au concept proche), *FretPets* (évoque l'animal virtuel dont on prend soin, et un ton enfantin), *Worrybuds* (trop proche de l'app *WorryBugs*).

## 1 bis. Logo et icône (choisis le 17 novembre)

- **Logo** : le Facteur (rendu 3D détouré) dans une pastille pêche `#F2D3AE`, puis « Fretlings » en **Fredoka** demi-gras, avec une petite flamme orange-rouge à la place du point du « i ». Encre cacao sur fond clair, crème sur fond sombre. Fichiers : `SourceArt/brand/logo/` (régénérés par `build_logo.py`).
- **Icône de l'app** : le Facteur qui dépasse du bas d'un carré crème dégradé, avec la flamme près de sa casquette (`fretlings_icon_1024.png`, carré plein, sans coins arrondis).
- **Rendus détourés** des cinq créatures, au calme et en panique, et de la famille : `SourceArt/brand/stickers/` (script `render_stickers.py`, Blender, Cycles). À réutiliser pour la vidéo, la couverture et Devpost.
- **Typo** : Fredoka (SIL OFL) pour le logo et les titres ; Lato pour les textes.

## 2. Personnalité

- Les créatures paniquent **pour** l'utilisateur ; lui reste calme. L'humour vient de ce décalage, jamais d'une moquerie.
- Ambiant avant tout : au repos, presque rien ne bouge. L'agitation est réservée à l'urgence.
- Le soulagement (soupir, rebond) est la récompense : on le montre, on le met en scène.

## 3. Couleurs

Toujours tirées des créatures (PRD §4.2 et §4.3). Jamais de bleu « tech », de néon ni de dégradé violet.

| Rôle | Couleur | Usage |
|---|---|---|
| Crème vinyle | `#EFE6D8` | Corps du Facteur, fond des lignes du panneau |
| Carte | `#FBF6EE` (≈ 95 % d'opacité) | Fond des bulles et panneaux |
| Encre cacao | `#4A3B33` | Tous les textes |
| Encre douce | `#9A897C` | Aides, textes secondaires |
| Pêche « attentif » | `#F2D3AE` | Survol, élément regardé |
| Orange « stressé » | `#F1A661` | Accent, appels à l'action |
| Rouge « panique » | `#E2553A` | **Seulement** pour l'urgence. Jamais décoratif. |
| Pastels des créatures | `#DCE3E6`, `#E6DCEB`, `#D7E3CF`, `#D9E4EC` | Une couleur par créature (Calendar, Tasks, Messages, Agents) |

L'urgence ne passe jamais par la seule couleur : taille, mouvement et expression disent la même chose (PRD §4.1).

## 4. Typographie

- **Lato** (SIL OFL 1.1, voir `CREDITS.md`), régulière pour le texte, grasse pour les titres.
- Phrases courtes, casse normale (pas de TOUT EN MAJUSCULES), pas de point final sur les aides d'une ligne.
- Dans le casque : texte d'au moins 0,9° de hauteur angulaire (PRD §5).

## 5. Interface dans l'espace

- **Cartes arrondies**, comme du vinyle : rayon généreux, aucun bord noir, aucun trait fin.
- Petites et sobres : au plus un titre, trois lignes, une aide.
- Toujours tournées vers l'utilisateur, au-dessus de la créature concernée, dans le cône central (±25°).
- Pas de logo, de nom de marque ni de personne réelle (PRD §2.1).

## 6. Mouvement et son

- Animations procédurales, souples : écrasement et étirement, rebonds amortis, jamais de saut brusque.
- Sons courts, doux, organiques (souffle, « pop » mat, petit carillon). Licence CC0 seulement.

## 7. Icône de l'app

- Le **Facteur seul**, de trois quarts, visage calme ou attentif, sur un fond crème `#EFE6D8` uni.
- Une petite flamme orange derrière la casquette, pour dire « il s'inquiète » en un coup d'œil.
- Rendu 3D du vrai modèle (Blender), sans texte, lisible à 64 px.
- *Dimensions exigées par le Meta Developer Dashboard : à vérifier avant l'export.*

## 8. Image de couverture (Devpost, store)

- Une **vraie photo ou capture** d'un bureau réel en passthrough, avec les créatures posées dessus, au moins une en panique et une calme.
- Le nom en Lato grasse, encre cacao, sur une zone calme de l'image. Le slogan en dessous, plus petit.
- Format 3:2 pour Devpost. *Formats du store Meta : à vérifier.*

## 9. Vidéo de présentation (< 3 min, en anglais)

Règles du concours (PRD §12) : uniquement de vraies images filmées dans le casque, pas d'IA générée, sous-titres anglais.

| Moment | Durée | Contenu |
|---|---|---|
| Accroche | 0:00–0:10 | Bureau réel ; le Facteur panique, flammes ; un regard, un pincement ; il soupire. |
| Le problème | 0:10–0:30 | Notifications qui interrompent : plans courts, sans marques visibles. |
| La famille | 0:30–1:10 | Chaque créature et ce qu'elle surveille, du calme à la panique. |
| Les gestes | 1:10–1:50 | Poser la main (onde), regarder (bulle), pincer (panneau), marquer lu, saisir et lâcher. |
| Le quotidien | 1:50–2:30 | Les créatures retrouvées à leur place le lendemain ; travail concentré, vision périphérique. |
| Fin | 2:30–2:50 | Toutes calmes. Nom, slogan, « Built for Meta Quest 3/3S and Meta VR Glasses ». |

- Image : lumière naturelle, bureau rangé, cadrage stable, au moins 1080p.
- Texte à l'écran : Lato, encre cacao sur carte crème, comme dans l'app.
- Musique : douce, CC0 ou libre de droits commerciaux, consignée dans `CREDITS.md`.

## 10. Textes de soumission (anglais)

- Ton : chaleureux, concret, un brin d'humour. Pas de jargon (« synergy », « AI-powered »…).
- Toujours *Mail*, *Calendar*, *Tasks*, *Messages* ; jamais Gmail, Outlook, Slack.
- Phrase d'accroche : *"Meet your Fretlings."*
- Slogan actuel (≤ 140 caractères) : *"Your inbox, your meetings, your to-dos — living on your desk as tiny creatures that panic so you don't have to."*
