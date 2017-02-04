using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Recurly.Test
{
    public class TransactionListTest : BaseTest
    {
        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListAllTransactions()
        {
            for (var x = 0; x < 5; x++)
            {
                var account = await CreateNewAccountWithBillingInfoAsync();
                var transaction = new Transaction(account.AccountCode, 3000 + x, "USD");
                transaction.CreateAsync();
            }

            var transactions = Transactions.List();
            transactions.Should().NotBeEmpty();
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListSuccessfulTransactions()
        {
            for (var x = 0; x < 2; x++)
            {
                var account = await CreateNewAccountWithBillingInfoAsync();
                var transaction = new Transaction(account.AccountCode, 3000 + x, "USD");
                transaction.CreateAsync();
            }

            var transactions = Transactions.List(TransactionList.TransactionState.Successful);
            transactions.Should().NotBeEmpty();
        }

        [Fact(Skip = "This feature is deprecated and no longer supported for accounts where line item refunds are turned on.")]
        public async Task ListVoidedTransactions()
        {
            for (var x = 0; x < 2; x++)
            {
                var account = await CreateNewAccountWithBillingInfoAsync();
                var transaction = new Transaction(account.AccountCode, 3000 + x, "USD");
                transaction.CreateAsync();

                transaction.RefundAsync();


            }

            var list = Transactions.List(TransactionList.TransactionState.Voided);
            list.Should().NotBeEmpty();
        }

        [Fact(Skip = "This feature is deprecated and no longer supported for accounts where line item refunds are turned on.")]
        public async Task ListRefundedTransactions()
        {
            for (var x = 0; x < 2; x++)
            {
                var account = await CreateNewAccountWithBillingInfoAsync();
                var transaction = new Transaction(account.AccountCode, 3000 + x, "USD");
                transaction.CreateAsync();
                transaction.RefundAsync(1500);
            }

            var list = Transactions.List(type: TransactionList.TransactionType.Refund);
            list.Should().NotBeEmpty();
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListTransactionsForAccount()
        {
            var account = await CreateNewAccountWithBillingInfoAsync();

            var transaction1 = new Transaction(account.AccountCode, 3000, "USD");
            transaction1.CreateAsync();

            var transaction2 = new Transaction(account.AccountCode, 200, "USD");
            transaction2.CreateAsync();

            var list = account.GetTransactions();
            list.Should().NotBeEmpty();
        }
    }
}
