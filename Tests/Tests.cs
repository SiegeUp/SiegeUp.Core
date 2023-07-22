
using NUnit.Framework;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace SiegeUp.Core.Tests    
{
    public class Tests
    {
        class TestSubClass
        {
            [AutoSerialize(0)]
            public int fieldInt;
        }
        struct TestSubStruct
        {
            [AutoSerialize(0)]
            public int fieldInt;
        }

        class TestStruct
        {
            [AutoSerialize(0)]
            public int fieldInt;

            [AutoSerialize(1)]
            public string fieldString;

            [AutoSerialize(2)]
            public TestSubStruct subStruct;

            [AutoSerialize(3)]
            public List<string> listOfStrs;

            [AutoSerialize(4)]
            public List<int> listOfInts;

            [AutoSerialize(5)]
            public List<TestSubStruct> listOfTestSubStruct;

            [AutoSerialize(6)]
            public float fieldFloat;

            [AutoSerialize(7)]
            public bool fieldBool;

            [AutoSerialize(8)]
            public short fieldShort;

            [AutoSerialize(9)]
            public byte[] fieldByteArray;

            [AutoSerialize(10)]
            public byte fieldByte;

            [AutoSerialize(11)]
            public TestSubClass testSubClass;

            [AutoSerialize(12)]
            public TestSubClass testSubClassNull;

            [AutoSerialize(13)]
            public string testRuStr;

            [AutoSerialize(14)]
            public int num;
        }


        [Test]
        public void TestAutoSerialize()
        {
            var testObj = new TestStruct {
                fieldInt = 100,
                fieldString = "200",
                subStruct = new TestSubStruct {
                    fieldInt = 300
                },
                listOfStrs = new List<string> { "A", "B", null, "D", null, "F" },
                listOfInts = new List<int> { 1, 2, 3, 4, 5 },
                listOfTestSubStruct = new List<TestSubStruct> { new() { fieldInt = 1 }, new() { fieldInt = 2 }, new() { fieldInt = 3 } },
                fieldFloat = 10.010f,
                fieldBool = true,
                fieldShort = 1000,
                fieldByte = 10,
                fieldByteArray = new byte[] { 1, 2, 3, 4 },
                testSubClass = new TestSubClass { fieldInt = 400 },
                testSubClassNull = null,
                testRuStr = "Тест",
                num = 10
            };

            var bytes = new byte[1024];
            int serializePos = 0;
            AutoSerializeTool.WriteObject(ref bytes, ref serializePos, testObj);
            Array.Resize(ref bytes, serializePos + 1);

            Debug.Log(BitConverter.ToString(bytes).Replace("-", ""));

            var newTestObj = new TestStruct();

            int deserializePos = 0;
            var objContext = new ObjectContext(null, null, AutoSerializeTool.currentFormatVersion, null, newTestObj, "Test", typeof(TestStruct));
            AutoSerializeTool.ReadObject(ref bytes, ref deserializePos, objContext);

            Assert.AreEqual(newTestObj.fieldInt, testObj.fieldInt);
            Assert.AreEqual(newTestObj.fieldString, testObj.fieldString);
            Assert.AreEqual(newTestObj.subStruct.fieldInt, testObj.subStruct.fieldInt);
            Assert.AreEqual(newTestObj.listOfStrs.Count, testObj.listOfStrs.Count);
            Assert.AreEqual(newTestObj.listOfInts.Count, testObj.listOfInts.Count);
            Assert.AreEqual(newTestObj.listOfTestSubStruct.Count, testObj.listOfTestSubStruct.Count);
            Assert.AreEqual(newTestObj.testRuStr, testObj.testRuStr);
            Assert.AreEqual(newTestObj.num, testObj.num);
        }
    }
}