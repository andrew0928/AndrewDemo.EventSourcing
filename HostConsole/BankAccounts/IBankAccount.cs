using System;

namespace HostConsole.BankAccounts
{
    interface IBankAccount
    {
        long TransRequire(long transID, decimal amount, string notes = null); // 對於這個帳戶的操作，正數代表轉帳進這個戶頭
        long TransCommit(long transID, long fromSN = 0);
        long TransCancel(long transID, long fromSN = 0);

        (decimal available, decimal processing) GetBalance();
    }

    internal enum BankAccountCommandTypeEnum
    {
        REQUIRE = 1,
        COMMIT = 3,
        CANCEL = 4,

        UNKNOWN = 9999
    }

    internal enum BankAccountRecordStateEnum
    {
        InProc = 1,
        Completed = 2,
        Cancelled = 3
    }

    internal enum BankAccountRecordTypeEnum
    {
        DEPOSIT = 1,
        WITHDRAW = 2
    }

    internal class BankAccountCommand
    {
        public long SN;
        public long FromSN;
        public long TransID;
        public BankAccountCommandTypeEnum Command = BankAccountCommandTypeEnum.UNKNOWN;
        public decimal Amount;
        public DateTime CommandTime;
        public string Notes;
    }

    internal class RecordItem
    {
        public long SN;
        public long TransID;
        public decimal DepositAmount;
        public decimal WithdrawAmount;
        public BankAccountRecordStateEnum State;
        public string Notes;
        public DateTime CreateTime;
        public DateTime UpdateTime;
    }
}
