# PlayerVoiceVolume

![LightShaper](https://raw.githubusercontent.com/L1GHTSHAPER/PlayerVoiceVolume/main/tools/assets/lightshaper-wordmark.png)

[Source code on GitHub](https://github.com/L1GHTSHAPER/PlayerVoiceVolume) | [Report an issue](https://github.com/L1GHTSHAPER/PlayerVoiceVolume/issues) | [Thunderstore](https://thunderstore.io/c/on-together/p/LightShaper/PlayerVoiceVolume/)

A BepInEx mod for [On Together](https://store.steampowered.com/app/2688490/On_Together/) that lets you set the **voice chat volume of each player separately**. One friend is too quiet, another one shouts? Turn them up or down, just for you. The mod remembers the volume for every player, so it is already set the next time you meet them.

**♥ Enjoying the mod? Leave a like on [Thunderstore](https://thunderstore.io/c/on-together/p/LightShaper/PlayerVoiceVolume/) and a ⭐ on [GitHub](https://github.com/L1GHTSHAPER/PlayerVoiceVolume) — it helps the project grow!**

## Settings menu

The microphone side button opens/closes default volume and volume-limit settings. Per-player controls remain in **Tab → Voice**, with the same saved values, mute and reset actions.

Cream panels, warm brown text, coral accents, rounded controls and game fonts. Side buttons form one group, show the mod name and the settings hotkey (where available), and move away from visible UI panels. If both edges are blocked, the buttons wait until space becomes available. Existing hotkeys, commands and configuration keys are preserved. Menus scroll on smaller screens; changes save automatically. Where shown, **Apply** saves a field draft. Invalid values keep the saved setting.

## Features

- A **Voice** tab in the player list (open it with **Tab**), next to the game's *Server* and *Banned* tabs.
- One row per player in the lobby: a slider from **%0** (silent) to **%200** (configurable up to %400), the value, and a button that resets the player to the default.
- The mic icon works like the one in the *Server* tab: it lights up while the player talks, so you can tell who is who, and clicking it toggles the game's own voice mute.
- Volumes are saved **per player** (by Steam ID) and applied automatically whenever they join, rejoin or reconnect to voice chat.
- Only changes what **you** hear: nothing is sent to other players, and their own settings are not affected.
- Works together with the game's Voice Volume setting, which still scales everyone.

## How the volume scale works

The game's voice chat (Vivox) adjusts each player in decibels. The slider is mapped so that the percentage follows perceived loudness: **every doubling is +10 dB**.

| Slider | Change | Sounds like |
|---|---|---|
| %0 | silenced | — |
| %25 | −20 dB | a quarter as loud |
| %50 | −10 dB | about half as loud |
| %100 | 0 dB | unchanged (default) |
| %200 | +10 dB | about twice as loud |
| %400 | +20 dB | about four times as loud |

Boosting a player who is already loud can make their voice distort; above %200 is mostly useful for very quiet microphones.

## Configuration

`BepInEx/config/ontogether.playervoicevolume.cfg` (created on first launch; editable from the mod manager's Config editor).

| Section | Key | Default | Description |
|---|---|---|---|
| General | `MaxVolumePercent` | `200` | Right end of the slider, 100–400. |
| General | `DefaultVolumePercent` | `100` | Volume of players you have not adjusted; the reset button returns a player to it. |

The saved volumes are in `BepInEx/config/ontogether.playervoicevolume.volumes.json`, one entry per player with their last seen nickname. You can edit or delete it while the game is closed.

## Installation

**Thunderstore Mod Manager / r2modman:** install from the mod list, or use *Settings -> Import local mod* with the package zip.

**Manual:** install [BepInExPack](https://thunderstore.io/c/on-together/p/BepInEx/BepInExPack/) and copy `PlayerVoiceVolume.dll` into `BepInEx/plugins/`.

---

## Русский

**♥ Нравится мод? Поставьте лайк на [Thunderstore](https://thunderstore.io/c/on-together/p/LightShaper/PlayerVoiceVolume/) и ⭐ звезду на [GitHub](https://github.com/L1GHTSHAPER/PlayerVoiceVolume) — это помогает проекту расти!**

Боковая кнопка микрофона открывает и закрывает настройки громкости по умолчанию и её предела. Управление каждым игроком остаётся в **Tab → Голос**; сохранённые значения, отключение звука и сброс работают как раньше.

Кремовые панели, коричневый текст, коралловые акценты и скруглённые элементы. Боковые кнопки собраны в одну группу; при наведении видны название мода и клавиша настроек, если она есть. Группа избегает видимых игровых панелей; когда места нет, кнопки скрываются до освобождения края. Настройки и прежние клавиши сохранены. Низкие окна прокручиваются, изменения сохраняются автоматически; кнопка «Применить», где она есть, сохраняет введённое значение.

Мод позволяет настроить **громкость голосового чата для каждого игрока отдельно** — только для вас, другие игроки ничего не заметят.

- В списке игроков (**Tab**) появляется вкладка **«Голос»** рядом с «Сервер» и «Блокировка».
- Для каждого игрока: ползунок от **%0** (тишина) до **%200** (максимум настраивается до %400), значение и кнопка сброса к громкости по умолчанию.
- Иконка микрофона подсвечивается, когда игрок говорит, а по клику включает/выключает игровое заглушение голоса — так же, как на вкладке «Сервер».
- Громкость запоминается для каждого игрока (по Steam ID) и применяется автоматически при каждом входе в лобби и переподключении к голосовому чату.
- Шкала: каждое удвоение процентов — это +10 дБ (примерно вдвое громче на слух): %50 — вдвое тише, %200 — вдвое громче.
- Настройки — `BepInEx/config/ontogether.playervoicevolume.cfg`, сохранённые громкости — `BepInEx/config/ontogether.playervoicevolume.volumes.json`.


Side settings buttons are opaque squares with rounded corners, a dark brown outline and proportionate icons, sized to match the game's right-hand controls. They hide with the native controls in Desktop mode, including tooltips and pointer hit areas.

Боковые кнопки настроек стали непрозрачными и квадратными: скруглённые углы, коричневая обводка и значки без растягивания. Размер соответствует высоте игровых кнопок справа. В Desktop-режиме они скрываются вместе с игровыми; подсказки и области нажатия также отключаются.
