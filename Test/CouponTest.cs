using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Recurly.Test
{
    public class CouponTest : BaseTest
    {
        [RecurlyFact(TestEnvironment.Type.Integration)]
        public void ListCoupons()
        {
            CreateNewCoupon(1);
            CreateNewCoupon(2);

            var coupons = Coupons.List();
            coupons.Should().NotBeEmpty();

        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListCouponsRedeemable()
        {
            var coupon1 = CreateNewCoupon(1);
            await coupon1.DeactivateAsync();
            CreateNewCoupon(2);

            var coupons = Coupons.List(Coupon.CouponState.Redeemable);
            coupons.Should().NotBeEmpty();
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task CouponsCanBeCreated()
        {
            var discounts = new Dictionary<string, int> {{"USD", 100}};
            var coupon = new Coupon(GetMockCouponCode(), GetMockCouponName(), discounts)
            {
                MaxRedemptions = 1
            };
            await coupon.CreateAsync();
            coupon.CreatedAt.Should().NotBe(default(DateTime));

            var coupons = Coupons.List().All;
            coupons.Should().Contain(coupon);
        }

        /// <summary>
        /// This test isn't constructed as expected, because the service apparently marks expired or maxed
        /// out coupons as "Inactive" rather than "MaxedOut" or "Expired".
        /// </summary>
        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListCouponsExpired()
        {
            var coupon = new Coupon(GetMockCouponCode(), GetMockCouponName("Expired test"), 10)
            {
                MaxRedemptions = 1
            };
            await coupon.CreateAsync();
            coupon.CreatedAt.Should().NotBe(default(DateTime));

            var account = await CreateNewAccountWithBillingInfoAsync();

            var redemption = await account.RedeemCouponAsync(coupon.CouponCode, "USD");
            redemption.CreatedAt.Should().NotBe(default(DateTime));

            var fromService = Coupons.GetAsync(coupon.CouponCode);
            fromService.Should().NotBeNull();

            var expiredCoupons = Coupons.List(Coupon.CouponState.Expired);
            expiredCoupons.Should().NotContain(coupon,
                    "the Recurly service marks this expired coupon as \"Inactive\", which cannot be searched for.");
        }

        /// <summary>
        /// This test isn't constructed as expected, because the service apparently marks expired or maxed
        /// out coupons as "Inactive" rather than "MaxedOut" or "Expired".
        /// </summary>
        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task ListCouponsMaxedOut()
        {
            var coupon = new Coupon(GetMockCouponCode(), GetMockCouponName("Maxed Out test"), 10)
            {
                MaxRedemptions = 1
            };
            await coupon.CreateAsync();
            coupon.CreatedAt.Should().NotBe(default(DateTime));

            var account = await CreateNewAccountWithBillingInfoAsync();

            var redemption = await account.RedeemCouponAsync(coupon.CouponCode, "USD");
            redemption.CreatedAt.Should().NotBe(default(DateTime));

            var fromService = Coupons.GetAsync(coupon.CouponCode);
            fromService.Should().NotBeNull();

            var expiredCoupons = Coupons.List(Coupon.CouponState.Expired);
            expiredCoupons.Should().NotContain(coupon,
                    "the Recurly service marks this expired coupon as \"Inactive\", which cannot be searched for.");
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task CreateCouponPercentAsync()
        {
            var coupon = new Coupon(GetMockCouponCode(), GetMockCouponName(), 10);
            await coupon.CreateAsync();

            coupon.CreatedAt.Should().NotBe(default(DateTime));

            coupon = await Coupons.GetAsync(coupon.CouponCode);

            coupon.Should().NotBeNull();
            coupon.DiscountPercent.Should().Be(10);
            coupon.DiscountType.Should().Be(Coupon.CouponDiscountType.Percent);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task CreateCouponDollars()
        {
            var discounts = new Dictionary<string, int> {{"USD", 100}, {"EUR", 50}};
            var coupon = new Coupon(GetMockCouponCode(), GetMockCouponName(), discounts);

            await coupon.CreateAsync();
            coupon.CreatedAt.Should().NotBe(default(DateTime));

            coupon = await Coupons.GetAsync(coupon.CouponCode);

            coupon.Should().NotBeNull();
            coupon.DiscountInCents.Should().Equal(discounts);
            coupon.DiscountType.Should().Be(Coupon.CouponDiscountType.Dollars);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task CreateCouponPlan()
        {
            var plan = new Plan(GetMockPlanCode("coupon plan"), "Coupon Test");
            plan.SetupFeeInCents.Add("USD", 500);
            plan.UnitAmountInCents.Add("USD", 5000);
            await plan.CreateAsync();
            PlansToDeactivateOnDispose.Add(plan);

            var coupon = new Coupon(GetMockCouponCode(), GetMockCouponName(), new Dictionary<string, int>());
            coupon.DiscountInCents.Add("USD", 100);
            coupon.Plans.Add(plan.PlanCode);

            await coupon.CreateAsync();
            Assert.Equal(1, coupon.Plans.Count);

            //plan.Deactivate(); BaseTest.Dispose() handles this
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task Coupon_plan_must_exist()
        {
            var coupon = new Coupon(GetMockCouponCode(), GetMockCouponName(), 10);
            coupon.Plans.Add("notrealplan");

            await Assert.ThrowsAsync<ValidationException>(coupon.CreateAsync);
        }

        [RecurlyFact(TestEnvironment.Type.Integration)]
        public async Task DeactivateCoupon()
        {
            var discounts = new Dictionary<string, int> { { "USD", 100 }, { "EUR", 50 } };
            var coupon = new Coupon(GetMockCouponCode(), GetMockCouponName(), discounts);
            await coupon.CreateAsync();
            coupon.CreatedAt.Should().NotBe(default(DateTime));

            await coupon.DeactivateAsync();

            coupon = await Coupons.GetAsync(coupon.CouponCode);
            coupon.Should().NotBeNull();
            coupon.State.Should().Be(Coupon.CouponState.Inactive);
        }
    }
}
