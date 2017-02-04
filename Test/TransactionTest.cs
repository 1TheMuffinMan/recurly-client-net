﻿using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Recurly.Test
{
    public class TransactionTest : BaseTest
    {
        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task LookupTransaction()
        {
            var acct = await CreateNewAccountWithBillingInfoAsync();
            var transaction = new Transaction(acct, 5000, "USD");
            transaction.CreateAsync();

            var fromService = Transactions.Get(transaction.Uuid);

            transaction.Should().Be(fromService);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task CreateTransactionNewAccount()
        {
            var account = NewAccountWithBillingInfo();
            var transaction = new Transaction(account, 5000, "USD");
            transaction.Description = "Description";

            transaction.CreateAsync();

            transaction.CreatedAt.Should().NotBe(default(DateTime));
            
            var fromService = await Transactions.Get(transaction.Uuid);
            var invoice = await fromService.GetInvoice();
            var line_items = invoice.Adjustments;
            
            line_items[0].Description.Should().Be(transaction.Description);
            
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task CreateTransactionExistingAccount()
        {
            var acct = await CreateNewAccountWithBillingInfoAsync();
            var transaction = new Transaction(acct.AccountCode, 3000, "USD");

            transaction.CreateAsync();

            transaction.CreatedAt.Should().NotBe(default(DateTime));
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public void CreateTransactionExistingAccountNewBillingInfo()
        {
            var account = new Account(GetUniqueAccountCode())
            {
                FirstName = "John",
                LastName = "Smith"
            };
            account.CreateAsync();
            account.BillingInfo = NewBillingInfo(account);
            var transaction = new Transaction(account, 5000, "USD");

            transaction.CreateAsync();

            transaction.CreatedAt.Should().NotBe(default(DateTime));
        }

        [Fact(Skip = "This feature is deprecated and no longer supported for accounts where line item refunds are turned on.")]
        public void RefundTransactionFull()
        {
            var acct = NewAccountWithBillingInfo();
            var transaction = new Transaction(acct, 5000, "USD");
            transaction.CreateAsync();

            transaction.RefundAsync();

            transaction.Status.Should().Be(Transaction.TransactionState.Voided);
        }

        [Fact(Skip = "This feature is deprecated and no longer supported for accounts where line item refunds are turned on.")]
        public void RefundTransactionPartial()
        {
            var account = NewAccountWithBillingInfo();
            var transaction = new Transaction(account, 5000, "USD");
            transaction.CreateAsync();

            transaction.RefundAsync(2500);

            account.GetTransactions().Should().HaveCount(2);
        }

    }
}
