using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Recurly.Test
{
    public class InvoiceTest : BaseTest
    {
        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task GetInvoice()
        {
            var account = await CreateNewAccountWithBillingInfoAsync();

            var adjustment = account.NewAdjustment("USD", 5000, "Test Charge");
            adjustment.CreateAsync();

            var invoice = account.InvoicePendingCharges();
            Assert.Equal("usst", invoice.TaxType);
            Assert.Equal(0.0875M, invoice.TaxRate.Value);

            var fromService = await Invoices.GetAsync(invoice.InvoiceNumber);

            invoice.Should().Be(fromService);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task GetInvoicePdf()
        {
            var account = await CreateNewAccountAsync();

            var adjustment = account.NewAdjustment("USD", 5000, "Test Charge");
            adjustment.CreateAsync();

            var invoice = account.InvoicePendingCharges();

            var pdf = await invoice.GetPdfAsync();

            pdf.Should().NotBeEmpty();
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task AdjustmentAggregationInAnInvoice()
        {
            var account = await CreateNewAccountAsync();

            var adjustment = account.NewAdjustment("USD", 5000, "Test Charge");
            adjustment.CreateAsync();

            adjustment = account.NewAdjustment("USD", 5000, "Test Charge 2");
            adjustment.CreateAsync();

            adjustment = account.NewAdjustment("USD", -2500, "Test Credit");
            adjustment.CreateAsync();

            var invoice = account.InvoicePendingCharges();

            invoice.State.Should().Be(Invoice.InvoiceState.Open);
            invoice.TotalInCents.Should().Be(7500);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task MarkSuccessful()
        {
            var account = await CreateNewAccountAsync();

            var adjustment = account.NewAdjustment("USD", 3999, "Test Charge");
            adjustment.CreateAsync();

            var invoice = account.InvoicePendingCharges();

            invoice.MarkSuccessfulAsync();

            Assert.Equal(1, invoice.Adjustments.Count);

            invoice.State.Should().Be(Invoice.InvoiceState.Collected);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task FailedCollection()
        {
            var account = await CreateNewAccountWithBillingInfoAsync();

            var adjustment = account.NewAdjustment("USD", 3999, "Test Charge");
            adjustment.CreateAsync();

            var invoice = account.InvoicePendingCharges();
            invoice.MarkFailedAsync();
            invoice.State.Should().Be(Invoice.InvoiceState.Failed);
            Assert.NotNull(invoice.ClosedAt);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task RefundSingle()
        {
            var account = await CreateNewAccountWithBillingInfoAsync();

            var adjustment = account.NewAdjustment("USD", 3999, "Test Charge");
            adjustment.CreateAsync();

            var invoice = account.InvoicePendingCharges();

            invoice.MarkSuccessfulAsync();

            invoice.State.Should().Be(Invoice.InvoiceState.Collected);

            Assert.Equal(1, invoice.Adjustments.Count);
            Assert.Equal(1, invoice.Adjustments.Capacity);

            // refund
            var refundInvoice = await invoice.RefundAsync(adjustment, false);
            Assert.NotEqual(invoice.Uuid, refundInvoice.Uuid);
            Assert.Equal(-3999, refundInvoice.SubtotalInCents);
            Assert.Equal(1, refundInvoice.Adjustments.Count);
            Assert.Equal(-1, refundInvoice.Adjustments[0].Quantity);
            Assert.Equal(0, refundInvoice.Transactions.Count);

            await account.CloseAsync();
        }

        [Fact(Skip = "This feature is deprecated and no longer supported for accounts where line item refunds are turned on.")]
        public async Task RefundMultiple()
        {
            var account = await CreateNewAccountWithBillingInfoAsync();

            var adjustment1 = account.NewAdjustment("USD", 1, "Test Charge 1");
            adjustment1.CreateAsync();

            var adjustment2 = account.NewAdjustment("USD", 2, "Test Charge 2", 2);
            adjustment2.CreateAsync();

            var invoice = account.InvoicePendingCharges();
            invoice.MarkSuccessfulAsync();

            System.Threading.Thread.Sleep(2000); // hack

            Assert.Equal(2, invoice.Adjustments.Count);
            Assert.Equal(1, invoice.Transactions.Count);
            Assert.Equal(7, invoice.Transactions[0].AmountInCents);

            // refund
            var refundInvoice = await invoice.RefundAsync(invoice.Adjustments);
            Assert.NotEqual(invoice.Uuid, refundInvoice.Uuid);
            Assert.Equal(-5, refundInvoice.SubtotalInCents);
            Assert.Equal(2, refundInvoice.Adjustments.Count);
            Assert.Equal(-1, refundInvoice.Adjustments[0].Quantity);
            Assert.Equal(-2, refundInvoice.Adjustments[1].Quantity);
            Assert.Equal(1, refundInvoice.Transactions.Count);
            Assert.Equal(5, refundInvoice.Transactions[0].AmountInCents);

            await account.CloseAsync();
        }


        [Fact(Skip = "This feature is deprecated and no longer supported for accounts where line item refunds are turned on.")]
        public async Task RefundOpenAmount()
        {
            var account = await CreateNewAccountWithBillingInfoAsync();

            var adjustment = account.NewAdjustment("USD", 3999, "Test Charge");
            adjustment.CreateAsync();

            var invoice = account.InvoicePendingCharges();

            invoice.MarkSuccessfulAsync();

            invoice.State.Should().Be(Invoice.InvoiceState.Collected);

            Assert.Equal(1, invoice.Adjustments.Count);
            Assert.Equal(1, invoice.Adjustments.Capacity);

            // refund
            var refundInvoice = invoice.RefundAmount(100); // 1 dollar
            Assert.NotEqual(invoice.Uuid, refundInvoice.Uuid);
            Assert.Equal(-91, refundInvoice.SubtotalInCents);  // 91 cents

            await account.CloseAsync();
        }
    }
}
