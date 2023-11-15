using MetaverseMax.Database;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Transactions;

namespace MetaverseMax.Database
{
    public class BlockchainTransactionDB : DatabaseBase
    {
        public BlockchainTransactionDB(MetaverseMaxDbContext _parentContext) : base(_parentContext)
        {
        }

        public BlockchainTransaction AddOrUpdate(BlockchainTransaction transactionData, string hash)
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
                    blockchainTransaction.owner_matic = transactionData.owner_matic;
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
    }
}
