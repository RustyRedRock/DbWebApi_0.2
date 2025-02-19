using System.Text.Json.Serialization;

namespace DB_Server_WebApi.Models
{
    public class GlobalTranslations
    {
        // TopBar
        [JsonPropertyName("TopBar_Button1")]
        public string TopBar_Button1 { get; set; }
        [JsonPropertyName("TopBar_Button2")]
        public string TopBar_Button2 { get; set; }
        [JsonPropertyName("TopBar_Button3")]
        public string TopBar_Button3 { get; set; }
        [JsonPropertyName("TopBar_Button4")]
        public string TopBar_Button4 { get; set; }
        [JsonPropertyName("TopBar_Option1")]
        public string TopBar_Option1 { get; set; }
        [JsonPropertyName("TopBar_Option2")]
        public string TopBar_Option2 { get; set; }

        // ConnectionPanel
        [JsonPropertyName("ConnectionPanel_Title")]
        public string ConnectionPanel_Title { get; set; }
        [JsonPropertyName("ConnectionPanel_PlaceholderText")]
        public string ConnectionPanel_PlaceholderText { get; set; }

        // ServerInfo
        [JsonPropertyName("ServerInfo_Title")]
        public string ServerInfo_Title { get; set; }
        [JsonPropertyName("ServerInfo_StatusOnline")]
        public string ServerInfo_StatusOnline { get; set; }
        [JsonPropertyName("ServerInfo_StatusOffline")]
        public string ServerInfo_StatusOffline { get; set; }

        // LegalBar
        [JsonPropertyName("LegalBar_Copyright")]
        public string LegalBar_Copyright { get; set; }
        [JsonPropertyName("LegalBar_Terms")]
        public string LegalBar_Terms { get; set; }
        [JsonPropertyName("LegalBar_Privacy")]
        public string LegalBar_Privacy { get; set; }
    }
}
