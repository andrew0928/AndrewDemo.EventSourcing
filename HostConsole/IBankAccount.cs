namespace HostConsole
{
    interface IBankAccount
    {
        long TransRequire(long transID, decimal amount); // 對於這個帳戶的操作，正數代表轉帳進這個戶頭
        long TransCommit(long transID, long fromSN = 0);
        long TransCancel(long transID, long fromSN = 0);

        (decimal available, decimal processing) GetBalance();
    }
}
