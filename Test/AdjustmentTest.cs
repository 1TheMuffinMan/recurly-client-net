using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Recurly.Test
{
    public class AdjustmentTest : BaseTest
    {

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task CreateAdjustment()
        {
            var account = await CreateNewAccountAsync();

            string desc = "Charge";
            var adjustment = account.NewAdjustment("USD", 5000, desc);

            await adjustment.CreateAsync();

            adjustment.CreatedAt.Should().NotBe(default(DateTime));
            Assert.False(adjustment.TaxExempt);
            Assert.Equal(desc, adjustment.Description);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task CreateAdjustmentWithProperties()
        {
            var account = await CreateNewAccountAsync();
            string desc = "my description";
            string accountingCode = "accountng code";
            string currency = "USD";
            int unitAmountInCents = 5000;
            int quantity = 2;

            var adjustment = account.NewAdjustment("ABC", 1000);
            adjustment.TaxExempt = true;
            adjustment.Description = desc;
            adjustment.Currency = currency;
            adjustment.Quantity = quantity;
            adjustment.AccountingCode = accountingCode;
            adjustment.UnitAmountInCents = unitAmountInCents;

            await adjustment.CreateAsync();

            adjustment.CreatedAt.Should().NotBe(default(DateTime));
            Assert.True(adjustment.TaxExempt);
            Assert.Equal(desc, adjustment.Description);
            Assert.Equal(currency, adjustment.Currency);
            Assert.Equal(quantity, adjustment.Quantity);
            Assert.Equal(accountingCode, adjustment.AccountingCode);
            Assert.Equal(unitAmountInCents, adjustment.UnitAmountInCents);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListAdjustments()
        {
            var account = await CreateNewAccountAsync();

            var adjustment = account.NewAdjustment("USD", 5000, "Charge", 1);
            await adjustment.CreateAsync();

            adjustment = account.NewAdjustment("USD", -1492, "Credit", 1);
            await adjustment.CreateAsync();

            account.InvoicePendingCharges();

            var adjustments = await account.GetAdjustmentsAsync();
            adjustments.Should().HaveCount(2);
        }

        /// <summary>
        /// This test will return two adjustments: one to negate the charge, the 
        /// other for the balance
        /// </summary>
        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListAdjustmentsOverCredit()
        {
            var account = await CreateNewAccountAsync();

            var adjustment = account.NewAdjustment("USD", 1234, "Charge", 1);
            await adjustment.CreateAsync();

            adjustment = account.NewAdjustment("USD", -5678, "Credit");
            await adjustment.CreateAsync();

            account.InvoicePendingCharges();

            var adjustments = await account.GetAdjustmentsAsync(Adjustment.AdjustmentType.Credit);
            adjustments.Should().HaveCount(2);

            var sum = adjustments[0].UnitAmountInCents + adjustments[1].UnitAmountInCents;
            sum.Should().Be(-10122);
        }


        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListAdjustmentsCredits()
        {
            var account = await CreateNewAccountAsync();

            var adjustment = account.NewAdjustment("USD", 3456, "Charge", 1);
            await adjustment.CreateAsync();

            adjustment = account.NewAdjustment("USD", -3456, "Charge", 1);
            await adjustment.CreateAsync();

            var adjustments = await account.GetAdjustmentsAsync(Adjustment.AdjustmentType.Credit);
            adjustments.Should().HaveCount(1);
            adjustments.Should().Contain(x => x.UnitAmountInCents == -3456);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListAdjustmentsCharges()
        {
            var account = await CreateNewAccountAsync();

            var adjustment = account.NewAdjustment("USD", 1234);
            await adjustment.CreateAsync();

            adjustment = account.NewAdjustment("USD", -5678, "list adjustments", 1);
            await adjustment.CreateAsync();

            account.InvoicePendingCharges();

            var adjustments = await account.GetAdjustmentsAsync(Adjustment.AdjustmentType.Charge);
            adjustments.Should().HaveCount(2);
            adjustments.Should().Contain(x => x.UnitAmountInCents == 1234);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListAdjustmentsPendingToInvoiced()
        {
            var account = await CreateNewAccountAsync();

            var adjustment = account.NewAdjustment("USD", 1234);
            await adjustment.CreateAsync();

            adjustment = account.NewAdjustment("USD", -5678, "");
            await adjustment.CreateAsync();


            var adjustments = await account.GetAdjustmentsAsync(state: Adjustment.AdjustmentState.Pending);
            adjustments.Should().HaveCount(2);

            account.InvoicePendingCharges();

            adjustments = await account.GetAdjustmentsAsync(state: Adjustment.AdjustmentState.Invoiced);
            adjustments.Should().HaveCount(3);
            adjustments.Should().OnlyContain(x => x.State == Adjustment.AdjustmentState.Invoiced);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task AdjustmentGet()
        {
            var account = await CreateNewAccountWithBillingInfoAsync();

            var adjustment = account.NewAdjustment("USD", 1234);
            await adjustment.CreateAsync();

            adjustment.Uuid.Should().NotBeNullOrEmpty();

            var fromService = Adjustments.Get(adjustment.Uuid);

            fromService.Uuid.Should().NotBeNull();
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task AdjustmentDelete()
        {
            var account = await CreateNewAccountWithBillingInfoAsync();

            var adjustment = account.NewAdjustment("USD", 1234);
            await adjustment.CreateAsync();

            adjustment.Uuid.Should().NotBeNullOrEmpty();

            await adjustment.DeleteAsync();

            Action get = () => Adjustments.Get(adjustment.Uuid);
            get.ShouldThrow<NotFoundException>();
        }
    }
}