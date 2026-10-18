# Créatures de bureau – fichiers 3D

## Contenu
- `creatures_bureau.blend` : la scène Blender complète. Collection **Famille** (les 4 créatures) et collection **Etats_Facteur** (le facteur dans ses 4 états).
- `Facteur.glb`, `Reunions.glb`, `Messages.glb`, `Taches.glb` : une créature par fichier, prête pour Unity, à taille réelle (environ 7 cm de haut).
- `build_creatures.py` : le script qui génère tout. Modifie les couleurs, formes ou tailles, puis relance-le dans Blender (onglet Scripting > Run Script).
- `rendu_etats_facteur.png` et `rendu_famille.png` : les aperçus.

## Comment c'est construit
Chaque créature est un objet racine qui contient des parties séparées. **Toutes les variantes sont incluses** dans le .glb : c'est le script Unity qui active et désactive les bonnes parties selon le niveau de stress.

| Partie | Rôle |
|---|---|
| `Body` | Le corps. Sa couleur change avec le stress |
| `EyeClosed_L/R` | Yeux fermés, état calme |
| `EyeOpen_L/R` + `EyeShine_L/R` | Yeux ouverts |
| `EyeWide_L/R` + `EyePupil_L/R` | Yeux écarquillés, panique |
| `Brow_L/R` | Sourcils froncés |
| `Cheek_L/R` | Joues, état calme |
| `MouthSmile` / `MouthFlat` / `MouthWorried` / `MouthOpen` | Les 4 bouches |
| `Sweat` | Goutte de sueur |
| `FlameMain/Left/Right/Core` (Facteur) | Flammes, mises à l'échelle selon le stress |
| `PostitPink/Blue` (Tâches) | Post-it supplémentaires qui s'empilent |

## Réglages par état (à reproduire dans Unity)

| État | Stress | Couleur du corps | Échelle | Parties visibles | Flammes (Main / côtés / Core) |
|---|---|---|---|---|---|
| Calme | 0 | `#EFE6D8` | 1,00 | EyeClosed, Cheek, MouthSmile | cachées |
| Attentif | 0,3 | `#F2D3AE` | 1,10 | EyeOpen, EyeShine, MouthFlat | 0,55 / – / – |
| Stressé | 0,6 | `#F1A661` | 1,25 | EyeOpen, Brow, MouthWorried, Sweat | 0,85 / 0,7 / 0,6 |
| Panique | 1 | `#E2553A` | 1,40 | EyeWide, EyePupil, Brow, MouthOpen | 1,3 / 1,15 / 1,1 |

Entre deux états, interpole la couleur, l'échelle et la taille des flammes de façon progressive. La respiration, les sautillements et le tremblement se font par code dans Unity.

## Import dans Unity
1. Installe le paquet **glTFast** (com.unity.cloud.gltfast) via le Package Manager.
2. Glisse les fichiers `.glb` dans ton dossier Assets.
3. Au démarrage, ton script désactive les parties qui ne correspondent pas à l'état calme.
4. Pour les flammes, remplace le matériau par un shader émissif ou des particules : l'aspect sera plus vivant.
