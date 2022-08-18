using System.Collections.Generic;
using System.Xml;
using Verse;

namespace QEthics.Ideology;

public class PatchOperationdoIdeoFeatures : PatchOperation
{
    private PatchOperation lastFailedOperation;
    private List<PatchOperation> operations;

    protected override bool ApplyWorker(XmlDocument xml)
    {
        if (!QEESettings.instance.doIdeologyFeatures)
        {
            return true;
        }

        foreach (var operation in operations)
        {
            if (operation.Apply(xml))
            {
                continue;
            }

            lastFailedOperation = operation;
            return false;
        }

        return true;
    }

    public override void Complete(string modIdentifier)
    {
        base.Complete(modIdentifier);
        lastFailedOperation = null;
    }

    public override string ToString()
    {
        var num = operations?.Count ?? 0;
        var text = $"{base.ToString()}(count={num}";
        if (lastFailedOperation != null)
        {
            text = $"{text}, lastFailedOperation={lastFailedOperation}";
        }

        return $"{text})";
    }
}