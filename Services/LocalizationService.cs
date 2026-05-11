namespace Musicly.Services;

/// <summary>
/// Simple localization service for Turkish/English.
/// </summary>
public class LocalizationService
{
    public string CurrentLanguage { get; private set; } = "tr";
    public event Action? OnLanguageChanged;

    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        ["tr"] = new()
        {
            // Navigation
            ["nav.home"] = "Ana Sayfa",
            ["nav.explore"] = "Keşfet",
            ["nav.stats"] = "İstatistikler",
            ["nav.favorites"] = "Favoriler",
            ["nav.radio"] = "Radyo",
            ["nav.youtube"] = "YouTube",
            ["nav.requests"] = "İstekler",
            ["nav.messages"] = "Mesajlar",
            ["nav.admin"] = "Admin",
            ["nav.profile"] = "Profil",
            ["nav.logout"] = "Çıkış",

            // Player
            ["player.nowPlaying"] = "Şimdi Çalıyor",
            ["player.lyrics"] = "Sözler",
            ["player.synced"] = "SENKRONİZE",
            ["player.shuffle"] = "Karıştır",
            ["player.repeat"] = "Tekrarla",
            ["player.download"] = "İndir",
            ["player.share"] = "Paylaş",
            ["player.queue"] = "Sıra",
            ["player.equalizer"] = "Ekolayzer",
            ["player.visualizer"] = "Görselleştirici",
            ["player.volume"] = "Ses",

            // Home
            ["home.title"] = "Müziğini Hisset",
            ["home.subtitle"] = "Premium ses deneyimi",
            ["home.yourMusic"] = "Şarkılarınız",

            // Explore
            ["explore.title"] = "Müzik Keşfet",
            ["explore.subtitle"] = "Türe göre keşfet, favorilerini bul",
            ["explore.search"] = "Şarkı, sanatçı, tür ara...",
            ["explore.browseGenre"] = "Türe Göre Göz At",
            ["explore.allTracks"] = "Tüm Şarkılar",
            ["explore.noTracks"] = "Şarkı bulunamadı. Farklı bir arama deneyin.",

            // YouTube
            ["youtube.title"] = "YouTube Müzik",
            ["youtube.subtitle"] = "YouTube'dan milyonlarca şarkı ara ve dinle",
            ["youtube.search"] = "Şarkı, sanatçı ara...",
            ["youtube.searchBtn"] = "🔍 Ara",
            ["youtube.searching"] = "Aranıyor...",
            ["youtube.noResults"] = "Sonuç bulunamadı. Farklı bir arama deneyin.",
            ["youtube.startSearch"] = "Dinlemeye başlamak için bir şarkı ara!",
            ["youtube.results"] = "Arama Sonuçları",

            // Stats
            ["stats.title"] = "Dinleme İstatistikleriniz",
            ["stats.subtitle"] = "Nasıl dinlediğini gör",
            ["stats.totalPlays"] = "Toplam Çalma",
            ["stats.totalTime"] = "Toplam Dinleme",
            ["stats.mostPlayed"] = "En Çok Dinlenen",
            ["stats.likedSongs"] = "Beğenilen Şarkılar",
            ["stats.trackDetails"] = "Şarkı Detayları",

            // Profile
            ["profile.joined"] = "Katıldı",
            ["profile.totalListens"] = "Toplam Dinleme",
            ["profile.favSongs"] = "Favori Şarkı",
            ["profile.comments"] = "Yorum",
            ["profile.listenTime"] = "Dinleme Süresi",
            ["profile.top5"] = "🏅 Top 5 Şarkı",
            ["profile.achievements"] = "🏆 Başarımlar",
            ["profile.favorites"] = "❤️ Favori Şarkılar",

            // Admin
            ["admin.title"] = "Yönetim Paneli",
            ["admin.users"] = "Kullanıcılar",
            ["admin.tracks"] = "Şarkılar",
            ["admin.analytics"] = "📊 Analitik",

            // General
            ["general.sortBy"] = "Sırala:",
            ["general.title"] = "Başlık",
            ["general.artist"] = "Sanatçı",
            ["general.genre"] = "Tür",
            ["general.close"] = "Kapat",
            ["general.save"] = "Kaydet",
            ["general.cancel"] = "İptal",
            ["general.delete"] = "Sil",
            ["general.loading"] = "Yükleniyor...",
            ["general.noData"] = "Henüz veri yok",

            // Language
            ["lang.switch"] = "🌐 EN",
        },
        ["en"] = new()
        {
            // Navigation
            ["nav.home"] = "Home",
            ["nav.explore"] = "Explore",
            ["nav.stats"] = "Stats",
            ["nav.favorites"] = "Favorites",
            ["nav.radio"] = "Radio",
            ["nav.youtube"] = "YouTube",
            ["nav.requests"] = "Requests",
            ["nav.messages"] = "Messages",
            ["nav.admin"] = "Admin",
            ["nav.profile"] = "Profile",
            ["nav.logout"] = "Logout",

            // Player
            ["player.nowPlaying"] = "Now Playing",
            ["player.lyrics"] = "Lyrics",
            ["player.synced"] = "SYNCED",
            ["player.shuffle"] = "Shuffle",
            ["player.repeat"] = "Repeat",
            ["player.download"] = "Download",
            ["player.share"] = "Share",
            ["player.queue"] = "Queue",
            ["player.equalizer"] = "Equalizer",
            ["player.visualizer"] = "Visualizer",
            ["player.volume"] = "Volume",

            // Home
            ["home.title"] = "Feel Your Music",
            ["home.subtitle"] = "Premium audio experience",
            ["home.yourMusic"] = "Your Music",

            // Explore
            ["explore.title"] = "Explore Music",
            ["explore.subtitle"] = "Discover tracks by genre, search for your favorites",
            ["explore.search"] = "Search tracks, artists, genres...",
            ["explore.browseGenre"] = "Browse by Genre",
            ["explore.allTracks"] = "All Tracks",
            ["explore.noTracks"] = "No tracks found. Try a different search.",

            // YouTube
            ["youtube.title"] = "YouTube Music",
            ["youtube.subtitle"] = "Search and play millions of songs from YouTube",
            ["youtube.search"] = "Search for songs, artists...",
            ["youtube.searchBtn"] = "🔍 Search",
            ["youtube.searching"] = "Searching...",
            ["youtube.noResults"] = "No results found. Try a different search.",
            ["youtube.startSearch"] = "Search for any song to start listening!",
            ["youtube.results"] = "Search Results",

            // Stats
            ["stats.title"] = "Your Listening Stats",
            ["stats.subtitle"] = "See how you've been vibing",
            ["stats.totalPlays"] = "Total Plays",
            ["stats.totalTime"] = "Total Listening Time",
            ["stats.mostPlayed"] = "Most Played Song",
            ["stats.likedSongs"] = "Liked Songs",
            ["stats.trackDetails"] = "Track Details",

            // Profile
            ["profile.joined"] = "Joined",
            ["profile.totalListens"] = "Total Listens",
            ["profile.favSongs"] = "Favorite Songs",
            ["profile.comments"] = "Comments",
            ["profile.listenTime"] = "Listening Time",
            ["profile.top5"] = "🏅 Top 5 Songs",
            ["profile.achievements"] = "🏆 Achievements",
            ["profile.favorites"] = "❤️ Favorite Songs",

            // Admin
            ["admin.title"] = "Admin Panel",
            ["admin.users"] = "Users",
            ["admin.tracks"] = "Tracks",
            ["admin.analytics"] = "📊 Analytics",

            // General
            ["general.sortBy"] = "Sort by:",
            ["general.title"] = "Title",
            ["general.artist"] = "Artist",
            ["general.genre"] = "Genre",
            ["general.close"] = "Close",
            ["general.save"] = "Save",
            ["general.cancel"] = "Cancel",
            ["general.delete"] = "Delete",
            ["general.loading"] = "Loading...",
            ["general.noData"] = "No data yet",

            // Language
            ["lang.switch"] = "🌐 TR",
        }
    };

    public string T(string key)
    {
        if (Translations.TryGetValue(CurrentLanguage, out var dict) && dict.TryGetValue(key, out var val))
            return val;
        return key; // Fallback: return key itself
    }

    public void ToggleLanguage()
    {
        CurrentLanguage = CurrentLanguage == "tr" ? "en" : "tr";
        OnLanguageChanged?.Invoke();
    }

    public void SetLanguage(string lang)
    {
        if (lang != CurrentLanguage && Translations.ContainsKey(lang))
        {
            CurrentLanguage = lang;
            OnLanguageChanged?.Invoke();
        }
    }
}
