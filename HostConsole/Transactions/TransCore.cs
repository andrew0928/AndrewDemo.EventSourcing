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
        // references: https://stripe.com/docs/api/payment_intents

        // Balance Transactions:
        // Balance transactions represent funds moving through your Stripe account. They're created for every type of transaction that comes into or flows out of your Stripe account balance.

        // Charges
        // to charge a credit or a debit card, you create a charge object.

        // Disputes
        // occurs when a customer questions your charge with their card issuer.

        // Payment Intents:
        // guides you through the process of collection a payment from your customer.

        // Setup Intents:
        // guides you through the process of setting up and saving a customer's payment credentials for future payments.

        // Setup Attempts
        // describes one attempted confirmation of a setup intent, whether that confirmation was successful or unsuccessful.

        // Payouts
        // when you receive funds from Stripe, or when you initiate a payout to either a bank account or debit card of a connected stripe account.

        // Refunds
        // allow you to refund a charge that has previously been created but not yet refunded.

        // Tokens
        // tokenization is the process stripe users to collect sensitive card or bank account details, or personally identifiable information (PII) , directly from your customers in a secure manner.

        
        // public command: payment intents, payouts
        // private command: balance transactions, charges

        // not in this time: disputes, setup intents, refunds, tokens (in payment gateway, not in payment core)



        public void CreatePaymentIntents()
        {
            // 1. 按照合約，扣掉金流手續費
            // 2. 轉移餘額到指定帳戶
        }

        public void CreatePayouts()
        {
            // 1. 按照合約，扣掉金流手續費
            // 2. 轉移金額到指定外部帳戶
        }

        // only for 91APP admin
        public void CreateBalanceTransactions()
        {
            // 1. 內部轉帳
        }

        // only for billing system
        public void CreateCharges()
        {
            // 1. 按照帳單，內部請款 (整批進行)
        }
    }
}
