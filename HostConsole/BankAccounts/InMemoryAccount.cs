using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace HostConsole.BankAccounts
{
    public class InMemoryAccount : IBankAccount
    {
        public string Name;

        public (string type, string id)[] AccountIdentities;
        // owner type:  consumer / service / payment / client
        // owner id:




        #region event store part
        private long _seed_for_SN = 0;
        private List<BankAccountCommand> _event_store = new List<BankAccountCommand>();
        #endregion

        #region projection part
        private decimal _balance;
        private decimal _inprocess_deposit;
        private decimal _inprocess_withdraw;
        private List<RecordItem> _records = new List<RecordItem>();
        #endregion



        public long TransCancel(long transID, long fromSN = 0)
        {
            var cmd = new BankAccountCommand()
            {
                SN = Interlocked.Increment(ref this._seed_for_SN),
                FromSN = fromSN,
                TransID = transID,
                Command = BankAccountCommandTypeEnum.CANCEL,
                CommandTime = DateTime.Now,
                Notes = "--"
            };

            this.TransCommandHandler(cmd);

            return cmd.SN;
        }

        public long TransCommit(long transID, long fromSN = 0)
        {
            var cmd = new BankAccountCommand()
            {
                SN = Interlocked.Increment(ref this._seed_for_SN),
                FromSN = fromSN,
                TransID = transID,
                Command = BankAccountCommandTypeEnum.COMMIT,
                CommandTime = DateTime.Now,
                Notes = "--"
            };

            this.TransCommandHandler(cmd);

            return cmd.SN;
        }

        public long TransIntent(long transID, decimal amount, string notes = null)
        {
            var cmd = new BankAccountCommand()
            {
                SN = Interlocked.Increment(ref this._seed_for_SN),
                FromSN = 0,
                TransID = transID,
                Amount = amount,
                Command = BankAccountCommandTypeEnum.INTENT,
                CommandTime = DateTime.Now,
                Notes = notes
            };

            if (this.TransCommandHandler(cmd) == false) throw new InvalidOperationException();

            return cmd.SN;
        }



        public (decimal available, decimal processing) GetBalance()
        {
            return (this._balance, this._inprocess_deposit);
        }


        private bool TransCommandHandler(BankAccountCommand cmd)
        {
            // todo: 如果存在任一筆 trans id 相同的 record, 但是狀態不是 InProc, 代表交易出現問題。

            switch (cmd.Command)
            {
                case BankAccountCommandTypeEnum.INTENT:
                    {
                        var rec = new RecordItem()
                        {
                            DepositAmount = (cmd.Amount > 0) ? (cmd.Amount) : (0),
                            WithdrawAmount = (cmd.Amount > 0) ? (0) : (0 - cmd.Amount),
                            CreateTime = cmd.CommandTime,
                            UpdateTime = cmd.CommandTime,
                            Notes = cmd.Notes ?? "--",
                            SN = cmd.SN,
                            State = BankAccountRecordStateEnum.InProc,
                            TransID = cmd.TransID
                        };

                        this._event_store.Add(cmd);
                        this._records.Add(rec);

                        this._inprocess_deposit += rec.DepositAmount;
                        this._inprocess_withdraw += rec.WithdrawAmount;
                        this._balance -= rec.WithdrawAmount;
                    }
                    break;


                case BankAccountCommandTypeEnum.COMMIT:
                    {
                        this._event_store.Add(cmd);

                        foreach(var rec in (from x in this._records where x.TransID == cmd.TransID select x))
                        {
                            if (cmd.FromSN > 0 && cmd.FromSN != rec.SN) throw new InvalidOperationException();
                            if (cmd.TransID != rec.TransID) throw new InvalidProgramException();

                            rec.State = BankAccountRecordStateEnum.Completed;
                            rec.Notes = cmd.Notes;
                            rec.SN = cmd.SN;
                            rec.UpdateTime = cmd.CommandTime;

                            this._balance += rec.DepositAmount;
                            this._inprocess_deposit -= rec.DepositAmount;
                            this._inprocess_withdraw -= rec.WithdrawAmount;
                        }
                    }
                    break;

                case BankAccountCommandTypeEnum.CANCEL:
                    {
                        this._event_store.Add(cmd);

                        foreach (var rec in (from x in this._records where x.TransID == cmd.TransID select x))
                        {
                            if (cmd.FromSN > 0 && cmd.FromSN != rec.SN) throw new InvalidOperationException();
                            if (cmd.TransID != rec.TransID) throw new InvalidProgramException();

                            rec.State = BankAccountRecordStateEnum.Cancelled;
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
            Console.WriteLine($"SN        TransID   Deposit   Withdraw  State     Notes");
            Console.WriteLine($"--------- --------- --------- --------- --------- ----------------------------------");
            foreach (var rec in this._records)
            {
                Console.WriteLine($"#{rec.SN.ToString().PadRight(8, ' ')} {rec.TransID}\t{rec.DepositAmount}\t{rec.WithdrawAmount}\t{rec.State}\t{rec.Notes}");
            }

            Console.WriteLine();
            Console.WriteLine($"- Balance: {this._balance}, TempDeposit: {this._inprocess_deposit}, TempWithdraw: {this._inprocess_withdraw}");

            Console.WriteLine();
            Console.WriteLine();
        }
    }
}
