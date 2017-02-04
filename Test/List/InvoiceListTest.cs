using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Recurly.Test
{
    public class InvoiceListTest : BaseTest
    {
        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task GetInvoices()
        {
            for (var x = 0; x < 6; x++)
            {
                var acct = await CreateNewAccountAsync();

                var adjustment = acct.NewAdjustment("USD", 500 + x, "Test Charge");
                adjustment.CreateAsync();

                var invoice = acct.InvoicePendingCharges();

                if (x < 2)
                {
                    // leave open
                }
                else if (x == 3 || x == 4)
                {
                    invoice.MarkFailedAsync();
                }
                else
                {
                    invoice.MarkSuccessfulAsync();
                }
            }

            var list = Invoices.List();
            list.Should().NotBeEmpty();
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task GetOpenInvoices()
        {
            for (var x = 0; x < 2; x++)
            {
                var account = await CreateNewAccountAsync();
                var adjustment = account.NewAdjustment("USD", 500 + x, "Test Charge");
                adjustment.CreateAsync();
                account.InvoicePendingCharges();
            }

            var list = Invoices.List(Invoice.InvoiceState.Open);
            list.Should().NotBeEmpty();
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task GetCollectedInvoices()
        {
            for (var x = 0; x < 2; x++)
            {
                var acct = await CreateNewAccountAsync();
                var adjustment = acct.NewAdjustment("USD", 500 + x, "Test Charge");
                adjustment.CreateAsync();
                var invoice = acct.InvoicePendingCharges();
                invoice.MarkSuccessfulAsync();
            }

            var list = Invoices.List(Invoice.InvoiceState.Collected);
            list.Should().NotBeEmpty();
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task GetFailedInvoices()
        {
            for (var x = 0; x < 2; x++)
            {
                var acct = await CreateNewAccountAsync();
                var adjustment = acct.NewAdjustment("USD", 500 + x, "Test Charge");
                adjustment.CreateAsync();
                var invoice = acct.InvoicePendingCharges();
                invoice.MarkFailedAsync();
            }

            var list = Invoices.List(Invoice.InvoiceState.Failed);
            list.Should().NotBeEmpty();
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task GetPastDueInvoices()
        {
            for (var x = 0; x < 2; x++)
            {
                var acct = await CreateNewAccountAsync();
                var adjustment = acct.NewAdjustment("USD", 500 + x, "Test charge");
                adjustment.CreateAsync();
                acct.InvoicePendingCharges();
            }

            var list = Invoices.List(Invoice.InvoiceState.PastDue);
            list.Should().NotBeEmpty();
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task GetProcessingInvoices()
        {
            var account = await CreateNewAccountWithACHBillingInfo();
            var adjustment = account.NewAdjustment("USD", 510, "ACH invoice test");
            adjustment.CreateAsync();
            account.InvoicePendingCharges();

            //The invoice starts out as open and then changes to processing 
            //so we need to wait shortly to experience that
            System.Threading.Thread.Sleep(1500);

            var list = Invoices.List(account.AccountCode);
            list.Should().NotBeEmpty();
            Assert.Equal(1, list.Count);
            Assert.True(list[0].State == Invoice.InvoiceState.Processing);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task GetInvoicesForAccount()
        {
            var account = await CreateNewAccountWithBillingInfoAsync();

            var adjustment = account.NewAdjustment("USD", 450, "Test Charge #1");
            adjustment.CreateAsync();

            var invoice = account.InvoicePendingCharges();
            invoice.MarkSuccessfulAsync();

            adjustment = account.NewAdjustment("USD", 350, "Test Charge #2");
            adjustment.CreateAsync();

            invoice = account.InvoicePendingCharges();
            invoice.MarkFailedAsync();

            var list = Invoices.List(account.AccountCode);
            Assert.Equal(2, list.Count);
        }
    }
}
