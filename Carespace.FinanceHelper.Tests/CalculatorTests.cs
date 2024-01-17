using System;
using System.IO;
using GryphonUtilities.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Carespace.FinanceHelper.Tests;

[TestClass]
public class CalculatorTests
{
    [ClassInitialize]
    public static void ClassInitialize(TestContext _)
    {
        _config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                            // Create appsettings.json for private settings
                                            .AddJsonFile("appsettings.json")
                                            .Build()
                                            .Get<Config>()!;

        Assert.IsNotNull(_config.Products);
    }

    [TestMethod]
    public void TestCalculateSharesNoPromo()
    {
        Transaction t = CreateTransaction(1);
        TestCalculateShares(t, 50, 50);
    }

    [TestMethod]
    public void TestCalculateSharesAppearingShare()
    {
        Transaction t1 = CreateTransaction(2);
        TestCalculateShares(t1, 100, 0);
        Transaction t2 = CreateTransaction(2, "Promo2");
        TestCalculateShares(t2, 50, 50);
    }

    [TestMethod]
    public void TestCalculateSharesDisappearingShare()
    {
        Transaction t1 = CreateTransaction(3);
        TestCalculateShares(t1, 50, 50);
        Transaction t2 = CreateTransaction(3, "Promo3");
        TestCalculateShares(t2, 100, 0m);
    }

    [TestMethod]
    public void TestCalculateSharesIncreasingShare()
    {
        Transaction t1 = CreateTransaction(4);
        TestCalculateShares(t1, 50, 50);
        Transaction t2 = CreateTransaction(4, "Promo4");
        TestCalculateShares(t2, 75, 25);
    }

    [TestMethod]
    public void TestCalculateSharesDecreasingShare()
    {
        Transaction t1 = CreateTransaction(5);
        TestCalculateShares(t1, 50, 50);
        Transaction t2 = CreateTransaction(5, "Promo5");
        TestCalculateShares(t2, 25, 75);
    }

    [TestMethod]
    public void TestCalculateSharesBook()
    {
        Transaction t = CreateTransaction(6);
        TestCalculateShares(t, 253.33m, 126.67m);
    }

    private static Transaction CreateTransaction(byte productId, string? promoCode = null)
    {
        return new Transaction
        {
            Amount = _config.Products[productId].Price,
            ProductId = productId,
            PromoCode = promoCode,
            Date = Date
        };
    }

    private static void TestCalculateShares(Transaction transaction, decimal shareAgent1, decimal shareAgent2)
    {
        Calculator.CalculateShares(transaction.Yield(), _config.Products);
        Assert.AreEqual(2, transaction.Shares.Count);
        Assert.IsTrue(transaction.Shares.ContainsKey(Agent1));
        Assert.IsTrue(transaction.Shares.ContainsKey(Agent2));
        Assert.AreEqual(shareAgent1, transaction.Shares[Agent1]);
        Assert.AreEqual(shareAgent2, transaction.Shares[Agent2]);
    }

    private const string Agent1 = "Agent1";
    private const string Agent2 = "Agent2";
    private static readonly DateOnly Date = new(2022, 1, 1);

    private static Config _config = null!;
}