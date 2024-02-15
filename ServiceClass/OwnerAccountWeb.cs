
using MetaverseMax.Database;

namespace MetaverseMax.ServiceClass
{
    public class OwnerAccountWeb : OwnerAccount
    {

        // attributes stored/retrieved from OwnerUni table
        public decimal balance { get; set; }
        public bool balance_visible { get; set; }
        public bool allow_link { get; set; }
    }
}
