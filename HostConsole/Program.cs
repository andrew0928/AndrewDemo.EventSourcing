using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace HostConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //Demo1_BasicTransaction();
            //Demo2_2PhaseCommit();
            Demo3_SAGA();
        }

        static void Demo3_SAGA()
        {
            var acc1 = new InMemoryAccount() { Name = "andrew" };
            var acc2 = new InMemoryAccount() { Name = "boris" };
            var acc3 = new InMemoryAccount() { Name = "jolin" };

            // acc1 - deposit $5000
            acc1.TransRequire(100, 5000);
            acc1.TransCommit(100);

            // step 1, acc1 withdraw $3000, transfer to acc2 deposit $3000
            bool step1_result = false;
            try
            {
                acc1.TransRequire(200, -3000);
                acc2.TransRequire(200, 3000);
                acc1.TransCommit(200);
                acc2.TransCommit(200);
                step1_result = true;
            }
            catch
            {
                acc1.TransCancel(200);
                acc2.TransCancel(200);
                step1_result = false;
            }
            if (step1_result == false) return;

            // step 2, acc1 withdraw $2500 fail
            // rollback step 2
            bool step2_result = false;
            try
            {
                acc1.TransRequire(300, -2500);
                acc3.TransRequire(300, 2500);
                acc1.TransCommit(300);
                acc3.TransCommit(300);
                step2_result = true;
            }
            catch
            {
                acc1.TransCancel(300);
                acc3.TransCancel(300);
                step2_result = false;
            }

            if (step2_result == false)
            {
                // rollback step 1 (in new transaction)
                try
                {
                    acc1.TransRequire(400, 3000);
                    acc2.TransRequire(400, -3000);
                    acc1.TransCommit(400);
                    acc2.TransCommit(400);
                }
                catch
                {
                    acc1.TransCancel(400);
                    acc2.TransCancel(400);
                    // something wrong!!!
                }
            }
        }
        static void Demo2_2PhaseCommit()
        {
            var acc1 = new InMemoryAccount() { Name = "andrew" };
            var acc2 = new InMemoryAccount() { Name = "boris" };
            var acc3 = new InMemoryAccount() { Name = "jolin" };

            // acc1 - deposit $5000
            acc1.TransRequire(100, 5000);
            acc1.TransCommit(100);

            try
            {
                // try transfer $3000 from acc1 to acc2, $2500 from acc1 to acc3, must in one transaction
                acc1.TransRequire(200, -3000);
                acc2.TransRequire(200, 3000);

                acc1.TransRequire(200, -2500);
                acc3.TransRequire(200, 2500);


                acc1.TransCommit(200);
                acc2.TransCommit(200);
                acc3.TransCommit(200);
            }
            catch
            {
                acc1.TransCancel(200);
                acc2.TransCancel(200);
                acc3.TransCancel(200);
            }
        }



        static void Demo1_BasicTransaction()
        {
            var acc = new InMemoryAccount();

            acc.TransRequire(100, 1000);
            acc.TransRequire(200, 5000);
            acc.TransRequire(300, 1000);

            acc.TransCommit(200);
            acc.TransCancel(300);
            acc.TransCommit(100);
        }

        
    }








    interface IBankAccount
    {
        long TransRequire(long transID, decimal amount); // 對於這個帳戶的操作，正數代表轉帳進這個戶頭
        long TransCommit(long transID, long fromSN = 0);
        long TransCancel(long transID, long fromSN = 0);

        (decimal available, decimal processing) GetBalance();
    }


    public class InMemoryAccount : IBankAccount
    {
        public string Name;

        private enum TransCommandTypeEnum
        {
            //WITHDRAW_REQUIRE = 1,
            //DEPOSIT_REQUIRE = 2,
            REQUIRE = 1,
            COMMIT = 3,
            CANCEL = 4,

            UNKNOWN = 9999
        }

        private enum RecordStateEnum
        {
            InProc = 1,
            Completed = 2,
            Cancelled = 3
        }

        private enum RecordTypeEnum
        {
            DEPOSIT = 1,
            WITHDRAW = 2
        }

        private class TransCommand
        {
            public long SN;
            public long FromSN;
            public long TransID;
            public TransCommandTypeEnum Command = TransCommandTypeEnum.UNKNOWN;
            public decimal Amount;
            public DateTime CommandTime;
            public string Notes;
        }

        private class RecordItem
        {
            public long SN;
            public long TransID;
            public decimal DepositAmount;
            public decimal WithdrawAmount;
            //public RecordTypeEnum RecType;
            public RecordStateEnum State;
            public string Notes;
            public DateTime CreateTime;
            public DateTime UpdateTime;
        }


        #region event store part
        private long _seed_for_SN = 0;
        private List<TransCommand> _event_store = new List<TransCommand>();
        #endregion

        #region projection part
        private decimal _balance;
        private decimal _inprocess_deposit;
        private decimal _inprocess_withdraw;
        //private Dictionary<long, RecordItem> _records = new Dictionary<long, RecordItem>();
        private List<RecordItem> _records = new List<RecordItem>();
        #endregion



        public long TransCancel(long transID, long fromSN = 0)
        {
            var cmd = new TransCommand()
            {
                SN = Interlocked.Increment(ref this._seed_for_SN),
                FromSN = fromSN,
                TransID = transID,
                Command = TransCommandTypeEnum.CANCEL,
                CommandTime = DateTime.Now,
                Notes = "--"
            };

            this.TransCommandHandler(cmd);

            return cmd.SN;
        }

        public long TransCommit(long transID, long fromSN = 0)
        {
            var cmd = new TransCommand()
            {
                SN = Interlocked.Increment(ref this._seed_for_SN),
                FromSN = fromSN,
                TransID = transID,
                Command = TransCommandTypeEnum.COMMIT,
                CommandTime = DateTime.Now,
                Notes = "--"
            };

            this.TransCommandHandler(cmd);

            return cmd.SN;
        }

        public long TransRequire(long transID, decimal amount)
        {
            var cmd = new TransCommand()
            {
                SN = Interlocked.Increment(ref this._seed_for_SN),
                FromSN = 0,
                TransID = transID,
                Amount = amount,
                Command = TransCommandTypeEnum.REQUIRE,
                CommandTime = DateTime.Now,
                Notes = "--"
            };

            if (this.TransCommandHandler(cmd) == false) throw new InvalidOperationException();

            return cmd.SN;
        }



        public (decimal available, decimal processing) GetBalance()
        {
            return (this._balance, this._inprocess_deposit);
        }


        private bool TransCommandHandler(TransCommand cmd)
        {
            switch (cmd.Command)
            {
                case TransCommandTypeEnum.REQUIRE:
                    {
                        var rec = new RecordItem()
                        {
                            DepositAmount = (cmd.Amount > 0) ? (cmd.Amount) : (0),
                            WithdrawAmount = (cmd.Amount > 0) ? (0) : (0 - cmd.Amount),
                            CreateTime = cmd.CommandTime,
                            UpdateTime = cmd.CommandTime,
                            Notes = cmd.Notes,
                            SN = cmd.SN,
                            State = RecordStateEnum.InProc,
                            TransID = cmd.TransID
                        };

                        this._event_store.Add(cmd);
                        this._records.Add(rec);

                        this._inprocess_deposit += rec.DepositAmount;
                        this._inprocess_withdraw += rec.WithdrawAmount;
                        this._balance -= rec.WithdrawAmount;
                    }
                    break;


                case TransCommandTypeEnum.COMMIT:
                    {
                        this._event_store.Add(cmd);

                        foreach(var rec in (from x in this._records where x.TransID == cmd.TransID select x))
                        {
                            if (cmd.FromSN > 0 && cmd.FromSN != rec.SN) throw new InvalidOperationException();
                            if (cmd.TransID != rec.TransID) throw new InvalidProgramException();

                            rec.State = RecordStateEnum.Completed;
                            rec.Notes = cmd.Notes;
                            rec.SN = cmd.SN;
                            rec.UpdateTime = cmd.CommandTime;

                            this._balance += rec.DepositAmount;
                            this._inprocess_deposit -= rec.DepositAmount;
                            this._inprocess_withdraw -= rec.WithdrawAmount;
                        }
                    }
                    break;

                case TransCommandTypeEnum.CANCEL:
                    {
                        this._event_store.Add(cmd);

                        foreach (var rec in (from x in this._records where x.TransID == cmd.TransID select x))
                        {
                            if (cmd.FromSN > 0 && cmd.FromSN != rec.SN) throw new InvalidOperationException();
                            if (cmd.TransID != rec.TransID) throw new InvalidProgramException();

                            rec.State = RecordStateEnum.Cancelled;
                            rec.Notes = cmd.Notes;
                            rec.SN = cmd.SN;
                            rec.UpdateTime = cmd.CommandTime;

                            this._balance += rec.WithdrawAmount;
                            this._inprocess_deposit -= rec.DepositAmount;
                            this._inprocess_withdraw -= rec.WithdrawAmount;
                        }
                    }
                    break;
            }


            this.Dump();

            // check risk rules
            if (this._balance < 0) return false;
            if (this._balance > 10000) return false;

            return true;
        }

        public void Dump()
        {
            Console.WriteLine($"Account Records ({this.Name}):");
            Console.WriteLine($"SN\tTransID\tDeposit\tWithdraw\tState\tNotes");
            foreach(var rec in this._records)
            {
                Console.WriteLine($"#{rec.SN}\t{rec.TransID}\t{rec.DepositAmount}\t{rec.WithdrawAmount}\t{rec.State}\t{rec.Notes}");
            }

            Console.WriteLine();
            Console.WriteLine($"- Balance: {this._balance}, TempDeposit: {this._inprocess_deposit}, TempWithdraw: {this._inprocess_withdraw}");

            Console.WriteLine();
            Console.WriteLine();
        }
    }
}
