using System;
using System.Collections.Generic;
using Client.Core;
using NUnit.Framework;
using Server;
using UnitTests.TestData;

namespace UnitTests
{
    [TestFixture]
    public class TestFixtureEvictionQueue
    {
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            ClientSideTypeDescription.RegisterType(typeof(TradeLike));
        }

        [Test]
        public void AddMoreThanCapacity()
        {
            EvictionQueue queue = new EvictionQueue();
            queue.Capacity = 1000;
            queue.EvictionCount = 100;

            for (int i = 0; i < 10000; i++)
            {
                TradeLike item = new TradeLike(i, 1000 + i, "aaa", DateTime.Now, 456);
                CachedObject packedItem = CachedObject.Pack(item);
                queue.AddNew(packedItem);
            }

            Assert.IsTrue(queue.EvictionRequired);
            Assert.AreEqual(queue.Count, 10000);
            ICollection<CachedObject> evicted = queue.GO();

            //should have removed 100 more than ( 10000 - 1000 )
            Assert.AreEqual(queue.Count, 900);
            Assert.IsFalse(queue.EvictionRequired);
            Assert.AreEqual(evicted.Count, 9100);
            //asking for eviction when bellow maximum capacity will not remove any item
            evicted = queue.GO();
            Assert.AreEqual(evicted.Count, 0);
        }

        [Test]
        public void AddTwiceRaisesAnException()
        {
            EvictionQueue queue = new EvictionQueue();
            queue.Capacity = 1000;
            queue.EvictionCount = 100;
            TradeLike item = new TradeLike(0, 1000, "aaa", DateTime.Now, 456);
            CachedObject packedItem = CachedObject.Pack(item);
            queue.AddNew(packedItem);

            //this call should raise an exception
            Assert.Throws<NotSupportedException>(() => queue.AddNew(packedItem));
        }

        [Test]
        public void EvictionOrder()
        {
            EvictionQueue queue = new EvictionQueue();
            queue.Capacity = 9;
            queue.EvictionCount = 2;

            List<CachedObject> allItems = new List<CachedObject>();

            for (int i = 0; i < 10; i++)
            {
                TradeLike item = new TradeLike(i, 1000 + i, "aaa", DateTime.Now, 456);
                CachedObject packedItem = CachedObject.Pack(item);
                queue.AddNew(packedItem);
                allItems.Add(packedItem);
            }

            //items in queue now: 0 1 2 3 4 5 6 7 8 9 

            Assert.IsTrue(queue.EvictionRequired);
            IList<CachedObject> evicted = queue.GO();

            //items in queue: 3 4 5 6 7 8 9 

            Assert.AreEqual(evicted.Count, 3);

            Assert.AreEqual(evicted[0].PrimaryKey, 0);
            Assert.AreEqual(evicted[1].PrimaryKey, 1);

            queue.Touch(allItems[3]);
            //items in queue: 4 5 6 7 8 9 3

            queue.Touch(allItems[4]);
            //items in queue: 5 6 7 8 9 3 4

            queue.Capacity = 7;
            evicted = queue.GO();

            Assert.AreEqual(evicted.Count, 2);

            Assert.AreEqual(evicted[0].PrimaryKey, 5);
            Assert.AreEqual(evicted[1].PrimaryKey, 6);
        }


        [Test]
        public void Remove()
        {
            EvictionQueue queue = new EvictionQueue();
            queue.Capacity = 7;
            queue.EvictionCount = 2;

            List<CachedObject> allItems = new List<CachedObject>();

            for (int i = 0; i < 10; i++)
            {
                TradeLike item = new TradeLike(i, 1000 + i, "aaa", DateTime.Now, 456);
                CachedObject packedItem = CachedObject.Pack(item);
                queue.AddNew(packedItem);
                allItems.Add(packedItem);
            }

            //items in queue now: 0 1 2 3 4 5 6 7 8 9 
            queue.TryRemove(allItems[0]);
            queue.TryRemove(allItems[2]);

            //items in queue now: 1 3 4 5 6 7 8 9 
            Assert.IsTrue(queue.EvictionRequired);
            IList<CachedObject> evicted = queue.GO();

            //items in queue now: 5 6 7 8 9 

            Assert.AreEqual(evicted.Count, 3);

            Assert.AreEqual(evicted[0].PrimaryKey, 1);
            Assert.AreEqual(evicted[1].PrimaryKey, 3);
        }
    }
}