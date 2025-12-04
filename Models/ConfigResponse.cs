using System.Text.Json.Serialization;

namespace TaskbarLyrics.Models
{
    public class LyricsConfig
    {
        [JsonPropertyName("font_family")]
        public string FontFamily { get; set; } = "MiSans";
        
        [JsonPropertyName("font_size")]
        public int FontSize { get; set; } = 16;
        
        [JsonPropertyName("font_color")]
        public string FontColor { get; set; } = "#FFFFFF";
        
        [JsonPropertyName("background_color")]
        public string BackgroundColor { get; set; } = "#00000000";
        
        [JsonPropertyName("alignment")]
        public string Alignment { get; set; } = "Center";
        
        [JsonPropertyName("show_translation")]
        public bool ShowTranslation { get; set; } = true;
        
        [JsonPropertyName("translation_font_size")]
        public int TranslationFontSize { get; set; } = 14;
        
        [JsonPropertyName("translation_font_color")]
        public string TranslationFontColor { get; set; } = "#CCCCCC";
        
        [JsonPropertyName("line_spacing")]
        public int LineSpacing { get; set; } = 2;
        
        [JsonPropertyName("highlight_color")]
        public string HighlightColor { get; set; } = "#FF00FFFF";
        
        [JsonPropertyName("highlight_gradient")]
        public bool HighlightGradient { get; set; } = false;
        
        [JsonPropertyName("highlight_animation")]
        public bool HighlightAnimation { get; set; } = true;

        [JsonPropertyName("hide_on_fullscreen")]
        public bool HideOnFullscreen { get; set; } = true;

        [JsonPropertyName("log_level")]
        public string LogLevel { get; set; } = "Auto"; // Auto, Off, Error, Info, Debug

        [JsonPropertyName("enable_lyrics_filter")]
        public bool EnableLyricsFilter { get; set; } = true;

        [JsonPropertyName("lyrics_filter_regex")]
        public string LyricsFilterRegex { get; set; } = "^([^：]*)：.*$|^([^:]*):.*$|^([^翻唱]*)翻唱.*$|^([^许可]*)许可.*$|^([^音乐人]*)音乐人.*$|^([^国风]*)国风.*$|^([^纯音乐]*)纯音乐.*$";

        [JsonPropertyName("position_offset_x")]
        public int PositionOffsetX { get; set; } = 0;

        [JsonPropertyName("position_offset_y")]
        public int PositionOffsetY { get; set; } = 0;
    }
}