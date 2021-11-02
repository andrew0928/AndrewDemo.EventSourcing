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
}
