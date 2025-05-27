using System;
using System.IO;
using System.Windows.Controls;

namespace Gra2D
{
    public class MusicStuff
    {
        private readonly GameConfig config;
        private readonly MediaElement mediaElement;

        public MusicStuff(GameConfig config, MediaElement mediaElement)
        {
            this.config = config;
            this.mediaElement = mediaElement ?? throw new ArgumentNullException(nameof(mediaElement));
        }

        public void PlayRandomGameMusic()
        {
            if (config.GameMusicFiles.Length > 0)
            {
                Random rnd = new Random();
                string musicFile = config.GameMusicFiles[rnd.Next(0, config.GameMusicFiles.Length)];
                PlayBackgroundMusic(musicFile);
            }
        }

        public void PlayBackgroundMusic(string fileName)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            if (File.Exists(filePath))
            {
                mediaElement.Source = new Uri(filePath);
                mediaElement.MediaEnded += (s, args) =>
                {
                    mediaElement.Position = TimeSpan.Zero;
                    mediaElement.Play();
                };
                mediaElement.Play();
            }
        }
    }
}