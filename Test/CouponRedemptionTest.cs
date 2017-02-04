using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Recurly.Test
{
    public class CouponRedemptionTest : BaseTest
    {
        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task RedeemCoupon()
        {
            var coupon = new Coupon(GetMockCouponCode(), GetMockCouponName(), 10);
            await coupon.CreateAsync();

            var account = await CreateNewAccountAsync();
            account.CreatedAt.Should().NotBe(default(DateTime));

            var redemption = await account.RedeemCouponAsync(coupon.CouponCode, "USD");

            redemption.Should().NotBeNull();
            redemption.Currency.Should().Be("USD");
            redemption.AccountCode.Should().Be(account.AccountCode);
            redemption.CreatedAt.Should().NotBe(default(DateTime));
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task LookupRedemption()
        {
            var coupon = new Coupon(GetMockCouponCode(), GetMockCouponName(), 10);
            await coupon.CreateAsync();

            var account = await CreateNewAccountAsync();
            account.CreatedAt.Should().NotBe(default(DateTime));

            var redemption = await account.RedeemCouponAsync(coupon.CouponCode, "USD");
            redemption.Should().NotBeNull();

            redemption = await account.GetActiveRedemptionAsync();
            redemption.CouponCode.Should().Be(coupon.CouponCode);
            redemption.AccountCode.Should().Be(account.AccountCode);
            redemption.CreatedAt.Should().NotBe(default(DateTime));

        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task RemoveCoupon()
        {
            var coupon = new Coupon(GetMockCouponCode(), GetMockCouponName(), 10);
            await coupon.CreateAsync();

            var account = await CreateNewAccountAsync();
            account.CreatedAt.Should().NotBe(default(DateTime));

            var redemption = await account.RedeemCouponAsync(coupon.CouponCode, "USD");
            redemption.Should().NotBeNull();

            await redemption.DeleteAsync();

            var activeRedemption = await account.GetActiveRedemptionAsync();
            activeRedemption.Should().Be(null);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task LookupCouponInvoice()
        {
            var discounts = new Dictionary<string, int> { { "USD", 1000 } };
            var coupon = new Coupon(GetMockCouponCode(), GetMockCouponName(), discounts);
            await coupon.CreateAsync();

            var plan = new Plan(GetMockPlanCode(), GetMockPlanCode())
            {
                Description = "Test Lookup Coupon Invoice"
            };
            plan.UnitAmountInCents.Add("USD", 1500);
            await plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            var account = await CreateNewAccountWithBillingInfoAsync();

            var redemption = await account.RedeemCouponAsync(coupon.CouponCode, "USD");

            var sub = new Subscription(account, plan, "USD", coupon.CouponCode);
            await sub.CreateAsync();

            // TODO complete this test

            var invoices = account.GetInvoices();

            invoices.Should().NotBeEmpty();

            var invoice = await Invoices.GetAsync(invoices.First().InvoiceNumber);
            var fromInvoice = await invoice.GetRedemptionAsync();

            redemption.Should().Be(fromInvoice);
        }

    }
}
