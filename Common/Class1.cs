namespace Common
{
    public class FileUpdateWatcher
    {
        public FileUpdateWatcher(string filepath)
        {
            FileSystemWatcher watcher = new FileSystemWatcher(filepath);
            watcher.EnableRaisingEvents = true;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += Watcher_Changed;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}