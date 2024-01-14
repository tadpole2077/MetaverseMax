using MetaverseMax.ServiceClass;
using System.ComponentModel.DataAnnotations.Schema;

public class OwnerTransactionWeb
{
    public IEnumerable<TransactionWeb> transaction_list { get; set; }
}

public class TransactionWeb
{   
    public string hash { get; set; }
    
    public char action { get; set; }

    public string event_recorded_gmt { get; set; }
    
    public decimal amount { get; set; }
}