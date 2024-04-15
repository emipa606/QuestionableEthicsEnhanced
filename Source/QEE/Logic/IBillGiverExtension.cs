using RimWorld;

namespace QEthics;

public interface IBillGiverExtension
{
    void Notify_BillAdded(Bill aBill);
    void Notify_BillDeleted(Bill aBill);
}