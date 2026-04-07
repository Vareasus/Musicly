// Ayca Music - Audio Interop
let audio = null;
let dotNetRef = null;
let timeInterval = null;
let savedVolume = 0.7;
let mobileAudioUnlocked = false;
let pendingPlay = false;
let isUnlocking = false;

// Mobile browsers require user gesture to unlock audio playback
function unlockMobileAudio() {
    if (!audio) return;

    // If there's a pending play request, execute it now (in user gesture context)
    if (pendingPlay && audio.src) {
        audio.play().then(function () {
            pendingPlay = false;
            mobileAudioUnlocked = true;
            console.log('Mobile audio: pending play succeeded');
        }).catch(function (e) {
            console.warn('Mobile audio: pending play failed:', e.message);
        });
        return;
    }

    if (mobileAudioUnlocked) return;

    // Try to unlock audio context by playing a silent moment
    isUnlocking = true;
    audio.muted = true;
    var p = audio.play();
    if (p) {
        p.then(function () {
            audio.pause();
            audio.muted = false;
            audio.currentTime = 0;
            mobileAudioUnlocked = true;
            isUnlocking = false;
            console.log('Mobile audio: unlocked successfully');
        }).catch(function () {
            audio.muted = false;
            isUnlocking = false;
        });
    }

    // Also resume AudioContext if suspended
    if (typeof audioCtx !== 'undefined' && audioCtx && audioCtx.state === 'suspended') {
        audioCtx.resume();
    }
}

function registerMobileUnlock() {
    document.addEventListener('touchstart', unlockMobileAudio, { passive: true });
    document.addEventListener('touchend', unlockMobileAudio, { passive: true });
    document.addEventListener('click', unlockMobileAudio);
}
registerMobileUnlock();

window.audioInterop = {
    init: function (ref) {
        dotNetRef = ref;
        audio = new Audio();
        audio.volume = 0.7;
        audio.setAttribute('playsinline', '');
        audio.setAttribute('webkit-playsinline', '');

        audio.addEventListener('loadedmetadata', () => {
            dotNetRef.invokeMethodAsync('OnDurationChanged', audio.duration);
        });

        audio.addEventListener('ended', () => {
            dotNetRef.invokeMethodAsync('OnTrackEnded');
        });

        audio.addEventListener('play', () => {
            if (isUnlocking) return; // Ignore muted unlock play
            dotNetRef.invokeMethodAsync('OnPlayStateChanged', true);
            startTimeUpdates();
        });

        audio.addEventListener('pause', () => {
            if (isUnlocking) return; // Ignore muted unlock pause
            dotNetRef.invokeMethodAsync('OnPlayStateChanged', false);
            stopTimeUpdates();
        });

        // Media Session API - Lock screen controls
        if ('mediaSession' in navigator) {
            navigator.mediaSession.setActionHandler('play', function () {
                if (audio) audio.play().catch(function () {});
            });
            navigator.mediaSession.setActionHandler('pause', function () {
                if (audio) audio.pause();
            });
            navigator.mediaSession.setActionHandler('previoustrack', function () {
                if (dotNetRef) dotNetRef.invokeMethodAsync('OnKeyboardAction', 'prev');
            });
            navigator.mediaSession.setActionHandler('nexttrack', function () {
                if (dotNetRef) dotNetRef.invokeMethodAsync('OnKeyboardAction', 'next');
            });
        }
        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            // Don't trigger if user is typing in an input
            if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;

            switch (e.code) {
                case 'Space':
                    e.preventDefault();
                    if (audio.paused) audio.play().catch(() => { }); else audio.pause();
                    break;
                case 'ArrowRight':
                    e.preventDefault();
                    audio.currentTime = Math.min(audio.duration || 0, audio.currentTime + 5);
                    break;
                case 'ArrowLeft':
                    e.preventDefault();
                    audio.currentTime = Math.max(0, audio.currentTime - 5);
                    break;
                case 'ArrowUp':
                    e.preventDefault();
                    audio.volume = Math.min(1, audio.volume + 0.05);
                    updateVolumeFill();
                    break;
                case 'ArrowDown':
                    e.preventDefault();
                    audio.volume = Math.max(0, audio.volume - 0.05);
                    updateVolumeFill();
                    break;
                case 'KeyM':
                    e.preventDefault();
                    dotNetRef.invokeMethodAsync('OnMuteToggle');
                    break;
            }
        });
    },

    loadTrack: function (src, title, artist, coverUrl) {
        if (audio) {
            audio.src = src;
            audio.load();
        }
        // Update Media Session for lock screen controls
        if ('mediaSession' in navigator) {
            var artwork = [];
            if (coverUrl) {
                artwork = [{ src: coverUrl, sizes: '256x256', type: 'image/png' }];
            }
            navigator.mediaSession.metadata = new MediaMetadata({
                title: title || 'Musicly',
                artist: artist || '',
                album: 'Musicly',
                artwork: artwork
            });
        }
    },

    play: function () {
        if (!audio) return;
        var p = audio.play();
        if (p) {
            p.catch(function (err) {
                console.warn('Play blocked, will retry on next tap:', err.message);
                pendingPlay = true;
            });
        }
    },

    pause: function () {
        if (audio) audio.pause();
        pendingPlay = false;
    },

    togglePlay: function () {
        if (!audio) return;
        if (audio.paused) {
            var p = audio.play();
            if (p) {
                p.catch(function (err) {
                    console.warn('Toggle play blocked:', err.message);
                    pendingPlay = true;
                });
            }
        } else {
            audio.pause();
            pendingPlay = false;
        }
    },

    seek: function (time) {
        if (audio) audio.currentTime = time;
    },

    seekPercent: function (pct) {
        if (audio && audio.duration) {
            audio.currentTime = pct * audio.duration;
        }
    },

    setVolume: function (vol) {
        if (audio) audio.volume = Math.max(0, Math.min(1, vol));
        updateVolumeFill();
    },

    getVolume: function () {
        return audio ? audio.volume : 0.7;
    },

    mute: function () {
        if (audio) {
            savedVolume = audio.volume;
            audio.volume = 0;
            updateVolumeFill();
        }
    },

    unmute: function () {
        if (audio) {
            audio.volume = savedVolume || 0.7;
            updateVolumeFill();
        }
    },

    dispose: function () {
        stopTimeUpdates();
        if (audio) {
            audio.pause();
            audio.src = '';
            audio = null;
        }
        dotNetRef = null;
    }
};

function updateVolumeFill() {
    const fill = document.getElementById('volumeFillEl');
    if (fill && audio) {
        fill.style.width = (audio.volume * 100) + '%';
    }
}

// ===== PROGRESS BAR SEEKING =====
window._seekBars = [];

window.initSeekBar = function (containerId) {
    const container = document.getElementById(containerId);
    if (!container) return;

    let dragging = false;

    function getSeekFill() {
        return container.querySelector('.seek-fill');
    }
    function getSeekThumb() {
        return container.querySelector('.seek-thumb');
    }

    function applyVisual(pct) {
        const fill = getSeekFill();
        const thumb = getSeekThumb();
        if (fill) fill.style.width = (pct * 100) + '%';
        if (thumb) thumb.style.left = (pct * 100) + '%';
    }

    function seekFromEvent(clientX) {
        const rect = container.getBoundingClientRect();
        const pct = Math.max(0, Math.min(1, (clientX - rect.left) / rect.width));
        applyVisual(pct);
        
        if (activePlayer === 'youtube' && ytPlayer && ytReady) {
            var dur = ytPlayer.getDuration();
            if (dur > 0) {
                ytPlayer.seekTo(pct * dur, true);
            }
        } else if (audio && audio.duration) {
            audio.currentTime = pct * audio.duration;
        }
    }

    container.addEventListener('mousedown', (e) => {
        dragging = true;
        e.preventDefault();
        e.stopPropagation();
        seekFromEvent(e.clientX);
    });

    document.addEventListener('mousemove', (e) => {
        if (dragging) seekFromEvent(e.clientX);
    });

    document.addEventListener('mouseup', () => {
        dragging = false;
    });

    // Touch support
    container.addEventListener('touchstart', (e) => {
        dragging = true;
        e.stopPropagation();
        seekFromEvent(e.touches[0].clientX);
    }, { passive: true });

    container.addEventListener('touchmove', (e) => {
        if (dragging) {
            e.preventDefault();
            seekFromEvent(e.touches[0].clientX);
        }
    }, { passive: false });

    container.addEventListener('touchend', () => {
        dragging = false;
    }, { passive: true });
};

// ===== VOLUME BAR =====
window.initVolumeBar = function (elementId, fillId) {
    const el = document.getElementById(elementId);
    const fill = document.getElementById(fillId);
    if (!el) return;

    function handleVolume(e) {
        const rect = el.getBoundingClientRect();
        const pct = Math.max(0, Math.min(1, (e.clientX - rect.left) / rect.width));
        if (audio) {
            audio.volume = pct;
        }
        savedVolume = pct;
        if (fill) fill.style.width = (pct * 100) + '%';
        
        if (activePlayer === 'youtube' && ytPlayer && ytReady) {
            ytVolume = Math.round(pct * 100);
            ytPlayer.setVolume(ytVolume);
        }
    }

    el.addEventListener('click', handleVolume);
    let dragging = false;
    el.addEventListener('mousedown', (e) => { dragging = true; handleVolume(e); });
    document.addEventListener('mousemove', (e) => { if (dragging) handleVolume(e); });
    document.addEventListener('mouseup', () => { dragging = false; });
};

function startTimeUpdates() {
    stopTimeUpdates();
    timeInterval = setInterval(() => {
        if (audio && dotNetRef) {
            dotNetRef.invokeMethodAsync('OnTimeUpdate', audio.currentTime);
        }
    }, 250);
}

function stopTimeUpdates() {
    if (timeInterval) {
        clearInterval(timeInterval);
        timeInterval = null;
    }
}

window.scrollLyricIntoView = function (index) {
    const container = document.getElementById('lyrics-scroll');
    if (!container) return;
    const lines = container.querySelectorAll('.lyric-line');
    if (lines[index]) {
        // Scroll only within the lyrics container, NOT the page
        const lineTop = lines[index].offsetTop;
        const lineHeight = lines[index].offsetHeight;
        const containerHeight = container.clientHeight;
        const targetScroll = lineTop - (containerHeight / 2) + (lineHeight / 2);
        container.scrollTo({ top: targetScroll, behavior: 'smooth' });
    }
};

// ===== EQUALIZER (Web Audio API) =====
let audioCtx = null;
let sourceNode = null;
let bassFilter = null;
let midFilter = null;
let trebleFilter = null;

window.audioInterop.initEq = function () {
    try {
        if (audioCtx) return;
        audioCtx = new (window.AudioContext || window.webkitAudioContext)();
        sourceNode = audioCtx.createMediaElementSource(audio);

        bassFilter = audioCtx.createBiquadFilter();
        bassFilter.type = 'lowshelf';
        bassFilter.frequency.value = 200;
        bassFilter.gain.value = 0;

        midFilter = audioCtx.createBiquadFilter();
        midFilter.type = 'peaking';
        midFilter.frequency.value = 1000;
        midFilter.Q.value = 1;
        midFilter.gain.value = 0;

        trebleFilter = audioCtx.createBiquadFilter();
        trebleFilter.type = 'highshelf';
        trebleFilter.frequency.value = 3000;
        trebleFilter.gain.value = 0;

        sourceNode.connect(bassFilter);
        bassFilter.connect(midFilter);
        midFilter.connect(trebleFilter);
        trebleFilter.connect(audioCtx.destination);
    } catch (e) {
        console.warn('EQ init failed:', e);
        // Fallback: connect directly
        if (sourceNode) sourceNode.connect(audioCtx.destination);
    }
};

window.audioInterop.setEq = function (bass, mid, treble) {
    if (bassFilter) bassFilter.gain.value = bass;
    if (midFilter) midFilter.gain.value = mid;
    if (trebleFilter) trebleFilter.gain.value = treble;
};

// ===== AUDIO VISUALIZER =====
let analyserNode = null;
let visualizerCanvas = null;
let visualizerCtx = null;
let visualizerRunning = false;
let visualizerAnimId = null;

window.audioInterop.initVisualizer = function (canvasId) {
    visualizerCanvas = document.getElementById(canvasId);
    if (!visualizerCanvas) return;
    visualizerCtx = visualizerCanvas.getContext('2d');

    try {
        if (!audioCtx || !sourceNode) return;

        // Create analyser if not exists
        if (!analyserNode) {
            analyserNode = audioCtx.createAnalyser();
            analyserNode.fftSize = 128;
            analyserNode.smoothingTimeConstant = 0.8;

            // Insert analyser into the chain: treble -> analyser -> destination
            if (trebleFilter) {
                trebleFilter.disconnect();
                trebleFilter.connect(analyserNode);
                analyserNode.connect(audioCtx.destination);
            } else {
                sourceNode.connect(analyserNode);
                analyserNode.connect(audioCtx.destination);
            }
        }

        visualizerRunning = true;
        drawVisualizer();
    } catch (e) {
        console.warn('Visualizer init failed:', e);
    }
};

window.audioInterop.stopVisualizer = function () {
    visualizerRunning = false;
    if (visualizerAnimId) {
        cancelAnimationFrame(visualizerAnimId);
        visualizerAnimId = null;
    }
};

function drawVisualizer() {
    if (!visualizerRunning || !analyserNode || !visualizerCtx || !visualizerCanvas) return;

    visualizerAnimId = requestAnimationFrame(drawVisualizer);

    var bufferLength = analyserNode.frequencyBinCount;
    var dataArray = new Uint8Array(bufferLength);
    analyserNode.getByteFrequencyData(dataArray);

    var canvas = visualizerCanvas;
    var ctx = visualizerCtx;
    var W = canvas.width = canvas.offsetWidth * (window.devicePixelRatio || 1);
    var H = canvas.height = canvas.offsetHeight * (window.devicePixelRatio || 1);

    ctx.clearRect(0, 0, W, H);

    var barCount = Math.min(bufferLength, 64);
    var barWidth = (W / barCount) * 0.7;
    var gap = (W / barCount) * 0.3;
    var x = 0;

    // Get accent color from CSS
    var accentColor = getComputedStyle(document.documentElement).getPropertyValue('--accent').trim() || '#e94590';

    for (var i = 0; i < barCount; i++) {
        var barHeight = (dataArray[i] / 255) * H * 0.9;
        if (barHeight < 2) barHeight = 2;

        // Gradient from accent to secondary
        var gradient = ctx.createLinearGradient(0, H, 0, H - barHeight);
        gradient.addColorStop(0, accentColor);
        gradient.addColorStop(0.5, accentColor + 'cc');
        gradient.addColorStop(1, accentColor + '44');

        ctx.fillStyle = gradient;
        ctx.beginPath();
        ctx.roundRect(x, H - barHeight, barWidth, barHeight, [3, 3, 0, 0]);
        ctx.fill();

        // Reflection
        ctx.fillStyle = accentColor + '15';
        ctx.fillRect(x, H, barWidth, barHeight * 0.2);

        x += barWidth + gap;
    }
}

// ===== SHARE =====
window.audioInterop.shareTrack = function (title, artist, url) {
    var shareData = {
        title: title + ' - ' + artist,
        text: '🎵 ' + title + ' by ' + artist + ' on Musicly',
        url: url || window.location.href
    };

    if (navigator.share) {
        navigator.share(shareData).catch(function () {});
    } else {
        // Fallback: copy to clipboard
        var text = shareData.text + '\n' + shareData.url;
        navigator.clipboard.writeText(text).then(function () {
            // Show a brief toast
            showToast('📋 Link kopyalandı!');
        }).catch(function () {});
    }
};

function showToast(message) {
    var existing = document.getElementById('musicly-toast');
    if (existing) existing.remove();

    var toast = document.createElement('div');
    toast.id = 'musicly-toast';
    toast.textContent = message;
    toast.style.cssText = 'position:fixed;bottom:100px;left:50%;transform:translateX(-50%);background:rgba(255,255,255,0.12);backdrop-filter:blur(20px);color:#fff;padding:12px 24px;border-radius:12px;font-size:14px;font-weight:500;z-index:10000;animation:toastIn 0.3s ease;border:1px solid rgba(255,255,255,0.1);';
    document.body.appendChild(toast);

    setTimeout(function () {
        toast.style.animation = 'toastOut 0.3s ease forwards';
        setTimeout(function () { toast.remove(); }, 300);
    }, 2000);
}

// Toast animations
var toastStyle = document.createElement('style');
toastStyle.textContent = '@keyframes toastIn{from{opacity:0;transform:translateX(-50%) translateY(20px)}to{opacity:1;transform:translateX(-50%) translateY(0)}}@keyframes toastOut{from{opacity:1;transform:translateX(-50%) translateY(0)}to{opacity:0;transform:translateX(-50%) translateY(20px)}}';
document.head.appendChild(toastStyle);

// ===== CROSSFADE =====
let crossfadeDuration = 0;

window.audioInterop.setCrossfade = function (seconds) {
    crossfadeDuration = seconds;
};

// ===== PAUSE (for sleep timer) =====
window.audioInterop.pause = function () {
    if (audio) {
        audio.pause();
        if (dotNetRef) dotNetRef.invokeMethodAsync('OnPlayStateChanged', false);
    }
};

// ===== SEEK =====
window.audioInterop.seek = function (time) {
    if (audio) audio.currentTime = time;
};

// ===== THEME & ACCENT =====
window.audioInterop.setTheme = function (theme) {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem('Musicly-theme', theme);
};

window.audioInterop.setAccent = function (accent) {
    document.documentElement.setAttribute('data-accent', accent);
    localStorage.setItem('Musicly-accent', accent);
};

window.audioInterop.getTheme = function () {
    const t = localStorage.getItem('Musicly-theme') || 'dark';
    document.documentElement.setAttribute('data-theme', t);
    return t;
};

window.audioInterop.getAccent = function () {
    const a = localStorage.getItem('Musicly-accent') || 'pink';
    document.documentElement.setAttribute('data-accent', a);
    return a;
};

// ===== KEYBOARD SHORTCUTS =====
document.addEventListener('keydown', function (e) {
    // Don't trigger when typing in inputs
    if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA' || e.target.isContentEditable) return;
    if (!dotNetRef) return;

    switch (e.key) {
        case ' ':
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnKeyboardAction', 'togglePlay');
            break;
        case 'n': case 'N':
            dotNetRef.invokeMethodAsync('OnKeyboardAction', 'next');
            break;
        case 'p': case 'P':
            dotNetRef.invokeMethodAsync('OnKeyboardAction', 'prev');
            break;
        case 'm': case 'M':
            dotNetRef.invokeMethodAsync('OnKeyboardAction', 'mute');
            break;
        case 's': case 'S':
            dotNetRef.invokeMethodAsync('OnKeyboardAction', 'shuffle');
            break;
        case 'r': case 'R':
            dotNetRef.invokeMethodAsync('OnKeyboardAction', 'repeat');
            break;
        case 'l': case 'L':
            dotNetRef.invokeMethodAsync('OnKeyboardAction', 'like');
            break;
        case 'q': case 'Q':
            dotNetRef.invokeMethodAsync('OnKeyboardAction', 'queue');
            break;
        case '?':
            dotNetRef.invokeMethodAsync('OnShowShortcuts');
            break;
        case 'ArrowUp':
            e.preventDefault();
            if (activePlayer === 'youtube' && ytPlayer && ytReady) {
                ytVolume = Math.min(100, ytVolume + 5);
                ytPlayer.setVolume(ytVolume);
            }
            if (audio) audio.volume = Math.min(1, audio.volume + 0.05);
            break;
        case 'ArrowDown':
            e.preventDefault();
            if (activePlayer === 'youtube' && ytPlayer && ytReady) {
                ytVolume = Math.max(0, ytVolume - 5);
                ytPlayer.setVolume(ytVolume);
            }
            if (audio) audio.volume = Math.max(0, audio.volume - 0.05);
            break;
        case 'ArrowLeft':
            e.preventDefault();
            if (activePlayer === 'youtube' && ytPlayer && ytReady) {
                ytPlayer.seekTo(Math.max(0, ytPlayer.getCurrentTime() - 5), true);
            } else if (audio) {
                audio.currentTime = Math.max(0, audio.currentTime - 5);
            }
            break;
        case 'ArrowRight':
            e.preventDefault();
            if (activePlayer === 'youtube' && ytPlayer && ytReady) {
                ytPlayer.seekTo(Math.min(ytPlayer.getDuration() || 0, ytPlayer.getCurrentTime() + 5), true);
            } else if (audio) {
                audio.currentTime = Math.min(audio.duration || 0, audio.currentTime + 5);
            }
            break;
    }
});

// Apply saved theme on load
(function () {
    const t = localStorage.getItem('Musicly-theme');
    if (t) document.documentElement.setAttribute('data-theme', t);
    const a = localStorage.getItem('Musicly-accent');
    if (a) document.documentElement.setAttribute('data-accent', a);
})();

// ===== DOWNLOAD TRACK =====
window.audioInterop.downloadTrack = function (src, filename) {
    fetch(src)
        .then(response => response.blob())
        .then(blob => {
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = filename.endsWith('.mp3') ? filename : filename + '.mp3';
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            window.URL.revokeObjectURL(url);
        })
        .catch(err => console.error('Download failed:', err));
};

// ===== PLAY BUTTON HANDLER (Event Delegation) =====
// Single handler for ALL play buttons. No @onclick, no HTML onclick needed.
// Runs in user gesture context → mobile audio works.
// Audio play/pause events notify Blazor of state changes automatically.
document.addEventListener('click', function(e) {
    var playBtn = e.target.closest('.bottom-play-btn, .play-btn');
    if (!playBtn) return;
    
    // Resume AudioContext if suspended (for equalizer)
    if (typeof audioCtx !== 'undefined' && audioCtx && audioCtx.state === 'suspended') {
        audioCtx.resume();
    }
    
    if (activePlayer === 'youtube' && ytPlayer && ytReady) {
        var state = ytPlayer.getPlayerState();
        if (state === YT.PlayerState.PLAYING) {
            ytPlayer.pauseVideo();
        } else {
            ytPlayer.playVideo();
        }
    } else if (audio) {
        if (audio.paused) {
            audio.play().catch(function(err) {
                console.warn('Play blocked:', err.message);
                pendingPlay = true;
            });
        } else {
            audio.pause();
            pendingPlay = false;
        }
    }
});

// ===== TRACK CLICK HANDLER =====
// When user clicks a track in the list, Blazor loads it via @onclick.
// But on mobile, the subsequent play() call fails. So we listen for
// loadeddata events and auto-play if audio was recently unlocked.
document.addEventListener('click', function(e) {
    var trackItem = e.target.closest('.track-item');
    if (!trackItem || !audio) return;
    
    mobileAudioUnlocked = true;
    // After Blazor loads the new track, auto-play it
    var onLoaded = function() {
        audio.removeEventListener('loadeddata', onLoaded);
        audio.play().catch(function() {});
    };
    audio.addEventListener('loadeddata', onLoaded);
});

// ===== YOUTUBE IFRAME PLAYER =====
let ytPlayer = null;
let ytReady = false;
let ytApiLoaded = false;
let activePlayer = 'audio'; // 'audio' or 'youtube'
let ytTimeInterval = null;
let ytVolume = 70; // 0-100

// Load YouTube IFrame API
function loadYouTubeApi() {
    if (ytApiLoaded) return;
    ytApiLoaded = true;
    var tag = document.createElement('script');
    tag.src = 'https://www.youtube.com/iframe_api';
    var firstScript = document.getElementsByTagName('script')[0];
    firstScript.parentNode.insertBefore(tag, firstScript);
}

// Called by YouTube API when ready
window.onYouTubeIframeAPIReady = function() {
    var container = document.getElementById('yt-player-container');
    if (!container) {
        // Create container if not exists
        container = document.createElement('div');
        container.id = 'yt-player-container';
        container.style.cssText = 'position:fixed;bottom:-9999px;left:-9999px;width:1px;height:1px;opacity:0;pointer-events:none;z-index:-1;';
        document.body.appendChild(container);
        
        var playerDiv = document.createElement('div');
        playerDiv.id = 'yt-player';
        container.appendChild(playerDiv);
    }
    
    ytPlayer = new YT.Player('yt-player', {
        height: '1',
        width: '1',
        playerVars: {
            autoplay: 0,
            controls: 0,
            disablekb: 1,
            fs: 0,
            modestbranding: 1,
            rel: 0,
            showinfo: 0,
            origin: window.location.origin
        },
        events: {
            onReady: function() {
                ytReady = true;
                ytPlayer.setVolume(ytVolume);
                console.log('YouTube player ready');
            },
            onStateChange: function(event) {
                if (activePlayer !== 'youtube' || !dotNetRef) return;
                
                switch (event.data) {
                    case YT.PlayerState.PLAYING:
                        dotNetRef.invokeMethodAsync('OnPlayStateChanged', true);
                        startYtTimeUpdates();
                        // Update duration
                        var dur = ytPlayer.getDuration();
                        if (dur > 0) {
                            dotNetRef.invokeMethodAsync('OnDurationChanged', dur);
                        }
                        break;
                    case YT.PlayerState.PAUSED:
                        dotNetRef.invokeMethodAsync('OnPlayStateChanged', false);
                        stopYtTimeUpdates();
                        break;
                    case YT.PlayerState.ENDED:
                        stopYtTimeUpdates();
                        dotNetRef.invokeMethodAsync('OnTrackEnded');
                        break;
                }
            },
            onError: function(event) {
                console.warn('YouTube player error:', event.data);
            }
        }
    });
};

function startYtTimeUpdates() {
    stopYtTimeUpdates();
    ytTimeInterval = setInterval(function() {
        if (ytPlayer && dotNetRef && activePlayer === 'youtube') {
            try {
                var currentTime = ytPlayer.getCurrentTime();
                dotNetRef.invokeMethodAsync('OnTimeUpdate', currentTime);
            } catch(e) {}
        }
    }, 250);
}

function stopYtTimeUpdates() {
    if (ytTimeInterval) {
        clearInterval(ytTimeInterval);
        ytTimeInterval = null;
    }
}

window.audioInterop.loadYouTubeTrack = function(videoId, title, artist, thumbnailUrl) {
    // Load YouTube API if not loaded
    if (!ytApiLoaded) {
        loadYouTubeApi();
    }
    
    // Pause regular audio
    if (audio) {
        audio.pause();
        audio.src = '';
    }
    stopTimeUpdates();
    
    activePlayer = 'youtube';
    
    // Update Media Session
    if ('mediaSession' in navigator) {
        var artwork = [];
        if (thumbnailUrl) {
            artwork = [{ src: thumbnailUrl, sizes: '480x360', type: 'image/jpeg' }];
        }
        navigator.mediaSession.metadata = new MediaMetadata({
            title: title || 'YouTube Music',
            artist: artist || '',
            album: 'Musicly',
            artwork: artwork
        });
    }
    
    // Wait for player to be ready, then load
    function tryLoad() {
        if (ytReady && ytPlayer && typeof ytPlayer.loadVideoById === 'function') {
            ytPlayer.loadVideoById(videoId);
            ytPlayer.setVolume(ytVolume);
        } else {
            setTimeout(tryLoad, 200);
        }
    }
    tryLoad();
};

window.audioInterop.playYouTube = function() {
    if (ytPlayer && ytReady && activePlayer === 'youtube') {
        ytPlayer.playVideo();
    }
};

window.audioInterop.pauseYouTube = function() {
    if (ytPlayer && ytReady) {
        ytPlayer.pauseVideo();
    }
};

window.audioInterop.seekYouTube = function(time) {
    if (ytPlayer && ytReady && activePlayer === 'youtube') {
        ytPlayer.seekTo(time, true);
    }
};

window.audioInterop.setYouTubeVolume = function(vol) {
    ytVolume = Math.round(vol * 100);
    if (ytPlayer && ytReady) {
        ytPlayer.setVolume(ytVolume);
    }
};

window.audioInterop.muteYouTube = function() {
    if (ytPlayer && ytReady) {
        ytPlayer.mute();
    }
};

window.audioInterop.unmuteYouTube = function() {
    if (ytPlayer && ytReady) {
        ytPlayer.unMute();
        ytPlayer.setVolume(ytVolume);
    }
};

window.audioInterop.getActivePlayer = function() {
    return activePlayer;
};

window.audioInterop.switchToAudio = function() {
    // Stop YouTube
    if (ytPlayer && ytReady) {
        try { ytPlayer.stopVideo(); } catch(e) {}
    }
    stopYtTimeUpdates();
    activePlayer = 'audio';
};

// Override play/pause for YouTube awareness
var originalPlay = window.audioInterop.play;
var originalPause = window.audioInterop.pause;

window.audioInterop.play = function() {
    if (activePlayer === 'youtube') {
        window.audioInterop.playYouTube();
    } else {
        if (!audio) return;
        var p = audio.play();
        if (p) {
            p.catch(function(err) {
                console.warn('Play blocked, will retry on next tap:', err.message);
                pendingPlay = true;
            });
        }
    }
};

window.audioInterop.pause = function() {
    if (activePlayer === 'youtube') {
        window.audioInterop.pauseYouTube();
    } else {
        if (audio) audio.pause();
        pendingPlay = false;
    }
    if (dotNetRef) dotNetRef.invokeMethodAsync('OnPlayStateChanged', false);
};

window.audioInterop.togglePlay = function() {
    if (activePlayer === 'youtube') {
        if (ytPlayer && ytReady) {
            var state = ytPlayer.getPlayerState();
            if (state === YT.PlayerState.PLAYING) {
                ytPlayer.pauseVideo();
            } else {
                ytPlayer.playVideo();
            }
        }
    } else {
        if (!audio) return;
        if (audio.paused) {
            var p = audio.play();
            if (p) {
                p.catch(function(err) {
                    console.warn('Toggle play blocked:', err.message);
                    pendingPlay = true;
                });
            }
        } else {
            audio.pause();
            pendingPlay = false;
        }
    }
};

// Override seek for YouTube awareness
window.audioInterop.seek = function(time) {
    if (activePlayer === 'youtube') {
        window.audioInterop.seekYouTube(time);
    } else {
        if (audio) audio.currentTime = time;
    }
};

// Override volume for YouTube awareness
var originalSetVolume = window.audioInterop.setVolume;
window.audioInterop.setVolume = function(vol) {
    if (activePlayer === 'youtube') {
        window.audioInterop.setYouTubeVolume(vol);
    }
    if (audio) audio.volume = Math.max(0, Math.min(1, vol));
    updateVolumeFill();
};

// Override mute/unmute for YouTube awareness
window.audioInterop.mute = function() {
    if (activePlayer === 'youtube') {
        window.audioInterop.muteYouTube();
    }
    if (audio) {
        savedVolume = audio.volume;
        audio.volume = 0;
        updateVolumeFill();
    }
};

window.audioInterop.unmute = function() {
    if (activePlayer === 'youtube') {
        window.audioInterop.unmuteYouTube();
    }
    if (audio) {
        audio.volume = savedVolume || 0.7;
        updateVolumeFill();
    }
};

// Pre-load YouTube API on page load for faster first play
loadYouTubeApi();
