using MetaverseMax.ServiceClass;
using MetaverseMax.BaseClass;

using NUnit.Framework;


namespace MetaverseMax.NUnit
{
    public class nunit_CitizenManage
    {
        nunit_CitizenManage() { }

        [Test]
        public void GetCitizenCount()
        {
            CitizenManage citizenManage = new(null, WORLD_TYPE.UNKNOWN);

           
            Assert.That(citizenManage.GetCitizenCount(null), Is.False, $"GetCitizenCount Returns 0 when passed Null Set of Citizens");
        }
    }
}
