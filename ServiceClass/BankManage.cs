using MetaverseMax.BaseClass;
using MetaverseMax.Database;
using Nethereum.Web3;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts.CQS;
using Nethereum.Util;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Contracts;
using Nethereum.Contracts.Extensions;
using System.Numerics;
using Nethereum.RPC.Eth.DTOs;
using System;
using Nethereum.BlockchainProcessing.BlockStorage.Entities;
using System.Text;
using System.Security.Cryptography;
using Nethereum.Signer;
using System.Drawing;
using System.ComponentModel;
using Nethereum.Model;
using static System.Collections.Specialized.BitVector32;
using System.Security.Cryptography.Xml;

namespace MetaverseMax.ServiceClass
{
    [Function("getBalanceOf", "uint256")]
    public class GetBalanceOfFunction : FunctionMessage
    {
        [Parameter("address", "account", 1)]
        public string account{ get; set; }
    }

    [Function("increaseAllowance")]
    public class BankWithdrawIncreaseAllowanceFunction : FunctionMessage
    {
        [Parameter("address", "recipient", 1)]
        public string recipient { get; set; }

        [Parameter("uint256", "amount", 2)]
        public BigInteger amount { get; set; }
    }

    // DTO Used to query events from the Blockchain log
    [Event("Approval")]
    public class ApprovalEventDTO : IEventDTO
    {
        [Parameter("address", "owner", 1, true)]
        public string owner { get; set; }

        [Parameter("address", "spender", 2, true)]
        public string spender { get; set; }

        [Parameter("uint256", "amount", 3, false)]
        public BigInteger amount { get; set; }
    }

    [Event("Transfer")]
    public class TransferEventDTO : IEventDTO
    {
        [Parameter("address", "from", 1, true)]
        public string from { get; set; }

        [Parameter("address", "to", 2, true)]
        public string to { get; set; }

        [Parameter("uint256", "amount", 3, false)]
        public BigInteger amount { get; set; }
    }

    // MMBank Deposit
    [Event("Deposit")]
    public class DepositDTO : IEventDTO
    {
        [Parameter("address", "from", 1, true)]
        public string from { get; set; }

        [Parameter("address", "to", 2, true)]
        public string to { get; set; }

        [Parameter("uint256", "amount", 3, false)]
        public BigInteger amount { get; set; }
    }

    // MMBank Deposit
    [Event("Withdraw")]
    public class WithdrawDTO : IEventDTO
    {
        [Parameter("address", "from", 1, true)]
        public string from { get; set; }

        [Parameter("address", "to", 2, true)]
        public string to { get; set; }

        [Parameter("uint256", "amount", 3, false)]
        public BigInteger amount { get; set; }
    }

    // Process : User triggered Withdraw
    //      GetWithdrawSignCode()
    //      WithdrawAllowanceApprove()
    //      ConfirmTransaction()
    //      WithdrawProcess()

    public class BankManage : ServiceBase
    {
        const string networkUrl = "https://data-seed-prebsc-1-s1.binance.org:8545";
        const string adminMMBankPrivateKey = "103ba2145001498e81add461fc9c60b56412b3dbcc177ff1a733f91646527dec";       // Admin Account: 0xFA87a94a37Ffd3e7d6Ae35FF33eB5d15A5A87467
        const string MMBankContractAddress = "0x9Adf2de8c24c25B3EB1fc542598b69C51eE558A7";
        const int MEGA_DECIMALS_COUNT = 18;   // also 18 places matches wei(1e18) to eth conversion.

        public BankManage(MetaverseMaxDbContext _parentContext, WORLD_TYPE worldTypeSelected) : base(_parentContext, worldTypeSelected)
        {
            worldType = worldTypeSelected;
            _context.worldTypeSelected = worldTypeSelected;
        }

        public decimal GetBalance(string ownerMaticKey)
        {
            OwnerManage ownerMange = new(_context, worldType);
            OwnerAccountWeb ownerAccountWeb = ownerMange.GetOwnerAccountWebByMatic(ownerMaticKey);
            
            return ownerAccountWeb == null ? 0 : ownerAccountWeb.balance;
        }

        public string GetBalanceTest()
        {
            //var url = "http://testchain.nethereum.com:8545";            
            var account = new Nethereum.Web3.Accounts.Account(adminMMBankPrivateKey);
            var web3 = new Web3(account, networkUrl);

            var balanceOfFunctionMessage = new GetBalanceOfFunction()
            {
                account = account.Address,
            };

            var balanceHandler = web3.Eth.GetContractQueryHandler<GetBalanceOfFunction>();
            var balanceBigInt =  balanceHandler.QueryAsync<BigInteger>(MMBankContractAddress, balanceOfFunctionMessage).Result;
            BigDecimal MegaBalance = Web3.Convert.FromWei(balanceBigInt);           

            return MegaBalance.ToString();
        }

        // Security Checks
        // Using actual deposit - check has deposit Event, with recipient MMBank, and use actual amount deposited.
        public bool ConfirmTransaction(string transactionHash)
        {
            bool transactoinConfirmed = false;
            try
            {
                var account = new Nethereum.Web3.Accounts.Account(adminMMBankPrivateKey);
                var web3 = new Web3(account, networkUrl);                                                
                DepositDTO depositDTO = null;
                WithdrawDTO withdrawDTO = null;

                var transactionReceipt = web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash).Result;

                var transferEventOutput = transactionReceipt.DecodeAllEvents<TransferEventDTO>();
                var approvalEventOutput = transactionReceipt.DecodeAllEvents<ApprovalEventDTO>();
                var depositEventOutput = transactionReceipt.DecodeAllEvents<DepositDTO>();
                var withdrawEventOutput = transactionReceipt.DecodeAllEvents<WithdrawDTO>();

                if (depositEventOutput.Count > 0)
                {
                    depositDTO = depositEventOutput[0].Event;
                    transactoinConfirmed = DepositProcess(transactionReceipt, transactionHash, depositEventOutput[0]);
                }
                else if (withdrawEventOutput.Count > 0)
                {
                    withdrawDTO = withdrawEventOutput[0].Event;
                    transactoinConfirmed = WithdrawProcess(transactionReceipt, transactionHash, withdrawDTO);
                }
            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("BankManage.ConfirmTransaction() : Error on processing transaction receipt: ", transactionHash));
            }

            return transactoinConfirmed;
        }

        private bool DepositProcess(TransactionReceipt transactionReceipt, string transactionHash, EventLog<DepositDTO> eventLog)
        {
            using MetaverseMaxDbContext_UNI _contextUni = new();        // Auto disposed at end of scope/method C#8
            BlockchainTransactionDB blockchainTransactionDB = new(_contextUni);
            OwnerManage ownerManage = new(_context, worldType);
            DepositDTO depositDTO = eventLog.Event;
            bool depositProcessed = false;
            bool alreadyProcessed = false;
            NETWORK chain = NETWORK.BINANCE_ID;

            // Check Receipt - deposit recipient == Valid MMBank Contract
            bool validRecipientMMBank = transactionReceipt.To.Equals(MMBankContractAddress, StringComparison.CurrentCultureIgnoreCase);

            // Check MMBank issued deposit event Log entry - not a spoof deposit event
            bool validMMBankDepositEvent = eventLog.Log.Address.Equals(MMBankContractAddress, StringComparison.CurrentCultureIgnoreCase);

            // Check if this transaction hash previously processed            
            alreadyProcessed = blockchainTransactionDB.AlreadyProcessed(transactionHash, BANK_ACTION.DEPOSIT);
            
            // 4 Rule Pass to processed with Recording Transaction and User Balance Change.
            if (validRecipientMMBank && validMMBankDepositEvent && alreadyProcessed == false && depositDTO != null)
            {
                BigDecimal MegaAmount = Web3.Convert.FromWei(depositDTO.amount);

                // Owner must exist to assign a deposit to, potentially a new owner, just arrived in world today.
                int ownerUniID = ownerManage.GetOwnerOrCreate(transactionReceipt.From);

                // Atomic unit with table locking to ensure Transactoin and account balance update occurs once and as a single transaction unit.
                depositProcessed = blockchainTransactionDB.RecordDeposit(transactionHash, transactionReceipt.From, ownerUniID, BANK_ACTION.DEPOSIT, Convert.ToDecimal(MegaAmount.ToString()), chain);

                // Get new owner balance from db, and assign to local cache
                if (depositProcessed)
                {
                    depositProcessed = ownerManage.UpdateOwnerBalanceFromDB(transactionReceipt.From);
                }
            }

            return depositProcessed;
        }

        // When withdraw transaction completes on client side - this server side call occurs to check blockchain event log and record in local db.
        // Note - A call to WithdrawAllowanceApprove will occur BEFORE this receipt call occurs.
        private bool WithdrawProcess(TransactionReceipt transactionReceipt, string transactionHash, WithdrawDTO withdrawDTO)
        {
            using MetaverseMaxDbContext_UNI _contextUni = new();        // Auto disposed at end of scope/method C#8
            
            BlockchainTransactionDB blockchainTransactionDB = new(_contextUni);
            BlockchainTransaction blockchainTransaction = new();
            int ownerUniID = 0;
            OwnerManage ownerManage = new(_context, worldType);            

            // Check Receipt is Valid MMBank Contract
            bool validContract = transactionReceipt.To.Equals(MMBankContractAddress, StringComparison.CurrentCultureIgnoreCase);

            // Check previously recorded blockchain - hacker may try to rerun the transaction event check.
            bool alreadyProcessed = blockchainTransactionDB.AlreadyProcessed(transactionHash, BANK_ACTION.WITHDRAW);

            if (validContract && alreadyProcessed == false && withdrawDTO != null)
            {
                BigDecimal MegaAmount = -Web3.Convert.FromWei(withdrawDTO.amount);

                // Map matic_key to owner_uni_key
                ownerUniID = ownerManage.GetOwnerUniIDByMatic(transactionReceipt.From);

                blockchainTransaction.hash = transactionHash;
                blockchainTransaction.amount = Convert.ToDecimal(MegaAmount.ToString());
                blockchainTransaction.action = BANK_ACTION.WITHDRAW;
                blockchainTransaction.event_recorded_utc = DateTime.UtcNow;

                blockchainTransactionDB.AddOrUpdate(blockchainTransaction, transactionHash, ownerUniID);
            }
            
            return true;
        }

        public bool WithdrawAllowanceApprove(decimal amount, string ownerMaticKey, string signed)
        {
            bool approvalAllowed = false;
            int transaction;
            decimal accountBalance = 0;
            try
            {
                using MetaverseMaxDbContext_UNI _contextUni = new();        // Auto disposed at end of scope/method C#8
                BlockchainTransactionDB blockchainTransactionDB = new(_contextUni);
                OwnerManage ownerManage = new(_context, worldType);                
                PersonalSignDB personalSignDB = new(_context, worldType);
                var account = new Nethereum.Web3.Accounts.Account(adminMMBankPrivateKey);
                var web3 = new Web3(account, networkUrl);
                NETWORK chain = NETWORK.BINANCE_ID;

                // Get active stored sign record for this account
                PersonalSign personalSign = personalSignDB.GetUnsignedByMaticKey(ownerMaticKey);

                // If no stored sign found or passed amount is not valid END PROCESSING
                if (personalSign == null)
                {
                    _context.LogEvent(String.Concat("BankManage.WithdrawAllowanceApprove() : No PersonalSign found for ownerMaticKey : ", ownerMaticKey));
                    approvalAllowed = false;
                }
                else if (amount <= 0)
                {
                    _context.LogEvent(String.Concat("BankManage.WithdrawAllowanceApprove() : Attempting to withdraw invalid amount for ownerMaticKey: ", ownerMaticKey, " amount: ", amount));
                    approvalAllowed = false;
                }
                else
                {                
                    // Check Sign is valid
                    // recover the signing account address using original message and signed message
                    var signer = new EthereumMessageSigner();
                    byte[] bytesMsg = GetCombinedBlurbCodeByte(amount, ownerMaticKey, personalSign.salt);
                    var signedSignature = signer.EcRecover(bytesMsg, signed);

                    OwnerAccount ownerAccount = ownerManage.GetOwnerAccountByMatic(ownerMaticKey);

                    if (signedSignature.ToLower() == ownerMaticKey.ToLower() && ownerMaticKey != string.Empty && ownerAccount != null)
                    {                        
                        // Sign is valid - store sign hash key, record can not be Reused. SECURITY STEP
                        personalSign.signed_key = signed;
                        personalSignDB.AddOrUpdate(personalSign);

                        // SPROC locking owner table - getting current balance, reducing balance, returning result - success = balance reduced, fail = balance change failed, do not proceed with withdraw
                        // Requirement for success : (A) Owner must exist,  (B) owner balance must be >= withdraw amount
                        // Atomic unit with table locking to ensure Transactoin and account balance update occurs once and as a single transaction unit.
                        transaction = blockchainTransactionDB.RecordWithdrawApproval(ownerMaticKey, ownerAccount.ownerUniID, BANK_ACTION.WITHDRAW_PENDING, amount, chain);

                        // Update local Owner account balance.
                        ownerManage.UpdateOwnerBalanceFromDB(ownerMaticKey);
                        accountBalance = ownerManage.GetOwnerBalanceByMatic(ownerMaticKey);


                        if (transaction > 0)
                        {
                            var increaseAllowanceFunctionMessage = new BankWithdrawIncreaseAllowanceFunction()
                            {
                                recipient = ownerMaticKey,
                                amount = Web3.Convert.ToWei(amount),
                            };

                            // Option: using a QueryHandler
                            //var balanceHandler = web3.Eth.GetContractQueryHandler<BankWithdrawIncreaseAllowanceFunction>();
                            //var result = balanceHandler.QueryAsync<BigInteger>(MMBankContractAddress, increaseAllowanceFunctionMessage).Result;

                            // Option: using a TransactionHandler - allows lookup of transaction properties
                            var allowanceHandler = web3.Eth.GetContractTransactionHandler<BankWithdrawIncreaseAllowanceFunction>();
                            var transactionReceipt = allowanceHandler.SendRequestAndWaitForReceiptAsync(MMBankContractAddress, increaseAllowanceFunctionMessage).Result;
                            var transactionHash = transactionReceipt.TransactionHash;

                            // Check Transaction was sucessful
                            approvalAllowed = transactionReceipt.Status.Value == BigInteger.One;      // bool value - true if successful

                            if (approvalAllowed)
                            {
                                blockchainTransactionDB.AssignHash(transaction, transactionHash);
                            }
                        }
                        else
                        {
                            _context.LogEvent(String.Concat("BankManage.WithdrawAllowanceApprove() : Attempting to withdraw using [invalid signature] for ownerMaticKey: ", ownerMaticKey, " amount: ", amount));
                            approvalAllowed = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("BankManage.WithdrawAllowanceApprove() : Error on approval of allowance to widthdraw. OwnerMatic: ", ownerMaticKey, " amount: ", amount));
            }

            return approvalAllowed;
        }

        public string GetWithdrawSignCode(decimal amount, string ownerMaticKey)
        {
            PersonalSignDB personalSignDB = new(_context, worldType);
            string salt = GetSalt(10);
            byte[] BlurbByte = GetBlurbByte(amount, ownerMaticKey);

            // hash code bytes array already in Hex utf8.
            string encodedString = GetEncodedByte(amount, ownerMaticKey, salt).ToHex();             

            // String suitable for MM Personal Sign-in
            string codeHex = Encoding.UTF8.GetString(BlurbByte).ToHexUTF8() + encodedString;

            personalSignDB.AddOrUpdate(new PersonalSign()
            {
                amount = Web3.Convert.ToWei(amount).ToString(),
                matic_key = ownerMaticKey,
                salt = salt,
                encode_byte = codeHex
            });

            return codeHex;
        }

        public byte[] GetBlurbByte(decimal amount, string ownerMaticKey)
        {
            string blurb = string.Concat(amount, " Mega Withdraw from Bank \nApproval Code:\n");            
            byte[] blurbBytes = Encoding.Default.GetBytes(blurb);

            return blurbBytes;
        }

        // Code Encrypted with SHA256
        // Purpose of this EncodedByte logic : 
        //      Ensure person-sign is from verified account,  the code is valid for specific time period.
        //      Objective: Only owner can approve withdraw amount.
        //      Security:
        //          If a hacker attempts to reuse the code, that has already been signed, it will fail.  
        //          if a hacker attempt to create a code for another account, when signed by their account it will fail
        //          if a hacker manages to scrap or sniff a valid signed code from a player device - if the code has already been used (to approve a withdraw) then its of no value/no use.
        //          Each Code + Sign(confimed) pair is logged to db.
        //          Any requested code, that is not sign approved is deleted after 24 hours.   Meaning either failed to proceed with sign-withdraw or hack attampt.
        //          When an account requests a new code - any pending code with no sign approved for that account is removed from db.
        public byte[] GetEncodedByte(decimal amount, string ownerMaticKey, string salt)
        {
            // Add Encypted salted code
            byte[] buffer = Encoding.UTF8.GetBytes( string.Concat("100", ownerMaticKey, salt));
            byte[] encodedCode = SHA256.HashData(buffer);       // 32 bytes

            byte[] encodedCodeUTF8 = Encoding.UTF8.GetBytes(encodedCode.ToHex());       // 64 bytes

            return encodedCodeUTF8;
        }

        public byte[] GetCombinedBlurbCodeByte(decimal amount, string ownerMaticKey, string salt)
        {
            byte[] blurbByte = GetBlurbByte(amount, ownerMaticKey);
            byte[] encodedCode = GetEncodedByte(amount, ownerMaticKey, salt);

            return blurbByte.Concat(encodedCode).ToArray();

        }

        private static string GetSalt(int maximumSaltLength)
        {
            var salt = new byte[maximumSaltLength];

            using (var generator = RandomNumberGenerator.Create())
            {
                generator.GetNonZeroBytes(salt);             
            }
            string saltHex = Convert.ToHexString(salt);

            //new RandomNumberGenerator()
            return saltHex;
        }
    }
}
