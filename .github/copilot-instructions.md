# GitHub Copilot Instructions for "Questionable Ethics Enhanced (Continued)" Mod

## Mod Overview and Purpose
"Questionable Ethics Enhanced (Continued)" (QEE) is a RimWorld mod designed to add advanced cloning and pawn manipulation features. Originally developed by KongMD and updated by Mario55770 for the 1.3 version, this mod allows players to grow organs, clone animals and humanoids, preserve skill levels through brain scanning, and manipulate pawns using ideological nerve stapling. QEE aims to enhance gameplay with new mechanics and customization, fitting seamlessly within RimWorld's style.

## Key Features and Systems
- **Organ Growth Vats:** Allows players to grow organs in vats for medical or experimental purposes.
- **Cloning Mechanic:** Clones traits and passions from the parent to new humanoid or animal clones.
- **Brain Scanning:** Enables skill preservation by scanning brains and applying templates to clones.
- **Nerve Stapling:** Offers instant recruitment and manipulation of hostile pawns.
- **Customizable Balancing:** Through the Mod Settings menu, gameplay aspects can be finely tuned by the player.
- **Compatibility Enhancements:** Adds integrated support for mods like Facial Animation, Biotech genes, and Vanilla Nutrient Paste Expanded, among others.

## Coding Patterns and Conventions
- **C# Class Organization:** JobDriver classes manage various tasks related to cloning and organ management.
- **WorkGiver Classes:** Provide customized job management by heavily modifying RimWorld's base WorkGiver classes due to encapsulated private members.
- **Conventions:** Follow C# naming conventions by using PascalCase for class and method names and camelCase for variables. Deprecated classes are clearly marked with inline comments.

## XML Integration
- All mod definition files, such as ThingDefs, Hediffs, and Recipes, rely on XML for configuration, ensuring the modularity of content additions.
- XML links critical game entities like items and recipes to def types such as `DefModExtension`, which allows for further customization and enhances compatibility.

## Harmony Patching
- **Purpose and Usage:** Harmony is used to patch methods of RimWorld's core and other mods, enabling behaviors like custom error messages for incompatible mods and augmenting existing game mechanics.
- **Method Patching:** Utilize Harmony patches for methods like `AddBill_Patch` and `GetWorkgiver_Patch`, maintaining compatibility and enhancing functionality.

## Suggestions for Copilot
- **Deprecation Warnings:** Copilot should suggest substitution or avoidance for deprecated classes marked in the comments.
- **Harmony Setup:** Ensure Copilot assists in setting up Harmony patches properly by identifying potential patch methods and target classes.
- **XML Matching:** Recommend generating XML nodes consistent with existing `Def` and `DefModExtension` structures.
- **Cross-Class Usage:** Since many classes share common interfaces or abstract base classes (`IThingHolder`, `IMaintainableGrower`), suggest patterns that ensure correct implementation of interface methods.
- **Compatibility Classes:** Auto-suggest compatibility checks, especially in `CompatibilityTracker` and other related classes, to ensure smooth integration with third-party mods.

> If further assistance is required in terms of mod development or specific coding implementations, the community and the mod's GitHub repository are active platforms for discussion and support.
