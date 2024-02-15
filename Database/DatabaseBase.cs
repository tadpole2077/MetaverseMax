using MetaverseMax.BaseClass;

namespace MetaverseMax.Database
{
    // abstract class type = must be inherited by a class, cant be used standalone
    public abstract class DatabaseBase : DBLogger
    {
        public WORLD_TYPE worldType;

        // Protected base method, can only be accessed via code(methods) from same class or derived class. 
        protected DatabaseBase(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
            _context = _parentContext;
            worldType = _parentContext.worldTypeSelected;
        }

        protected DatabaseBase() : base(WORLD_TYPE.UNIVERSE)
        {
            
            worldType = WORLD_TYPE.UNIVERSE;
        }

    }
}
