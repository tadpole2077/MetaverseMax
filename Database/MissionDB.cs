using MetaverseMax.BaseClass;

namespace MetaverseMax.Database
{
    public class MissionDB : DatabaseBase
    {

        public MissionDB(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext)
        {
            worldType = worldTypeSelected;
            _parentContext.worldTypeSelected = worldType;
        }

    }
}
