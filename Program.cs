/*
 * File: Program.cs
 * Author: Savannah Alfaro, sea2985
 */

using System.Diagnostics;

namespace Disk_Usage
{
    class DiskUsage
    {
        // help message constant
        private const string HelpMessage =
            "Usage: du [-s] [-p] [-b] <path>\n" +
            "Summarize disk usage of the set of FILEs, recursively for directories.\n" +
            "\n" +
            "You MUST specify one of the parameters, -s, -p, or -b\n" +
            "-s       Run in single threaded mode\n" +
            "-p       Run in parallel mode (uses all available processors)\n" +
            "-b       Run in both single threaded and parallel mode.\n" +
            "         Runs parallel follow by sequential mode";

        // class attributes
        private int _folders;
        private int _files;
        private long _bytes;
        
        // class attribute locks
        private readonly object _folderLock = new Object();
        private readonly object _fileLock = new Object();
        private readonly object _byteLock = new Object();

        /// <summary>
        /// Processes all files in a given directory, recurses on all
        /// subdirectories, and processes the files they contain sequentially.
        /// </summary>
        /// <param name="directory">(DirectoryInfo) The initial directory</param>
        /// <param name="diskUsage">(DiskUsage) The DiskUsage object</param>
        private void ProcessDirectorySequential(DirectoryInfo directory, DiskUsage diskUsage)
        {
            try
            {
                // process all files in the directory
                foreach (FileInfo file in directory.GetFiles())
                {
                    diskUsage._bytes += file.Length;
                    diskUsage._files += 1;
                }
            }
            catch (Exception)
            {
                // squash
            }

            try
            {
                // recurse all subdirectories in the directory
                foreach (DirectoryInfo subdirectory in directory.GetDirectories())
                {
                    diskUsage._folders += 1;
                    ProcessDirectorySequential(subdirectory, diskUsage);
                }
            }
            catch (Exception)
            {
                // squash
            }
        }
        
        /// <summary>
        /// Processes all files in a given directory, recurses on all
        /// subdirectories, and processes the files they contain in parallel.
        /// </summary>
        /// <param name="directory">(DirectoryInfo) The initial directory</param>
        /// <param name="diskUsage">(DiskUsage) The DiskUsage object</param>
        private void ProcessDirectoryParallel(DirectoryInfo directory, DiskUsage diskUsage)
        {
            try
            {
                // process all files in the directory
                foreach (FileInfo file in directory.GetFiles())
                {
                    lock (_byteLock)
                    {
                        diskUsage._bytes += file.Length;
                    }

                    lock (_fileLock)
                    {
                        diskUsage._files += 1;
                    }
                }
            }
            catch (Exception)
            {
                // squash
            }

            try
            {
                // recurse all subdirectories in the directory
                Parallel.ForEach(directory.GetDirectories(), subdirectory =>
                {
                    lock (_folderLock)
                    {
                        diskUsage._folders += 1;
                    }
                    ProcessDirectoryParallel(subdirectory, diskUsage);
                });
            }
            catch (Exception)
            {
                // squash
            }
        }

        /// <summary>
        /// Runs the DiskUsage program in sequential mode.
        /// </summary>
        /// <param name="path">(String) The path of the initial directory</param>
        /// <param name="parallelSequentialMode">(Boolean) True if running both modes</param>
        /// <param name="diskUsage">(DiskUsage) The DiskUsage object</param>
        private void SequentialMode(String path, Boolean parallelSequentialMode, DiskUsage diskUsage)
        {
            // start timer
            var timer = new Stopwatch();
            timer.Start();

            // process directory info
            DirectoryInfo directory = new DirectoryInfo(path);
            ProcessDirectorySequential(directory, diskUsage);

            // stop timer
            timer.Stop();
            
            // display results
            if (parallelSequentialMode)
            {
                Console.WriteLine("\nSequential Calculated in: {0}s", timer.Elapsed.TotalSeconds);
                Console.WriteLine("{0:N0} folders, {1:N0} files, {2:N0} bytes", _folders, _files, _bytes);
            }
            else
            {
                Console.WriteLine("Directory \'{0}\':\n", path);
                Console.WriteLine("Sequential Calculated in: {0}s", timer.Elapsed.TotalSeconds);
                Console.WriteLine("{0:N0} folders, {1:N0} files, {2:N0} bytes", _folders, _files, _bytes);
            }
            
            // reset values
            _folders = 0;
            _files = 0;
            _bytes = 0;
        }

        /// <summary>
        /// Runs the DiskUsage program in parallel mode.
        /// </summary>
        /// <param name="path">(String) The path of the initial directory</param>
        /// <param name="diskUsage">(DiskUsage) The DiskUsage object</param>
        private void ParallelMode(String path, DiskUsage diskUsage)
        {
            // start timer
            var timer = new Stopwatch();
            timer.Start();
            
            // process directory info
            DirectoryInfo directory = new DirectoryInfo(path);
            ProcessDirectoryParallel(directory, diskUsage);

            // end timer
            timer.Stop();
            
            // display results
            Console.WriteLine("Directory \'{0}\':\n", path);
            Console.WriteLine("Parallel Calculated in: {0}s", timer.Elapsed.TotalSeconds);
            Console.WriteLine("{0:N0} folders, {1:N0} files, {2:N0} bytes", _folders, _files, _bytes);
            
            // reset values
            _folders = 0;
            _files = 0;
            _bytes = 0;
        }

        /// <summary>
        /// Runs the DiskUsage program in both sequential and parallel mode.
        /// </summary>
        /// <param name="path">(String) The path of the initial directory</param>
        /// <param name="diskUsage">(DiskUsage) The DiskUsage object</param>
        private void ParallelSequentialMode(String path, DiskUsage diskUsage)
        {
            // process directory info
            ParallelMode(path, diskUsage);
            SequentialMode(path, true, diskUsage);
        }
        
        /// <summary>
        /// Main method to instantiate and run the DiskUsage program.
        /// </summary>
        /// <param name="args">(string) Command line arguments</param>
        public static void Main(string[] args)
        {
            // check command line arguments
            if (args.Length != 2)
            {
                Console.WriteLine(HelpMessage);
            }
            else
            {
                // instantiate Disk Usage
                var diskUsage = new DiskUsage();
                
                // determine path
                var path = args[1];
                
                // determine program mode
                switch (args[0])
                {
                    case "-s":
                        diskUsage.SequentialMode(path, false, diskUsage);
                        break;
                    case "-p":
                        diskUsage.ParallelMode(path, diskUsage);
                        break;
                    case "-b":
                        diskUsage.ParallelSequentialMode(path, diskUsage);
                        break;
                    default:
                        Console.WriteLine(HelpMessage);
                        break;
                }
                
                // print new line at end of program
                Console.WriteLine("");
            }
        }
    }
}