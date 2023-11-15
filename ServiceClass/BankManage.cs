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


        private bool WithdrawProcess(TransactionReceipt transactionReceipt, string transactionHash, WithdrawDTO withdrawDTO)
        {
            BlockchainTransactionDB blockchainTransactionDB = new(_context);
            BlockchainTransaction blockchainTransaction = new();
            OwnerManage ownerManage = new(_context, worldType);

            // Check Receipt is Valid MMBank Contract
            bool validContract = transactionReceipt.To.Equals(MMBankContractAddress, StringComparison.CurrentCultureIgnoreCase);

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

                ownerManage.UpdateBalance(blockchainTransaction.owner_matic, blockchainTransaction.amount);
            }

            return true;
        }

        public bool WithdrawAllowanceApprove(decimal amount, string ownerMaticKey)
        {
            try
            {
                var account = new Account(adminMMBankPrivateKey);
                var web3 = new Web3(account, networkUrl);

                var increaseAllowanceFunctionMessage = new BankWithdrawIncreaseAllowanceFunction()
                {
                    recipient = ownerMaticKey,
                    amount = Web3.Convert.ToWei(amount),
                };

                var balanceHandler = web3.Eth.GetContractQueryHandler<BankWithdrawIncreaseAllowanceFunction>();
                var result = balanceHandler.QueryAsync<BigInteger>(MMBankContractAddress, increaseAllowanceFunctionMessage).Result;
            }
            catch (Exception ex)
            {
                DBLogger dbLogger = new(_context, worldType);
                dbLogger.logException(ex, String.Concat("BankManage.WithdrawAllowanceApprove() : Error on approval of allowance to widthdraw. OwnerMatic: ", ownerMaticKey, " amount: ", amount));
            }

            return true;
        }
    }
}
