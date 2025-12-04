using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TaskbarLyrics.Models;

namespace TaskbarLyrics
{
    public class LyricsApiService
    {
        private readonly HttpClient _httpClient;
        private const string LyricsApiUrl = "http://localhost:35374/api/lyric";
        private const string LyricsPwApiUrl = "http://localhost:35374/api/lyricfile";
        private const string ConfigApiUrl = "http://localhost:35374/api/config";
        private const string NowPlayingApiUrl = "http://localhost:35374/api/now-playing";
        private const string PlayPauseApiUrl = "http://localhost:35374/api/play-pause";
        private const string NextTrackApiUrl = "http://localhost:35374/api/next-track";
        private const string PreviousTrackApiUrl = "http://localhost:35374/api/previous-track";

        public LyricsApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public async Task<LyricsResponse> GetLyricsAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(LyricsApiUrl);
                return JsonConvert.DeserializeObject<LyricsResponse>(response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取歌词时出错: {ex.Message}");
                try
                {
                    var response = await _httpClient.GetStringAsync(LyricsPwApiUrl);
                    return JsonConvert.DeserializeObject<LyricsResponse>(response);
                }
                catch (Exception ex2)
                {
                    Debug.WriteLine($"从联网搜索API {LyricsPwApiUrl} 获取歌词时出错: {ex2.Message}");
                    return new LyricsResponse { Status = "error" };
                }
            }
        }


        public async Task<NowPlayingResponse> GetNowPlayingAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync(NowPlayingApiUrl);
                return JsonConvert.DeserializeObject<NowPlayingResponse>(response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取当前播放信息时出错: {ex.Message}");
                return new NowPlayingResponse { Status = "error" };
            }
        }

        public async Task<bool> PlayPauseAsync()
        {
            try
            {
                // 移除频繁的API调用日志输出
                var response = await _httpClient.GetAsync(PlayPauseApiUrl);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"播放/暂停时出错: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> NextTrackAsync()
        {
            try
            {
                // 移除频繁的API调用日志输出
                var response = await _httpClient.GetAsync(NextTrackApiUrl);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"下一曲时出错: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PreviousTrackAsync()
        {
            try
            {
                // 移除频繁的API调用日志输出
                var response = await _httpClient.GetAsync(PreviousTrackApiUrl);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"上一曲时出错: {ex.Message}");
                return false;
            }
        }
    }

}
