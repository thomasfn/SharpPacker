using System;
using System.Text;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SharpPacker;

namespace SharpPackerTests
{
    [TestClass]
    public class PackFileTests
    {
        private static readonly byte[] TestData1 = Encoding.ASCII.GetBytes("Hello world");
        private static readonly byte[] TestData2 = Encoding.ASCII.GetBytes("This is a string");
        private static readonly byte[] TestData3 = Encoding.ASCII.GetBytes("PackFile 123");

        [TestMethod]
        public void SimpleWriteRead()
        {
            PackFile file1 = new PackFile("test.pck");
            Assert.IsTrue(file1.AddFile("test1", TestData1), "AddFile returned false");
            VerifyFile(file1, "test1", TestData1, "before file1 save");
            try
            {
                file1.Save();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception when saving file1: " + ex.Message);
                return;
            }
            file1 = null;


            PackFile file2 = new PackFile("test.pck");
            try
            {
                file2.Load();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception when loading file2: " + ex.Message);
                return;
            }
            VerifyFile(file2, "test1", TestData1, "after file2 load");
        }

        [TestMethod]
        public void AddRemove()
        {
            PackFile file1 = new PackFile("test.pck");
            Assert.IsTrue(file1.AddFile("test1", TestData1), "AddFile returned false");
            Assert.IsTrue(file1.AddFile("test2", TestData2), "AddFile returned false");
            VerifyFile(file1, "test1", TestData1, "before file1 save");
            VerifyFile(file1, "test2", TestData2, "before file1 save");
            Assert.AreEqual(2, file1.FileCount, "FileCount mismatch before save");
            try
            {
                file1.Save();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception when saving file1: " + ex.Message);
                return;
            }
            file1 = null;


            PackFile file2 = new PackFile("test.pck");
            try
            {
                file2.Load();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception when loading file2: " + ex.Message);
                return;
            }
            Assert.AreEqual(2, file2.FileCount, "FileCount mismatch after load back");
            VerifyFile(file2, "test1", TestData1, "after file2 load");
            VerifyFile(file2, "test2", TestData2, "after file2 load");
            file2.RemoveFile("test1");
            Assert.AreEqual(1, file2.FileCount, "FileCount mismatch after remove");
            Assert.IsFalse(file2.FileExists("test1"), "test1 still exists");
            file2.AddFile("test3", TestData3);
            VerifyFile(file2, "test3", TestData3, "after file2 load");
            Assert.AreEqual(2, file2.FileCount, "FileCount mismatch after add");
            try
            {
                file2.Save();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception when saving file2): " + ex.Message);
                return;
            }
            file2 = null;

            PackFile file3 = new PackFile("test.pck");
            try
            {
                file3.Load();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception when loading file3 back: " + ex.Message);
                return;
            }
            Assert.AreEqual(2, file3.FileCount, "FileCount mismatch after load back");
            Assert.IsFalse(file3.FileExists("test1"), "test1 still exists after load back");
            VerifyFile(file3, "test2", TestData2, "after file3 load");
            VerifyFile(file3, "test3", TestData3, "after file3 load");
        }

        [TestMethod]
        public void Move()
        {
            PackFile file1 = new PackFile("test.pck");
            Assert.IsTrue(file1.AddFile("test1", TestData1), "AddFile returned false");
            Assert.IsTrue(file1.AddFile("test2", TestData2), "AddFile returned false");
            VerifyFile(file1, "test1", TestData1, "after file2 load");
            VerifyFile(file1, "test2", TestData2, "after file2 load");
            Assert.AreEqual(2, file1.FileCount, "FileCount mismatch before save");
            try
            {
                file1.Save();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception when saving file1: " + ex.Message);
                return;
            }
            file1 = null;

            PackFile file2 = new PackFile("test.pck");
            try
            {
                file2.Load();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception when loading file2: " + ex.Message);
                return;
            }

            Assert.IsFalse(file2.MoveFile("test1", "test2"), "Bad move was allowed");
            Assert.IsTrue(file2.MoveFile("test1", "test3"), "Good move was denied");
            Assert.IsFalse(file2.FileExists("test1"), "test1 still exists before load back");

            VerifyFile(file2, "test2", TestData2, "after move");
            VerifyFile(file2, "test3", TestData1, "after move");

            try
            {
                file2.Save();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception when saving file2: " + ex.Message);
                return;
            }
            file2 = null;

            PackFile file3 = new PackFile("test.pck");
            try
            {
                file3.Load();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception when loading file3 back: " + ex.Message);
                return;
            }

            Assert.IsFalse(file3.FileExists("test1"), "test1 still exists after load back");
            VerifyFile(file3, "test2", TestData2, "after file3 load");
            VerifyFile(file3, "test3", TestData1, "after file3 load");
        }

        [TestMethod]
        public void UpdateFile()
        {
            PackFile file1 = new PackFile("test.pck");
            Assert.IsTrue(file1.AddFile("test1", TestData1), "AddFile returned false");
            Assert.IsTrue(file1.AddFile("test2", TestData2), "AddFile returned false");
            VerifyFile(file1, "test1", TestData1, "after file2 load");
            VerifyFile(file1, "test2", TestData2, "after file2 load");
            Assert.AreEqual(2, file1.FileCount, "FileCount mismatch before save");
            try
            {
                file1.Save();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception when saving file1: " + ex.Message);
                return;
            }
            file1 = null;

            PackFile file2 = new PackFile("test.pck");
            try
            {
                file2.Load();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception when loading file2: " + ex.Message);
                return;
            }

            file2.UpdateFile("test1", TestData3);

            VerifyFile(file2, "test1", TestData3, "after update");
            VerifyFile(file2, "test2", TestData2, "after update");

            try
            {
                file2.Save();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception when saving file2: " + ex.Message);
                return;
            }
            file2 = null;

            PackFile file3 = new PackFile("test.pck");
            try
            {
                file3.Load();
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception when loading file3 back: " + ex.Message);
                return;
            }

            VerifyFile(file3, "test1", TestData3, "after file3 load");
            VerifyFile(file3, "test2", TestData2, "after file3 load");
        }

        private static void VerifyFile(PackFile packfile, string filename, byte[] expected, string id)
        {
            // See that it exists
            Assert.IsTrue(packfile.FileExists(filename), string.Format("{0} doesn't exist ({1})", filename, id));

            // See that the length is ok
            Assert.AreEqual(expected.Length, packfile.FileLength(filename), string.Format("{0} has bad length (FileLength, {1})", filename, id));

            // Test GetFileRaw
            int len;
            byte[] data = packfile.GetFileRaw(filename, out len);
            Assert.AreEqual(expected.Length, packfile.FileLength(filename), string.Format("{0} has bad length (GetFileRaw, {1})", filename, id));
            Assert.IsTrue(Compare(data, expected), string.Format("{0} has data mismatch (GetFileRaw, {1})", filename, id));

            // Test GetFile
            Stream strm = packfile.GetFile(filename, out len);
            Assert.AreEqual(expected.Length, packfile.FileLength(filename), string.Format("{0} has bad length (GetFile, {1})", filename, id));
            data = new byte[len];
            strm.Read(data, 0, len);
            strm.Close();
            Assert.IsTrue(Compare(data, expected), string.Format("{0} has data mismatch (GetFile, {1})", filename, id));
        }

        private static bool Compare(byte[] arr1, byte[] arr2)
        {
            if (arr1.Length != arr2.Length) return false;
            for (int i = 0; i < arr1.Length; i++)
                if (arr1[i] != arr2[i]) return false;
            return true;
        }
    }
}
