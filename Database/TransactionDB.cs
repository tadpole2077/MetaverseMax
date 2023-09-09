namespace MetaverseMax.Database
{
    public class TransactionDB : DatabaseBase
    {
        public TransactionDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
            worldType = _parentContext.worldTypeSelected;
        }    
    }
}
