using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        Assert.IsNotNull(_config.Shares);

        Shares.Clear();
        foreach (string p in _config.Shares.Keys)
        {
            Shares[p] = _config.Shares[p].ToList();
        }
    }

    [TestMethod]
    public void TestCalculateSharesNoPromo()
    {
        Transaction t = CreateTransaction(100, 1);
        TestCalculateShares(t, 50, 50);
    }

    [TestMethod]
    public void TestCalculateSharesAppearingShare()
    {
        Transaction t1 = CreateTransaction(100, 2);
        TestCalculateShares(t1, 100, 0);
        Transaction t2 = CreateTransaction(100, 2, "Promo2");
        TestCalculateShares(t2, 50, 50);
    }

    [TestMethod]
    public void TestCalculateSharesDisappearingShare()
    {
        Transaction t1 = CreateTransaction(100, 3);
        TestCalculateShares(t1, 50, 50);
        Transaction t2 = CreateTransaction(100, 3, "Promo3");
        TestCalculateShares(t2, 100, 0m);
    }

    [TestMethod]
    public void TestCalculateSharesIncreasingShare()
    {
        Transaction t1 = CreateTransaction(100, 4);
        TestCalculateShares(t1, 50, 50);
        Transaction t2 = CreateTransaction(100, 4, "Promo4");
        TestCalculateShares(t2, 75, 25);
    }

    [TestMethod]
    public void TestCalculateSharesDecreasingShare()
    {
        Transaction t1 = CreateTransaction(100, 5);
        TestCalculateShares(t1, 50, 50);
        Transaction t2 = CreateTransaction(100, 5, "Promo5");
        TestCalculateShares(t2, 25, 75);
    }

    [TestMethod]
    public void TestCalculateSharesBook()
    {
        Transaction t = CreateTransaction(380, 2957145);
        TestCalculateShares(t, 253.33m, 126.67m);
    }

    [TestMethod]
    public void TestCalculateSharesTicket()
    {
        Transaction t1 = CreateTransaction(100, 3166947);
        TestCalculateShares(t1, 89, 11);
        Transaction t2 = CreateTransaction(100, 3166947, "OldFriend");
        TestCalculateShares(t2, 100, 0m);
    }

    private static Transaction CreateTransaction(decimal amount, int digisellerProductId, string? promoCode = null)
    {
        return new Transaction
        {
            Amount = amount,
            DigisellerProductId = digisellerProductId,
            PromoCode = promoCode,
            Date = Date
        };
    }

    private static void TestCalculateShares(Transaction transaction, decimal shareAgent3, decimal shareAgent4)
    {
        Calculator.CalculateShares(transaction.Yield(), Shares);
        Assert.AreEqual(2, transaction.Shares.Count);
        Assert.IsTrue(transaction.Shares.ContainsKey(Agent1));
        Assert.IsTrue(transaction.Shares.ContainsKey(Agent2));
        Assert.AreEqual(shareAgent3, transaction.Shares[Agent1]);
        Assert.AreEqual(shareAgent4, transaction.Shares[Agent2]);
    }

    private const string Agent1 = "Agent1";
    private const string Agent2 = "Agent2";
    private static readonly DateOnly Date = new(2022, 1, 1);

    // ReSharper disable once NullableWarningSuppressionIsUsed
    //   _config initializes in ClassInitialize
    private static Config _config = null!;
    private static readonly Dictionary<string, List<Share>> Shares = new();
}