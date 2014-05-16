SharpPacker
===========

A simple library that allows multiple files to be packed into a single large file. Suitable for packing game assets or structured data.


Example Usage
-------------

```csharp
// To create a new pack file
PackFile mypackfile = new PackFile("data.pck");
mypackfile.AddFile("test.txt", Encoding.ASCII.GetBytes("This is a test text document"));
mypackfile.AddFile("test2.txt", someotherbytearrayhere);
mypackfile.Save();

// To load an existing pack file
PackFile mypackfile = new PackFile("data.pck");
mypackfile.Load();
Debug.Assert(mypackfile.FileExists("test.txt"));
int len;
byte[] data = mypackfile.GetFileRaw("test.txt", out len);
Console.WriteLine(Encoding.ASCII.GetString(data));

// Or, you can use streams
int len;
Stream strm = mypackfile.GetFile("test.txt", out len);
// do something with strm
strm.Close();
