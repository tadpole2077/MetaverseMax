﻿using Microsoft.AspNetCore.Mvc.ModelBinding;
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

    public class QueryParametersOwnerDataTron
    {
        // Using BingRequired attribute and not Required as it forces a specific use of a parameter name
        [BindRequired]
        public string owner_tron_public { get; set; }

    }

    public class QueryParametersOwnerOffer
    {
        [BindRequired]
        public bool active { get; set; }

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


    // Model Binding Class for Controller Parameters
    public class QueryParametersPlotSync
    {

        // Using BingRequired attribute and not Required as it forces a specific use of a parameter name
        [BindRequired]
        public string secure_token { get; set; }

        [BindRequired]
        public int world_type { get; set; }

        [BindRequired]
        public int interval { get; set; }
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
}