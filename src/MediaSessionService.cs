using System;
using System.IO;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Foundation;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace HavegetDynamicIsland
{
    public sealed class MediaSnapshot
    {
        public bool Available;
        public string Title = "";
        public string Artist = "";
        public string Album = "";
        public string SourceApp = "";
        public bool Playing;
        public TimeSpan Position;
        public TimeSpan Duration;
        public ImageSource Artwork;
        public string ArtworkKey = "";
    }

    public sealed class MediaSessionService
    {
        private GlobalSystemMediaTransportControlsSessionManager _manager;
        private string _artworkKey = "";
        private ImageSource _artwork;

        private static T Await<T>(IAsyncOperation<T> operation)
        {
            T result = default(T);
            Exception error = null;
            using (ManualResetEventSlim done = new ManualResetEventSlim(false))
            {
                operation.Completed = delegate(IAsyncOperation<T> finished, AsyncStatus status)
                {
                    try
                    {
                        if (status == AsyncStatus.Completed) result = finished.GetResults();
                        else error = new Exception("WinRT async status: " + status + " " + finished.ErrorCode);
                    }
                    catch (Exception ex) { error = ex; }
                    finally { done.Set(); }
                };
                done.Wait();
            }
            if (error != null) throw error;
            return result;
        }

        private void EnsureManager()
        {
            if (_manager != null) return;
            _manager = Await(GlobalSystemMediaTransportControlsSessionManager.RequestAsync());
        }

        private GlobalSystemMediaTransportControlsSession CurrentSession()
        {
            try
            {
                EnsureManager();
                return _manager == null ? null : _manager.GetCurrentSession();
            }
            catch { return null; }
        }

        public MediaSnapshot Poll()
        {
            MediaSnapshot snapshot = new MediaSnapshot();
            try
            {
                GlobalSystemMediaTransportControlsSession session = CurrentSession();
                if (session == null) return snapshot;

                GlobalSystemMediaTransportControlsSessionPlaybackInfo info = session.GetPlaybackInfo();
                GlobalSystemMediaTransportControlsSessionMediaProperties properties = Await(session.TryGetMediaPropertiesAsync());
                GlobalSystemMediaTransportControlsSessionTimelineProperties timeline = session.GetTimelineProperties();

                snapshot.Available = true;
                snapshot.SourceApp = session.SourceAppUserModelId;
                snapshot.Title = string.IsNullOrEmpty(properties.Title) ? "未知曲目" : properties.Title;
                snapshot.Artist = string.IsNullOrEmpty(properties.Artist) ? "未知艺术家" : properties.Artist;
                snapshot.Album = properties.AlbumTitle;
                snapshot.Playing = info != null && info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                snapshot.Position = timeline.Position;
                snapshot.Duration = timeline.EndTime;
                if (snapshot.Duration < TimeSpan.Zero) snapshot.Duration = TimeSpan.Zero;
                if (snapshot.Position < TimeSpan.Zero) snapshot.Position = TimeSpan.Zero;
                if (snapshot.Duration > TimeSpan.Zero && snapshot.Position > snapshot.Duration) snapshot.Position = snapshot.Duration;

                string key = snapshot.SourceApp + "|" + snapshot.Title + "|" + snapshot.Artist + "|" + snapshot.Album;
                if (!string.Equals(key, _artworkKey, StringComparison.Ordinal))
                {
                    _artworkKey = key;
                    _artwork = ReadArtwork(properties.Thumbnail);
                }
                snapshot.Artwork = _artwork;
                snapshot.ArtworkKey = _artworkKey;
                return snapshot;
            }
            catch
            {
                return snapshot;
            }
        }

        private static ImageSource ReadArtwork(IRandomAccessStreamReference reference)
        {
            if (reference == null) return null;
            try
            {
                IRandomAccessStreamWithContentType stream = Await(reference.OpenReadAsync());
                if (stream == null) return null;
                ulong length = stream.Size;
                if (length == 0 || length > int.MaxValue) return null;
                byte[] bytes = new byte[(int)length];
                DataReader reader = new DataReader(stream.GetInputStreamAt(0));
                Await(reader.LoadAsync((uint)length));
                reader.ReadBytes(bytes);
                reader.Dispose();
                stream.Dispose();

                BitmapImage image = new BitmapImage();
                using (MemoryStream memory = new MemoryStream(bytes))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = memory;
                    image.EndInit();
                }
                image.Freeze();
                return image;
            }
            catch { return null; }
        }

        public bool TogglePlayPause()
        {
            try
            {
                GlobalSystemMediaTransportControlsSession session = CurrentSession();
                if (session == null) return false;
                return Await(session.TryTogglePlayPauseAsync());
            }
            catch { return false; }
        }

        public bool Next()
        {
            try
            {
                GlobalSystemMediaTransportControlsSession session = CurrentSession();
                return session != null && Await(session.TrySkipNextAsync());
            }
            catch { return false; }
        }

        public bool Previous()
        {
            try
            {
                GlobalSystemMediaTransportControlsSession session = CurrentSession();
                return session != null && Await(session.TrySkipPreviousAsync());
            }
            catch { return false; }
        }

        public bool Seek(double seconds)
        {
            try
            {
                GlobalSystemMediaTransportControlsSession session = CurrentSession();
                if (session == null) return false;
                if (seconds < 0) seconds = 0;
                long ticks = (long)Math.Round(seconds * TimeSpan.TicksPerSecond);
                return Await(session.TryChangePlaybackPositionAsync(ticks));
            }
            catch { return false; }
        }
    }
}
