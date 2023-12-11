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
            OwnerDB ownerDB = new(_context);

            return ownerDB.GetOwnerBalance(ownerMaticKey);
        }

        public string GetBalanceTest()
        {
            //var url = "http://testchain.nethereum.com:8545";            
            var account = new Account(adminMMBankPrivateKey);
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

        public int ConfirmTransaction(string transactionHash)
        {            
            try
            {
                var account = new Account(adminMMBankPrivateKey);
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
                    DepositProcess(transactionReceipt, transactionHash, depositDTO);
                }
                else if (withdrawEventOutput.Count > 0)
                {
                    withdrawDTO = withdrawEventOutput[0].Event;
                    WithdrawProcess(transactionReceipt, transactionHash, withdrawDTO);
                }

            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("BankManage.ConfirmTransaction() : Error on processing transaction receipt: ", transactionHash));
            }

            return 0;
        }

        private bool DepositProcess(TransactionReceipt transactionReceipt, string transactionHash, DepositDTO depositDTO)
        {
            BlockchainTransactionDB blockchainTransactionDB = new(_context);
            BlockchainTransaction blockchainTransaction = new();
            OwnerManage ownerManage = new(_context, worldType);

            // Check Receipt is Valid MMBank Contract
            bool validContract = transactionReceipt.To.Equals(MMBankContractAddress, StringComparison.CurrentCultureIgnoreCase);

            bool alreadyProcessed = blockchainTransactionDB.AlreadyProcessed(transactionHash, BANK_ACTION.DEPOSIT);

            if (validContract && alreadyProcessed == false && depositDTO != null)
            {
                BigDecimal MegaAmount = Web3.Convert.FromWei(depositDTO.amount);

                blockchainTransaction.hash = transactionHash;
                blockchainTransaction.owner_matic = transactionReceipt.From;
                blockchainTransaction.amount = Convert.ToDecimal(MegaAmount.ToString());
                blockchainTransaction.action = BANK_ACTION.DEPOSIT;
                blockchainTransaction.event_recorded_utc = DateTime.UtcNow;

                blockchainTransactionDB.AddOrUpdate(blockchainTransaction, transactionHash);

                ownerManage.UpdateBalance(blockchainTransaction.owner_matic, blockchainTransaction.amount);
            }

            return true;
        }

        // When withdraw transaction completes on client side - this server side call occurs to check blockchain event log and record in local db.
        // Note - A call to WithdrawAllowanceApprove will occur BEFORE this receipt call occurs.
        private bool WithdrawProcess(TransactionReceipt transactionReceipt, string transactionHash, WithdrawDTO withdrawDTO)
        {
            BlockchainTransactionDB blockchainTransactionDB = new(_context);
            BlockchainTransaction blockchainTransaction = new();

            // Check Receipt is Valid MMBank Contract
            bool validContract = transactionReceipt.To.Equals(MMBankContractAddress, StringComparison.CurrentCultureIgnoreCase);

            // Check previously recorded blockchain - hacker may try to rerun the transaction event check.
            bool alreadyProcessed = blockchainTransactionDB.AlreadyProcessed(transactionHash, BANK_ACTION.WITHDRAW);

            if (validContract && alreadyProcessed == false && withdrawDTO != null)
            {
                BigDecimal MegaAmount = -Web3.Convert.FromWei(withdrawDTO.amount);

                blockchainTransaction.hash = transactionHash;
                blockchainTransaction.owner_matic = transactionReceipt.From;
                blockchainTransaction.amount = Convert.ToDecimal(MegaAmount.ToString());
                blockchainTransaction.action = BANK_ACTION.WITHDRAW;
                blockchainTransaction.event_recorded_utc = DateTime.UtcNow;

                blockchainTransactionDB.AddOrUpdate(blockchainTransaction, transactionHash);
            }

            return true;
        }

        public bool WithdrawAllowanceApprove(decimal amount, string ownerMaticKey, string signed)
        {
            bool approvalAllowed = false;
            decimal accountBalance = 0;
            try
            {
                OwnerManage ownerManage = new(_context, worldType);
                OwnerDB ownerDB = new(_context);
                PersonalSignDB personalSignDB = new(_context, worldType);
                var account = new Account(adminMMBankPrivateKey);
                var web3 = new Web3(account, networkUrl);                

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

                    if (signedSignature.ToLower() == ownerMaticKey.ToLower() && ownerMaticKey != string.Empty)
                    {                        
                        // Sign is valid - store sign hash key, record can not be Reused. SECURITY STEP
                        personalSign.signed_key = signed;
                        personalSignDB.AddOrUpdate(personalSign);

                        // CHECK withdraw amount is valid 
                        accountBalance = ownerDB.GetOwnerBalance(ownerMaticKey);

                        if (accountBalance >= amount)
                        {
                            approvalAllowed = true;

                            var increaseAllowanceFunctionMessage = new BankWithdrawIncreaseAllowanceFunction()
                            {
                                recipient = ownerMaticKey,
                                amount = Web3.Convert.ToWei(amount),
                            };

                            // TODO Add improved hander for blockchain contract call, on error log and prevent transaction proceeding.
                            var balanceHandler = web3.Eth.GetContractQueryHandler<BankWithdrawIncreaseAllowanceFunction>();
                            var result = balanceHandler.QueryAsync<BigInteger>(MMBankContractAddress, increaseAllowanceFunctionMessage).Result;

                            // Update account balance - reduce by withdraw amount, include update of local cache accounts.
                            ownerManage.UpdateBalance(ownerMaticKey, -amount);
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
