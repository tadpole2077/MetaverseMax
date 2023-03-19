
namespace MetaverseMax.ServiceClass
{
    public class OwnerPet
    {
        public int pet_count { get; set; }
        public string last_updated { get; set; }
        public IEnumerable<PetWeb> pet { get; set; }
    }

    public class PetWeb
    {
        public int token_id { get; set; }
        public string trait { get; set; }
        public int level { get; set; }
        public string name { get; set; }
    }

}
