using System;
using System.Collections.Generic;
using System.Threading;

namespace HostConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var acc = new InMemoryAccount();

            acc.TransRequire(100, 1000);
            acc.Dump();

            acc.TransRequire(200, 5000);
            acc.Dump();

            acc.TransRequire(300, 1000);
            acc.Dump();


            acc.TransCommit(200);
            acc.Dump();
            
            acc.TransCancel(300);
            acc.Dump();
            
            acc.TransCommit(100);
            acc.Dump();
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
        private enum TransCommandTypeEnum
        {
            REQUIRE = 1,
            COMMIT = 2,
            CANCEL = 3
        }

        private enum RecordStateEnum
        {
            InProc = 1,
            Completed = 2,
            Cancelled = 3
        }

        private class TransCommand
        {
            public long SN;
            public long FromSN;
            public long TransID;
            public TransCommandTypeEnum Command = TransCommandTypeEnum.REQUIRE;
            public decimal Amount;
            public DateTime CommandTime;
            public string Notes;
        }

        private class RecordItem
        {
            public long SN;
            public long TransID;
            public decimal Amount;
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
        private decimal _available_balance;
        private decimal _inprocess_balance;
        private Dictionary<long, RecordItem> _records = new Dictionary<long, RecordItem>();
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

            this.TransCommandHandler(cmd);

            return cmd.SN;
        }



        public (decimal available, decimal processing) GetBalance()
        {
            return (this._available_balance, this._inprocess_balance);
        }


        private bool TransCommandHandler(TransCommand cmd)
        {
            switch (cmd.Command)
            {
                case TransCommandTypeEnum.REQUIRE:
                    {
                        this._event_store.Add(cmd);

                        this._records.Add(cmd.TransID, new RecordItem()
                        {
                            Amount = cmd.Amount,
                            CreateTime = cmd.CommandTime,
                            UpdateTime = cmd.CommandTime,
                            Notes = cmd.Notes,
                            SN = cmd.SN,
                            State = RecordStateEnum.InProc,
                            TransID = cmd.TransID
                        });

                        this._inprocess_balance += cmd.Amount;
                    }
                    break;

                case TransCommandTypeEnum.COMMIT:
                    {
                        this._event_store.Add(cmd);

                        var rec = this._records[cmd.TransID];
                        if (cmd.FromSN > 0 && cmd.FromSN != rec.SN) throw new InvalidOperationException();
                        if (cmd.TransID != rec.TransID) throw new InvalidProgramException();

                        rec.State = RecordStateEnum.Completed;
                        rec.Notes = cmd.Notes;
                        rec.SN = cmd.SN;
                        rec.UpdateTime = cmd.CommandTime;

                        this._inprocess_balance -= rec.Amount;
                        this._available_balance += rec.Amount;
                    }
                    break;

                case TransCommandTypeEnum.CANCEL:
                    {
                        this._event_store.Add(cmd);

                        var rec = this._records[cmd.TransID];
                        if (cmd.FromSN > 0 && cmd.FromSN != rec.SN) throw new InvalidOperationException();
                        if (cmd.TransID != rec.TransID) throw new InvalidProgramException();

                        rec.State = RecordStateEnum.Cancelled;
                        rec.Notes = cmd.Notes;
                        rec.SN = cmd.SN;
                        rec.UpdateTime = cmd.CommandTime;

                        this._inprocess_balance -= rec.Amount;
                    }
                    break;
            }

            return true;
        }

        public void Dump()
        {
            Console.WriteLine("Account Records:");
            Console.WriteLine("SN\tTransID\tAmount\tState\tNotes");
            foreach(var rec in this._records.Values)
            {
                Console.WriteLine($"#{rec.SN}\t{rec.TransID}\t{rec.Amount}\t{rec.State}\t{rec.Notes}");
            }

            Console.WriteLine();
            Console.WriteLine($"Balance:");
            Console.WriteLine($"- Available: {this._available_balance}, InProcessing: {this._inprocess_balance}");

            Console.WriteLine();
            Console.WriteLine();
        }
    }
}
