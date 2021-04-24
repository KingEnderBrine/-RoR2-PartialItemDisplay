# Description
Allows you to hide some item displays based on a character.

#### Warning
Be very careful with disabling equipment displays, because doing so might break it in some cases (for example `Milky Chrysalis`, `The Crowdfunder`).

# InLobbyConfig
Sections are named by a character they are corresponding to.
Each section can be disabled.
The `Default` section is applied if it is enabled and a section for a character is disabled.
If you want to disable all item displays for all characters, then enable only `Default` section, select list types equal to `Whitelist`, and don't add any items/equipment to lists.
![](https://cdn.discordapp.com/attachments/706089456855154778/795635695725051924/unknown.png)

# Changelog
**1.1.2**

* Fixed an issue where the mod will fail if a character name starts from space

**1.1.1**

* Fixed an issue where only `Default` section was available.

**1.1.0**

* Removed r2api dependency

**1.0.3**

* Fixed an issue when some symbols in character names causing errors.

**1.0.2**

* Added several null checks for more stability.

**1.0.1**

* Added `InLobbyConfig` dependency

**1.0.0**

* Mod release.