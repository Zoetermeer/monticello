using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Monticello.Common;

namespace MonticelloTests {
    [TestClass]
    public class CommonTests {
        [TestMethod]
        public void TestAppendItems()
        {
            var sb = new StringBuilder();
            var ls = new int[] { 1, 2, 3, 4 };
            sb.AppendItems(items: ls);
            Assert.AreEqual("1 2 3 4", sb.ToString());
        }
    }
}
