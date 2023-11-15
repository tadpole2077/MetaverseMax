namespace MetaverseMax.Database
{
    public class UnitTransferDB : DatabaseBase
    {
        public UnitTransferDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
            worldType = _parentContext.worldTypeSelected;
        }    
    }
}
