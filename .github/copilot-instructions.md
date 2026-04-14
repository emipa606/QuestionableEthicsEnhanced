# GitHub Copilot Instructions for Questionable Ethics Enhanced (Continued) Mod

Welcome to the Questionable Ethics Enhanced (Continued) mod project for RimWorld! This guide provides an overview of the project's structure and helps guide contributors on how to use GitHub Copilot effectively during development.

## Mod Overview and Purpose

The "Questionable Ethics Enhanced (Continued)" (QEE) mod provides advanced features and systems for cloning, organ growth, and pawn manipulation in RimWorld. It enhances the original Questionable Ethics mod by adding various new mechanics, improving compatibility, and fixing existing bugs.

## Key Features and Systems

- **Cloning Systems:** Clone animals and humanoids, preserving traits and passions. Display progress with graphical indicators.
- **Organ Growth:** Allows players to grow organs in vats with detailed interaction descriptions.
- **Brain Scanning and Templates:** Capture and apply brain templates to cloned pawns.
- **Nerve Stapling:** Mechanism to instantly recruit hostile pawns or suppress violent thoughts.
- **Ideological Integration:** Supports choosing ethics and ideological precepts, with compatibility for various mods including Facial Animation and Biotech genes.

## Coding Patterns and Conventions

- Follow C# conventions while working with classes, methods, and namespaces.
- Each feature or component is generally encapsulated in a class with a clear role.
- Classes that are marked as deprecated should not be used in new implementations.
- Public methods generally expose essential functionality, while private methods handle internal logic.

## XML Integration

- The mod uses XML files to define game data like recipes, traits, and items.
- Always ensure XML definitions are unique to avoid conflicts with other mods.
- Use `DefModExtension` to extend existing game definitions with custom data.

## Harmony Patching

- Harmony allows runtime method patching to modify or extend base game functionality.
- Utilize the `HarmonyPatches` class for implementing such patches.
- Patches should be specific and isolated to avoid unexpected behavior.
- Ensure compatibility by checking existing patches or potential method replacement issues.

## Suggestions for Copilot

1. **Cloning and Vat Systems:** Use Copilot to generate code for handling clone progress visuals or ingredient management within vats.
2. **Compatibility Layer:** Generate code snippets for compatibility checks with popular mods and provide friendly error messages.
3. **Bug Fixes and Improvements:** Leverage Copilot to analyze or refactor existing methods, particularly those prone to failure like genome salvaging or brain templating.
4. **User Interface Helper Methods:** Implement utility functions for enhancing user interface interactions and feedback.
5. **Debugging and Logging:** Utilize Copilot to streamline debugging code blocks or conditional logging statements for troubleshooting.

By following these guidelines and using Copilot intelligently, you can significantly enhance your development process for the Questionable Ethics Enhanced mod. Happy modding!

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.
