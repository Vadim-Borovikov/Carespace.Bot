using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Carespace.FinanceHelper.Tests
{
    [TestClass]
    public sealed class CommonTests
    {
        [TestMethod]
        public void TestExtractPatameter()
        {
            string s = string.Format(Format, Value);

            string parameter = Utils.ExtractParameter(s, Format);
            Assert.AreEqual(Value.ToString(), parameter);
        }

        [TestMethod]
        public void TestExtractIntPatameter()
        {
            string s = string.Format(Format, Value);

            int? parameter = Utils.ExtractIntParameter(s, Format);
            if (parameter.HasValue)
            {
                Assert.AreEqual(Value, parameter.Value);
            }
            else
            {
                Assert.Fail();
            }
        }

        private static readonly string Format = $"{Prefix}{{0}}{Postfix}";
        private const int Value = 42;

        private const string Prefix = "Some random string";
        private const string Postfix = "Some other random string";
    }
}
