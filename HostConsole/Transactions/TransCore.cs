using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HostConsole.Transactions
{



    public class TransCore
    {
        public TransCore(string service_type, string owner_id)
        {

        }


        public void TransRequire()
        {
            // 1. load contracts
            // 2. resolve all related bank accounts
            // 3. resolve bank commands
            // 4. evaluate risk
            // 5. execute (3) commands, return execute result
            
            // 6. (next step) commit or rollback transaction
        }
    }
}
