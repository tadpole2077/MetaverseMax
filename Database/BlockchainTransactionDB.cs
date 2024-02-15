using MetaverseMax.BaseClass;
using MetaverseMax.Database;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Transactions;

namespace MetaverseMax.Database
{
    public class BlockchainTransactionDB : DatabaseBase
    {
        private new readonly MetaverseMaxDbContext_UNI _context;

        public BlockchainTransactionDB(MetaverseMaxDbContext_UNI _parentContext) : base()
        {
            _context = _parentContext;
        }

        public BlockchainTransaction AddOrUpdate(BlockchainTransaction transactionData, string hash, int ownerUni_ID)
        {
            bool newTransaction = false;
            BlockchainTransaction blockchainTransaction = null;
            bool transactionInLocalContent = false;
            string hashCode = hash.Replace("0x", "");

            try
            {
                blockchainTransaction = _context.BlockchainTransaction.Where(x => x.hash == hashCode).FirstOrDefault();

                // Corner Case: Check if Transaction previously generated (only in local context) but not yet saved to db
                transactionInLocalContent = _context.BlockchainTransaction.Local.Any(e => e.hash == hashCode);

                if (transactionInLocalContent == false)
                {

                    if (blockchainTransaction == null)
                    {
                        blockchainTransaction = new();
                        newTransaction = true;
                    }

                    blockchainTransaction.hash = hashCode;
                    blockchainTransaction.owner_uni_id = ownerUni_ID;
                    blockchainTransaction.action = transactionData.action;
                    blockchainTransaction.event_recorded_utc = transactionData.event_recorded_utc;
                    blockchainTransaction.amount = transactionData.amount;
                    blockchainTransaction.approval_amount = transactionData.approval_amount;
                    blockchainTransaction.approval_recorded_utc = transactionData.approval_recorded_utc;
                    blockchainTransaction.note = transactionData.note;

                    if (newTransaction)
                    {
                        blockchainTransaction = _context.BlockchainTransaction.Add(blockchainTransaction).Entity;
                    }

                    _context.SaveWithRetry(true);
                }

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("blockchainTransactionDB::AddOrUpdate() : Error adding/Updating transaction with hash: ", hash));

                // De-associate from the faulty row. Improves fault tolerance - allows SaveChange() to complete - context with other pending transactions
                if (newTransaction == true)
                {
                    _context.Entry(blockchainTransaction).State = EntityState.Detached;
                }
            }

            return blockchainTransaction;
        }

        public bool AlreadyProcessed(string hash, char action)
        {
            BlockchainTransaction blockchainTransaction = null;
            bool transactionProcessed = false;
            string hashCode = hash.Replace("0x", "");

            try
            {
                blockchainTransaction = _context.BlockchainTransaction.Where(x => x.hash == hashCode && x.action == action).FirstOrDefault();

                // Corner Case: Check if Transaction previously generated (only in local context) but not yet saved to db
                transactionProcessed = blockchainTransaction != null;

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("blockchainTransactionDB::AlreadyProcessed() : Error checking transaction with hash: ", hash, " and action: ", action));
            }

            return transactionProcessed;
        }

        public List<BlockchainTransaction> GetByOwnerUniID(int ownerUniID)
        {
            List<BlockchainTransaction> ownerTransactionList = null;

            try
            {
                ownerTransactionList = _context.BlockchainTransaction.Where(x => x.owner_uni_id == ownerUniID)
                    .OrderByDescending(x => x.event_recorded_utc)
                    .ToList();
            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("blockchainTransactionDB::GetByOwnerUniID() : Error getting list by Owner_Uni_ID ", ownerUniID));
            }

            return ownerTransactionList;
        }

        public bool RecordDeposit(string hash, string ownerMatic, int ownerUniID, char action, decimal amount, NETWORK chainId)
        {
            int result;
            bool processed = false;
            try
            {
                SqlParameter hashParameter = new ("@hash", hash);
                SqlParameter ownerParameter = new ("@owner_uni_id", ownerUniID);
                SqlParameter depositorParameter = new("@depositor_matic_key", ownerMatic);
                SqlParameter actionParameter = new ("@action", action);
                SqlParameter amountParameter = new ("@amount", amount);
                SqlParameter chainParameter = new("@chain_id", (int)chainId);
                
                SqlParameter processedResult = new()
                {
                    ParameterName = "@processed",
                    SqlDbType = System.Data.SqlDbType.Bit,
                    Direction = System.Data.ParameterDirection.Output,

                };

                result = _context.Database.ExecuteSqlRaw("EXEC @processed = dbo.sp_transaction_deposit @hash, @depositor_matic_key, @owner_uni_id, @action, @amount, @chain_id", 
                    new[] { hashParameter, depositorParameter, ownerParameter, actionParameter, amountParameter, processedResult, chainParameter });

                processed = (bool)processedResult.Value;

            }
            catch (Exception ex)
            {
                logException(ex, String.Concat("OwnerDB::RecordDeposit() : Error recording deposit for owner universe ID ", ownerUniID, " with depositer matic ", ownerMatic, " on Transaction : ", hash, " and amount ", amount));                
            }

            return processed;
        }
    }
}
