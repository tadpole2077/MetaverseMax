using MetaverseMax.BaseClass;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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

    public class QueryParametersOwnerDarkMode
    {
        // Using BingRequired attribute and not Required as it forces a specific use of a parameter name
        [BindRequired]
        public string owner_matic_key { get; set; }

        [BindRequired]
        public bool dark_mode { get; set; }

    }

    public class QueryParametersOwnerVisible
    {
        // Using BingRequired attribute and not Required as it forces a specific use of a parameter name
        [BindRequired]
        public string owner_matic_key { get; set; }

        [BindRequired]
        public bool balance_visible { get; set; }

    }

    public class QueryParametersMatic
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

    public class QueryParametersAlert
    {
        [BindRequired]
        public string matic_key { get; set; }

        [BindRequired]
        public ALERT_TYPE alert_type { get; set; }

        [BindRequired]
        public int id { get; set; }

        [BindRequired]
        public int action { get; set; }

    }
    public class QueryParametersAlertGet
    {
        [BindRequired]
        public string matic_key { get; set; }

        [BindRequired]
        public int district_id { get; set; }

    }

    public class QueryParametersAlertSingleGet
    {
        [BindRequired]
        public string matic_key { get; set; }

        [BindRequired]
        public int alert_type { get; set; }

    }

    public class QueryParametersAlertPendingGet
    {
        [BindRequired]
        public string matic_key { get; set; }

        [BindRequired]
        public int pending_alert { get; set; }

    }

    public class QueryParametersAlertDelete
    {
        [BindRequired]
        public string matic_key { get; set; }

        [BindRequired]
        public int alert_pending_key { get; set; }

    }

    public class QueryParametersFund
    {
        [BindRequired]
        public int district_id { get; set; }

        [BindRequired]
        public int daysHistory { get; set; }

    }

    public class QueryParametersDistrictId
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

    public class QueryParametersDistributeUpdate
    {

        // Using BingRequired attribute and not Required as it forces a specific use of a parameter name
        [BindRequired]
        public string secure_token { get; set; }

        [BindRequired]
        public int interval { get; set; }

        [BindRequired]
        public int distribute_action { get; set; }

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

        [BindRequired]
        public string requester_matic { get; set; }
        
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

    public class QueryParametersEndpoint
    {
        [BindRequired]
        public string contract_name { get; set; }
    }
    
    public class QueryParametersTransaction
    {
        [BindRequired]
        public string from_wallet { get; set; }

        [BindRequired]
        public string to_wallet { get; set; }

        [BindRequired]
        public int unit_type { get; set; }

        [BindRequired]
        public int unit_amount { get; set; }

        [BindRequired]
        public decimal value { get; set; }

        [BindRequired]
        public string hash { get; set; }

        [BindRequired]
        public int status { get; set; }

        [BindRequired]
        public int blockchain { get; set; }

        [BindRequired]
        public int transaction_type { get; set; }

        [BindRequired]
        public int token_id { get; set; }
    }

    public class QueryParametersTransactionReceipt
    {
        [BindRequired]
        public string hash { get; set; }

    }

    public class QueryParametersWithdrawAllowanceApprove
    {
        [BindRequired]
        public decimal amount { get; set; }

        [BindRequired]
        public string ownerMaticKey { get; set; }

        [BindRequired]
        public string personalSign { get; set; }
    }

    public class QueryParametersWithdrawSignCode
    {
        [BindRequired]
        public decimal amount { get; set; }

        [BindRequired]
        public string ownerMaticKey { get; set; }
    }

    }
