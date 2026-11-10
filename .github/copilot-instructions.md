# GitHub Copilot Instructions for Terran's Security Buildings (Continued)

## Mod Overview and Purpose
Terran's Security Buildings (Continued) is a RimWorld mod that adds three types of security buildings from StarCraft II into the game. The mod introduces a new resource called Chem-Gas, which is produced in the Biofuel Refinery using Uranium and Chemfuel. It also adds three new buildings: Auto-turret, Bunker, and Planetary Fortress, each with unique features and functionalities.

## Key Features and Systems
### New Resource: Chem-Gas
- **Description:** A new resource similar to Vespene Gas from StarCraft II.
- **Production:** Can be produced in the Biofuel Refinery using Uranium and Chemfuel.
- **Usage:** Required for the operation of the Auto-turret, Planetary Fortress, and other related buildings.

### New Buildings:
1. **Auto-turret**
   - **Description:** An airborne automatic turret for fixed-point defense.
   - **Requirements:** Chem-Gas to operate.
   - **Power:** Provides 160w power.
   - **Damage:** Explodes upon receiving serious damage.
   - **Additional Features:** Can set forced targets manually.

2. **Bunker**
   - **Description:** Fortifications that can accommodate four combatants with full-angle firing holes.
   - **Requirements:** Source code adapted from another mod.
   - **Upgrade:** Can be upgraded (WIP).
   - **Issue:** Known incompatibility with certain other mods (Shield Generators by Frontier Developments and Alpha Animal) when used together with MVCF.

3. **Planetary Fortress**
   - **Description:** A strong defensive fortress equipped with the "Twin Ibiks Cannon."
   - **Requirements:** Chem-Gas to operate.
   - **Power:** Provides 2000w power and can be used as a 2000w battery.
   - **Buff:** Provides a mood buff.
   - **Damage:** Explodes upon receiving serious damage.
   - **Additional Features:** Can set forced targets manually.

## Coding Patterns and Conventions
- **Coding Language:** C# is used for implementing new buildings and features.
- **XML Integration:** XML files are used to define the properties and behaviors of the new buildings and resources.
- **Harmony Patching:** Harmony is used to patch existing RimWorld code to enable compatibility with other mods.

## XML Integration
The mod defines several XML files to add new buildings and resources. These files are located in the `1.7/Defs` directory and include:
- **AutoTurret_Building.xml:** Defines the Auto-turret building.
- **Bunker_Building.xml:** Defines the Bunker building.
- **ThoughtDefs.xml:** Defines new thoughts or effects.
- **AutoTurret_TurretTop.xml:** Defines the turret gun for the Auto-turret.
- **PlanetaryFortress_Building.xml:** Defines the Planetary Fortress building.

### Example XML Code
xml
<ThingDef Name="AutoTurret_Building" ParentName="Building_TurretGun" abstract="true">
  <modExtensions>
    <li Class="TerranSecurityBuildings.ModExtension_AutoTurret"/>
  </modExtensions>
  <defName>AutoTurret_Building</defName>
  <label>Auto-turret</label>
  <description>An airborne automatic turret for fixed-point defense.</description>
  <thingCategories>
    <li>Building</li>
  </thingCategories>
</ThingDef>


## Harmony Patching
Harmony is used to patch existing RimWorld code to enable compatibility with other mods. The mod includes two patches for Harmony:
- **Patch 1:** Ensures compatibility with certain other mods.
- **Patch 2:** Fixes known issues.

### Example Harmony Patch Code
csharp
using HarmonyLib;
using RimWorld;

namespace TerranSecurityBuildings
{
    [HarmonyPatch(typeof(SomeExistingClass), "SomeExistingMethod")]
    public static class HarmonyPatch_FixCompatibility
    {
        public static void Postfix(ref SomeExistingClass __instance, ref SomeExistingResult __result)
        {
            // Patch code here
        }
    }
}


## Suggestions for Copilot
- **XML Generation:** Copilot can assist in generating XML files for new buildings and resources based on existing examples.
- **Harmony Patches:** Copilot can help in creating Harmony patches to fix compatibility issues with other mods.
- **C# Class Implementations:** Copilot can assist in implementing new C# classes for buildings and features, ensuring they follow the coding conventions and integrate with existing RimWorld systems.
- **Debugging and Testing:** Copilot can provide code snippets for debugging and testing, helping to identify and fix issues related to building functionalities.

## Additional Notes
- **Mod Dependencies:** Ensure that Harmony is installed and enabled for the mod to function correctly.
- **Compatibility Issues:** Refer to the mod's documentation for known compatibility issues and workarounds.
- **Reporting Errors:** Use the Discord channel for error reporting and provide logs for better support.

By following these guidelines, you can effectively utilize GitHub Copilot to enhance and maintain the Terran's Security Buildings (Continued) mod for RimWorld.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).
- When mentioning SonarQube issues, automatically use the SonarQube MCP service to fetch and address issues instead of making inferred fixes without querying SonarQube first.
- When mentioning the rimworld log, automatically use the Rimworld MCP service to fetch the log.

