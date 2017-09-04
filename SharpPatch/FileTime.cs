// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;

namespace SharpPatch
{
    /// <summary>
    /// FileTime.
    /// </summary>
    class FileTime
    {
        private DateTime CreateTime;
        private DateTime LastAccessTime;
        private DateTime LastWriteTime;

        public FileTime(string file)
        {
            CreateTime = File.GetCreationTime(file);
            LastAccessTime = File.GetLastAccessTime(file);
            LastWriteTime = File.GetLastWriteTime(file);
        }

        public void UpdateCheckFile(string checkFile)
        {
            File.WriteAllText(checkFile, "");
            UpdateFile(checkFile);
        }

        /// <summary>
        /// Checks the file.
        /// </summary>
        /// <param name="checkfile">The file to check.</param>
        /// <returns>true if the file exist and has the same LastWriteTime </returns>
        public bool CheckFileUpToDate(string checkfile)
        {
            return File.Exists(checkfile) && File.GetLastWriteTime(checkfile) == LastWriteTime;
        }

        public void UpdateFile(string file)
        {
            File.SetCreationTime(file, CreateTime);
            File.SetLastWriteTime(file, LastWriteTime);
            File.SetLastAccessTime(file, LastAccessTime);
        }
    }
}
