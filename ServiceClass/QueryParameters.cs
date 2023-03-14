using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaverseMax.ServiceClass
{
    // Model Binding Class for Controller Parameters
    public class QueryParametersDistrictOwner
    {
        // Using BingRequired attribute and not Required as it forces a specific use of a parameter name
        [BindRequired]
        public int district_id { get; set; }

        [BindRequired]
        public int update_instance { get; set; }

    }

    public class QueryParametersOwnerData
    {
        // Using BingRequired attribute and not Required as it forces a specific use of a parameter name
        [BindRequired]
        public int plotX { get; set; }

        [BindRequired]
        public int plotY { get; set; }
    }

    public class QueryParametersOwnerDataMatic
    {
        // Using BingRequired attribute and not Required as it forces a specific use of a parameter name
        [BindRequired]
        public string owner_matic_key { get; set; }

    }

    public class QueryParametersOwnerDataMaticRefresh
    {
        // Using BingRequired attribute and not Required as it forces a specific use of a parameter name
        [BindRequired]
        public string owner_matic_key { get; set; }

        [BindRequired]
        public bool refresh { get; set; }

        [BindRequired]
        public string requester { get; set; }

    }

    public class QueryParametersOwnerDataKey
    {
        // Using BingRequired attribute and not Required as it forces a specific use of a parameter name
        [BindRequired]
        public string owner_public_key { get; set; }

    }

    public class QueryParametersOwnerOffer
    {
        //[BindRequired]
        //public bool active { get; set; }

        [BindRequired]
        public string matic_key { get; set; }

    }


    public class QueryParametersSecurity
    {
        [BindRequired]
        public string secure_token { get; set; }

    }

    public class QueryParametersFund
    {
        [BindRequired]
        public int district_id { get; set; }

        [BindRequired]
        public int daysHistory { get; set; }

    }

    public class QueryParametersDistrict
    {
        // Using BingRequired attribute and not Required as it forces a specific use of a parameter name
        [BindRequired]
        public int district_id { get; set; }

    }

    public class QueryParametersDistrictGetOpened
    {
        [BindRequired]
        public bool opened { get; set; }

    }

    public class QueryParametersDistrictGetAll
    {
        [BindRequired]
        public bool opened { get; set; }

        public bool includeTaxHistory { get; set; } = true;

    }

    public class QueryParametersPlotSync
    {

        // Using BingRequired attribute and not Required as it forces a specific use of a parameter name
        [BindRequired]
        public string secure_token { get; set; }

        [BindRequired]
        public int interval { get; set; }
    }

    public class QueryParametersPlotSingle
    {
        [BindRequired]
        public int plot_id { get; set; }

        [BindRequired]
        public int posX { get; set; }

        [BindRequired]
        public int posY { get; set; }
    }

    public class QueryParametersTypeLevel
    {
        [BindRequired]
        public int type { get; set; }

        [BindRequired]
        public int level { get; set; }
    }

    public class QueryParametersGetPlotMatric
    {
        [BindRequired]
        public string start_pos_x { get; set; }

        [BindRequired]
        public string start_pos_y { get; set; }

        [BindRequired]
        public string end_pos_x { get; set; }

        [BindRequired]
        public string end_pos_y { get; set; }

        [BindRequired]
        public string secure_token { get; set; }

        [BindRequired]
        public string interval { get; set; }

    }

    public class QueryParametersTokenID
    {

        // Using BingRequired attribute and not Required as it forces a specific use of a parameter name
        [BindRequired]
        public int token_id { get; set; }

    }

    public class QueryParametersTokenID_IPEfficiency
    {

        // Using BingRequired attribute and not Required as it forces a specific use of a parameter name
        [BindRequired]
        public int token_id { get; set; }

        [BindRequired]
        public int full_refresh { get; set; }

        [BindRequired]
        public string requester { get; set; }
    }

    public class QueryParametersCitizenHistory
    {
        // Using BingRequired attribute and not Required as it forces a specific use of a parameter name
        [BindRequired]
        public int token_id { get; set; }

        [BindRequired]
        public long production_date { get; set; }

    }

}
