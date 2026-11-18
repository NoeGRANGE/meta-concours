# Fretlings — textes de la soumission Devpost

Prêts à copier-coller, en anglais. Les éléments entre `[crochets]` sont à compléter par toi. Aucun nom de marque tierce dans les textes (règlement, section 4).

---

## Elevator pitch (165 / 200 caractères)

Your inbox, meetings and to-dos live on your real desk as tiny mixed-reality creatures that panic so you don't have to. Look, pinch, and watch them sigh with relief.

---

## About the project (Markdown)

```markdown
## Inspiration

Notifications are built to interrupt. Every badge and every ping asks for your attention *right now*, even when it could wait an hour. We wanted the opposite: information you can feel from the corner of your eye, without being pulled out of deep work.

Mixed reality made it possible to put that information **where you already work: on your real desk**. Instead of a panel of numbers, we imagined a small family of creatures, a bit like designer toys, that calmly sit next to your keyboard and *worry for you*. When something needs you, they show it. When you take care of it, they sigh with relief.

## What it does

Fretlings puts five tiny creatures on your desk. Each one watches one part of your work day:

- **Mail** heats up as important emails pile up, until small flames rise behind its cap.
- **Calendar** gets nervous as your next meeting gets closer.
- **Tasks** collects sticky notes when things go overdue.
- **Messages** starts chattering when someone is waiting for an answer.
- **Agents** wears a hard hat with a head lamp and a row of beads, one per AI coding session: grey running, blue done, orange waiting for you, red failed.

Every creature has a single stress value that drives everything about it: its face, its size (up to 1.4×), its colour (from cream to orange to red), its motion (breathing, hopping, trembling) and its effects (sweat, flames). Urgency is never told by colour alone, so it stays readable for colour-blind users.

You interact with your hands only:

- **Look** at a creature: it gets a soft outline and a bubble such as "3 important emails · oldest 2 h".
- **Pinch** while looking at it: a small panel opens with its three most urgent items.
- **Pinch a line** to mark it read or done. When everything is handled, the creature **deflates, sighs and bounces back to calm**. That sigh is the reward.
- **Grab** a creature to move it; let go and it drops back onto the desk with a little bounce.

The first minute teaches itself without a wall of text: lay your hand flat on your desk, watch a ripple run across it, meet the Mail creature, look, pinch, clear its inbox, and the rest of the family drops in. The next day, they are exactly where you left them.

Fretlings has two data modes. **Demo mode**, used in the submitted build, plays a realistic scripted day (invented names, no real accounts), so anyone can try the whole experience in three minutes with no sign-in. **Live mode** connects Mail, Calendar, Tasks and Agents to your real accounts and coding sessions through a small companion on your computer, which signs in through your browser and receives your coding agents' hooks; switching modes is a single setting.

## How we built it

- **Unity 6 (URP)** with the **Meta XR Core SDK, Interaction SDK and MR Utility Kit** (v207), OpenXR, for Meta Quest 3 / 3S and Meta VR Glasses.
- **Hand-based desk calibration.** We could not rely on a room scan: many users never label their desk, and the glasses may not have one. So the app asks you to lay your hand flat on the desk, detects a flat, still hand from the joint heights, and builds the desk plane from it. When the room scan has a table at the same height, we snap to it.
- **Spatial anchors.** Each desk gets a persistent anchor; the creatures are children of it, so they come back exactly where you left them, across sessions and even across several desks (home, office).
- **Gaze and pinch.** Quest 3 and 3S have no eye tracking, so we target with the head direction, with a precise ray and a wider fallback that picks the creature nearest the line of sight. A pinch from either hand selects.
- **Procedural animation.** No keyframes: squash and stretch, breathing, hops, trembling and the relief sigh are all driven from the smoothed stress value, with hysteresis so faces never flicker between states.
- **One data interface, two modes.** The creatures read from a common data-source interface, implemented by the demo player and by the Live sources. In Live mode the headset finds the desktop companion on the local network, pairs with a code, and reads unread mail, upcoming meetings, open tasks and coding-agent sessions; marking an item read or done goes back to the real account. Only a session's name, state and time ever leave the coding agents' hooks.
- **Original assets only.** The creatures are modelled by a Blender Python script; every sound effect and the video's music are synthesized by our own code. No third-party art or audio.
- **Tested headless.** 74 automated EditMode and PlayMode tests cover the stress formulas, the demo scenario, the desk calibration and memory, onboarding, and the look-and-pinch flow.

Performance on a Quest 3S with the five creatures and the demo running: **a steady 72 fps**, GPU around 25 %, CPU around 17 %, no stale frames.

## Challenges we ran into

- **Real hands are not test hands.** On the headset, a hand lying flat measured 2.7–2.8 cm between its lowest and highest joints, just over our first tolerance, and the interaction rig exposes several "hands" per side, one of which sat frozen near the eyes. We logged what the app saw from the hands during calibration, then fixed both.
- **Anchors that silently did not persist.** Our desk was "anchored" a millisecond after calibration, which is impossible for a real spatial anchor: the headset was not yet reported as present when the session started, so the app fell back to in-memory anchors. Timestamps in the logs gave it away.
- **Head gaze is not eye gaze.** People look at a small creature with their eyes, not their head. A wider fallback ray and an outline that confirms the target made selection feel natural.
- **Teaching without text.** The onboarding had to adapt to what the user does: clear the inbox early, wait, or explore. Every path ends with the family arriving.

## Accomplishments that we're proud of

- A concept that only makes sense in mixed reality: your real desk, your real work, and creatures that live next to it.
- A first minute that explains itself with one gesture at a time.
- Desk memory that just works: put the headset back on and they are there.

## What we learned

Ambient design is mostly restraint: almost nothing moves when all is well, strong motion is reserved for real urgency, and sounds stay short and soft. And on hardware, measure before you guess: the logs told us more than any assumption.

## What's next for Fretlings

- **Live mode for everyone**: sign-in straight from the headset, more mail, calendar and task providers, and chat for the Messages creature.
- **More coding agents** reporting to the Agents creature, and a one-click hook setup in the companion.
- **Snooze** a creature by putting it to sleep on the edge of the desk, a **daily recap** and a streak for the end-of-day ritual.
- **Eye-based gaze on Meta VR Glasses**, and a design tuned for their field of view.
```

---

## Built with (tags)

unity, c#, meta-xr-sdk, meta-interaction-sdk, mr-utility-kit, openxr, universal-render-pipeline, hand-tracking, passthrough, spatial-anchors, scene-understanding, meta-quest, blender, python, numpy, gltf, ffmpeg

---

## "Try it out" links

- [Lien d'invitation du canal « Competition » du Meta Developer Dashboard] (à créer)

---

## Project media (galerie, 3:2, jusqu'à 15 images)

Images dans `SourceArt/brand/devpost/gallery/` (1500 × 1000, JPG, toutes sous 250 Ko), régénérées par `SourceArt/brand/build_gallery.py`. Ordre conseillé, avec la description à coller (140 caractères au plus) :

| # | Image | Description |
|---|---|---|
| 1 | `01_desk_hero.jpg` | Fretlings on a real desk, seen through the headset: the Agents creature panics as a coding session waits, and a hand reaches in. |
| 2 | `02_hand_on_desk.jpg` | Setup takes one gesture: lay your hand flat on your desk. No controllers and no room scan needed. |
| 3 | `03_meet_mail.jpg` | Your first Fretling drops onto the desk. The Mail creature worries about your inbox so you don't have to. |
| 4 | `04_detail_panel.jpg` | Look at a creature, then pinch: a small panel lists its three most urgent items. Pinch a line to mark it read. |
| 5 | `05_family.jpg` | Meet the family: Mail, Calendar, Tasks, Messages and Agents, lined up next to your keyboard. |
| 6 | `06_states.jpg` | One stress value drives everything: face, colour, size and motion, from calm to panic. Never colour alone. |
| 7 | `07_agents_panel.jpg` | Agents watches AI coding sessions: one bead per session, its lamp glows while one waits for your reply. |
| 8 | `08_messages_waiting.jpg` | Messages starts chattering when people wait for an answer. The longer they wait, the more it worries. |
| 9 | `09_relief.jpg` | Handle the last message and it deflates, sighs and calms down. That sigh is the reward. |
| 10 | `10_calendar_panic.jpg` | Calendar turns red and trembles as your next meeting gets close, without a single pop-up. |
| 11 | `11_remembers_desk.jpg` | Persistent spatial anchors: relaunch the app and every creature is right where you left it. |
| 12 | `12_brand.jpg` | Fretlings: tiny creatures that panic so you don't have to. Hands-first, for Meta Quest 3, 3S and Meta VR Glasses. |

## Video

[Lien YouTube ou Vimeo, public ou non répertorié, sans mot de passe]

---

## Champs du formulaire

| Champ | Réponse |
|---|---|
| Submitter type | **Individual** [à confirmer] |
| Country of residence | [ton pays] |
| E-mail of the developer organization / Start membership | [ton e-mail du programme Start] |
| Team or organization name | [le nom à afficher, par exemple « Fretlings by … »] |
| Track | **Productivity** |
| Division | **New** |
| Build path | **Unity** |
| Release channel URL | [lien du canal « Competition »] |

### What Meta VR capability(ies) did you use in your project?

Hand tracking (Interaction SDK: hand grab, pinch), head-gaze targeting with pinch to select, color passthrough (mixed reality), scene understanding with MR Utility Kit (desk tables), persistent spatial anchors, and spatialized audio. Built with the Meta XR Core SDK, Interaction SDK and MR Utility Kit v207 on OpenXR.

### Describe how Hand Interactions are implemented into your experience.

Fretlings is fully hands-first; controllers are never needed.
- **Desk calibration with a flat hand**: during setup, the app reads the hand joints and waits for a hand laid flat and still on the desk (joints at the same height, within a few millimetres of drift), then builds the desk plane under it. One or two hands work.
- **Look and pinch**: the user looks at a creature (head gaze with a forgiving fallback ray, confirmed by a coloured outline and a peek bubble) and pinches thumb and index of either hand to open its panel; looking at a line and pinching marks it read or done. Pinching the creature again closes the panel.
- **Grab and drop**: creatures are grabbable with the Interaction SDK hand grab; when released they fall onto the desk with simulated gravity, squash on impact, bounce and stand back up facing the user. Their new place is saved in the desk's spatial anchor frame.
- Every interaction works seated, within arm's reach, in the central field of view.

### What makes you excited about the future of VR? (≤ 150 words)

We are excited about headsets becoming something you wear while you work, not something you put on to leave the world. Light glasses with colour passthrough turn your real desk into a canvas, and that changes what software can be: not another window competing for attention, but calm objects that live in your space and speak in the background. Hands and eyes make it feel natural: you look, you pinch, you move on. We think the most useful mixed-reality apps will be the quiet ones you keep running all day, and Fretlings is our first step in that direction: information you can feel without being interrupted.

### What is the target launch date for your app?

**Q2 2027**, alongside the Meta VR Glasses, with a first public build on Meta Quest 3 / 3S earlier in 2027.
