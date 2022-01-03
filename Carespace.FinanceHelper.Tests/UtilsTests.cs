using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Carespace.FinanceHelper.Tests
{
    [TestClass]
    public sealed class UtilsTests
    {
        [TestMethod]
        public void TestCalculateSharesNoPromo()
        {
            Configuration config = Helper.GetConfig();
            Transaction t = CreateTransaction(100, 1);
            TestCalculateShares(config, t, 45.8m, 45.8m, 1.5m, 2.9m, 4);
        }

        [TestMethod]
        public void TestCalculateSharesAppearingShare()
        {
            Configuration config = Helper.GetConfig();
            Transaction t1 = CreateTransaction(100, 2);
            TestCalculateShares(config, t1, 91.6m, 0m, 1.5m, 2.9m, 4);
            Transaction t2 = CreateTransaction(100, 2, "Promo2");
            TestCalculateShares(config, t2, 45.8m, 45.8m, 1.5m, 2.9m, 4);
        }

        [TestMethod]
        public void TestCalculateSharesDisappearingShare()
        {
            Configuration config = Helper.GetConfig();
            Transaction t1 = CreateTransaction(100, 3);
            TestCalculateShares(config, t1, 45.8m, 45.8m, 1.5m, 2.9m, 4);
            Transaction t2 = CreateTransaction(100, 3, "Promo3");
            TestCalculateShares(config, t2, 91.6m, 0m, 1.5m, 2.9m, 4);
        }

        [TestMethod]
        public void TestCalculateSharesIncreasingShare()
        {
            Configuration config = Helper.GetConfig();
            Transaction t1 = CreateTransaction(100, 4);
            TestCalculateShares(config, t1, 45.8m, 45.8m, 1.5m, 2.9m, 4);
            Transaction t2 = CreateTransaction(100, 4, "Promo4");
            TestCalculateShares(config, t2, 68.7m, 22.9m, 1.5m, 2.9m, 4);
        }

        [TestMethod]
        public void TestCalculateSharesDecreasingShare()
        {
            Configuration config = Helper.GetConfig();
            Transaction t1 = CreateTransaction(100, 5);
            TestCalculateShares(config, t1, 45.8m, 45.8m, 1.5m, 2.9m, 4);
            Transaction t2 = CreateTransaction(100, 5, "Promo5");
            TestCalculateShares(config, t2, 22.9m, 68.7m, 1.5m, 2.9m, 4);
        }

        [TestMethod]
        public void TestCalculateSharesBook()
        {
            Configuration config = Helper.GetConfig();
            Transaction t = CreateTransaction(380, 2957145);
            TestCalculateShares(config, t, 232.05m, 116.03m, 5.7m, 11.02m, 15.2m);
        }

        [TestMethod]
        public void TestCalculateSharesTicket()
        {
            Configuration config = Helper.GetConfig();
            Transaction t1 = CreateTransaction(100, 3166947);
            TestCalculateShares(config, t1, 81.52m, 10.08m, 1.5m, 2.9m, 4);
            Transaction t2 = CreateTransaction(100, 3166947, "OldFriend");
            TestCalculateShares(config, t2, 91.6m, 0m, 1.5m, 2.9m, 4);
        }

        private static Transaction CreateTransaction(decimal price, int digisellerProductId, string promoCode = null)
        {
            return new Transaction(price, price, digisellerProductId, promoCode, 135120565, Transaction.PayMethod.BankCard);
        }

        private static void TestCalculateShares(Configuration config, Transaction transaction, decimal shareAgent3,
            decimal shareAgent4, decimal digisellerFee, decimal payMasterFee, decimal tax)
        {
            Utils.CalculateShares(new[] { transaction }, config.TaxFeePercent, config.DigisellerFeePercent,
                config.PayMasterFeePercents, config.Shares);
            Assert.AreEqual(digisellerFee, transaction.DigisellerFee);
            Assert.AreEqual(payMasterFee, transaction.PayMasterFee);
            Assert.AreEqual(tax, transaction.Tax);
            Assert.AreEqual(2, transaction.Shares.Count);
            Assert.IsTrue(transaction.Shares.ContainsKey(Agent1));
            Assert.IsTrue(transaction.Shares.ContainsKey(Agent2));
            Assert.AreEqual(shareAgent3, transaction.Shares[Agent1]);
            Assert.AreEqual(shareAgent4, transaction.Shares[Agent2]);
        }

        private const string Agent1 = "Agent1";
        private const string Agent2 = "Agent2";
    }
}
