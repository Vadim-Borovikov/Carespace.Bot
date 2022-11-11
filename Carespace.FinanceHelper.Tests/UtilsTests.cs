using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GryphonUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Carespace.FinanceHelper.Tests;

[TestClass]
public class UtilsTests
{
    [ClassInitialize]
    public static void ClassInitialize(TestContext _)
    {
        _config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                            // Create appsettings.json for private settings
                                            .AddJsonFile("appsettings.json")
                                            .Build()
                                            .Get<ConfigJson>();

        Assert.IsNotNull(_config.TaxFeePercent);
        Assert.IsNotNull(_config.DigisellerFeePercent);
        Assert.IsNotNull(_config.PayMasterFeePercents);
        Assert.IsNotNull(_config.ShareJsons);

        Shares.Clear();
        foreach (string p in _config.ShareJsons.Keys)
        {
            Shares[p] = _config.ShareJsons[p].GetValue().RemoveNulls().Select(s => s.Convert()).ToList();
        }
    }

    [TestMethod]
    public void TestCalculateSharesNoPromo()
    {
        Transaction t = CreateTransaction(100, 1);
        TestCalculateShares(t, 45.8m, 45.8m, 1.5m, 2.9m, 4);
    }

    [TestMethod]
    public void TestCalculateSharesAppearingShare()
    {
        Transaction t1 = CreateTransaction(100, 2);
        TestCalculateShares(t1, 91.6m, 0m, 1.5m, 2.9m, 4);
        Transaction t2 = CreateTransaction(100, 2, "Promo2");
        TestCalculateShares(t2, 45.8m, 45.8m, 1.5m, 2.9m, 4);
    }

    [TestMethod]
    public void TestCalculateSharesDisappearingShare()
    {
        Transaction t1 = CreateTransaction(100, 3);
        TestCalculateShares(t1, 45.8m, 45.8m, 1.5m, 2.9m, 4);
        Transaction t2 = CreateTransaction(100, 3, "Promo3");
        TestCalculateShares(t2, 91.6m, 0m, 1.5m, 2.9m, 4);
    }

    [TestMethod]
    public void TestCalculateSharesIncreasingShare()
    {
        Transaction t1 = CreateTransaction(100, 4);
        TestCalculateShares(t1, 45.8m, 45.8m, 1.5m, 2.9m, 4);
        Transaction t2 = CreateTransaction(100, 4, "Promo4");
        TestCalculateShares(t2, 68.7m, 22.9m, 1.5m, 2.9m, 4);
    }

    [TestMethod]
    public void TestCalculateSharesDecreasingShare()
    {
        Transaction t1 = CreateTransaction(100, 5);
        TestCalculateShares(t1, 45.8m, 45.8m, 1.5m, 2.9m, 4);
        Transaction t2 = CreateTransaction(100, 5, "Promo5");
        TestCalculateShares(t2, 22.9m, 68.7m, 1.5m, 2.9m, 4);
    }

    [TestMethod]
    public void TestCalculateSharesBook()
    {
        Transaction t = CreateTransaction(380, 2957145);
        TestCalculateShares(t, 232.05m, 116.03m, 5.7m, 11.02m, 15.2m);
    }

    [TestMethod]
    public void TestCalculateSharesTicket()
    {
        Transaction t1 = CreateTransaction(100, 3166947);
        TestCalculateShares(t1, 81.52m, 10.08m, 1.5m, 2.9m, 4);
        Transaction t2 = CreateTransaction(100, 3166947, "OldFriend");
        TestCalculateShares(t2, 91.6m, 0m, 1.5m, 2.9m, 4);
    }

    private static Transaction CreateTransaction(decimal price, int digisellerProductId, string? promoCode = null)
    {
        Dictionary<string, object?> valueSet = new()
        {
            { "Комментарий", null },
            { "Дата", DateTime.Today },
            { "Сумма", price },
            { "Цена", price },
            { "Товар", digisellerProductId },
            { "Промокод", promoCode },
            { "Покупка", 135120565 },
            { "Способ", Transaction.PayMethod.BankCard }
        };
        return Transaction.Load(valueSet);
    }

    private static void TestCalculateShares(Transaction transaction, decimal shareAgent3,
        decimal shareAgent4, decimal digisellerFee, decimal payMasterFee, decimal tax)
    {
        Assert.IsNotNull(_config.TaxFeePercent);
        Assert.IsNotNull(_config.DigisellerFeePercent);
        Assert.IsNotNull(_config.PayMasterFeePercents);
        Utils.CalculateShares(new[] { transaction }, _config.TaxFeePercent.Value, _config.DigisellerFeePercent.Value,
            _config.PayMasterFeePercents, Shares);
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

    // ReSharper disable once NullableWarningSuppressionIsUsed
    //   _config initializes in ClassInitialize
    private static ConfigJson _config = null!;
    private static readonly Dictionary<string, List<Share>> Shares = new();
}