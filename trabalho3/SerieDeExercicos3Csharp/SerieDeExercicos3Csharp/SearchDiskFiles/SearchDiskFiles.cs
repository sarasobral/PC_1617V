using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SearchDiskFiles {
    class SearchResult {
        public int totalFiles;
        public int totalFilesWithExtension;
        public ConcurrentBag<String> files = new ConcurrentBag<string>();
    }
    class SearchDiskFiles {
        // Caller must catch the AggregateException
        public static Task<SearchResult> Find(string folder, string ext, string keyWord, CancellationToken token)
        {
            var task = Task.Factory.StartNew<SearchResult>(() => {
                var result = new SearchResult();
                // get all the files in that directory to count
                var files = Directory.GetFiles(folder);
                result.totalFiles = files.Length; // save the count
                // filter the files by the extension we want (toArray because we want the size)
                var filesWithExtension = files.Where(f => f.EndsWith("." + ext)).ToArray();
                result.totalFilesWithExtension = filesWithExtension.Length;
                // if canceled at this point, we dont need to read all the files
                token.ThrowIfCancellationRequested();
                // used to throttled the IO concurrency in case we have many files
                SemaphoreSlim semaphore = new SemaphoreSlim(20);
                // read the contents of all the files
                // we assume there are multiple cores on this system
                // if not, we should do a sync version
                Parallel.ForEach(filesWithExtension, (file, loopSate) => {
                    semaphore.Wait(token); // cancel the wait if token is signaled
                    using (StreamReader reader = File.OpenText(file))
                    {
                        string line = null;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.Contains(keyWord))
                            {
                                result.files.Add(file);
                                break;
                            }
                        }
                    }
                    if (token.IsCancellationRequested)
                        loopSate.Stop(); // cancel all the remaining loop tasks
                    semaphore.Release();
                });
                return result;
            }, token);
            return task;
        }
    }
}
