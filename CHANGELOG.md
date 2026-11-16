**English** | [日本語](CHANGELOG.ja.md)

# Changelog

This page lists what changed in each version of KMIDI Player. Downloads are on the [Releases](https://github.com/pritezza/KMIDI-Player/releases) page.

## 1.1.0 - 2026-11-16

### Added

- **Instrument names in the channel panel.** The name of the instrument each channel is playing is shown under CH, M and S. Turn it on with **View** → **Instrument Names** → **Show Instrument Names** (off by default).
  - Names come from instrument definition files in the format used by the Domino MIDI sequencer. A GM2 definition is included. To use another sound module's names (for example SC-88Pro or MU50), put its definition file in the `Modules` folder next to the settings file, and choose it from the same menu. **Open Definition Folder** opens that folder.
  - Bank select is taken into account. If a bank is not in the file, the main name for that program is shown.
  - Drum parts are detected from channel 10, the GS and XG part settings, and the bank (XG and GM2).
  - Hover over a name to see the program number, the bank, and the definition in use.
  - Drag the gap between the S column and the keyboard to make the name column wider. Double-click it to go back to the original width.
- **Master volume.** A slider and a mute button are at the right end of the top row. They change the volume of the sound module itself (Universal Master Volume), and the volume is sent again after each reset. Whether it has any effect depends on the sound module: some modules and software synths ignore this message. The Microsoft GS Wavetable Synth built into Windows responds to it.
- **UI scale.** **View** → **UI Scale** makes the whole interface 100% to 200% larger: text, buttons, lists, the channel panel and the piano roll.
- **About** now has a link to the author's page on GitHub.

### Changed

- The display language is English the first time you start KMIDI Player. If you already use KMIDI Player, your language setting stays as it is.
- **Theme**, **UI Scale** and **Language / 言語** are now in the **View** menu. The language menu is labeled in both English and Japanese, so you can find it whichever language is shown.
- The level meter settings are now part of **Settings** → **Preferences**, together with the CCs that are not sent to the sound module.
- Text is now shown in the Windows UI font (Segoe UI on English Windows, Yu Gothic UI on Japanese Windows) instead of the older default font. The layout keeps the same size.
- The **View** menu is grouped by purpose: windows and panels, what the channel panel shows, appearance, and window behavior. The channel panel options can only be chosen while the channel panel is shown.

## 1.0.0 - 2026-10-31

- First public release.
