using RimWorld;
using UnityEngine;
using Verse;

namespace QEthics;

public sealed class Dialog_RenameCustomNameComp : Window
{
    private string curName;

    private bool focusedRenameField;
    public CustomNameComp nameComp;

    private int startAcceptingInputAtFrame;

    public Dialog_RenameCustomNameComp()
    {
        forcePause = true;
        doCloseX = true;
        absorbInputAroundWindow = true;
        closeOnAccept = false;
        closeOnClickedOutside = true;
    }

    private bool AcceptsInput => startAcceptingInputAtFrame <= Time.frameCount;

    private int MaxNameLength => 28;

    public override Vector2 InitialSize => new Vector2(280f, 175f);

    public void WasOpenedByHotkey()
    {
        startAcceptingInputAtFrame = Time.frameCount + 1;
    }

    private AcceptanceReport NameIsValid(string name)
    {
        return name.Length != 0;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Small;
        var keyPressed = false;
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            keyPressed = true;
            Event.current.Use();
        }

        GUI.SetNextControlName("RenameField");
        var text = Widgets.TextField(new Rect(0f, 15f, inRect.width, 35f), curName);
        switch (AcceptsInput)
        {
            case true when text.Length < MaxNameLength:
                curName = text;
                break;
            case false:
                ((TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl)).SelectAll();
                break;
        }

        if (!focusedRenameField)
        {
            UI.FocusControl("RenameField", this);
            focusedRenameField = true;
        }

        if (!(Widgets.ButtonText(new Rect(15f, inRect.height - 35f - 15f, inRect.width - 15f - 15f, 35f), "OK") ||
              keyPressed))
        {
            return;
        }

        var acceptanceReport = NameIsValid(curName);
        if (!acceptanceReport.Accepted)
        {
            if (acceptanceReport.Reason.NullOrEmpty())
            {
                Messages.Message("NameIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
            }
            else
            {
                Messages.Message(acceptanceReport.Reason, MessageTypeDefOf.RejectInput, false);
            }
        }
        else
        {
            SetName(curName);
            Find.WindowStack.TryRemove(this);
        }
    }

    public void SetName(string name)
    {
        nameComp.customName = name;
    }
}