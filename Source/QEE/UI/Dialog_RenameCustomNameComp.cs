﻿using Verse;

namespace QEthics;

public class Dialog_RenameCustomNameComp : Dialog_Rename
{
    public CustomNameComp nameComp;

    public override void SetName(string name)
    {
        nameComp.customName = name;
    }
}