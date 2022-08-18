using System.Collections.Generic;
using RimWorld;
using Verse;

namespace QEthics;

/// <summary>
///     Specially tailored BackgroundDef for the mod.
/// </summary>
public class BackstoryDef : Def
{
    public string baseDesc = null;

    public string bodyTypeFemale = null; //Female
    public string bodyTypeGlobal = null;
    public string bodyTypeMale = null; //Male

    [Unsaved] public string identifier = null;

    public BackstorySlot slot = BackstorySlot.Childhood;
    public List<string> spawnCategories = new List<string>();
    public string title = "";
    public string titleFemale = "";
    public string titleShort = "";
    public string titleShortFemale = "";

    public Backstory GetFromDatabase()
    {
        BackstoryDatabase.TryGetWithIdentifier(identifier, out var bs, false);

        return bs;
    }
}