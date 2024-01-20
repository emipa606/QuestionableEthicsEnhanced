# QuestionableEthicsEnhanced

![Image](https://i.imgur.com/buuPQel.png)

Update of KongMDs mod
https://steamcommunity.com/sharedfiles/filedetails/?id=1787359789
Based on the 1.3 update by Mario55770
https://steamcommunity.com/sharedfiles/filedetails/?id=2566962342

- Adopted the mod on request by Mario55770
- Added "filling" graphics for the vats, makes it easier to see how much is missing, can be disabled
- Added support for cloning all features added by https://steamcommunity.com/workshop/filedetails/?id=1635901197]Facial Animation
- Added support for Biotech genes and xenotypes
- Added support for https://steamcommunity.com/sharedfiles/filedetails/?id=2830943477]Animal Genetics

## Additions by Mario55770

Choose your ethics, ideology support integrated. Precepts are implemented at least on a first pass basis. Nerve stapling now also can automatically knock out pawns with more violent thoughts. Nerve staple's enslave and max suppression. Now also allows brain scanning to copy pawn ideology.

![Image](https://i.imgur.com/pufA0kM.png)

	
![Image](https://i.imgur.com/Z4GOv8H.png)

Questionable Ethics Enhanced (QEE) is a content mod that adds cloning and pawn manipulation features to Rimworld. See the Change Notes tab in Steam Workshop or the Github repository for info on the latest updates.

# Features




- **Grow organs** in vats!
- **Clone** animals and humanoids! Clones have the traits and passions of their "parent".
- **Scan brains** to preserve the skill levels of a pawn! Apply any brain template to your clones, to get them up-to-speed fast.
- **Recruit** hostile pawns instantly with nerve stapling!
- **Balance** the mod as you wish, with the Mod Settings menu (see preview image above)
- **Enhancements** to the original Questionable Ethics mod




![Image](https://i.imgur.com/Sr8GRm1.jpg])

![Image](https://i.imgur.com/7HhYJby.jpg])


# Changes to Questionable Ethics mechanics


Questionable Ethics Enhanced is a successor to the Questionable Ethics mod created by ChJees. At release, there are 14  bugfixes 15 new enhancements, and a few new compatibility patches that were not in QE. https://github.com/KongMD-Steam/QuestionableEthicsEnhanced/issues?page=1&amp;q=is%3Aissue+is%3Aclosed]Full list here. Here are the big changes:



-  The https://steamcommunity.com/sharedfiles/filedetails/?id=2213696082]Life Support System and https://steamcommunity.com/sharedfiles/filedetails/?id=1785162951]Crude Bionics have been moved to standalone mods, to improve mod compatibility.
- All descriptions have been re-written with an emphasis on **discoverability** (how do I use this item and where do I obtain it) and **correct grammar**.
- **Failed cloning no longer causes Cloning Vat ingredient loading to break**. This was a high-impact bug in QE and many people asked for a fix.
- Nutrient Solution and Protein Mash recipes, output, and costs have been rebalanced across the board. See mod preview image for details.
- Cloning vats now display how many days remain until clone is fully grown, instead of a percentage of completion.
- The Apply Brain Templating toils have been re-written and many bugs have been fixed. Get https://github.com/KongMD-Steam/QuestionableEthicsEnhanced/blob/master/Languages/English/Keyed/QuestionableEthics.xml]feedback messages during pawn selection, if it's an invalid target.
- Family relationships will no longer be generated for clones



# Compatibility

This mod patches in changes to vanilla organ defs, but does not remove anything from the vanilla game. All defNames in the mod are unique and should not conflict with other mods. Any mods determined to be incompatible will throw a custom error message upon loading the game, when QEE and that mod are both enabled. **Consider this mod compatible with your modlist, unless specified below:**

**Enhanced Functionality**


- Bionic Icons - Textures from this mod will be used for the organs. Highly recommended upgrade over vanilla textures!
- Evolved Organs - Has exciting changes to its mechanics when QEE is enabled. There are too many to list, but https://github.com/Xahkarias/Evolved-Organs/blob/master/About/Patch%20Notes.txt]go here for more details.
- Evolved Organs Redux - Has exciting changes to its mechanics when QEE is enabled. See https://github.com/Xahkarias/EvolvedOrgansRedux/wiki/Compatibility#high-synergy]this link for more details.
- Race mods built with Humanoid Alien Races 2.0 - Races will be able to to be cloned in the Cloning Vat.
- Rah's Bionics and Surgery Expansion (RBSE)


- Organ installation requires RBSE research pre-req 'Organ Transplantation'
- Organs need to be refrigerated
- Organ Rejection hediff added when organs are implanted, if the RBSE ModSetting for this is enabled.





**Fully Compatible**


- Android Tiers - TX Series
- Bioreactor
- Combat Extended - no patch required
- EPOE - Use the 'EPOE - Forked' version posted by Aurani, Victorique, and Tarojun, for best compatibility
- Harvest Organs Post-Mortem (HOPM) - All natural organs from QEE have a chance to spawn after autopsy
- LOTR races from Lord of the Rims by Jecrell (and the LOTR Continued mods by Mile)
- Pawnmorpher



**Partially Incompatible**


- [Beta] METRO 2033 - Mutants - Custom AI code that will cause cloned animals to attack colonists or colony buildings
- Harvest Everything - A lot of duplicate functionality
- Hospitality - A Postfix from Hospitality can cause performance drain while organ and pawn vats are active/loading. The impact of this drain will be more noticeable with large modlists. To minimize the perf. drain, remove Bills from organ vats when done. The fix for this is non-trivial, but it's on the to-do list.
- Multiplayer - The multiplayer mode of the game will not work properly, but single player should work fine.
- Vanilla Factions Expanded - Insectoids - Insects are technically cloneable, but the mod has custom AI code that will cause cloned insectoids to always go Manhunter after cloning.
- Warforged - Warforged pawns can't be cloned (they are robots that have no meat). See the GitHub issue for more details.



**Incompatible**


- Questionable Ethics



If you run into errors when adding this mod to your modlist, please let me know in the comments. This mod includes a Debug Mode toggle in the Mod Settings, which I would recommend setting to Enabled, if you have problems.

![Image](https://i.imgur.com/PwoNOj4.png)



-  See if the the error persists if you just have this mod and its requirements active.
-  If not, try adding your other mods until it happens again.
-  Post your error-log using https://steamcommunity.com/workshop/filedetails/?id=818773962]HugsLib or the standalone https://steamcommunity.com/sharedfiles/filedetails/?id=2873415404]Uploader and command Ctrl+F12
-  For best support, please use the Discord-channel for error-reporting.
-  Do not report errors by making a discussion-thread, I get no notification of that.
-  If you have the solution for a problem, please post it to the GitHub repository.
-  Use https://github.com/RimSort/RimSort/releases/latest]RimSort to sort your mods



https://steamcommunity.com/sharedfiles/filedetails/changelog/2850854272]![Image](https://img.shields.io/github/v/release/emipa606/QuestionableEthicsEnhanced?label=latest%20version&style=plastic&color=9f1111&labelColor=black)

